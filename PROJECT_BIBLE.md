# PROJECT BIBLE — Planora
> Version 3.0 | App Name: Planora | Read this ENTIRELY before writing a single line of code.
> **v1.0:** Full Azure consolidation — Cosmos DB, Azure PostgreSQL, Azure Redis, Azure Service Bus, Azure Communication Services. Firebase for push.
> **v1.0:** Microsoft Entra External ID replaces Azure AD B2C (discontinued May 2025).
> **v2.0:** Skyscanner + Kayak denied. Bookings Tab removed. Three trip creation modes. Edit Mode.
> **v3.0:** Affiliate dependency removed entirely — deeplinks only, no partner approval needed. Booking model redesigned: CHOICEABLE vs NON-CHOICEABLE POI split. Multi-link cards per choiceable item. Vault redesigned as trip-aware document wallet with per-POI booking status. New visual identity: White + Black + #0E2038/#213A5C/#90A4B0 palette, Oswald + Playfair Display + Bebas Neue fonts. Dark theme abolished.

---

## 0. WHAT THIS APP IS

**App Name: Planora**
A **cross-platform mobile travel app** (iOS + Android):
1. **Plan** — AI-generated trip, Manual spot-by-spot, or Browse predefined agency packages
2. **Book** — Platform deeplinks surface per Choiceable item. No affiliate IDs. No partner approval. Links that work on day one.
3. **Store** — Vault: trip-aware document wallet. User tracks booking status per spot, uploads tickets, scans QR codes.
4. **Stay Safe** — SOS, emergency contacts, day-aware proximity safety

It is NOT a booking engine. It does NOT process payments. It is a planning + facilitation + safety + storage layer.

---

## 1. CORE CONCEPT — CHOICEABLE vs NON-CHOICEABLE

This is the central design principle of Planora's booking model.

```
CHOICEABLE ITEM
  Definition: A spot where the user has meaningful options to compare and choose between.
  Examples:   Hotels, Flights, Car rentals, Cabs, Trains
  Behaviour:  Shows 2–3 shortlisted platform deeplinks inline on the trip card.
              + a "Browse more" button that opens a full platform list bottom sheet.

NON-CHOICEABLE ITEM
  Definition: A spot where the destination is fixed. The user is going THERE. No comparison needed.
  Examples:   Gateway of India, Hawa Mahal, Jantar Mantar, a hiking trail, a restaurant (specific)
  Behaviour:  Shows info card (visit time, entry fee, hours, directions link).
              If online ticketing exists → one direct ticket deeplink.
              No "compare options" UI. No Browse button.
```

**The rule:**
- Choosing WHICH ONE → **Choiceable** (which hotel? which flight? which cab?)
- Going THERE → **Non-Choiceable** (Hawa Mahal is Hawa Mahal — no choice to make)

**Stored on every POI as `poi_class`:**
```
"choiceable"      → hotel, flight, cab, train, car_rental
"non_choiceable"  → monument, museum, park, beach, specific_restaurant, attraction
```

**Choiceable inline card:**
```
┌─────────────────────────────────────────┐
│  🏨 Where to Stay — Night 1             │
│  Near: City Palace area, Jaipur         │
├─────────────────────────────────────────┤
│  [Booking.com ↗]  [Agoda ↗]  [MMT ↗]  │
│                                         │
│         + Browse more options           │
└─────────────────────────────────────────┘
```

**Non-Choiceable inline card:**
```
┌─────────────────────────────────────────┐
│  🏛  Hawa Mahal                         │
│  ⏱ 60 min  ·  🕘 Best: Morning         │
│  💰 ₹50 entry  ·  🕐 09:00–17:00       │
│                                         │
│  [🎫 Buy Ticket ↗]  [📍 Directions ↗]  │
└─────────────────────────────────────────┘
```

---

## 2. CORE MODULES

### MODULE 1 — Itinerary Engine

