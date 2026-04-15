# PROJECT BIBLE — Planora
> Version 2.0 | App Name: Planora | Read this ENTIRELY before writing a single line of code.
> **v1.0:** Full Azure consolidation — MongoDB Atlas → Cosmos DB, Supabase → Azure PostgreSQL, Upstash → Azure Cache for Redis + Azure Service Bus, Twilio → Azure Communication Services. Firebase retained for push notifications only.
> **v1.0:** Azure AD B2C discontinued for new customers (May 2025) → replaced with **Microsoft Entra External ID**. Tenant created via `entra.microsoft.com`, NOT `portal.azure.com`. All SDK/env keys identical.
> **v2.0:** Booking partnerships updated — Skyscanner and Kayak declined. Primary hotel/activity affiliate is now **Booking.com via AWIN**. GCP Free Plan active — all Google APIs operational. Bookings Tab removed from bottom nav — bookings now live contextually inside generated trips and the Vault. Three trip creation modes added. Trip editing (add/delete spots) added. Package browsing redesigned as AWIN-powered web search.

---

## 0. WHAT THIS APP IS

**App Name: Planora**
A **cross-platform mobile travel app** (iOS + Android) that is a one-stop solution:
1. **Plan** — Three modes: AI-generated trip, Manual spot-by-spot builder, or Browse & import tour packages
2. **Book** — Contextual booking options appear per-spot inside a generated or manual trip. No separate Bookings tab.
3. **Store** — Centralised Vault for all tickets, PDFs, QR codes (offline accessible). All confirmed bookings consolidate here.
4. **Stay Safe** — Day-aware emergency contacts, SOS broadcast, nearby hospital/police

It is NOT a booking engine. It does NOT process payments. It is a planning + facilitation + storage + safety layer.

---

## 1. CORE MODULES

### MODULE 1 — AI Itinerary Engine (Heart of the App)
**Inspired by:** Roamy.travel
**Purpose:** Takes user inputs → runs a multi-agent AI pipeline → returns a complete day-by-day itinerary

#### Pre-Wizard Screen — Three Entry Cards:
```
┌──────────────────────────────────────────────────────┐
│                  How do you want to start?           │
├──────────────────────┬──────────────────┬────────────┤
│  🤖 AI Generate     │ 🗺 Build Manually │ 📦 Browse  │
│  My Trip            │                  │ Packages   │
└──────────────────────┴──────────────────┴────────────┘
```

**Card 1 — AI Generate My Trip** → opens 6-step wizard (see below)
**Card 2 — Build Manually** → opens Manual Trip Builder (Module 1C)
**Card 3 — Browse Packages** → opens Package Browser (Module 1B)

#### Module 1A — AI Generate Trip (Wizard, 6 steps):
| Step | Input | Notes |
|------|-------|-------|
| 1 | Where To | City/country, multi-destination toggle, map pin |
| 2 | Dates | Calendar picker. If Step 3 filled → shows color-coded price grid |
| 3 | Where From | OPTIONAL. Origin city. Enables flight price color coding |
| 4 | Group + Budget | Solo/Couple/Family/Friends, pax count, Budget: Low/Mid/Luxury |
| 5 | Themes | Food, Adventure, Shopping, Party, History, Nature (multi-select chips) |
| 6 | Details | Dietary needs, accessibility, must-visit places, avoid list |

#### AI Engine Architecture — HYBRID MULTI-AGENT (Semantic Kernel):
- **First generation:** Full 6-agent sequential chain
- **Edit mode:** Single Planner Agent with tool calls (fast, diff-only patch)
- **Manual spot add:** Single Spot Enrichment Agent called per spot (1 spot at a time)

**Agent Chain (in order):**
```
Orchestrator → Research → Route Optimiser → Tour Package Injector → Budget → Day Assembly → Personalisation
```

