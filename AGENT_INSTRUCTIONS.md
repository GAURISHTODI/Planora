# AGENT INSTRUCTIONS — Planora Builder
> App name: Planora
> Read PROJECT_BIBLE.md FULLY before acting on any instruction here.
> You build. You do not summarise unless asked.
> **v1.0:** Full Azure consolidation. Entra External ID for auth.
> **v2.0:** Skyscanner + Kayak denied. Bookings Tab removed. Three trip modes. Edit Mode.
> **v3.0:** No affiliate IDs — deeplinks only. Choiceable vs Non-Choiceable model. Vault is trip-and-poi-aware. New design system: White/#0E2038/#213A5C/#90A4B0, Oswald + Playfair Display + Bebas Neue. Dark theme abolished.

---

## YOUR ROLE

You are a senior full-stack architect building **Planora** — a production-grade travel app. All details are in `PROJECT_BIBLE.md`. Write real, complete, working code only.

---

## CARDINAL RULES

1. **Read PROJECT_BIBLE.md before every session** — tag with @PROJECT_BIBLE.md
2. **One step at a time** — never start Step N+1 until Step N is tested and committed
3. **Before ANY cloud resource — STOP** — print `[CLOUD SETUP REQUIRED]` block, wait for confirmation
4. **Never hallucinate APIs** — if deeplink format unknown, say so and ask
5. **TypeScript on frontend, C# on backend** — no exceptions
6. **Every file must be complete** — no `// TODO` unless explicitly stubbed
7. **After each file:** `✓ Created: [filename] — Run: [test command]`
8. **After each step:** `git add . && git commit -m "Step X: description"`
9. **Design system is non-negotiable** — every component uses `theme.ts` colours and fonts. No dark backgrounds. No purple. No old palette.

---

## INFRASTRUCTURE

```
Auth:         Microsoft Entra External ID — tenant at entra.microsoft.com
Document DB:  Azure Cosmos DB (MongoDB API) — MongoDB.Driver SDK
SQL DB:       Azure Database for PostgreSQL (Flexible Server) — Npgsql SDK
File Storage: Azure Blob Storage
Queue:        Azure Service Bus — Azure Functions trigger bindings
Cache:        Azure Cache for Redis — StackExchange.Redis SDK
AI:           OpenAI API DIRECTLY — AddOpenAIChatCompletion() NOT AddAzureOpenAIChatCompletion()
SMS:          Azure Communication Services
Push:         Firebase Cloud Messaging
Hosting:      Railway.app (dev), Azure Container Apps (prod)
Affiliates:   NONE — plain deeplinks only, no partner IDs required
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
  "CosmosDb": { "ConnectionString": "PASTE_HERE", "DatabaseName": "planora" },
  "PostgreSQL": { "ConnectionString": "PASTE_HERE" },
  "AzureStorage": { "ConnectionString": "PASTE_HERE" },
  "AzureRedis": { "ConnectionString": "PASTE_HERE" },
  "AzureServiceBus": { "ConnectionString": "PASTE_HERE", "QueueName": "ai-generation-jobs" },
  "AzureB2C": {
    "Instance": "https://TENANTNAME.b2clogin.com",
    "ClientId": "PASTE_HERE",
    "Domain": "TENANTNAME.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_signupsignin"
  },
  "AzureCommunicationServices": { "ConnectionString": "PASTE_HERE", "FromPhone": "PASTE_HERE" },
  "Google": { "PlacesApiKey": "PASTE_HERE" },
  "Firebase": { "ServerKey": "PASTE_HERE" },
  "DeeplinkBuilder": { "DefaultRadiusKm": 2, "MaxSuggestedOptions": 3, "MaxBrowseLinks": 5 }
}
```
> No Affiliates section. No partner IDs. This file is gitignored.

---

## MODEL SELECTION

| Task | Model |
|------|-------|
| Architecture, agent design, complex debugging | **Claude Opus 4.6** |
| Bulk file generation (5+ files, scaffold) | **Claude Sonnet 4.6** |
| Quick edits, single file, UI tweaks | **Claude Sonnet 4.6** |
| Cloud config, API research | **Gemini 2.5 Pro** |
| Full codebase review | **Gemini 2.5 Pro** |