#### Pre-Wizard Screen — Three Entry Cards:
```
┌────────────────────────────────────────────────────┐
│           How do you want to start?                │
├──────────────────┬──────────────────┬──────────────┤
│  AI Generate     │  Build Manually  │  Browse      │
│  My Trip         │                  │  Packages    │
└──────────────────┴──────────────────┴──────────────┘
```

---

#### MODULE 1A — AI Generate Trip

**Wizard (6 steps):**
| Step | Input | Notes |
|------|-------|-------|
| 1 | Where To | City/country, multi-destination toggle, map pin |
| 2 | Dates | Calendar picker |
| 3 | Where From | OPTIONAL. Origin city |
| 4 | Group + Budget | Solo/Couple/Family/Friends, pax count, Low/Mid/Luxury |
| 5 | Themes | Food, Adventure, Shopping, Party, History, Nature |
| 6 | Details | Dietary, accessibility, must-visit places, avoid |

**Agent Chain (Semantic Kernel):**
```
Orchestrator → Research → Route Optimiser → Tour Package Injector → Budget → Day Assembly → Personalisation
```

| Agent | Responsibility | External Call |
|-------|---------------|---------------|
| Orchestrator | Reads inputs, builds task graph | None |
| Research | Fetches POIs, assigns poi_class | Google Places API |
| Route Optimiser | Clusters POIs into day groups by geo | Distance Matrix API |
| Tour Package Injector | Merges locked package blocks | Internal Package DB |
| Budget | Cost estimates per tier | Internal pricing JSON |
| Day Assembly | Time slots 9am–10pm, meal + travel gaps | None |
| Personalisation | GPT-4o rerank + enrichment per POI | OpenAI API (GPT-4o) |

**What Personalisation Agent outputs per Non-Choiceable POI:**
```json
{
  "poi_id": "UUID",
  "name": "Hawa Mahal",
  "poi_class": "non_choiceable",
  "category": "monument",
  "lat": 26.9239, "lng": 75.8267,
  "visit_duration_minutes": 60,
  "best_time_of_day": "morning",
  "estimated_cost": "₹50",
  "ai_summary": "One of Jaipur's most iconic facades...",
  "photo_url": "...",
  "ticket_url": "https://asi.payumoney.com/...",
  "is_free": false,
  "opening_hours": "09:00–17:00",
  "google_place_id": "ChIJ..."
}
```

**What Personalisation Agent outputs per Choiceable POI (hotel):**
```json
{
  "poi_id": "UUID",
  "name": "Hotel Stay — Jaipur Night 1",
  "poi_class": "choiceable",
  "category": "hotel",
  "lat": 26.9124, "lng": 75.7873,
  "checkin": "2025-12-01",
  "checkout": "2025-12-02",
  "suggested_options": [
    { "platform": "Booking.com", "url": "https://www.booking.com/searchresults.html?latitude=26.9124&longitude=75.7873&radius=2&checkin_year=2025&checkin_month=12&checkin_monthday=1&checkout_year=2025&checkout_month=12&checkout_monthday=2&group_adults=2&no_rooms=1" },
    { "platform": "Agoda",       "url": "https://www.agoda.com/search?latitude=26.9124&longitude=75.7873&checkIn=2025-12-01&checkOut=2025-12-02&rooms=1&adults=2" },
    { "platform": "MakeMyTrip",  "url": "https://www.makemytrip.com/hotels/hotel-listing/?city=JAI&checkin=01122025&checkout=02122025&roomStayQualifier=2e0e&country=IN" }
  ],
  "browse_links": [
    { "platform": "OYO",     "url": "https://www.oyorooms.com/search/?location=Jaipur&check_in=2025-12-01&check_out=2025-12-02&guest_count=2" },
    { "platform": "Goibibo", "url": "https://www.goibibo.com/hotels/hotels-in-jaipur/?ci=20251201&co=20251202&r=1&a=2" }
  ]
}
```