| Agent | Responsibility | External Call |
|-------|---------------|---------------|
| Orchestrator | Reads inputs, builds task graph, sequences agents | None |
| Research | Fetches POIs matching themes + destination | Google Places API + Azure AI Search (vector) |
| Route Optimiser | Clusters POIs by geo-proximity into day groups | Google Maps Distance Matrix API |
| Tour Package Injector | Merges agency package day blocks as locked blocks | Internal Tour Package DB |
| Budget | Estimates cost per POI, hotel, transport by tier | Internal pricing DB |
| Day Assembly | Assigns time slots (9am–10pm), meal gaps, travel gaps | None |
| Personalisation | GPT-4o reranks/reweights output against user themes | OpenAI API (GPT-4o) |

**Job tracking:** Every generation job stored in `ai_generation_jobs` with status, current_agent, progress_pct. Client polls via SSE. If any agent fails → only that step retries.

**Live progress shown to user:** "Researching top heritage sites in Jaipur…" → "Optimising Day 2 route…" → "Estimating your budget…"

#### Output — Trip Card (same for all three creation modes):
- Day tabs at top (Day 1, Day 2…)
- Google Maps embedded with colour-coded POI pins + route polyline (each day = distinct colour)
- Each POI: name, category, visit time, best_time_of_day badge, estimated cost, AI summary, photo
- **Edit button (top right of trip card)** — enters Edit Mode
- Estimated budget shown per day + total
- Booking options shown per POI inline (no separate Bookings tab)

---

#### Module 1A-EDIT — Modifiable Trip (Edit Mode)

Triggered by tapping **Edit** button (top right of a generated or manual trip).

**Delete a spot:**
- In edit mode, each POI card shows a ✕ delete icon
- Tap ✕ → confirm prompt → POI removed from that day
- Day route polyline + map re-renders immediately
- No AI call needed — purely local state update → PATCH to Cosmos DB

**Add a spot:**
- Tap **+ Add Spot** button (shown at bottom of any day in edit mode)
- Opens a search sheet powered by Google Places API
- User types place name → Places autocomplete results shown
- User selects a place → loading state: "Adding this to your trip…"
- **Placement algorithm (local — no AI call needed):**
  1. Get lat/lng of selected place
  2. Calculate which existing day's POI cluster centroid is geographically nearest
  3. If that day has < 6 POIs → insert into that day at the nearest time slot gap
  4. If that day is full → check the adjacent day (previous or next) with fewest POIs
  5. Insert into chosen day, re-sort by geo-proximity within the day
  6. Re-render day tab + map
- **Spot Enrichment Agent is called** to fetch: photo, AI summary, estimated cost, visit duration, booking_type, booking_url, geocoordinates — same enrichment as the full agent chain does per POI
- PATCH updated day to Cosmos DB
- Map colour coding updates to reflect the day the spot was placed in

> Note: The full 6-agent chain is NOT re-run for an add/delete. Only the Spot Enrichment Agent fires for adds. Deletes are purely local + a Cosmos PATCH.

---

#### Module 1B — Browse Packages

**Purpose:** Let users discover and import pre-built tour packages from external platforms, presented in a clean unified Planora format.

**Key decision:** Rather than building complex multi-platform scrapers or requiring affiliate acceptance, Package Browse uses **AWIN-powered Booking.com affiliate search** as the primary channel. A curated set of deeplinks to package/tour search pages on trusted platforms is offered as secondary options. No scraping. No iframe embedding. No complex data normalisation across 10 platforms.

**User Flow:**

1. From the pre-wizard screen, user taps **Browse Packages**
2. A search screen appears:
   - "Where do you want to go?" → destination input (Places API autocomplete)
   - "Travel dates" → date range picker (optional — defaults to ±30 days flexible)
   - "Duration" → chip selector: Weekend, 3–5 days, 1 week, 2 weeks, Custom
   - "Trip type" → chips: Beach, Cultural, Adventure, Luxury, Budget
3. Tap **Search** → results screen shows two sections:

**Section A — Booking.com Tour/Activity Packages (via AWIN)**
- Deeplinks constructed to Booking.com Attractions + Tours search filtered by destination + dates
- Cards show: destination, package type label, "View on Booking.com" CTA
- On tap → opens Booking.com in device browser (AWIN affiliate link with `aid` param)

**Section B — Search Other Platforms**
- A curated row of platform chips: Viator, GetYourGuide, Club Mahindra, MakeMyTrip Holidays, Thomas Cook
- Each chip → opens that platform's package search URL pre-filled with destination + dates
- These are standard deeplinks, NOT affiliate-tracked (unless those affiliates are later approved)

4. User finds a package they like on the external platform, books it there
5. On return to Planora → **"I've booked a package — import it"** button on the search results screen
6. Tapping this opens the Manual Trip Builder (Module 1C) with a prompt: "Build your package trip day by day"
7. User manually adds the spots from their purchased package — system enriches each spot as it's added

> **Why this approach:** Building a platform that scrapes and normalises packages from 10+ platforms is disproportionately complex and fragile. AWIN/Booking.com gives legitimate affiliate monetisation. The "import after booking" flow via Manual Builder gives users the Planora planning/safety/vault layer on top of any package they buy anywhere.

**AWIN Integration:**
- Register at awin.com → apply to Booking.com programme
- Booking.com affiliate `aid` parameter goes into `appsettings → Affiliates:BookingComAid`
- All Booking.com links (hotels AND tours/activities search) use this same `aid`

---

#### Module 1C — Build Manually (Spot-by-Spot)

**Purpose:** User builds their own trip from scratch, adding one spot at a time. Planora enriches each spot as it's added. This is the TripIt-killer flow — user owns the full itinerary, Planora provides the intelligence layer.

**Token efficiency:** Instead of one 10,000-token AI call for a 10-spot trip, this uses ~1,000 tokens per spot enrichment call. User controls exactly how many spots they add.

**Flow:**

1. User taps **Build Manually** from pre-wizard screen
2. Enter trip basics: destination, dates, trip name, group size, budget tier
3. Trip scaffold created with empty day containers
4. User taps **+ Add Spot** on any day
5. Search sheet opens (Google Places API autocomplete)
6. User selects a place → **Spot Enrichment Agent fires:**
   - Fetches: photo (Google Places), AI summary (GPT-4o ~800 tokens), estimated visit duration, estimated cost, geocoordinates, booking_type, booking_url, opening_hours
   - Adds spot to that day in a sensible position (sorted by time of day / geo-proximity to other spots already in that day)
7. Spot card appears on the day with loading skeleton → populates as enrichment completes
8. User continues adding spots across days
9. Map updates in real time — colour-coded by day
10. At any point, user can tap **Done** → trip is saved as a complete trip in the same Trip Card format as AI-generated trips
11. Booking options, Vault, Safety all work identically from this point

**Edit after manual build:** Same edit mode as Module 1A-EDIT — add more spots or delete existing ones at any time.

---

### MODULE 2 — Contextual Booking (no separate tab)

**Core concept:** Planora is NOT a booking platform. Booking options surface contextually inside the trip, per POI. There is no standalone Bookings tab in the bottom navigation.

**Where booking options appear:**
- Inside a generated or manual trip → each POI card has a "Book" section below it
- Trip card → "Book" tab (alongside Days, Map, Vault tabs within the trip detail screen)
- Each POI chip in the booking tab shows its booking_type

**Booking Categories (same as before, rendered per POI or per day):**
- Flights → Skyscanner (pending partner approval), MakeMyTrip, Ixigo, Cleartrip
- Hotels & Stay → Booking.com (AWIN), Agoda, MakeMyTrip
- Activities/Tickets → Booking.com Activities (AWIN), Viator, GetYourGuide
- Cabs/Transfers → Uber, Ola (deeplink with pickup/drop pre-filled)
- Trains → IRCTC, ConfirmTkt
- Dining → Zomato, EazyDiner
- Events/Shows → BookMyShow, Ticketmaster