Display `[RECOMMENDED MODEL: X]` at start of every task.

---

## SESSION START

```
=== PLANORA SESSION START ===
Reading PROJECT_BIBLE.md...
Current Build Step: [USER FILLS]
Recommended Model:  [you decide]
Last completed:     [USER FILLS]
Next action:        [you decide]
=============================
```

---

## CLOUD SETUP BLOCK FORMAT

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Resource: [name]                         ║
║ Step 1: [exact URL + action]             ║
║ Step 2: [exact navigation]               ║
║ Step 3: Copy [X] → appsettings           ║
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
npx expo install expo-font expo-linear-gradient
npx expo install @expo-google-fonts/oswald @expo-google-fonts/playfair-display @expo-google-fonts/bebas-neue
```

**Folder structure:**
```
PlanoraApp/
  app/
    (tabs)/
      _layout.tsx       ← 5 tabs: Home, Trips, Vault, Safety, Profile
                          ⚠ NO Bookings tab
      index.tsx         ← Home
      trips.tsx         ← My Trips list
      vault.tsx         ← Trip Vault
      safety.tsx        ← SafeTravel Shield
      profile.tsx
    (onboarding)/
      _layout.tsx
      slide1.tsx through slide6.tsx
      signin.tsx
      signup.tsx
    _layout.tsx         ← root layout, font loading, auth check
  assets/
    images/             ← copy all from Planora/images/
    logos/              ← platform logos: bookingcom.png, agoda.png, mmt.png, ola.png, uber.png,
                          irctc.png, confirmtkt.png, ixigo.png, cleartrip.png, yatra.png,
                          oyo.png, goibibo.png, easemytrip.png
  components/ui/
    Button.tsx          ← Primary (black) and Secondary (white/navy border) variants
    Card.tsx            ← white bg, border #E2E8EC, radius 12
    Badge.tsx           ← "Free Entry", "Buy at gate", "Booked", "Pending"
    Skeleton.tsx
    Toast.tsx
    SafetyFab.tsx
    ChoiceableCard.tsx  ← Choiceable POI inline card (options + Browse button)
    NonChoiceableCard.tsx ← Non-Choiceable POI info card
    BrowseSheet.tsx     ← Bottom sheet for full platform list
  store/
    tripStore.ts
    wizardStore.ts
    authStore.ts
  constants/
    theme.ts            ← DEFINITIVE design system (see PROJECT_BIBLE Section 3)
  services/
    api.ts              ← axios base instance → EXPO_PUBLIC_API_BASE_URL
  .env
  .gitignore
```

**Font loading in root `_layout.tsx`:**
```typescript
import { useFonts } from 'expo-font';
import { Oswald_200ExtraLight, Oswald_400Regular, Oswald_600SemiBold, Oswald_700Bold } from '@expo-google-fonts/oswald';
import { PlayfairDisplay_400Regular, PlayfairDisplay_700Bold, PlayfairDisplay_400Regular_Italic } from '@expo-google-fonts/playfair-display';
import { BebasNeue_400Regular } from '@expo-google-fonts/bebas-neue';

// In component:
const [fontsLoaded] = useFonts({
  Oswald_200ExtraLight, Oswald_400Regular, Oswald_600SemiBold, Oswald_700Bold,
  PlayfairDisplay_400Regular, PlayfairDisplay_700Bold, PlayfairDisplay_400Regular_Italic,
  BebasNeue_400Regular,
});
if (!fontsLoaded) return <SplashScreen />;
```

**Design system rules — enforce on every component:**
- Background: `theme.colors.background` = `#FFFFFF`
- Cards: white bg + `borderColor: '#E2E8EC'` border
- Primary button: `backgroundColor: '#000000'`, text `#FFFFFF`, Oswald_600SemiBold
- Section titles: BebasNeue_400Regular, `theme.fontSize.h1`, `theme.colors.navyDeep`
- Body text: Oswald_400Regular, `theme.fontSize.body`, `theme.colors.textBody`
- Muted text: Oswald_200ExtraLight, `theme.colors.textMuted`
- Destination names on cards: PlayfairDisplay_400Regular_Italic
- NO dark backgrounds, NO purple, NO coral