**Day example output (Jaipur Day 1):**
```
Day 1 — Jaipur Heritage

09:00  Hawa Mahal           [Non-Choiceable]  ₹50 ticket link + directions
11:00  Jantar Mantar        [Non-Choiceable]  ₹200 ticket link + directions
13:00  Laxmi Misthan Bhandar [Non-Choiceable]  directions only
15:00  City Palace Museum   [Non-Choiceable]  ₹500 ticket link + directions
19:00  Chokhi Dhani         [Non-Choiceable]  ₹800 entry link

--- Hotel Stay — Night 1   [Choiceable] ---
  Booking.com ↗  ·  Agoda ↗  ·  MakeMyTrip ↗
  + Browse more options
```

**Job tracking:** `ai_generation_jobs` in PostgreSQL — status, current_agent, progress_pct. SSE stream to client.

---

#### MODULE 1A-EDIT — Edit Mode

Any trip (generated, manual, or loaded package) has an **Edit** button (top right of trip card).

**Delete a spot:**
- Edit mode active → ✕ icon on each POI card
- Tap ✕ → confirm dialog → `DELETE /api/v1/trips/{id}/days/{d}/pois/{p}`
- Day re-renders + map updates
- No AI call. Pure local state + Cosmos PATCH.

**Add a spot:**
- Tap **+ Add Spot** on any day in edit mode
- Google Places API autocomplete search sheet
- User selects → `POST /api/v1/ai/enrich-spot` fires (Spot Enrichment Agent)
- ~800 tokens: Places API details + GPT-4o summary + poi_class assignment + deeplinks built
- **Placement algorithm (local, no AI call):**
  - Calculate centroid of each day's POI cluster
  - Place in the day whose centroid is nearest to new spot's lat/lng
  - If that day ≥ 6 POIs → try adjacent days
  - Re-sort day by geo-proximity + best_time_of_day
- Map + polyline updates

---

#### MODULE 1B — Browse Packages

**Flow:**
1. User taps **Browse Packages** → search screen (destination, duration, theme, budget chips)
2. Package cards shown: photo, title, duration, price-per-person, agency name
3. Tap a package → full package detail (read-only day-by-day preview)
4. **"Load to My Trips"** → package becomes a full trip in the user's trip list
5. Edit Mode available from that point — user can add/remove/swap spots

**Package data (PostgreSQL, managed by agency admin portal):**
```json
{
  "package_id": "UUID",
  "agency_id": "UUID",
  "title": "Golden Triangle — 7 Days",
  "destination": "Delhi, Agra, Jaipur",
  "duration_days": 7,
  "price_per_person": 25000,
  "budget_tier": "mid",
  "themes": ["history", "culture"],
  "day_plan": [ ... array of day objects with pre-assigned POIs ... ]
}
```

---

#### MODULE 1C — Build Manually (Spot-by-Spot)

**Token model:** ~800 tokens per spot vs ~10,000 for full AI generation.

**Flow:**
1. Tap **Build Manually** → enter: destination, trip name, dates, group size, budget tier
2. Empty day containers shown
3. Tap **+ Add Spot** on any day → Places autocomplete sheet
4. Select place → Spot Enrichment Agent (~800 tokens):
   - Places API: photo, geocoords, name, address, website
   - GPT-4o: ai_summary, poi_class, entry fee, ticket URL, visit duration
   - Deeplinks constructed for Choiceable items
5. Skeleton card → populates → map pin added
6. Tap **Done** → trip saved in full Trip Card format

**Adding a hotel in manual mode:**
- Tap **+ Add Hotel** shortcut chip (always visible at bottom of each day)
- poi_class = "choiceable", category = "hotel"
- Deeplinks built using that day's POI cluster centroid as lat/lng

---

### MODULE 2 — Booking via Deeplinks

**No affiliate IDs. No partner approval needed. All deeplinks are plain URLs.**

All deeplink URLs are:
- Built at trip/spot generation time by `DeeplinkBuilderService.cs`
- Stored directly on the POI document in Cosmos DB
- At booking time: client calls `Linking.openURL(poi.suggested_options[n].url)` — that's it

**Choiceable → inline card shows 2–3 options + Browse button**
**Non-Choiceable → info card + single ticket URL if exists + Google Maps directions link**

See `BOOKING_ARCHITECTURE.md` for all deeplink URL formats.

---

### MODULE 3 — Trip Vault

