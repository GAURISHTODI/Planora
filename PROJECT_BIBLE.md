# PROJECT BIBLE — Planora
> Version 1.0 | App Name: Planora | Read this ENTIRELY before writing a single line of code.
> **v1.0:** Full Azure consolidation — MongoDB Atlas → Cosmos DB, Supabase → Azure PostgreSQL, Upstash → Azure Cache for Redis + Azure Service Bus, Twilio → Azure Communication Services. Firebase retained for push notifications only.
> **v1.0:** Azure AD B2C discontinued for new customers (May 2025) → replaced with **Microsoft Entra External ID**. Tenant created via `entra.microsoft.com`, NOT `portal.azure.com`. All SDK/env keys identical.

---


## 0. WHAT THIS APP IS

**App Name: Planora**
A **cross-platform mobile travel app** (iOS + Android) that is a one-stop solution:
1. **Plan** — AI generates a day-by-day, map-based trip itinerary
2. **Book** — Redirects user to external booking platforms with dates pre-filled
3. **Store** — Centralised wallet for all tickets, PDFs, QR codes (offline accessible)
4. **Stay Safe** — Day-aware emergency contacts, SOS broadcast, nearby hospital/police

It is NOT a booking engine. It does NOT process payments. It is a planning + facilitation + storage + safety layer.

---

## 1. FOUR CORE MODULES

### MODULE 1 — AI Itinerary Engine (Heart of the App)
**Inspired by:** Roamy.travel
**Purpose:** Takes user inputs → runs a multi-agent AI pipeline → returns a complete day-by-day itinerary

#### User Inputs (Wizard, 6 steps):
| Step | Input | Notes |
|------|-------|-------|
| 1 | Where To | City/country, multi-destination toggle, map pin |
| 2 | Dates | Calendar picker. If Step 3 filled → shows Skyscanner-style color-coded price grid on dates |
| 3 | Where From | OPTIONAL. Origin city. Enables flight price color coding on dates |
| 4 | Group + Budget | Solo/Couple/Family/Friends, pax count, Budget: Low/Mid/Luxury |
| 5 | Themes | Food, Adventure, Shopping, Party, History, Nature (multi-select chips) |
| 6 | Details | Dietary needs, accessibility, must-visit places, avoid list |

**Pre-wizard screen:** "Plan my own trip" vs "Browse agency/tour packages"
- If user selects a tour package → its day_plan JSON is injected into the itinerary before AI runs
- AI fills remaining days around the package blocks

#### AI Engine Architecture — HYBRID MULTI-AGENT (Semantic Kernel):
- **First generation:** Full 6-agent sequential chain
- **Edit mode:** Single planner agent with tool calls (fast, diff-only patch)

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
| Day Assembly | Assigns time slots (9am–10pm), meal gaps, travel gaps, sorts by best_time_of_day | None |
| Personalisation | GPT-4o reranks/reweights output against user themes | OpenAI API (GPT-4o) |

**Job tracking:** Every generation job is stored in `ai_generation_jobs` table with status, current_agent, progress_pct. Client polls via SSE. If any agent fails → only that step retries. Entire chain does NOT restart.

**Live progress shown to user:** "Researching top heritage sites in Jaipur…" → "Optimising Day 2 route…" → "Estimating your budget…" etc.

#### Output — Trip Card:
- Day tabs at top (Day 1, Day 2…)
- Google Maps embedded with POI pins + route polyline
- Each POI: name, category, visit time, best_time_of_day badge, estimated cost, AI summary
- User can: drag to reorder POIs, add new place (global search), delete place, shuffle days
- Estimated budget shown per day + total

---

### MODULE 2 — Smart Booking Gateway
**Inspired by:** Skyscanner (meta-search model)
**Core concept:** Planora is a PLATFORM FOR PLATFORMS. It does not book anything. It redirects.

#### How it works:
1. User taps a booking category (Flights, Hotel, Activities, etc.)
2. Backend reads trip dates, destination, pax from Cosmos DB
3. Constructs affiliate deeplinks for each platform with dates + pax pre-filled
4. Shows side-by-side platform comparison with indicative prices
5. User picks → browser opens → books on external platform
6. Dates and details are NEVER re-entered by user