**Onboarding slides:**
- Full screen `ImageBackground` from `assets/images/`
- Gradient: `LinearGradient` — `rgba(14,32,56,0.25)` top → `rgba(14,32,56,0.85)` bottom
- Headline: BebasNeue_400Regular, `fontSize: 48`, white, ALL CAPS
- Subtext: PlayfairDisplay_400Regular_Italic, `fontSize: 16`, white
- Logo: top-center, white tint
- Dots: white active, `rgba(255,255,255,0.4)` inactive
- Skip: top-right, Oswald 400, white

**Slide headlines:**
```
slide1  "YOUR NEXT ADVENTURE STARTS HERE"    sub: "Planora plans it all for you"
slide2  "PLAN SMARTER, TRAVEL BETTER"         sub: "AI-powered day-by-day itineraries"
slide3  "EVERY STAY, PERFECTLY PLACED"        sub: "Hotels matched to where you spend the day"
slide4  "DISCOVER HIDDEN GEMS"                sub: "Beyond the tourist trail"
slide5  "BUILT FOR INDIAN EXPLORERS"          sub: "Local intel, global standards"
slide6  "FROM BEACHES TO MOUNTAINS"           sub: "One app for every trip"
```

Test: `npx expo start` → all slides swipeable → fonts loading correctly → logo visible → colour palette correct.

---

### STEP 2 — Microsoft Entra External ID Auth
`[RECOMMENDED MODEL: Claude Opus 4.6]`

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Resource: Microsoft Entra External ID    ║
║ ⚠ Azure AD B2C DISCONTINUED May 2025    ║
║ Step 1: entra.microsoft.com (NOT portal) ║
║ Step 2: Entra ID → Manage tenants →     ║
║         Create → External               ║
║ Step 3: Tenant: Planora                  ║
║         Domain: planoratravel           ║
║ Step 4: App registrations → New          ║
║         Redirect URI: exp://localhost   ║
║ Step 5: Copy Client ID → .env           ║
║ Step 6: User flows → signupsignin        ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

Files:
- `PlanoraApp/services/auth/authService.ts`
- `PlanoraApp/app/(onboarding)/signin.tsx` — white bg, navy form card
- `PlanoraApp/app/(onboarding)/signup.tsx` — white bg, navy form card
- `PlanoraApp/store/authStore.ts`

Test: login returns JWT, stored securely, redirected to tabs.

---

### STEP 3 — Pre-Wizard Screen + Wizard UI
`[RECOMMENDED MODEL: Claude Sonnet 4.6]`

**Pre-wizard screen — 3-card layout:**
```
┌────────────────────────────────────────────────────┐
│           How do you want to start?                │  ← Oswald 700, #0E2038
├──────────────────┬──────────────────┬──────────────┤
│  AI Generate     │  Build Manually  │  Browse      │  ← white cards, #E2E8EC border
│  My Trip         │                  │  Packages    │  ← Oswald 600, #0E2038
│  ↓ wizard        │  ↓ manual-builder│  ↓ packages  │
└──────────────────┴──────────────────┴──────────────┘
```

Files:
- `PlanoraApp/app/wizard/_layout.tsx` — progress bar (Step X of 6), `#0E2038` fill
- `PlanoraApp/app/wizard/step1-destination.tsx`
- `PlanoraApp/app/wizard/step2-dates.tsx`
- `PlanoraApp/app/wizard/step3-origin.tsx`
- `PlanoraApp/app/wizard/step4-group.tsx`
- `PlanoraApp/app/wizard/step5-themes.tsx` — chips: active `#213A5C`/white, inactive `#F5F6F8`/`#90A4B0`
- `PlanoraApp/app/wizard/step6-details.tsx`
- `PlanoraApp/store/wizardStore.ts`

Test: all 6 steps complete → wizardStore populated → 3-card screen navigates correctly.

---

### STEP 4 — .NET API + Cosmos DB + PostgreSQL
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
║ Cosmos DB + PostgreSQL                   ║
║ → FULL_PROCESS.md Part A Steps 4–5      ║
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
      Models/TripDocument.cs           ← includes trip_type: ai_generated|manual|package
      Models/ItineraryDayDocument.cs
      Models/PlaceOfInterestDocument.cs ← includes poi_class, suggested_options, browse_links
    UserService/
      UserController.cs, UserService.cs
  Infrastructure/
    CosmosDbClient.cs
    AppDbContext.cs
  Middleware/
    GlobalExceptionMiddleware.cs
  Program.cs
  appsettings.json
  appsettings.Development.json