**Purpose:** Trip-and-POI-aware document wallet. Organised by trips, then by spots within each trip.

#### Vault Home:
```
My Vault

  Jaipur Trip — Dec 1–7        ✅ 4 booked  ⏳ 3 pending  →
  Goa Weekend — Jan 10–12      ⏳ All pending              →

  + Upload a Document (not linked to a trip)
```

#### Inside a Trip in the Vault:
```
Jaipur Trip — Dec 1–7

Day 1
  Hawa Mahal           ⏳ Not booked    [Add Ticket / Note]
  Jantar Mantar        ✅ Ticket added  [View]
  Hotel Night 1        ✅ Booking ref   [View]

Day 2
  Amber Fort           ⏳ Not booked    [Add Ticket / Note]
  City Palace          ✅ Ticket added  [View]
```

**Add Ticket / Note options (bottom sheet):**
- Upload PDF or photo
- Scan QR code
- Enter booking reference manually
- Forward confirmation email (auto-parsed by OCR)

**Per vault_item stored:**
- Linked to specific `trip_id` + `poi_id`
- `booking_status`: booked / not_booked
- `type`: ticket / hotel_confirmation / flight_confirmation / booking_ref / other
- `blob_url` + `parsed_data` JSONB
- `is_offline_cached` (MMKV)

**Smart features:**
- Check-in reminders push notification (24h before spot time)
- Flight status alerts (Firebase)
- Calendar sync
- Share itinerary (public link)
- All vault data MMKV-cached — works offline

---

### MODULE 4 — SafeTravel Shield
Persistent FAB on every screen.

**A) Emergency Contacts** — country-aware, auto-loaded from trip destination
**B) SOS** — 3s hold → disclaimer → biometric → GPS broadcast via SMS + push
**C) Day-wise Proximity** — nearest hospital + police, cached per day offline

---

### MODULE 5 — Finance Layer
- Add expenses per day/category
- Multi-currency + live conversion
- Group split calculator
- Post-trip spend vs budget report

---

### MODULE 6 — Tour Package Registry
- Agency admin portal (separate from mobile app)
- Agencies register packages with full day_plan JSON
- Mobile: search, preview, load package → becomes a full user trip

---

## 3. VISUAL DESIGN SYSTEM

> This is the definitive design spec. Every screen and component must follow this exactly. The old dark purple/indigo/grey theme is completely abolished.

### 3.1 Colour Palette

```
White:         #FFFFFF   — all backgrounds, all card surfaces
Black:         #000000   — primary CTA buttons, strongest text
Navy Deep:     #0E2038   — hero gradients, map Day 1, brand anchor colour, section headers
Navy Mid:      #213A5C   — secondary buttons, active states, Day 2 map colour, accents
Steel Blue:    #90A4B0   — borders, muted text, tags, dividers, Day 3 map colour, inactive states
```

**Additional derived values (never use anything outside this list):**
```
Surface Muted:  #F5F6F8  — secondary card backgrounds, input fill
Border Light:   #E2E8EC  — very light dividers, bottom nav top border
Status Green:   #2CB67D  — ONLY used for "Booked" status indicator
Status Pending: #90A4B0  — "Not booked yet" indicator
Error:          #CC0000  — only destructive actions
```

**Abolished colours (do not use anywhere):**
- `#6C63FF` (indigo/purple) — gone
- `#FF6584` (coral) — gone
- `#0F0E17` (near black bg) — gone
- `#1A1A2E` (dark card) — gone
- `#FF8906` (orange) — gone
- `#A7A9BE` (muted purple-grey) — gone

**Usage rules:**
- All page backgrounds: `#FFFFFF`
- All card backgrounds: `#FFFFFF`
- Primary buttons: `#000000` bg, `#FFFFFF` text
- Secondary buttons: `#FFFFFF` bg, `#0E2038` border + text
- Active chips/tabs: `#213A5C` bg, `#FFFFFF` text
- Inactive chips/tabs: `#F5F6F8` bg, `#90A4B0` text
- Section headers: `#0E2038` text
- Body text: `#213A5C`
- Muted / secondary text: `#90A4B0`
- Map: light style, Day colours cycle `#0E2038` → `#213A5C` → `#90A4B0` → `#4A6FA5` → `#6B8FA8`

