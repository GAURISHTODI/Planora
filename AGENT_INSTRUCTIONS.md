# AGENT INSTRUCTIONS — Planora Builder
> App name: Planora
> Read PROJECT_BIBLE.md FULLY before acting on any instruction here.
> You build. You do not summarise unless asked.
> **v1.0:** Full Azure consolidation. MongoDB Atlas → Cosmos DB (MongoDB API). Supabase → Azure PostgreSQL. Upstash → Azure Cache for Redis + Azure Service Bus. Twilio → Azure Communication Services. Firebase retained for push.
> **v1.0:** Azure AD B2C discontinued for new customers (May 2025) → **Microsoft Entra External ID**. Tenant created at `entra.microsoft.com`. All SDK/env keys unchanged.
> **v2.0:** Skyscanner + Kayak denied → **AWIN/Booking.com** is primary affiliate partner. GCP Free Plan active — all Google APIs working. Bookings Tab removed from bottom nav — bookings are contextual inside trips and the Vault. Three trip creation modes added (AI, Manual, Package Browse). Edit Mode (add/delete spots) added. Spot Enrichment Agent added. Pre-wizard screen updated to 3-card layout.

---

## YOUR ROLE

You are a senior full-stack architect building **Planora** — a production-grade travel app. All details are in `PROJECT_BIBLE.md`. Write real, complete, working code only.

---

## CARDINAL RULES

1. **Read PROJECT_BIBLE.md before every session** — tag with @PROJECT_BIBLE.md at start
2. **One step at a time** — never start Step N+1 until Step N is tested and committed
3. **Before ANY cloud resource — STOP** — print `[CLOUD SETUP REQUIRED]` block, wait for confirmation
4. **Never hallucinate APIs** — if affiliate URL format unknown, say so and ask
5. **TypeScript on frontend, C# on backend** — no exceptions
6. **Every file must be complete** — no `// TODO` unless explicitly told to stub
7. **After each file:** `✓ Created: [filename] — Run: [test command]`
8. **After each step:** `git add . && git commit -m "Step X: description"`

---

## INFRASTRUCTURE — CRITICAL

```
Auth:         Microsoft Entra External ID — tenant created at entra.microsoft.com (NOT portal.azure.com)
              Azure AD B2C is discontinued for new customers as of May 2025
              Same PKCE/OIDC flow, same expo-auth-session setup, same env variable names
Document DB:  Azure Cosmos DB (MongoDB API) — use MongoDB.Driver SDK
              Connection string from Azure portal, NOT mongodb.com
SQL DB:       Azure Database for PostgreSQL (Flexible Server) — use Npgsql SDK
File Storage: Azure Blob Storage
Queue:        Azure Service Bus — Azure Functions trigger bindings (no HTTP layer)
Cache:        Azure Cache for Redis — use StackExchange.Redis SDK
AI:           OpenAI API DIRECTLY — use AddOpenAIChatCompletion(), NOT AddAzureOpenAIChatCompletion()
SMS:          Azure Communication Services (SMS)
Push:         Firebase Cloud Messaging (push notifications ONLY — simplest, most reliable)
Hosting:      Railway.app for dev, Azure Container Apps for production
Affiliate:    AWIN (Booking.com programme) — Skyscanner and Kayak DENIED, do not use
```

---

## .ENV FILES

### Frontend — `PlanoraApp/.env`
```env
EXPO_PUBLIC_API_BASE_URL=http://localhost:5000/api/v1
EXPO_PUBLIC_GOOGLE_MAPS_KEY=PASTE_HERE
EXPO_PUBLIC_B2C_CLIENT_ID=PASTE_HERE
EXPO_PUBLIC_B2C_TENANT_NAME=PASTE_HERE
EXPO_PUBLIC_B2C_POLICY=B2C_1_signupsignin
```