```

Implement all Trip Service endpoints from PROJECT_BIBLE Section 7.

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
║ OpenAI + Azure Redis + Azure Service Bus ║
║ → FULL_PROCESS.md Part A Steps 6–7      ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

```csharp
// Semantic Kernel — OpenAI DIRECTLY
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
    SpotEnrichmentAgent.cs      ← NEW: single-spot enrichment for Manual + Edit
  Services/
    DeeplinkBuilderService.cs   ← NEW: builds all deeplinks per BOOKING_ARCHITECTURE.md
  Functions/
    AgentWorkerFunction.cs      ← Azure Service Bus trigger
  Models/
    TripGenerationRequest.cs
    AgentJobStatus.cs
    ItineraryOutput.cs
    SpotEnrichmentRequest.cs
    SpotEnrichmentResult.cs
    DeeplinkContext.cs
    DeeplinkOption.cs
  SemanticKernelSetup.cs
```

**SpotEnrichmentAgent — target ~800 tokens:**
```
Input:  Google Place ID + trip context (destination, dates, pax, budget_tier, day POIs for centroid calc)
Output: poi_class, ai_summary, estimated_cost, visit_duration_minutes,
        ticket_url (if non_choiceable + has ticketing), is_free, opening_hours,
        suggested_options + browse_links (if choiceable, from DeeplinkBuilderService)

Step 1: Google Places Details API → name, photo, coords, website, opening_hours
Step 2: GPT-4o (~800 tokens) → ai_summary, poi_class, entry_fee, ticket_url
Step 3: If choiceable → DeeplinkBuilderService.BuildSuggestedOptions() + BuildBrowseLinks()
Step 4: Return full enriched POI document
```

**DeeplinkBuilderService** — implement all methods per BOOKING_ARCHITECTURE.md Section 7.

Test: POST /api/v1/ai/generate → Service Bus job queued.
Test: POST /api/v1/ai/enrich-spot → returns enriched POI with poi_class + deeplinks.

---

### STEP 6 — Full Agent Chain
`[RECOMMENDED MODEL: Opus 4.6 per agent, Sonnet 4.6 for files]`

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Google Cloud APIs (GCP Free Plan active) ║
║ Step 1: console.cloud.google.com         ║
║ Step 2: Enable: Places API (New),        ║
║         Distance Matrix, Maps SDKs      ║
║ Step 3: Create API Key → save to        ║
║         appsettings + .env              ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

Build one agent at a time, test before next:

1. **ResearchAgent** — Places API, scored POI list, poi_class assigned per POI
2. **RouteOptimiserAgent** — Distance Matrix, cluster into day groups
3. **TourPackageInjector** — merge locked package blocks from PostgreSQL
4. **BudgetAgent** — cost estimates from pricing JSON per tier
5. **DayAssemblyAgent** — time slots 9am–10pm, meal + travel gaps
6. **PersonalisationAgent** — GPT-4o rerank + enrichment per POI + call DeeplinkBuilderService for all Choiceable POIs

Write one xUnit test per agent.

---

### STEP 7 — Trip Card UI — Choiceable/Non-Choiceable Rendering
`[RECOMMENDED MODEL: Claude Sonnet 4.6]`

**This step builds the core trip viewing experience. No new cloud resources.**

**Key components to build:**

`ChoiceableCard.tsx`:
```
┌─────────────────────────────────────────┐  ← white, border #E2E8EC, radius 12
│  🏨 Hotel Stay — Night 1               │  ← Oswald 600, #0E2038
│  Near: City Palace area                │  ← Playfair Display italic, #90A4B0
├─────────────────────────────────────────┤
│  [Booking.com ↗]  [Agoda ↗]  [MMT ↗]  │  ← secondary buttons (white/navy border)
├─────────────────────────────────────────┤
│         + Browse more options           │  ← ghost style, #213A5C text
└─────────────────────────────────────────┘
```

`NonChoiceableCard.tsx`:
```
┌─────────────────────────────────────────┐  ← white, border #E2E8EC, radius 12
│  [photo]  Hawa Mahal                   │  ← Oswald 600 18px, #0E2038
│           "Jaipur's iconic facade..."  │  ← Playfair Display italic 14px, #213A5C
│  ⏱ 60 min  ·  🕘 Morning  ·  💰 ₹50  │  ← Oswald 200, #90A4B0
│  🕐 09:00–17:00                        │
│                                         │
│  [🎫 Buy Ticket ↗]  [📍 Directions ↗] │  ← secondary buttons
└─────────────────────────────────────────┘
```

`BrowseSheet.tsx` (bottom sheet for "+ Browse more options"):
```
Title: "More options near Day 1 spots"  ← Oswald 700, #0E2038
[Booking.com logo]  Booking.com          [Open ↗]
[OYO logo]          OYO                  [Open ↗]
[Goibibo logo]      Goibibo              [Open ↗]
[Cleartrip logo]    Cleartrip            [Open ↗]
```

Files:
```
PlanoraApp/components/trip/
  ChoiceableCard.tsx
  NonChoiceableCard.tsx
  BrowseSheet.tsx         ← reusable bottom sheet for all categories
  DayCard.tsx             ← contains list of POI cards for one day
  TripMapView.tsx         ← colour-coded pins + polylines, light map theme