#### Booking Categories:
- Flights → Skyscanner, MakeMyTrip, Ixigo, Yatra, Cleartrip, Vimaan
- Hotels & Stay → Booking.com, Agoda, OYO, MakeMyTrip
- Activities → Viator, GetYourGuide
- Cabs/Transfers → Uber, Ola (deeplink with pickup/drop pre-filled)
- Dining → Zomato, EazyDiner
- Events/Museums → platform deeplinks where available

#### Revenue Model:
Pure **CPC affiliate commission**. Register as affiliate partner on each platform. Every redirect that converts earns commission. No in-app payments ever.

#### Technical:
- Booking Svc is a pure **URL factory** — reads trip data, calls each platform's affiliate link builder
- Prices cached in **Azure Cache for Redis, 15-minute TTL**
- Client opens URL via `Linking.openURL()` — opens device browser

---

### MODULE 3 — Trip Vault (TripIt-style)
**Inspired by:** TripIt
**Purpose:** Central offline-first storage for all travel documents

#### Ingestion methods:
1. **Auto-save** — if booked via Module 2 and platform returns booking ref, auto-logged
2. **PDF/image upload** — user uploads from files or camera
3. **Email forward** — user forwards booking confirmation email → OCR parser extracts details
4. **QR scan** — scan physical ticket

#### What's stored:
- Flights, hotels, activities, events, restaurant reservations
- Stored as: blob_url (Azure Blob Storage), parsed_data JSONB, type, source
- Rendered as: structured timeline sorted by date
- All cached locally via **MMKV on device — works 100% offline**

#### Smart features:
- Flight status push alerts (Firebase Cloud Messaging)
- Check-in reminders (24h before)
- Gate change alerts
- Calendar sync (Google/Apple)
- Share itinerary link (public or private)

---

### MODULE 4 — SafeTravel Shield
**Purpose:** Emergency safety layer, accessible from EVERY screen via persistent FAB button

#### Three sub-sections:

**A) Central Emergency Contacts (country-aware)**
- Auto-loaded based on trip destination country
- Tourist police, national helpline, embassy contacts
- One-tap: Call, Email, Visit website

**B) SOS Trigger**
- 3-second press hold → legal disclaimer screen → biometric confirm → FIRE
- Disclaimer: "This is a real emergency alert. Misuse may result in legal consequences."
- On trigger: broadcasts live GPS location to inner circle (contacts saved in profile)
- Sends push notification + SMS via Firebase Cloud Messaging + Azure Communication Services
- All SOS events logged to `sos_events` table with lat/lng for audit

**C) Day-wise Proximity Safety**
- Updates each day based on that day's planned locations
- Shows: nearest hospital + directions, nearest police station + directions
- Cached offline at trip start — works without internet on travel day

---

### MODULE 5 — Finance Layer (Budget Tracker)
- Add expenses per day, per category (food, transport, activities, shopping)
- Multi-currency with live conversion
- Group expense split calculator (who paid, split with whom, net owed)
- Post-trip spend report vs estimated budget
- Stored in `trip_expenses` table (Azure PostgreSQL)

---

### MODULE 6 — Tour Package Registry
- Agency onboarding portal (separate admin flow, not mobile)
- Agencies register packages: title, destination, duration, day_plan JSON, price_per_person, availability
- Mobile app: search packages by destination + theme + days
- Selected package → injects day blocks into AI Day Assembly Agent as LOCKED blocks
- AI fills remaining days around these locked blocks

---

## 2. TECH STACK (FULL)

### Frontend
| Technology | Purpose |
|-----------|---------|
| React Native (Expo SDK 51+) | Cross-platform iOS + Android |
| Expo Router (file-based) | Navigation |
| Zustand | Global state management |
| MMKV | Ultra-fast local key-value (offline data) |
| SQLite (expo-sqlite) | Local structured data for offline vault |
| Google Maps SDK (react-native-maps) | Map view, POI pins, route polylines |
| React Query (TanStack) | Server state, caching, background sync |

### Backend
| Technology | Purpose |
|-----------|---------|
| .NET 8 Web API | Core REST microservices |
| Azure API Management | Gateway: auth, rate limiting, routing, versioning |
| SignalR | Real-time group itinerary sync |
| Azure Functions | Background agent workers (triggered by Azure Service Bus) |
| Azure Container Apps | All services containerised, auto-scale |