### Backend — `PlanoraApp.API/appsettings.Development.json`
```json
{
  "OpenAI": { "ApiKey": "sk-PASTE_HERE", "Model": "gpt-4o" },
  "CosmosDb": {
    "ConnectionString": "PASTE_HERE",
    "DatabaseName": "planora"
  },
  "PostgreSQL": {
    "ConnectionString": "PASTE_HERE"
  },
  "AzureStorage": { "ConnectionString": "PASTE_HERE" },
  "AzureRedis": {
    "ConnectionString": "PASTE_HERE"
  },
  "AzureServiceBus": {
    "ConnectionString": "PASTE_HERE",
    "QueueName": "ai-generation-jobs"
  },
  "AzureB2C": {
    "Instance": "https://TENANTNAME.b2clogin.com",
    "ClientId": "PASTE_HERE",
    "Domain": "TENANTNAME.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_signupsignin"
  },
  "AzureCommunicationServices": {
    "ConnectionString": "PASTE_HERE",
    "FromPhone": "PASTE_HERE"
  },
  "Google": { "PlacesApiKey": "PASTE_HERE" },
  "Firebase": { "ServerKey": "PASTE_HERE" },
  "Affiliates": {
    "BookingComAid": "PASTE_FROM_AWIN_DASHBOARD",
    "AgodaCid": "PASTE_FROM_AGODA",
    "ViatorMcid": "PASTE_FROM_VIATOR",
    "GygPartnerId": "PASTE_FROM_GYG"
  }
}
```
**This file is gitignored. Never commit it.**

---

## MODEL SELECTION

> YOU cannot switch models. USER switches manually in Antigravity dropdown.
> Display `[RECOMMENDED MODEL: X]` at start of every task.

| Task | Model |
|------|-------|
| Architecture, agent design, complex debugging | **Claude Opus 4.6** |
| Bulk file generation (5+ files, full scaffold) | **Claude Sonnet 4.6** |
| Quick edits, single file fixes, UI tweaks | **Claude Sonnet 4.6** |
| Cloud config, API research, external docs | **Gemini 2.5 Pro** |
| Full codebase review | **Gemini 2.5 Pro** |

---

## SESSION START (every session)

```
=== PLANORA SESSION START ===
Reading PROJECT_BIBLE.md...
Current Build Step: [USER FILLS]
Recommended Model:  [you decide]
Last completed:     [USER FILLS]
Next action:        [you decide]
=============================
```
Ask: "Confirm current step and last completed, then I'll begin."

---

## CLOUD SETUP BLOCK FORMAT

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Resource: [name]                         ║
║ Step 1: [exact URL + action]             ║
║ Step 2: [exact navigation]               ║
║ Step 3: Copy [X] →                       ║
║         appsettings.Development.json     ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

---

## BUILD STEP INSTRUCTIONS

---

### STEP 1 — Expo Scaffold + Navigation + Onboarding
`[RECOMMENDED MODEL: Claude Sonnet 4.6]`

```bash
npx create-expo-app PlanoraApp --template blank-typescript
cd PlanoraApp
npx expo install expo-router react-native-safe-area-context react-native-screens
npx expo install zustand @tanstack/react-query
npx expo install react-native-mmkv expo-sqlite
npx expo install react-native-maps
npx expo install expo-secure-store expo-local-authentication
npx expo install expo-image-picker expo-document-picker expo-camera
npx expo install react-native-gesture-handler react-native-reanimated
```

**Folder structure to create:**
```
PlanoraApp/
  app/
    (tabs)/
      _layout.tsx       ← 5 tabs: Home, Trips, Vault, Safety, Profile
                          ⚠ NO Bookings tab — removed in v2.0
      index.tsx
      trips.tsx
      vault.tsx         ← replaces bookings.tsx
      safety.tsx
      profile.tsx
    (onboarding)/
      _layout.tsx
      slide1.tsx
      slide2.tsx
      slide3.tsx
      slide4.tsx
      slide5.tsx
      slide6.tsx
      signin.tsx
      signup.tsx
    _layout.tsx
  assets/
    images/
  components/ui/
    Button.tsx, Card.tsx, Badge.tsx, Skeleton.tsx, Toast.tsx, SafetyFab.tsx
  store/
    tripStore.ts
    wizardStore.ts
    authStore.ts
  constants/
    theme.ts
  services/
    api.ts
  .env
  .gitignore
```

**Planora brand colors (theme.ts):**
```typescript
export const theme = {
  primary: '#6C63FF',
  secondary: '#FF6584',
  background: '#0F0E17',
  surface: '#1A1A2E',
  text: '#FFFFFE',
  textMuted: '#A7A9BE',
  success: '#2CB67D',
  warning: '#FF8906',
}
```