**Revenue model:** CPC affiliate commission via AWIN (Booking.com confirmed partner). Other platforms: register as affiliate where available; use plain deeplinks otherwise.

**Technical:** Booking Svc is a pure URL factory. Prices cached in Azure Cache for Redis, 15-minute TTL. Opens via `Linking.openURL()`.

---

### MODULE 3 — Trip Vault

**Inspired by:** TripIt
**Purpose:** Central offline-first storage for all travel documents and confirmed bookings.

> The old "Bookings" tab is fully replaced by the Vault. Any booking made via Planora-generated deeplinks, manually saved, or uploaded by the user consolidates here.

#### Ingestion methods:
1. **Manual save** — after user books on an external platform, they can tap "Save to Vault" from the POI booking section
2. **PDF/image upload** — from files or camera
3. **Email forward** — OCR parser extracts details
4. **QR scan** — physical ticket

#### What's stored:
- Flights, hotels, activities, events, restaurant reservations
- Stored as: blob_url (Azure Blob Storage), parsed_data JSONB, type, source
- Rendered as: structured timeline sorted by travel_date
- All cached locally via MMKV — works 100% offline

#### Smart features:
- Flight status push alerts (Firebase Cloud Messaging)
- Check-in reminders (24h before)
- Calendar sync (Google/Apple)
- Share itinerary link

---

### MODULE 4 — SafeTravel Shield
**Purpose:** Emergency safety layer, accessible from EVERY screen via persistent FAB button

#### Three sub-sections:

**A) Central Emergency Contacts (country-aware)**
- Auto-loaded based on trip destination country
- Tourist police, national helpline, embassy contacts
- One-tap: Call, Email, Visit website

**B) SOS Trigger**
- 3-second press hold → legal disclaimer → biometric confirm → FIRE
- On trigger: broadcasts live GPS location to inner circle
- Sends push notification + SMS via Firebase + Azure Communication Services
- All SOS events logged to `sos_events` table

**C) Day-wise Proximity Safety**
- Updates each day based on that day's planned locations
- Shows: nearest hospital + directions, nearest police station + directions
- Cached offline at trip start

---

### MODULE 5 — Finance Layer
- Add expenses per day, per category
- Multi-currency with live conversion
- Group expense split calculator
- Post-trip spend vs budget report
- Stored in `trip_expenses` (Azure PostgreSQL)

---

### MODULE 6 — Tour Package Registry
- Agency onboarding portal (admin flow, not mobile)
- Agencies register packages: title, destination, duration, day_plan JSON, price_per_person
- Mobile app: search packages by destination + theme + days
- Selected package → injects day blocks into AI Day Assembly Agent as LOCKED blocks
- AI fills remaining days around locked blocks

---

## 2. TECH STACK (FULL)

### Frontend
| Technology | Purpose |
|-----------|---------|
| React Native (Expo SDK 51+) | Cross-platform iOS + Android |
| Expo Router (file-based) | Navigation |
| Zustand | Global state management |
| MMKV | Ultra-fast local key-value (offline data) |
| React Native Maps | Google Maps integration |
| TanStack Query | Server state, caching |
| expo-auth-session | PKCE auth flow |
| expo-secure-store | Token storage |
| expo-local-authentication | Biometric for SOS |
| react-native-gesture-handler | Swipe, long press |
| react-native-reanimated | Animations |