### AI Engine
| Technology | Purpose |
|-----------|---------|
| Microsoft Semantic Kernel (.NET) | Multi-agent orchestration, planner, memory, plugins |
| OpenAI API (GPT-4o) | Personalisation agent + edit mode agent — called DIRECTLY, not via Azure OpenAI |
| Azure AI Search | Vector index for semantic POI search |
| Azure Service Bus | Agent task queue |

### Data
| Technology | Purpose |
|-----------|---------|
| Azure Cosmos DB (MongoDB API) | Trips + itinerary_days (flexible nested JSON). Uses MongoDB wire protocol — same MongoDB.Driver SDK, zero code changes vs Atlas |
| Azure Database for PostgreSQL (Flexible Server) | Users, bookings, expenses, safety data. Same Npgsql SDK as Supabase |
| Azure Blob Storage | PDFs, QR codes, ticket files |
| Azure Cache for Redis | Flight prices (15min TTL), session data |

### Auth + Infra + Notifications
| Technology | Purpose |
|-----------|---------|
| Microsoft Entra External ID | Auth, social login (Google, Apple). Direct replacement for Azure AD B2C — discontinued for new customers May 2025. Tenant created at `entra.microsoft.com`. Same OIDC/PKCE flow, same token format, same env keys. |
| Azure Key Vault | Secrets management |
| Azure DevOps | CI/CD pipelines |
| Azure Communication Services (SMS) | SMS for SOS broadcast |
| Firebase Cloud Messaging | Push notifications (flight alerts, reminders). Retained — Azure Notification Hubs calls FCM under the hood anyway; direct FCM is simpler |
| App Center | OTA updates, crash reporting |

### External APIs (no Azure equivalent — stays external)
| API | Used For |
|-----|---------|
| Google Places API | POI research (Research Agent) |
| Google Maps Distance Matrix | Route clustering (Route Agent) |
| Skyscanner Affiliate API | Flight deeplinks |
| Booking.com Partner API | Hotel deeplinks |
| Viator / GetYourGuide API | Activity deeplinks |
| MakeMyTrip / Ixigo / Yatra | Indian platform affiliate links |
| Uber / Ola deeplinks | Cab booking redirects |

---

## 3. ONBOARDING FLOW — IMAGE BACKGROUNDS

App name: **Planora**
Logo file: `images/logo.jpg`

Onboarding screens use full-screen background images with text overlay and subtle dark gradient at bottom for text readability.

### Screen sequence:
| Screen | Image file | Text overlay |
|--------|-----------|--------------|
| 1 | `images/beachbest.jpg` | "Your next adventure starts here" |
| 2 | `images/purple indigo.jpg` | "Plan smarter, travel better" |
| 3 | `images/stay.jpg` | "Every stay, perfectly placed" |
| 4 | `images/temple.jpg` | "Discover hidden gems" |
| 5 | `images/galleryindore.jpg` | "Built for Indian explorers" |
| 6 | `images/tibetmountains.jpg` | "From beaches to mountains" |
| 7 | `images/signin.jpg` | Sign In screen (logo + sign in form over image) |
| 8 | `images/signup.jpg` | Sign Up screen (logo + sign up form over image) |

### Image overlay implementation:
- Full screen `ImageBackground` component
- Dark gradient overlay: `rgba(0,0,0,0.35)` top, `rgba(0,0,0,0.65)` bottom
- Logo shown on screens 1 and 7 and 8
- Text: white, bold, bottom-aligned with safe area padding
- Swipeable (react-native-swiper or expo-router tabs with gestures)
- Dot indicators at bottom
- "Skip" button top-right on screens 1–6
- Screen 7 = Login, Screen 8 = Signup (forms rendered over image background)

---

## 4. DATABASE SCHEMA (COMPLETE)

### Azure Cosmos DB (MongoDB API) Collections

**trips**
```json
{
  "trip_id": "UUID",
  "user_id": "UUID",
  "title": "string",
  "destination": { "city": "", "country": "", "lat": 0, "lng": 0 },
  "origin": { "city": "", "country": "" },
  "start_date": "DATE",
  "end_date": "DATE",
  "budget_tier": "low|mid|luxury",
  "group_type": "solo|couple|family|friends",
  "pax_count": 1,
  "themes": ["food", "adventure"],
  "extra_preferences": { "dietary": "", "avoid": [], "must_visit": [] },
  "status": "pending|generating|complete|error",
  "share_token": "UUID",
  "created_at": "TIMESTAMP"
}
```