---

### 3.2 Typography

**Install:**
```bash
npx expo install @expo-google-fonts/oswald @expo-google-fonts/playfair-display @expo-google-fonts/bebas-neue expo-font
```

**Import:**
```typescript
import {
  Oswald_200ExtraLight,
  Oswald_400Regular,
  Oswald_600SemiBold,
  Oswald_700Bold,
} from '@expo-google-fonts/oswald';
import {
  PlayfairDisplay_400Regular,
  PlayfairDisplay_700Bold,
  PlayfairDisplay_400Regular_Italic,
} from '@expo-google-fonts/playfair-display';
import { BebasNeue_400Regular } from '@expo-google-fonts/bebas-neue';
```

**Font roles:**
| Role | Font | Weight | Notes |
|------|------|--------|-------|
| `heading1` | Bebas Neue | 400 | ALL CAPS ALWAYS — screen titles, onboarding headlines, trip name hero |
| `heading2` | Oswald | 700 | Day headers, card section headers |
| `heading3` | Oswald | 600 | POI names, sub-section labels |
| `body` | Oswald | 400 | All body text, descriptions, form labels |
| `bodyLight` | Oswald | 200 | Tags, sub-labels, muted hints |
| `accent` | Playfair Display | 400 italic | Destination names on cards, hero sub-headings |
| `accentBold` | Playfair Display | 700 | Package names, featured destination names |
| `stat` | Bebas Neue | 400 | Numbers: totals, distances, durations, counts |

**Size scale:**
```typescript
export const fontSize = {
  hero:  48,  // Bebas Neue — onboarding, trip name hero
  h1:    32,  // Bebas Neue — screen title
  h2:    22,  // Oswald 700
  h3:    18,  // Oswald 600
  body:  15,  // Oswald 400
  small: 13,  // Oswald 400 or 200
  micro: 11,  // Oswald 200
  stat:  28,  // Bebas Neue — numbers
  accentLg: 20, // Playfair Display italic — destination hero
  accentSm: 16, // Playfair Display italic — card sub-heading
}
```

---

### 3.3 Theme File — `constants/theme.ts`

```typescript
export const theme = {
  colors: {
    white:        '#FFFFFF',
    black:        '#000000',
    navyDeep:     '#0E2038',
    navyMid:      '#213A5C',
    steelBlue:    '#90A4B0',

    background:   '#FFFFFF',
    surface:      '#FFFFFF',
    surfaceMuted: '#F5F6F8',

    textPrimary:  '#0E2038',
    textBody:     '#213A5C',
    textMuted:    '#90A4B0',
    textOnDark:   '#FFFFFF',

    border:       '#90A4B0',
    borderLight:  '#E2E8EC',

    btnPrimary:   '#000000',
    btnSecondary: '#FFFFFF',
    btnActive:    '#213A5C',

    statusBooked:  '#2CB67D',
    statusPending: '#90A4B0',
    error:         '#CC0000',

    mapDay: ['#0E2038', '#213A5C', '#90A4B0', '#4A6FA5', '#6B8FA8'],
  },

  fonts: {
    heading1:   'BebasNeue_400Regular',
    heading2:   'Oswald_700Bold',
    heading3:   'Oswald_600SemiBold',
    body:       'Oswald_400Regular',
    bodyLight:  'Oswald_200ExtraLight',
    accent:     'PlayfairDisplay_400Regular_Italic',
    accentBold: 'PlayfairDisplay_700Bold',
    stat:       'BebasNeue_400Regular',
  },

  fontSize: {
    hero: 48, h1: 32, h2: 22, h3: 18,
    body: 15, small: 13, micro: 11, stat: 28,
    accentLg: 20, accentSm: 16,
  },

  spacing: {
    xs: 4, sm: 8, md: 16, lg: 24, xl: 32, xxl: 48,
  },

  radius: {
    sm: 6, md: 12, lg: 20, full: 999,
  },

  shadow: {
    card: {
      shadowColor: '#0E2038',
      shadowOffset: { width: 0, height: 2 },
      shadowOpacity: 0.07,
      shadowRadius: 8,
      elevation: 3,
    },
  },
}
```