**Onboarding:**
- Each slide: full-screen ImageBackground
- LinearGradient overlay: `rgba(0,0,0,0.1)` → `rgba(0,0,0,0.7)`
- Logo top-center on slides 1, signin, signup
- Swipe gesture between slides
- "Skip" top-right on slides 1–6

Test: `npx expo start` → onboarding swipeable → all screens load.

---

### STEP 2 — Microsoft Entra External ID Auth
`[RECOMMENDED MODEL: Claude Opus 4.6]`

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Resource: Microsoft Entra External ID    ║
║ ⚠ Azure AD B2C is DISCONTINUED for new  ║
║   customers (May 2025). Use Entra.       ║
║ Step 1: Go to entra.microsoft.com        ║
║ Step 2: Entra ID → Manage tenants →     ║
║         Create → External               ║
║ Step 3: Tenant: Planora                  ║
║         Domain: planoratravel            ║
║ Step 4: App registrations → New          ║
║         Redirect URI: exp://localhost   ║
║ Step 5: Copy Client ID →                 ║
║         EXPO_PUBLIC_B2C_CLIENT_ID       ║
║ Step 6: User flows → signupsignin        ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

Files:
- `PlanoraApp/services/auth/authService.ts`
- `PlanoraApp/app/(onboarding)/signin.tsx`
- `PlanoraApp/app/(onboarding)/signup.tsx`
- `PlanoraApp/store/authStore.ts`

Test: full login returns JWT, stored securely, redirected to tabs.

---

### STEP 3 — Trip Wizard UI + Pre-Wizard Screen
`[RECOMMENDED MODEL: Claude Sonnet 4.6]`

**Pre-wizard screen — 3-card layout:**
```
┌──────────────────────────────────────────────────────┐
│                  How do you want to start?           │
├──────────────────────┬──────────────────┬────────────┤
│  🤖 AI Generate     │ 🗺 Build Manually │ 📦 Browse  │
│  My Trip            │                  │ Packages   │
└──────────────────────┴──────────────────┴────────────┘
```
- Card 1 → `/(wizard)/step1-destination`
- Card 2 → `/(manual-builder)/setup`
- Card 3 → `/(packages)/browse`

**AI Wizard files:**
- `PlanoraApp/app/wizard/_layout.tsx` — progress bar (Step X of 6)
- `PlanoraApp/app/wizard/step1-destination.tsx`
- `PlanoraApp/app/wizard/step2-dates.tsx` — date range + color-coded grid
- `PlanoraApp/app/wizard/step3-origin.tsx` — optional, toggle-gated
- `PlanoraApp/app/wizard/step4-group.tsx`
- `PlanoraApp/app/wizard/step5-themes.tsx`
- `PlanoraApp/app/wizard/step6-details.tsx`
- `PlanoraApp/store/wizardStore.ts`

Test: all 6 steps complete → wizardStore populated → 3-card pre-wizard screen visible.

---

### STEP 4 — .NET API + Azure Cosmos DB + Azure PostgreSQL
`[RECOMMENDED MODEL: Claude Sonnet 4.6]`

```bash
dotnet new webapi -n PlanoraApp.API --use-controllers
cd PlanoraApp.API
dotnet add package MongoDB.Driver
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.AspNetCore.SignalR
dotnet add package Microsoft.EntityFrameworkCore.Design
```

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Resource 1: Azure Cosmos DB (MongoDB API)║
║ Resource 2: Azure PostgreSQL             ║
║ → Follow FULL_PROCESS.md Steps 4–5      ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

Folder structure:
```
PlanoraApp.API/
  Services/
    TripService/
      TripController.cs
      TripService.cs
      TripRepository.cs
      Models/TripDocument.cs        ← includes trip_type field
      Models/ItineraryDayDocument.cs
      Models/PlaceOfInterestDocument.cs
    UserService/
      UserController.cs
      UserService.cs
  Infrastructure/
    CosmosDbClient.cs
    AppDbContext.cs
  Middleware/
    GlobalExceptionMiddleware.cs
  Program.cs
  appsettings.json
  appsettings.Development.json  ← gitignored
```