PlanoraApp/app/trip/
  [trip_id]/
    _layout.tsx           ← tabs: Days | Map | Vault
    index.tsx             ← Days view (day tabs + POI cards)
    map.tsx               ← full map view
    vault.tsx             ← trip's vault items (see Step 9)
```

**No separate Booking tab or route.** Choiceable cards are embedded directly in the Days view.

Test: AI-generated trip renders with correct card types — Choiceable shows options, Non-Choiceable shows info. Browse sheet opens. Deeplinks open in browser.

---

### STEP 8 — Manual Builder + Edit Mode
`[RECOMMENDED MODEL: Claude Sonnet 4.6]`

No new cloud resources.

#### 8A — Manual Trip Builder

Files:
```
PlanoraApp/app/(manual-builder)/
  setup.tsx             ← destination, dates, trip name, group, budget
  builder.tsx           ← day canvases + "+ Add Spot" + "+ Add Hotel" shortcut
  spot-search.tsx       ← Places autocomplete bottom sheet
  spot-loading.tsx      ← skeleton card while enrichment runs

PlanoraApp/store/manualTripStore.ts
```

**Flow:**
1. User taps **+ Add Spot** on a day → `spot-search.tsx` opens
2. User selects place → `POST /api/v1/ai/enrich-spot`
3. Skeleton card shown while enrichment runs
4. Card populates (Non-Choiceable info card OR Choiceable options card)
5. Map pin added in that day's colour

**+ Add Hotel shortcut chip:**
- Always visible at bottom of each day
- Skips search — creates Choiceable hotel POI directly
- DeeplinkBuilderService uses day's existing POI centroid for hotel search lat/lng

#### 8B — Edit Mode

Files:
```
PlanoraApp/components/trip/
  EditModeBar.tsx         ← top bar shown in edit mode: "Editing" label + Done button
  EditablePOICard.tsx     ← wraps ChoiceableCard/NonChoiceableCard with ✕ delete icon
  AddSpotSheet.tsx        ← reusable: Places search + enrichment trigger