---

### 3.4 Component Design Rules

**Cards:**
- White background
- Border: `1px solid #E2E8EC` OR shadow (not both)
- `border-radius: 12px`, padding `16px`
- No dark backgrounds anywhere

**Buttons:**
- Primary: `bg #000000`, text `#FFFFFF`, Oswald 600, `border-radius 6px`, height 48px
- Secondary: `bg #FFFFFF`, border `1.5px #0E2038`, text `#0E2038`
- Ghost chip: `bg #F5F6F8`, text `#213A5C`, `border-radius 999px`
- Active chip: `bg #213A5C`, text `#FFFFFF`
- Destructive: `bg #FFFFFF`, border `1.5px #CC0000`, text `#CC0000`

**Bottom Navigation:**
- Background: `#FFFFFF`
- Top border: `1px solid #E2E8EC`
- Active icon + label: `#0E2038`
- Inactive: `#90A4B0`

**Input fields:**
- Background: `#FFFFFF`
- Border: `1px solid #90A4B0`, focus: `#0E2038`
- Label: Oswald 400, `#213A5C`
- Placeholder: Oswald 200, `#90A4B0`

**Map:**
- Light map style (Google Maps default light — NOT dark mode)
- POI pins coloured by day (`theme.colors.mapDay` array)
- Route polyline matches day colour

**Onboarding slides:**
- Full screen `ImageBackground`
- Gradient: `rgba(14, 32, 56, 0.25)` top → `rgba(14, 32, 56, 0.85)` bottom
- Headline: Bebas Neue 48, white, ALL CAPS
- Subtext: Playfair Display italic 16, white
- Dots: white active, `rgba(255,255,255,0.4)` inactive
- Planora logo: top center, white tint

---

## 4. TECH STACK

### Frontend
| Technology | Purpose |
|-----------|---------|
| React Native (Expo SDK 51+) | Cross-platform iOS + Android |
| Expo Router (file-based) | Navigation |
| Zustand | Global state |
| MMKV | Offline storage |
| React Native Maps | Google Maps (light theme) |
| TanStack Query | Server state + caching |
| @expo-google-fonts | Oswald, Playfair Display, Bebas Neue |
| expo-font | Font loading |
| expo-auth-session | PKCE auth |
| expo-secure-store | Token storage |
| expo-local-authentication | Biometric for SOS |
| react-native-gesture-handler | Swipe, long press |
| react-native-reanimated | Animations |

### Backend (.NET 8, C#)
- MongoDB.Driver (Cosmos DB MongoDB API)
- Npgsql + EF Core (Azure PostgreSQL)
- StackExchange.Redis (Azure Cache for Redis)
- Azure.Messaging.ServiceBus
- Microsoft.SemanticKernel + Connectors.OpenAI
- Microsoft.AspNetCore.SignalR

---

## 5. BOTTOM NAVIGATION

```
Tab 1: Home      — new trips, recent, recommendations
Tab 2: My Trips  — all trips list
Tab 3: Vault     — trip-aware document wallet
Tab 4: Safety    — SafeTravel Shield (also persistent FAB)
Tab 5: Profile
```

> NO Bookings tab. Deeplinks live inside each trip card. Documents/confirmations live in the Vault.

---

## 6. DATA MODELS

### Cosmos DB Collections:
```
trips                 ← trip metadata, wizard inputs, status, trip_type
itinerary_days        ← per-day data, POI list
places_of_interest    ← POI doc including poi_class, deeplinks, enrichment
```