Implement all Trip Service endpoints from PROJECT_BIBLE Section 5, including:
- `POST /{trip_id}/days/{d}/pois` — used by Manual Builder AND Edit Mode
- `DELETE /{trip_id}/days/{d}/pois/{p}` — Edit Mode delete

Test: `dotnet run` → GET /api/v1/trips returns 200.

---

### STEP 5 — Semantic Kernel + Orchestrator + Spot Enrichment Agent
`[RECOMMENDED MODEL: Claude Opus 4.6]`

```bash
dotnet new classlib -n PlanoraApp.AI
cd PlanoraApp.AI
dotnet add package Microsoft.SemanticKernel
dotnet add package Microsoft.SemanticKernel.Connectors.OpenAI
dotnet add package StackExchange.Redis
dotnet add package Azure.Messaging.ServiceBus
```

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ OpenAI API Key + Azure Redis + Service  ║
║ Bus → Follow FULL_PROCESS.md Steps 6–7  ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

**Semantic Kernel — OpenAI DIRECTLY:**
```csharp
builder.AddOpenAIChatCompletion(
    modelId: config["OpenAI:Model"],
    apiKey: config["OpenAI:ApiKey"]
);
// NOT AddAzureOpenAIChatCompletion
```

Files:
```
PlanoraApp.AI/
  Agents/
    OrchestratorAgent.cs
    ResearchAgent.cs
    RouteOptimiserAgent.cs
    TourPackageInjector.cs
    BudgetAgent.cs
    DayAssemblyAgent.cs
    PersonalisationAgent.cs
    SpotEnrichmentAgent.cs       ← NEW: single-spot enrichment for Manual + Edit mode
  Functions/
    AgentWorkerFunction.cs       ← Azure Service Bus trigger
  Models/
    TripGenerationRequest.cs
    AgentJobStatus.cs
    ItineraryOutput.cs
    SpotEnrichmentRequest.cs     ← NEW
    SpotEnrichmentResult.cs      ← NEW
  SemanticKernelSetup.cs
```

**SpotEnrichmentAgent — prompt target ~800 tokens:**
```csharp
// Input: Google Place ID + trip context (destination, budget_tier, dates)
// Output: photo_url, ai_summary, estimated_cost, visit_duration_minutes,
//         booking_type (A/B/C), booking_url, opening_hours, entry_fee_approx,
//         geocoordinates (from Places API directly — no GPT needed for coords)

// Step 1: Google Places Details API call → get name, address, photos, website, coords
// Step 2: GPT-4o call (~800 tokens) → ai_summary + booking enrichment
// Return combined result
```

**Exposed via REST:**
```
POST /api/v1/ai/enrich-spot
Body: { place_id: string, trip_id: string, day_number: int }
Returns: enriched POI document
```

Test: POST /api/v1/ai/generate → job in Service Bus queue. POST /api/v1/ai/enrich-spot → returns enriched POI.

---

### STEP 6 — Full Agent Chain
`[RECOMMENDED MODEL: Opus 4.6 per agent, Sonnet 4.6 for writing files]`

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Resource: Google Cloud APIs              ║
║ GCP Free Plan is active — APIs working  ║
║ Step 1: console.cloud.google.com         ║
║ Step 2: Enable:                          ║
║   Places API (New)                      ║
║   Distance Matrix API                   ║
║   Maps SDK Android + iOS                ║
║ Step 3: Create API Key                   ║
║ Step 4: Copy to appsettings +.env        ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

Build one agent at a time, test before next:

1. **ResearchAgent** — Google Places API, scored POI list by theme
2. **RouteOptimiserAgent** — Distance Matrix, cluster into day groups
3. **TourPackageInjector** — merge locked package blocks from PostgreSQL
4. **BudgetAgent** — cost estimates from pricing JSON per tier
5. **DayAssemblyAgent** — time slots 9am–10pm, meal + travel gaps
6. **PersonalisationAgent** — GPT-4o rerank + ai_summary per POI

Also extend **ResearchAgent** with `EnrichPoiWithBookingData()` per BOOKING_ARCHITECTURE.md Section 3.

Write one xUnit test per agent with mocked inputs.

---