```

**Delete:**
- Tap ✕ → confirm: "Remove Hawa Mahal from Day 1?" → DELETE endpoint → re-render

**Add in edit mode:**
- Same flow as Manual Builder spot add
- Placement algorithm (TypeScript, client-side):
```typescript
function findBestDayForSpot(
  newLat: number, newLng: number, days: Day[]
): number {
  const haversine = (lat1: number, lng1: number, lat2: number, lng2: number) => {
    const R = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLng = (lng2 - lng1) * Math.PI / 180;
    const a = Math.sin(dLat/2)**2 +
              Math.cos(lat1 * Math.PI/180) * Math.cos(lat2 * Math.PI/180) *
              Math.sin(dLng/2)**2;
    return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  };

  const eligibleDays = days.filter(d =>
    d.pois.filter(p => p.poiClass === 'non_choiceable').length < 6
  );
  if (eligibleDays.length === 0) return days[0].dayNumber;

  return eligibleDays.reduce((best, day) => {
    const nonChoiceablePois = day.pois.filter(p => p.poiClass === 'non_choiceable');
    if (nonChoiceablePois.length === 0) return best;
    const centerLat = nonChoiceablePois.reduce((s, p) => s + p.lat, 0) / nonChoiceablePois.length;
    const centerLng = nonChoiceablePois.reduce((s, p) => s + p.lng, 0) / nonChoiceablePois.length;
    const dist = haversine(centerLat, centerLng, newLat, newLng);
    const bestNc = best ? days.find(d => d.dayNumber === best)!.pois.filter(p => p.poiClass === 'non_choiceable') : [];
    const bestCLat = bestNc.reduce((s, p) => s + p.lat, 0) / (bestNc.length || 1);
    const bestCLng = bestNc.reduce((s, p) => s + p.lng, 0) / (bestNc.length || 1);
    const bestDist = best ? haversine(bestCLat, bestCLng, newLat, newLng) : Infinity;
    return dist < bestDist ? day.dayNumber : best;
  }, 0 as number);
}
```

Test: Edit mode on generated trip → delete a POI → map updates. Add a spot → enrichment runs → placed in correct day by centroid proximity.

---

### STEP 9 — Trip Vault
`[RECOMMENDED MODEL: Claude Sonnet 4.6]`

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Resource 1: Azure Blob Storage           ║
║ Resource 2: Azure AI Document Intel.     ║
║ → FULL_PROCESS.md Part A Steps 8–9      ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

**Vault is trip-and-POI-aware. Organised by trips, then by spots within each trip.**

Files:
```
PlanoraApp.API/Services/VaultService/
  VaultController.cs            ← POST /upload, POST /save-ref, GET /{trip_id}, PATCH /{item_id}/status, DELETE /{item_id}
  BlobStorageService.cs
  OcrParserService.cs           ← Azure AI Document Intelligence
  Models/VaultItem.cs           ← includes poi_id, booking_status fields

PlanoraApp/app/vault/
  index.tsx                     ← Vault home: list of user's trips with booked/pending counts
  trip-vault.tsx                ← inside one trip: day-by-day spots with booking status
  add-item.tsx                  ← bottom sheet: Upload PDF | Scan QR | Enter ref manually
  item-detail.tsx               ← full view of one vault item (parsed_data + blob)
```

**Vault home screen design:**
```
MY VAULT                          ← BebasNeue h1, #0E2038

Jaipur Trip — Dec 1–7
  ✅ 4 booked  ·  ⏳ 3 pending    ← Oswald 400, status colours
                           →

Goa Weekend — Jan 10–12
  ⏳ All pending                  →

+ Upload a document               ← ghost button
```

**Trip vault screen design:**
```
JAIPUR TRIP — DEC 1–7            ← BebasNeue h1, #0E2038

Day 1
  Hawa Mahal           ⏳         [Add Ticket]     ← Oswald 600 + status badge
  Jantar Mantar        ✅         [View]
  Hotel Night 1        ✅         [View]

Day 2
  Amber Fort           ⏳         [Add Ticket]
```

**Add Ticket bottom sheet options:**
- 📎 Upload PDF / Photo → Blob storage + OCR parse
- 📷 Scan QR Code → `expo-camera`
- ✏️ Enter booking reference → manual save

**booking_status** on vault_items:
- `"booked"` — user has added a ticket, ref, or document for this POI
- `"not_booked"` — default, no document yet

MMKV key: `vault:{trip_id}` → all vault items for that trip, readable offline.

Test: upload PDF → OCR parses → appears under correct trip+POI. Booking status updates. Works offline via MMKV.

---

### STEP 10 — SafeTravel Shield
`[RECOMMENDED MODEL: Claude Opus 4.6]`

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Resource 1: Azure Communication Services ║
║ Resource 2: Firebase (push only)         ║
║ → FULL_PROCESS.md Part A Step 10        ║
║   + Part B Firebase section             ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

Files:
- `PlanoraApp.API/Services/SafetyService/SafetyController.cs`
- `PlanoraApp.API/Services/SafetyService/EmergencyContactsService.cs`
- `PlanoraApp.API/Services/SafetyService/SosService.cs`
- `PlanoraApp.API/Services/SafetyService/ProximityService.cs`
- `PlanoraApp/app/safety/shield.tsx` — white bg, `#0E2038` header, emergency contact cards
- `PlanoraApp/app/safety/sos.tsx` — `#0E2038` hero, white hold button
- `PlanoraApp/components/ui/SafetyFab.tsx` — `#0E2038` FAB, white icon, persistent on all screens