### POI Document — key fields:
```json
{
  "poi_id": "UUID",
  "trip_id": "UUID",
  "day_number": 1,
  "poi_class": "choiceable | non_choiceable",
  "category": "hotel | flight | cab | train | monument | museum | park | restaurant | ...",
  "name": "...",
  "lat": 0.0, "lng": 0.0,
  "sort_order": 1,
  "visit_duration_minutes": 60,
  "best_time_of_day": "morning | afternoon | evening | any",
  "estimated_cost": "₹200",
  "ai_summary": "...",
  "photo_url": "...",
  "opening_hours": "09:00–17:00",
  "is_free": false,
  "ticket_url": "...",
  "google_place_id": "ChIJ...",
  "suggested_options": [
    { "platform": "Booking.com", "url": "...", "logo": "bookingcom" },
    { "platform": "Agoda",       "url": "...", "logo": "agoda" },
    { "platform": "MakeMyTrip",  "url": "...", "logo": "mmt" }
  ],
  "browse_links": [
    { "platform": "OYO",     "url": "...", "logo": "oyo" },
    { "platform": "Goibibo", "url": "...", "logo": "goibibo" },
    { "platform": "Cleartrip","url": "...", "logo": "cleartrip" }
  ]
}
```

### PostgreSQL Tables:
```sql
-- users
user_id UUID PK, entra_oid VARCHAR UNIQUE, name VARCHAR,
email VARCHAR UNIQUE, phone VARCHAR, avatar_url TEXT,
home_city VARCHAR, preferences JSONB, created_at TIMESTAMP

-- agencies
agency_id UUID PK, name VARCHAR, verified BOOL, contact JSONB

-- packages
package_id UUID PK, agency_id UUID FK, title VARCHAR,
destination VARCHAR, duration_days INT, price_per_person DECIMAL,
budget_tier TEXT, themes TEXT[], day_plan JSONB, is_active BOOL

-- emergency_contacts
contact_id UUID PK, country_code CHAR(2), agency_type TEXT,
name VARCHAR, phone VARCHAR, email VARCHAR, url TEXT

-- sos_events
sos_id UUID PK, user_id UUID, trip_id UUID,
triggered_at TIMESTAMP, lat DECIMAL, lng DECIMAL,
notified_contacts JSONB, resolved_at TIMESTAMP

-- vault_items
item_id UUID PK, trip_id UUID, poi_id UUID, user_id UUID,
type TEXT, blob_url TEXT, parsed_data JSONB,
booking_status TEXT, source TEXT, is_offline_cached BOOL,
created_at TIMESTAMP

-- trip_expenses
expense_id UUID PK, trip_id UUID, user_id UUID,
category TEXT, amount DECIMAL, currency CHAR(3),
paid_by UUID, split_with JSONB, receipt_url TEXT, expense_date DATE

-- ai_generation_jobs
job_id UUID PK, trip_id UUID, status TEXT,
current_agent VARCHAR, progress_pct INT,
error_log TEXT, started_at TIMESTAMP, completed_at TIMESTAMP
```

---

## 7. API CONTRACTS

### Trip Service — /api/v1/trips
```
POST   /                              Create trip + queue AI job
GET    /{trip_id}                     Full trip with days + POIs
PATCH  /{trip_id}                     Edit metadata
DELETE /{trip_id}                     Soft delete
GET    /{trip_id}/days                Days with POIs
POST   /{trip_id}/days/{d}/pois       Add POI to day
DELETE /{trip_id}/days/{d}/pois/{p}   Remove POI
PATCH  /{trip_id}/days/{d}/reorder    Reorder POIs
GET    /{trip_id}/share-link          Public share token
WS     /ws/trips/{trip_id}            SignalR real-time sync
```

### AI Service — /api/v1/ai
```
POST /generate              Full agent chain → job_id
POST /enrich-spot           Single spot (place_id + trip_id + day_number) → enriched POI with deeplinks
GET  /jobs/{id}/status      SSE: { agent, progress_pct, message }
```

### Vault Service — /api/v1/vault
```
POST /upload                Upload PDF/image → OCR → store
POST /save-ref              Save booking reference for a poi_id
GET  /{trip_id}             All vault items grouped by poi_id
PATCH /{item_id}/status     Update booking_status
DELETE /{item_id}           Remove item
```