### STEP 7 — Booking Gateway (Contextual — No Bookings Tab)
`[RECOMMENDED MODEL: Claude Sonnet 4.6]`

> ⚠ IMPORTANT CHANGE from v1.0: There is NO standalone Bookings tab in the app nav.
> Booking options are rendered contextually inside the Trip Card (per POI, and in a "Book" tab within the trip detail screen).
> Skyscanner and Kayak are NOT used — they denied the partnership.
> Primary affiliate is Booking.com via AWIN (`aid` param).

Azure Cache for Redis already set up in Step 5 — use same credentials.

**Files:**
```
PlanoraApp.API/Services/BookingService/
  BookingController.cs
  AffiliateLinkBuilder.cs     ← see BOOKING_ARCHITECTURE.md for all deeplink formats
  PriceCacheService.cs        ← Azure Cache for Redis, 15min TTL
  Models/BookingOption.cs
  Models/PoiBookingOptions.cs
  Models/OfflineInfo.cs
  Models/BookingRequest.cs

PlanoraApp/app/trip/
  [trip_id]/
    _layout.tsx               ← tabs: Days | Map | Book | Vault
    book/
      hub.tsx                 ← 8-category grid (flights, hotels, activities, cabs, trains, dining, events, tickets)
      hotel-day.tsx           ← day tabs + mini map + hotel platforms
      activity-poi.tsx        ← per-POI Type A/B/C rendering
      flight.tsx
      cab.tsx
      train.tsx
      dining.tsx
      events.tsx
```

**Affiliate partner status:**
- Booking.com → CONFIRMED via AWIN. Use `aid` from `Affiliates:BookingComAid`
- Agoda → register at partners.agoda.com
- Viator → register at viator.com/partner
- GetYourGuide → register at partner.getyourguide.com
- MakeMyTrip / Ixigo / IRCTC / Uber / Ola / Zomato / BookMyShow → plain deeplinks (no affiliate ID required for basic deeplinks)
- Skyscanner → DENIED. Remove from all deeplink code.
- Kayak → DENIED. Remove from all deeplink code.

See `BOOKING_ARCHITECTURE.md` for all deeplink URL formats and the Type A/B/C booking decision logic.

Test: GET /api/v1/booking/options/{trip_id}/flight → returns platform deeplinks array. Booking.com hotel link contains `aid` param.

---

### STEP 8 — Manual Trip Builder + Edit Mode
`[RECOMMENDED MODEL: Claude Sonnet 4.6]`

**No new cloud resources needed for this step.**

#### 8A — Manual Trip Builder

Files:
```
PlanoraApp/app/(manual-builder)/
  setup.tsx             ← destination, dates, trip name, group size, budget tier
  builder.tsx           ← main day-by-day canvas, + Add Spot button per day
  spot-search.tsx       ← Google Places autocomplete sheet
  spot-loading.tsx      ← skeleton card while enrichment runs

PlanoraApp/store/
  manualTripStore.ts    ← state for manual trip in progress
```

**Spot add flow:**
1. User taps **+ Add Spot** on a day
2. `spot-search.tsx` opens as a bottom sheet
3. Google Places autocomplete returns results as user types
4. User selects a result → `POST /api/v1/ai/enrich-spot` fires
5. Skeleton card shown while loading
6. Enriched POI card populates in the day
7. Map updates with new pin

**Placement logic (in `builder.tsx`):**
- New spot always added at bottom of selected day by default
- User can drag to reorder within the day after adding
- Map polyline re-renders

#### 8B — Edit Mode (add/delete on any trip)

Edit Mode is accessible from any trip (AI-generated, manual, or package-import) via the **Edit** button (top right of trip card).

Files:
```
PlanoraApp/components/trip/
  EditModeOverlay.tsx       ← wraps trip card when edit=true, shows delete icons + Add Spot
  SpotDeleteButton.tsx      ← ✕ icon per POI, confirm dialog → DELETE /api/v1/trips/{id}/days/{d}/pois/{p}
  AddSpotSheet.tsx          ← reusable Places search + enrichment trigger
```