**SOS rules:**
- 3000ms LongPressGestureHandler
- Disclaimer modal → biometric (expo-local-authentication) → POST /safety/sos
- Log every attempt
- NEVER fire on single tap

Test: 3s hold → disclaimer → biometric → inner circle notified via SMS + push.

---

### STEP 11 — Finance + Package Browse + Polish
`[RECOMMENDED MODEL: Sonnet 4.6 for files, Gemini 2.5 Pro for review]`

#### Finance:
- `PlanoraApp/app/finance/expenses.tsx`
- `PlanoraApp/app/finance/split.tsx`
- `PlanoraApp/app/finance/report.tsx`
- `PlanoraApp.API/Services/FinanceService/ExpenseController.cs`

#### Package Browse:
```
PlanoraApp/app/(packages)/
  browse.tsx      ← search: destination + duration chips + theme chips
  results.tsx     ← package cards (photo, title, price, agency, days)
  detail.tsx      ← full day-by-day preview + "Load to My Trips" button
```

`POST /api/v1/tours/load` → creates a new trip from package → same Trip Card format.

#### Polish checklist:
- [ ] Fonts loading correctly on both platforms (iOS + Android)
- [ ] White backgrounds enforced everywhere (no dark surfaces)
- [ ] Loading skeletons (white card with animated `#E2E8EC` shimmer)
- [ ] Error boundaries
- [ ] Offline banners (@react-native-community/netinfo) — `#0E2038` banner, white text
- [ ] Empty states — Playfair Display italic copy, `#90A4B0`
- [ ] App icon (logo.jpg, 1024x1024, white background)
- [ ] Splash screen (white, Planora logo centre, `#0E2038` loading indicator)
- [ ] TestFlight + Play Store internal track

Final review: Gemini 2.5 Pro — "review all files for consistency with design system, missing error handling, security issues, and any remaining purple/dark colours."

---

## ERROR HANDLING

Frontend:
```typescript
try {
  const result = await api.createTrip(data);
} catch (error: any) {
  if (error?.response?.status === 401) router.replace('/(onboarding)/signin');
  else if (error?.response?.status === 503) showToast('AI is busy, try again in a moment');
  else showToast('Something went wrong');
}
```

Backend:
```csharp
// GlobalExceptionMiddleware in Program.cs
// Per-agent: catch → retry once (2s delay) → mark FAILED with error_log
// Never propagate agent exception
```

---

## WHEN STUCK

| Situation | Action |
|-----------|--------|
| Build error | Sonnet 4.6 — paste exact error, "fix this only" |
| Architecture confusion | Opus 4.6 — re-read PROJECT_BIBLE section |
| Cloud/Azure issue | Gemini 2.5 Pro |
| Wrong colours/fonts showing | Sonnet 4.6 — verify theme.ts is imported, not hardcoded values |
| Full codebase check | Gemini 2.5 Pro |

---

## SESSION TRACKER

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

App Status:
  [ ] PlanoraApp running (npx expo start)
  [ ] PlanoraApp.API running (dotnet run)
  [ ] Fonts loading: Oswald + Playfair Display + Bebas Neue
  [ ] Background is white everywhere (no dark surfaces)
  [ ] Pre-wizard 3-card screen visible
  [ ] NO Bookings tab in bottom nav (Vault is Tab 3)
  [ ] Choiceable cards show inline deeplinks + Browse button
  [ ] Non-Choiceable cards show info + ticket link + directions
  [ ] No affiliate IDs in any deeplink URLs
  [ ] Vault organised by trips + spots with booking_status
  [ ] .env created + gitignored
  [ ] appsettings.Development.json created + gitignored
```