### Safety Service — /api/v1/safety
```
GET  /contacts/{country_code}    Emergency contacts
GET  /nearby?lat=&lng=&day_id=   Nearest hospital + police
POST /sos                         Trigger SOS
```

### Tour Service — /api/v1/tours
```
GET  /search?dest=&theme=&days=   Search packages
GET  /{package_id}                Full package + day_plan
POST /load                        Load package → new trip for user
```

### Finance Service — /api/v1/expenses
```
POST /              Add expense
GET  /{trip_id}     All expenses
GET  /{trip_id}/split     Split per person
GET  /{trip_id}/report    Spend vs budget
```

---

## 8. AGENT EXECUTION

### Full AI Generation:
```
1. Client POSTs wizard inputs → trip PENDING → Azure Service Bus job
2. Orchestrator builds task graph
3. Research Agent → Places API → scored POI list, poi_class assigned
4. Route Optimiser → Distance Matrix → day clusters
5. Tour Package Injector → locked blocks merged
6. Budget Agent → cost estimates
7. Day Assembly → time slots 9am–10pm
8. Personalisation Agent → GPT-4o per POI:
   - ai_summary, ticket_url, entry fee
   - DeeplinkBuilderService constructs suggested_options + browse_links
9. Cosmos DB updated → COMPLETE → SSE → renders
```

### Spot Enrichment (Edit Mode + Manual Builder):
```
1. POST /api/v1/ai/enrich-spot (place_id, trip_id, day_number)
2. Google Places API → photo, coords, website, name (~instant)
3. GPT-4o (~800 tokens) → ai_summary, poi_class, entry_fee, ticket_url, visit_duration
4. DeeplinkBuilderService → builds suggested_options + browse_links for Choiceable
5. Local placement algo → determines day + sort_order
6. PATCH Cosmos DB → client re-renders
```

---

## 9. BUILD SEQUENCE (11 STEPS)

| Step | What | Cloud Needed |
|------|------|-------------|
| 1 | Expo scaffold + navigation + onboarding with new design system | Nothing |
| 2 | Auth (Microsoft Entra External ID) | Entra External ID |
| 3 | Pre-wizard 3-card screen + Wizard UI | Nothing |
| 4 | .NET API + Cosmos DB + PostgreSQL | Cosmos DB + PostgreSQL |
| 5 | Semantic Kernel + Orchestrator + Spot Enrichment Agent | OpenAI + Redis + Service Bus |
| 6 | Full agent chain + DeeplinkBuilderService | Google Cloud APIs |
| 7 | Trip Card UI — Choiceable/Non-Choiceable rendering | Nothing new |
| 8 | Manual Builder + Edit Mode | Nothing new |
| 9 | Trip Vault (trip-and-poi-aware, status tracking) | Blob Storage + Doc Intelligence |
| 10 | SafeTravel Shield | Azure ACS + Firebase |
| 11 | Finance + Package Browse + Polish | Nothing new |

---

## 10. KEY ARCHITECTURAL DECISIONS

- **No affiliate IDs** — all deeplinks are plain URLs. No partner approval needed. Work on day one. Affiliate params can be appended later without changing any architecture.
- **Choiceable vs Non-Choiceable** — the defining UX principle. Choiceable: show options + Browse. Non-Choiceable: show info + one ticket link.
- **Deeplinks built at generation time** — stored on POI document. Booking tap = `Linking.openURL()`. No runtime URL construction.
- **~800 tokens per Spot Enrichment** — efficient for Manual Builder and Edit Mode.
- **Local placement algorithm** — no AI for deciding which day a spot belongs to. Pure geo-math on cluster centroids.
- **Vault is trip-and-poi-aware** — every document linked to a specific spot. Organised, not a file dump.
- **White + navy design** — clean, editorial, premium. Absolutely no dark backgrounds or purple.
- **Bebas Neue** — impact headlines, ALL CAPS, editorial weight.
- **Playfair Display italic** — warmth and travel-magazine character on destination names.
- **Oswald** — clean geometric at all weights from 200 to 700 for all body/UI text.