**Delete flow:**
1. Edit mode active → ✕ shown on each POI card
2. Tap ✕ → confirm dialog: "Remove [POI Name] from Day 2?"
3. Confirm → `DELETE /api/v1/trips/{id}/days/{d}/pois/{p}`
4. Day re-renders, map updates
5. No AI call

**Add spot placement algorithm (client-side, no AI call):**
```typescript
function findBestDay(newSpotLat: number, newSpotLng: number, days: Day[]): number {
  // 1. For each day, calculate centroid of existing POIs
  const centroids = days.map(day => ({
    dayNumber: day.dayNumber,
    lat: average(day.pois.map(p => p.lat)),
    lng: average(day.pois.map(p => p.lng)),
    poiCount: day.pois.length,
  }));
  // 2. Find day whose centroid is geographically nearest to new spot
  const nearest = centroids
    .filter(c => c.poiCount < 6)  // cap per day
    .sort((a, b) => haversineDistance(a, newSpotLat, newSpotLng)
                   - haversineDistance(b, newSpotLat, newSpotLng))[0];
  // 3. If all days full (≥6 POIs), pick day with fewest POIs
  return nearest?.dayNumber ?? dayWithFewestPois(days).dayNumber;
}
```

Test: Edit mode opens on generated trip → delete POI → map updates → add spot → enrichment runs → spot placed correctly.

---

### STEP 9 — Trip Vault (replaces Bookings Tab)
`[RECOMMENDED MODEL: Claude Sonnet 4.6]`

> The Vault is now the consolidation point for ALL confirmed bookings and travel documents.
> The old "Bookings" tab is GONE. Vault is Tab 3 in bottom nav.

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Resource 1: Azure Blob Storage           ║
║ Resource 2: Azure AI Document Intel.     ║
║ → Follow FULL_PROCESS.md Steps 8–9      ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

Files:
- `PlanoraApp.API/Services/VaultService/VaultController.cs` — includes `POST /save-booking` endpoint
- `PlanoraApp.API/Services/VaultService/BlobStorageService.cs`
- `PlanoraApp.API/Services/VaultService/OcrParserService.cs`
- `PlanoraApp/app/vault/index.tsx` — timeline sorted by travel_date
- `PlanoraApp/app/vault/upload.tsx` — PDF picker + camera + QR scanner
- `PlanoraApp/app/vault/save-booking.tsx` — manual save after external booking

**"Save to Vault" flow (from trip booking section):**
- After user taps a booking deeplink, a "Saved it? Add to Vault" prompt appears on return to app
- User taps → enters booking reference + confirmation number (optional)
- `POST /api/v1/vault/save-booking` → creates vault_item record
- Appears in Vault timeline

MMKV: write all vault items to `vault:{trip_id}` on fetch.

Test: upload PDF → OCR parses → timeline shows → accessible offline. Save-booking flow works from trip detail.

---

### STEP 10 — SafeTravel Shield
`[RECOMMENDED MODEL: Claude Opus 4.6]`

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Resource 1: Azure Communication Services ║
║ Resource 2: Firebase (push only)         ║
║ → Follow FULL_PROCESS.md Steps 10–11    ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

**Azure Communication Services SMS:**
```csharp
dotnet add package Azure.Communication.Sms
var client = new SmsClient(config["AzureCommunicationServices:ConnectionString"]);
await client.SendAsync(from: config["AzureCommunicationServices:FromPhone"],
    to: recipientPhone, message: sosMessage);
```

Files:
- `PlanoraApp.API/Services/SafetyService/SafetyController.cs`
- `PlanoraApp.API/Services/SafetyService/EmergencyContactsService.cs`
- `PlanoraApp.API/Services/SafetyService/SosService.cs`
- `PlanoraApp.API/Services/SafetyService/ProximityService.cs`
- `PlanoraApp/app/safety/shield.tsx`
- `PlanoraApp/app/safety/sos.tsx`
- `PlanoraApp/components/ui/SafetyFab.tsx` — rendered in root `_layout.tsx`, visible everywhere

SOS rules:
- 3000ms LongPressGestureHandler minimum
- Disclaimer modal → biometric → POST /safety/sos
- Log every attempt
- NEVER fire on single tap

Test: 3s hold → disclaimer → biometric → inner circle notified via SMS + push.

---