### Backend
| Technology | Purpose |
|-----------|---------|
| .NET 8 Web API (C#) | REST API |
| Semantic Kernel | AI agent orchestration |
| MongoDB.Driver | Cosmos DB (MongoDB API) |
| Npgsql + EF Core | Azure PostgreSQL |
| StackExchange.Redis | Azure Cache for Redis |
| Azure.Messaging.ServiceBus | Queue for AI jobs |
| SignalR | Real-time group trip sync |

### Cloud
| Service | Replaces | Purpose |
|---------|---------|---------|
| Microsoft Entra External ID | Azure AD B2C (discontinued May 2025) | Auth |
| Azure Cosmos DB (MongoDB API) | MongoDB Atlas | Document storage |
| Azure Database for PostgreSQL | Supabase | Relational data |
| Azure Cache for Redis | Upstash Redis | Booking price cache |
| Azure Service Bus | Upstash QStash | AI job queue |
| Azure Blob Storage | — | Vault documents |
| Azure AI Document Intelligence | — | OCR for vault uploads |
| Azure Communication Services | Twilio | SMS for SOS |
| Firebase Cloud Messaging | — | Push notifications |
| Google Cloud (Places, Maps, Matrix) | — | POI search + routing |
| OpenAI API (direct) | — | GPT-4o for AI agents |
| AWIN | Skyscanner/Kayak (denied) | Booking.com affiliate |

---

## 3. BOTTOM NAVIGATION — UPDATED

```
Tab 1: Home        ← new trips, recent, recommendations
Tab 2: My Trips    ← list of all generated + manual trips
Tab 3: Vault       ← all saved bookings/docs (replaces old Bookings tab)
Tab 4: Safety      ← SafeTravel Shield (or accessible via FAB from anywhere)
Tab 5: Profile
```

> ⚠ The old "Bookings" tab is REMOVED. Booking options live inside each trip. Confirmed bookings live in the Vault.

---

## 4. DATA MODELS

### Cosmos DB Collections:
```
trips                    ← trip metadata, wizard inputs, status, trip_type
itinerary_days           ← per-day data + POI list + booking_summary
places_of_interest       ← POI details, booking enrichment, geocoords
```

### PostgreSQL Tables:
```sql
-- users
user_id UUID PK, entra_oid VARCHAR UNIQUE, name VARCHAR,
email VARCHAR UNIQUE, phone VARCHAR, avatar_url TEXT,
home_city VARCHAR, preferences JSONB, created_at TIMESTAMP

-- agencies
agency_id UUID PK, name VARCHAR, verified BOOL,
contact JSONB, location JSONB, specialities TEXT[], review_score DECIMAL

-- emergency_contacts
contact_id UUID PK, country_code CHAR(2), agency_type TEXT,
name VARCHAR, phone VARCHAR, email VARCHAR, url TEXT, is_tourist_cell BOOL

-- sos_events
sos_id UUID PK, user_id UUID, trip_id UUID,
triggered_at TIMESTAMP, lat DECIMAL, lng DECIMAL,
notified_contacts JSONB, resolved_at TIMESTAMP

-- trip_expenses
expense_id UUID PK, trip_id UUID, user_id UUID,
category TEXT, amount DECIMAL, currency CHAR(3),
paid_by UUID, split_with JSONB, receipt_url TEXT, expense_date DATE

-- vault_items
item_id UUID PK, trip_id UUID, user_id UUID,
type TEXT, blob_url TEXT, parsed_data JSONB,
source TEXT, is_offline_cached BOOL

-- ai_generation_jobs
job_id UUID PK, trip_id UUID,
status TEXT, current_agent VARCHAR,
progress_pct INT, error_log TEXT,
started_at TIMESTAMP, completed_at TIMESTAMP
```

### Trip Document (Cosmos DB) — `trip_type` field added:
```json
{
  "trip_id": "UUID",
  "user_id": "UUID",
  "trip_type": "ai_generated | manual | package_import",
  "status": "PENDING | GENERATING | COMPLETE | FAILED",
  "destination": { "city": "", "country": "", "lat": 0, "lng": 0 },
  "origin": { "city": "", "iata": "" },
  "dates": { "start": "YYYY-MM-DD", "end": "YYYY-MM-DD" },
  "pax": 2,
  "budget_tier": "mid",
  "themes": ["history", "food"],
  "created_at": "ISO8601"
}
```

---

## 5. API CONTRACTS

### Trip Service — /api/v1/trips
```
POST   /                              Create trip + queue AI job → returns trip_id + job_id
GET    /{trip_id}                     Full trip with all days + POIs
PATCH  /{trip_id}                     Edit trip metadata
DELETE /{trip_id}                     Soft delete
GET    /{trip_id}/days                Day list with POIs
POST   /{trip_id}/days/{d}/pois       Add POI to a day (used by Manual Builder + Edit Mode)
DELETE /{trip_id}/days/{d}/pois/{p}   Remove POI (Edit Mode delete)
PATCH  /{trip_id}/days/{d}/reorder    Drag-reorder POIs (body: sorted poi_id array)
GET    /{trip_id}/share-link          Generate/get public share token
WS     /ws/trips/{trip_id}            SignalR: real-time group sync
```

### AI Service — /api/v1/ai
```
POST /generate              Start full 6-agent chain → returns job_id
POST /edit                  Single-agent edit (body: trip_id + delta) → returns diff patch
POST /enrich-spot           Spot Enrichment Agent (body: place_id, trip_id, day_number) → returns enriched POI
GET  /jobs/{id}/status      SSE stream: { agent, progress_pct, message }
```

### Booking Service — /api/v1/booking
```
GET /options/{trip_id}/{category}?dayNumber={n}   Returns array of { platform, deeplink_url, indicative_price }
```

### Vault Service — /api/v1/vault
```
POST /upload           Upload PDF/image → OCR parse → store → return item_id
POST /save-booking     Manually save a booking reference from a trip POI
GET  /{trip_id}        All vault items for trip
DELETE /{item_id}      Remove item
```

### Safety Service — /api/v1/safety
```
GET  /contacts/{country_code}    Emergency contacts for country
GET  /nearby?lat=&lng=&day_id=   Nearest hospital + police with directions
POST /sos                         Trigger SOS
```

### Tour Service — /api/v1/tours
```
GET  /search?dest=&theme=&days=   Search packages
GET  /{package_id}                Full package with day_plan
POST /inject                      Merge package into trip days
GET  /agencies                    Verified agencies list
```

### Finance Service — /api/v1/expenses
```
POST /                    Add expense
GET  /{trip_id}           All expenses for trip
GET  /{trip_id}/split     Calculated split per person
GET  /{trip_id}/report    Full spend vs budget report
```

---

## 6. AGENT EXECUTION — STEP BY STEP

### Mode A — Full AI Generation:
```
1. Client POSTs wizard inputs (trip_type: "ai_generated")
2. Trip Svc creates trip (PENDING) → publishes to Azure Service Bus
3. Azure Function picks job → Orchestrator reads inputs → builds task graph
4. Research Agent → Google Places API + Azure AI Search → scored POI list
5. Route Agent → Distance Matrix → clusters POIs into day groups
6. Tour Package Injector → merges any locked package blocks
7. Budget Agent → cost estimates per tier
8. Day Assembly Agent → time slots 9am–10pm, meal + travel gaps
9. Personalisation Agent → GPT-4o reranks → final itinerary JSON
10. Writes to Cosmos DB → COMPLETE → SSE push to client → renders
```

### Mode B — Edit Mode (single agent, diff only):
```
E1. User action (add/remove/swap POI)
E2. For DELETE: local state update + Cosmos PATCH (no AI call)
E3. For ADD via search: Spot Enrichment Agent fires (POST /ai/enrich-spot)
E4. Enrichment Agent → Google Places details + GPT-4o (~800 tokens) → returns POI data
E5. Placement algorithm runs locally → determines day + position
E6. PATCH day to Cosmos DB → client re-renders affected day
```

### Mode C — Manual Builder (spot-by-spot):
```
M1. User creates trip scaffold (trip_type: "manual")
M2. For each spot added by user: POST /ai/enrich-spot
M3. Spot Enrichment Agent → Places API + GPT-4o (~800 tokens per spot)
M4. Spot placed in day → day re-renders
M5. User repeats M2–M4 for each spot
M6. User taps Done → trip status COMPLETE
```

---

## 7. BUILD SEQUENCE (11 STEPS)

| Step | What to Build | Cloud Needed |
|------|--------------|--------------|
| 1 | Expo scaffold + navigation + onboarding | Nothing |
| 2 | Auth (Microsoft Entra External ID) | Microsoft Entra External ID |
| 3 | Trip Wizard UI + Pre-wizard 3-card screen | Nothing |
| 4 | .NET Trip Service + Cosmos DB + PostgreSQL | Azure Cosmos DB + Azure PostgreSQL |
| 5 | Semantic Kernel + Orchestrator + Spot Enrichment Agent | OpenAI + Azure Service Bus + Redis |
| 6 | Full agent chain (6 agents) | Google Cloud APIs |
| 7 | Booking Gateway (contextual, no Bookings tab) | Azure Redis (already done) |
| 8 | Manual Trip Builder + Edit Mode (add/delete spots) | Nothing new |
| 9 | Trip Vault (replaces Bookings tab) | Azure Blob + Azure Doc Intelligence |
| 10 | SafeTravel Shield | Azure ACS + Firebase |
| 11 | Finance + Package Browse + Polish | Nothing new |

**Rule:** NEVER build two steps in one session.

---

## 8. KEY ARCHITECTURAL DECISIONS

- **Cosmos DB (MongoDB API) over Atlas** — same MongoDB.Driver SDK, zero code changes
- **Azure PostgreSQL over Supabase** — same Npgsql SDK
- **Azure Cache for Redis over Upstash** — same StackExchange.Redis client
- **Azure Service Bus over Upstash QStash** — native Azure, Functions trigger bindings work directly
- **Azure Communication Services over Twilio** — same REST model, lower cost
- **Firebase for push** — direct FCM is simpler than Azure Notification Hubs for mobile
- **OpenAI API directly** — simpler and cheaper at this scale
- **Microsoft Entra External ID for auth** — Azure AD B2C discontinued May 2025; Entra External ID is direct replacement
- **AWIN / Booking.com as primary affiliate** — Skyscanner and Kayak denied. AWIN confirmed partnership. Booking.com covers hotels AND tours/activities.
- **No Bookings tab** — bookings are contextual per POI inside a trip. The Vault consolidates confirmed bookings. This keeps the app focused.
- **Three trip creation modes** — AI-generated, Manual (spot-by-spot), Package Browse. All produce the same Trip Card output format.
- **Spot-by-spot enrichment for Manual mode** — ~800 tokens per spot vs ~10,000 tokens for a full trip. Efficient. Scalable.
- **Local placement algorithm for Edit Mode spot adds** — no AI call needed. Day chosen by geo-proximity of cluster centroid to new spot's lat/lng.
- **Package Browse via AWIN + deeplinks only** — no scraping, no complex normalisation, no platform-specific parsing. Simple and maintainable.
- **MMKV over AsyncStorage** — 30x faster, essential for offline
- **SignalR on Trip Svc** — real-time group itinerary sync
- **Tour packages as LOCKED day blocks** — AI wraps around them

---

## 9. PROJECT FOLDER STRUCTURE

```
Planora/
  images/
    logo.jpg
    beachbest.jpg
    purple indigo.jpg
    stay.jpg
    temple.jpg
    galleryindore.jpg
    tibetmountains.jpg
    signin.jpg
    signup.jpg
  PROJECT_BIBLE.md
  AGENT_INSTRUCTIONS.md
  BOOKING_ARCHITECTURE.md
  FULL_PROCESS.md
  .gitignore
  PlanoraApp/                     ← Expo React Native frontend
  PlanoraApp.API/                 ← .NET 8 Web API backend
  PlanoraApp.AI/                  ← Semantic Kernel agent library
```