**itinerary_days**
```json
{
  "day_id": "UUID",
  "trip_id": "UUID",
  "day_number": 1,
  "date": "DATE",
  "hotel_suggestion": { "name": "", "lat": 0, "lng": 0, "booking_deeplink": "" },
  "transport_mode": "walk|drive|transit",
  "budget_estimate": 0.00,
  "weather_snapshot": {},
  "is_tour_package": false,
  "package_ref_id": null,
  "sort_order": 1,
  "notes": ""
}
```

**places_of_interest**
```json
{
  "poi_id": "UUID",
  "day_id": "UUID",
  "google_place_id": "string",
  "name": "string",
  "category": "monument|restaurant|activity|shopping|nightlife|park|museum",
  "lat": 0, "lng": 0,
  "visit_time_minutes": 90,
  "best_time_of_day": "morning|afternoon|evening|night",
  "estimated_cost": 0.00,
  "ai_summary": "string",
  "sort_order": 1,
  "is_user_added": false
}
```

### Azure PostgreSQL Tables

```sql
-- users
user_id UUID PK, email VARCHAR, display_name VARCHAR,
auth_provider TEXT, default_currency CHAR(3),
theme_prefs JSONB, subscription_tier TEXT,
inner_circle JSONB, created_at TIMESTAMP

-- bookings
booking_id UUID PK, trip_id UUID, user_id UUID,
category TEXT, platform VARCHAR, redirect_url TEXT,
booking_ref VARCHAR, status TEXT,
travel_date DATE, attachment_url TEXT

-- tour_packages
package_id UUID PK, agency_id UUID, title VARCHAR,
destination JSONB, duration_days INT, day_plan JSONB,
price_per_person DECIMAL, category TEXT,
availability JSONB, rating DECIMAL, is_verified BOOL

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

---

## 5. API CONTRACTS

### Trip Service — /api/v1/trips
```
POST   /                              Create trip + queue AI job → returns trip_id + job_id
GET    /{trip_id}                     Full trip with all days + POIs
PATCH  /{trip_id}                     Edit trip metadata
DELETE /{trip_id}                     Soft delete
GET    /{trip_id}/days                Day list with POIs
POST   /{trip_id}/days/{d}/pois       Add POI to a day
DELETE /{trip_id}/days/{d}/pois/{p}   Remove POI
PATCH  /{trip_id}/days/{d}/reorder    Drag-reorder POIs (body: sorted poi_id array)
GET    /{trip_id}/share-link          Generate/get public share token
WS     /ws/trips/{trip_id}            SignalR: real-time group sync
```

### AI Service — /api/v1/ai
```
POST /generate          Start full 6-agent chain → returns job_id
POST /edit              Single-agent edit (body: trip_id + delta) → returns diff patch
GET  /jobs/{id}/status  SSE stream: { agent, progress_pct, message }
```

### Booking Service — /api/v1/booking
```
GET /options/{trip_id}/{category}   Returns array of { platform, deeplink_url, indicative_price }
```

### Vault Service — /api/v1/vault
```
POST /upload           Upload PDF/image → OCR parse → store → return item_id
GET  /{trip_id}        All vault items for trip
DELETE /{item_id}      Remove item
```

### Safety Service — /api/v1/safety
```
GET  /contacts/{country_code}    Emergency contacts for country
GET  /nearby?lat=&lng=&day_id=   Nearest hospital + police with directions
POST /sos                         Trigger SOS (body: user_id, trip_id, lat, lng)
```

### Tour Service — /api/v1/tours
```
GET  /search?dest=&theme=&days=   Search packages
GET  /{package_id}                Full package with day_plan
POST /inject                      Merge package into trip days (body: trip_id, package_id, start_day)
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