### STEP 11 — Finance + Package Browse + Polish
`[RECOMMENDED MODEL: Sonnet 4.6 for files, Gemini 2.5 Pro for codebase review]`

#### Finance:
- `PlanoraApp/app/finance/expenses.tsx`
- `PlanoraApp/app/finance/split.tsx`
- `PlanoraApp/app/finance/report.tsx`
- `PlanoraApp.API/Services/FinanceService/ExpenseController.cs`

#### Package Browse (Module 1B):
```
PlanoraApp/app/(packages)/
  browse.tsx            ← destination input + date range + duration + type chips
  results.tsx           ← Section A (Booking.com AWIN deeplinks) + Section B (other platform chips)
  import-prompt.tsx     ← "I've booked a package — import it" CTA → routes to Manual Builder
```

**Booking.com tours/activities deeplink (AWIN):**
```
https://www.booking.com/attractions/searchresults.html
  ?ss={DESTINATION_ENCODED}
  &checkin={YYYY-MM-DD}&checkout={YYYY-MM-DD}
  &aid={BOOKING_AID}
```

**Other platform chips (Section B — plain deeplinks):**
```
Viator:           https://www.viator.com/search/{DESTINATION}?startDate={DATE}
GetYourGuide:     https://www.getyourguide.com/s/?q={DESTINATION}&date_from={DATE}
MakeMyTrip Hols:  https://www.makemytrip.com/holidays-india/{DESTINATION}/
Thomas Cook:      https://www.thomascook.in/holidays/{DESTINATION}/
Club Mahindra:    https://www.clubmahindra.com/holiday-destinations
```

#### Polish checklist:
- [ ] Dark mode (Planora dark theme)
- [ ] Loading skeletons
- [ ] Error boundaries
- [ ] Offline banners (@react-native-community/netinfo)
- [ ] Empty states
- [ ] App icon (logo.jpg 1024x1024)
- [ ] Splash screen
- [ ] TestFlight + Play Store internal track

Final review: switch to **Gemini 2.5 Pro**, upload all files, ask "review for consistency, missing error handling, security issues."

---

## ERROR HANDLING

Frontend:
```typescript
try {
  const result = await api.createTrip(data);
} catch (error: any) {
  if (error?.response?.status === 401) router.replace('/(onboarding)/signin');
  else if (error?.response?.status === 503) showToast('AI is busy, try again');
  else showToast('Something went wrong');
}
```

Backend:
```csharp
// GlobalExceptionMiddleware in Program.cs
// Per-agent: catch → retry once (2s delay) → mark FAILED with error_log
// Never propagate agent exception — log and continue
```

---

## WHEN STUCK

| Situation | Action |
|-----------|--------|
| Build error | Sonnet 4.6 — paste exact error, "fix this only" |
| Architecture confusion | Opus 4.6 — re-read PROJECT_BIBLE section |
| Cloud/Azure issue | Gemini 2.5 Pro — describe issue |
| Full codebase check | Gemini 2.5 Pro — paste all files |

---

## SESSION TRACKER
> Update manually after each step

```
Current Step:     [ ]
Last Completed:   [ ]

Cloud Resources:
  [ ] Microsoft Entra External ID (entra.microsoft.com)
  [ ] Azure Cosmos DB (MongoDB API) — planora-cosmos
  [ ] Azure Database for PostgreSQL — planora-postgres
  [ ] OpenAI API key
  [ ] Azure Cache for Redis — planora-cache
  [ ] Azure Service Bus — planora-bus (queue: ai-generation-jobs)
  [ ] Google Cloud APIs (Places, Matrix, Maps) — GCP Free Plan active
  [ ] Azure Blob Storage — planoravault
  [ ] Azure AI Document Intelligence
  [ ] Azure Communication Services — planora-acs
  [ ] Firebase project (push notifications only)
  [ ] AWIN account + Booking.com affiliate programme approved

App Status:
  [ ] PlanoraApp running (npx expo start)
  [ ] PlanoraApp.API running (dotnet run)
  [ ] Onboarding images loading correctly
  [ ] .env created + gitignored
  [ ] appsettings.Development.json created + gitignored
  [ ] Pre-wizard 3-card screen visible
  [ ] No Bookings tab in bottom nav (Vault is Tab 3)
```