### First Generation (full chain):
```
1. Client POSTs wizard inputs
2. Trip Svc creates trip (status: PENDING) → publishes to Azure Service Bus queue
3. Azure Function picks job → Orchestrator Agent reads inputs → builds task graph
4. Research Agent → Google Places API + Azure AI Search vector query → scored POI list
5. Route Agent → Distance Matrix API → clusters POIs into day groups (min travel time)
6. Tour Package Injector → if package selected, merges day_plan blocks as LOCKED
7. Budget Agent → assigns estimated cost from pricing DB per budget tier
8. Day Assembly Agent → assigns 9am–10pm time slots, meal gaps, travel gaps
9. Personalisation Agent → GPT-4o reranks against themes → final itinerary JSON
10. Writes to Cosmos DB → status: COMPLETE → SSE push to client → renders on device
```

### Edit Mode (single agent):
```
E1. User action (add/remove/swap POI, change budget)
E2. Single Planner Agent loads current day JSON + user delta
E3. GPT-4o with tool calls for Maps/Places API
E4. Returns DIFF PATCH only → Cosmos DB partial update → client re-renders affected day
```

---

## 7. BUILD SEQUENCE (10 STEPS)

| Step | What to Build | Cloud Needed |
|------|--------------|--------------|
| 1 | Expo scaffold + navigation + onboarding with image backgrounds | Nothing |
| 2 | Auth (Microsoft Entra External ID) | Microsoft Entra External ID (entra.microsoft.com) |
| 3 | Trip Wizard UI (6-step form) | Nothing |
| 4 | .NET Trip Service + Cosmos DB + Azure PostgreSQL | Azure Cosmos DB + Azure PostgreSQL |
| 5 | Semantic Kernel + Orchestrator | OpenAI API key + Azure Service Bus + Azure Cache for Redis |
| 6 | Full agent chain (6 agents) | Google Cloud APIs |
| 7 | Booking Gateway | Azure Cache for Redis (already done in Step 5) |
| 8 | Trip Vault | Azure Blob Storage + Azure AI Document Intelligence |
| 9 | SafeTravel Shield | Azure Communication Services + Firebase (push only) |
| 10 | Finance + Tours + Polish | Nothing new |

**Rule:** NEVER build two steps in one session. Complete, test, commit before moving on.

---

## 8. KEY ARCHITECTURAL DECISIONS

- **Cosmos DB (MongoDB API) over Atlas** — fresh Azure account removes student restrictions; Cosmos DB MongoDB API uses the IDENTICAL MongoDB.Driver SDK — zero code changes, just swap the connection string
- **Azure PostgreSQL over Supabase** — same Npgsql SDK; all in one Azure portal for billing and monitoring
- **Azure Cache for Redis over Upstash Redis** — native Azure service, same StackExchange.Redis client
- **Azure Service Bus over Upstash QStash** — native Azure queue; Azure Functions trigger bindings work directly with Service Bus with no HTTP layer needed
- **Azure Communication Services over Twilio** — available on fresh Azure account; same REST model, lower cost per SMS
- **Firebase Cloud Messaging retained for push** — Azure Notification Hubs calls FCM for Android under the hood; using FCM directly is simpler, more reliable, and the SDK is mature
- **OpenAI API directly** — Azure OpenAI blocked on student accounts; direct API, one config line difference; fresh non-student account makes Azure OpenAI available but direct call is still simpler and cheaper at this scale
- **Microsoft Entra External ID for auth** — Azure AD B2C is discontinued for new customers as of May 2025; Entra External ID is the direct replacement with identical PKCE/user flow model, same OIDC endpoints, same token format, same env variable names. Tenant MUST be created at `entra.microsoft.com`, not `portal.azure.com`
- **Azure Blob for file storage** — available and reliable
- **Azure AI Search for vector POI search** — native to the stack, integrates with Semantic Kernel
- **Semantic Kernel over LangChain** — native .NET, Azure-native agent patterns
- **MMKV over AsyncStorage** — 30x faster, essential for offline ticket access
- **SignalR on Trip Svc** — real-time sync for group itinerary editing
- **Tour packages as LOCKED day blocks** — AI wraps around them, never overwrites

---

## 9. PROJECT FOLDER STRUCTURE

```
Planora/                          ← root folder
  images/                         ← onboarding + logo images
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
  FULL_PROCESS.md
  .gitignore
  PlanoraApp/                     ← Expo React Native frontend
  PlanoraApp.API/                 ← .NET 8 Web API backend
  PlanoraApp.AI/                  ← Semantic Kernel agent library
```
