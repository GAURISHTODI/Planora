# AGENT INSTRUCTIONS — Planora Builder
> App name: Planora
> Read PROJECT_BIBLE.md FULLY before acting on any instruction here.
> You build. You do not summarise unless asked.
> **v1.0:** Full Azure consolidation. MongoDB Atlas → Cosmos DB (MongoDB API). Supabase → Azure PostgreSQL. Upstash → Azure Cache for Redis + Azure Service Bus. Twilio → Azure Communication Services. Firebase retained for push.
> **v1.0:** Azure AD B2C discontinued for new customers (May 2025) → **Microsoft Entra External ID**. Tenant created at `entra.microsoft.com`. All SDK/env keys unchanged.

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
Access in code as: `process.env.EXPO_PUBLIC_*`

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
  "Firebase": { "ServerKey": "PASTE_HERE" }
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
      _layout.tsx       ← 5 tabs: Home, Trips, Bookings, Vault, Profile
      index.tsx
      trips.tsx
      bookings.tsx
      vault.tsx
      profile.tsx
    (onboarding)/
      _layout.tsx
      slide1.tsx        ← beachbest.jpg background
      slide2.tsx        ← purple indigo.jpg background
      slide3.tsx        ← stay.jpg background
      slide4.tsx        ← temple.jpg background
      slide5.tsx        ← galleryindore.jpg background
      slide6.tsx        ← tibetmountains.jpg background
      signin.tsx        ← signin.jpg background + login form
      signup.tsx        ← signup.jpg background + signup form
    _layout.tsx         ← root layout, checks auth → routes to onboarding or tabs
  assets/
    images/             ← copy all images from root /images/ folder here
  components/ui/        ← Button, Card, Badge, Skeleton, Toast, SafetyFab
  store/
    tripStore.ts
    wizardStore.ts
    authStore.ts
  constants/
    theme.ts            ← Planora brand colors, fonts, spacing
  services/
    api.ts              ← axios base instance → EXPO_PUBLIC_API_BASE_URL
  .env
  .gitignore
```

**Onboarding implementation rules:**
- Each slide: full-screen `ImageBackground` from `assets/images/`
- Gradient overlay: `LinearGradient` from `expo-linear-gradient`
  - Top: `rgba(0,0,0,0.1)` → Bottom: `rgba(0,0,0,0.7)`
- Planora logo (`logo.jpg`) shown top-center on slides 1, signin, signup
- Text: white, bold, positioned bottom 20% of screen
- Dot indicators between text and bottom edge
- "Skip" button: top-right, white, slides 1–6 only
- Swipe gesture between slides using `react-native-gesture-handler`
- Slides 7 (signin) and 8 (signup): image background + semi-transparent form card on top
- After successful auth → navigate to `/(tabs)/`

**Slide text overlay:**
```
slide1  "Your next adventure starts here"      subtext: "Planora plans it all for you"
slide2  "Plan smarter, travel better"           subtext: "AI-powered day-by-day itineraries"
slide3  "Every stay, perfectly placed"          subtext: "Hotels matched to your budget"
slide4  "Discover hidden gems"                  subtext: "Beyond the tourist trail"
slide5  "Built for Indian explorers"            subtext: "Local intel, global standards"
slide6  "From beaches to mountains"             subtext: "One app for every trip"
```

**Planora brand colors (use in theme.ts):**
```typescript
export const theme = {
  primary: '#6C63FF',      // indigo-purple
  secondary: '#FF6584',    // coral accent
  background: '#0F0E17',   // near black
  surface: '#1A1A2E',      // dark card
  text: '#FFFFFE',
  textMuted: '#A7A9BE',
  success: '#2CB67D',
  warning: '#FF8906',
}
```

Test: `npx expo start` → onboarding slides swipeable → all 8 screens load → logo visible.

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
║                                          ║
║ Step 1: Go to entra.microsoft.com        ║
║         (NOT portal.azure.com)          ║
║ Step 2: Entra ID → Overview →           ║
║         Manage tenants → Create         ║
║ Step 3: Select "External" → Continue    ║
║ Step 4: Tenant name: Planora             ║
║         Domain: planoratravel            ║
║         → planoratravel.onmicrosoft.com ║
║         Country: India → Review+Create  ║
║         (takes up to 30 min)            ║
║ Step 5: Switch to new tenant:            ║
║         Settings icon → Directories +  ║
║         subscriptions → Switch          ║
║ Step 6: App registrations → New          ║
║         Name: PlanoraApp                ║
║         Redirect URI (Mobile/Desktop):  ║
║         exp://localhost → Register      ║
║ Step 7: Copy Application (client) ID    ║
║         → PlanoraApp/.env              ║
║         EXPO_PUBLIC_B2C_CLIENT_ID       ║
║ Step 8: User flows → New user flow →    ║
║         Sign up and sign in             ║
║         Name: signupsignin              ║
║         → B2C_1_signupsignin           ║
║         Identity providers: Email+Google║
║ Step 9: Save tenant domain              ║
║         planoratravel →                ║
║         EXPO_PUBLIC_B2C_TENANT_NAME    ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

Files:
- `PlanoraApp/services/auth/authService.ts` — PKCE flow using expo-auth-session
- `PlanoraApp/app/(onboarding)/signin.tsx` — complete sign in screen
- `PlanoraApp/app/(onboarding)/signup.tsx` — complete sign up screen
- `PlanoraApp/store/authStore.ts` — token storage in expo-secure-store

Test: full login returns JWT, stored securely, user redirected to tabs.

---

### STEP 3 — Trip Wizard UI
`[RECOMMENDED MODEL: Claude Sonnet 4.6]`

Files:
- `PlanoraApp/app/wizard/_layout.tsx` — progress bar (Step X of 6)
- `PlanoraApp/app/wizard/step1-destination.tsx` — city autocomplete search
- `PlanoraApp/app/wizard/step2-dates.tsx` — date range picker + color-coded grid
- `PlanoraApp/app/wizard/step3-origin.tsx` — optional, toggle-gated
- `PlanoraApp/app/wizard/step4-group.tsx` — group type chips + pax counter
- `PlanoraApp/app/wizard/step5-themes.tsx` — multi-select theme chips
- `PlanoraApp/app/wizard/step6-details.tsx` — free text + preferences
- `PlanoraApp/store/wizardStore.ts` — all state in Zustand, persisted to MMKV

Pre-wizard screen: two cards — "Plan my own trip" and "Browse agency packages"

Date picker: green = cheapest, amber = mid, red = expensive (placeholder data until Step 7).

Test: all 6 steps complete → wizardStore has all values.

---

### STEP 4 — .NET API + Azure Cosmos DB + Azure PostgreSQL
`[RECOMMENDED MODEL: Claude Sonnet 4.6, Opus 4.6 for architecture questions]`

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
║ Step 1: portal.azure.com → Create        ║
║         Resource → Azure Cosmos DB       ║
║ Step 2: API: Azure Cosmos DB for         ║
║         MongoDB (IMPORTANT — NOT Core)  ║
║ Step 3: Resource group: planora-rg       ║
║         Account name: planora-cosmos     ║
║         Region: Central India            ║
║ Step 4: Go to resource →                 ║
║         Connection strings →            ║
║         Copy Primary Connection String  ║
║ Step 5: Save → appsettings              ║
║         CosmosDb:ConnectionString       ║
║                                          ║
║ Resource 2: Azure PostgreSQL             ║
║ Step 6: portal.azure.com → Create        ║
║         Resource → Azure Database for   ║
║         PostgreSQL Flexible Server      ║
║ Step 7: Resource group: planora-rg       ║
║         Server name: planora-postgres    ║
║         Region: Central India            ║
║         Compute: Burstable B1ms (free)  ║
║         Admin user + password → save    ║
║ Step 8: Networking → Allow Azure         ║
║         services + your IP              ║
║ Step 9: Go to resource → Connection      ║
║         strings → copy ADO.NET string  ║
║         Replace {password} with yours  ║
║ Step 10: Save → appsettings             ║
║          PostgreSQL:ConnectionString    ║
║ Step 11: Connect via psql or pgAdmin    ║
║          Run CREATE TABLE SQL from      ║
║          PROJECT_BIBLE Section 4       ║
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
      Models/TripDocument.cs
      Models/ItineraryDayDocument.cs
      Models/PlaceOfInterestDocument.cs
    UserService/
      UserController.cs
      UserService.cs
  Infrastructure/
    CosmosDbClient.cs           ← MongoDB.Driver connecting to Cosmos DB MongoDB API
    AppDbContext.cs             ← Npgsql EF Core connecting to Azure PostgreSQL
  Middleware/
    GlobalExceptionMiddleware.cs
  Program.cs
  appsettings.json
  appsettings.Development.json   ← gitignored
```

**Cosmos DB connection (MongoDB.Driver — identical to Atlas):**
```csharp
// CosmosDbClient.cs
var client = new MongoClient(config["CosmosDb:ConnectionString"]);
var db = client.GetDatabase(config["CosmosDb:DatabaseName"]);
// Exact same API as MongoDB Atlas — no other code changes needed
```

Implement all Trip Service endpoints from PROJECT_BIBLE Section 5.
Use `MongoDB.Driver` — Cosmos DB MongoDB API is wire-compatible.

Test: `dotnet run` → GET /api/v1/trips returns 200.

---

### STEP 5 — Semantic Kernel + Orchestrator
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
║ Resource 1: OpenAI API Key               ║
║ → already have sk-... key               ║
║ → add to appsettings OpenAI:ApiKey      ║
║                                          ║
║ Resource 2: Azure Cache for Redis        ║
║ Step 1: portal.azure.com → Create        ║
║         Resource → Azure Cache for Redis ║
║ Step 2: Resource group: planora-rg       ║
║         DNS name: planora-cache          ║
║         Region: Central India            ║
║         SKU: Basic C0 (free/cheapest)   ║
║ Step 3: Go to resource → Access keys →  ║
║         Copy Primary connection string  ║
║ Step 4: Save → appsettings              ║
║         AzureRedis:ConnectionString     ║
║                                          ║
║ Resource 3: Azure Service Bus            ║
║ Step 5: portal.azure.com → Create        ║
║         Resource → Service Bus          ║
║ Step 6: Resource group: planora-rg       ║
║         Namespace: planora-bus           ║
║         Region: Central India            ║
║         Pricing: Basic                  ║
║ Step 7: Go to namespace → Queues →      ║
║         + Queue → name: ai-generation-  ║
║         jobs                            ║
║ Step 8: Shared access policies →        ║
║         RootManageSharedAccessKey →    ║
║         Copy Primary Connection String ║
║ Step 9: Save → appsettings             ║
║         AzureServiceBus:ConnectionString║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

Semantic Kernel config — OpenAI DIRECTLY:
```csharp
var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(
    modelId: config["OpenAI:Model"],   // "gpt-4o"
    apiKey: config["OpenAI:ApiKey"]
);
// NOT AddAzureOpenAIChatCompletion
```

**Azure Service Bus (replaces Upstash QStash):**
```csharp
// Publishing a job
var client = new ServiceBusClient(config["AzureServiceBus:ConnectionString"]);
var sender = client.CreateSender("ai-generation-jobs");
await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(job)));

// Azure Functions trigger (in AgentWorkerFunction.cs)
[Function("AgentWorker")]
public async Task Run([ServiceBusTrigger("ai-generation-jobs",
    Connection = "AzureServiceBus:ConnectionString")] string messageBody) { ... }
```

**Azure Cache for Redis (replaces Upstash Redis):**
```csharp
var redis = await ConnectionMultiplexer.ConnectAsync(config["AzureRedis:ConnectionString"]);
var db = redis.GetDatabase();
// Same StackExchange.Redis API — no other code changes
```

Files:
```
PlanoraApp.AI/
  Agents/
    OrchestratorAgent.cs      ← full implementation
    ResearchAgent.cs          ← stub
    RouteOptimiserAgent.cs    ← stub
    TourPackageInjector.cs    ← stub
    BudgetAgent.cs            ← stub
    DayAssemblyAgent.cs       ← stub
    PersonalisationAgent.cs   ← stub
  Functions/
    AgentWorkerFunction.cs    ← Azure Service Bus trigger
  Models/
    TripGenerationRequest.cs
    AgentJobStatus.cs
    ItineraryOutput.cs
  SemanticKernelSetup.cs
```

Orchestrator: read job → call agents in sequence → update Cosmos DB job status → push SSE → write final itinerary.
Error rule: catch per agent, retry once, then FAILED with error_log. Never crash worker.

Test: POST /api/v1/ai/generate → job appears in Azure Service Bus queue.

---

### STEP 6 — Full Agent Chain
`[RECOMMENDED MODEL: Opus 4.6 per agent, Sonnet 4.6 for writing files]`

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Resource: Google Cloud APIs              ║
║ Step 1: console.cloud.google.com         ║
║ Step 2: Enable:                          ║
║   Places API (New)                      ║
║   Distance Matrix API                   ║
║   Maps SDK Android + iOS                ║
║ Step 3: Create API Key                   ║
║ Step 4: Copy →                           ║
║   appsettings Google:PlacesApiKey       ║
║   PlanoraApp/.env EXPO_PUBLIC_GOOGLE_   ║
║   MAPS_KEY                              ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

Build one agent at a time, test before next:

1. **ResearchAgent** — Google Places API, scored POI list by theme
2. **RouteOptimiserAgent** — Distance Matrix, cluster into day groups
3. **TourPackageInjector** — merge locked package blocks from Azure PostgreSQL
4. **BudgetAgent** — cost estimates from pricing JSON per tier
5. **DayAssemblyAgent** — time slots 9am–10pm, meal + travel gaps
6. **PersonalisationAgent** — GPT-4o rerank + ai_summary per POI

Write one xUnit test per agent with mocked inputs.

---

### STEP 7 — Booking Gateway
`[RECOMMENDED MODEL: Claude Sonnet 4.6]`

Azure Cache for Redis already set up in Step 5 — use same credentials.

Files:
- `PlanoraApp.API/Services/BookingService/BookingController.cs`
- `PlanoraApp.API/Services/BookingService/AffiliateLinkBuilder.cs`
  - `BuildLink(platform, origin, destination, checkIn, checkOut, paxCount) → string`
  - Platforms: Skyscanner, Booking.com, MakeMyTrip, Ixigo, Yatra, Viator, Uber, Ola
  - If exact format unknown: clearly label placeholder, tell user to get from affiliate portal
- `PlanoraApp.API/Services/BookingService/PriceCacheService.cs` — Azure Cache for Redis, 15min TTL
- `PlanoraApp/app/bookings/hub.tsx` — category grid
- `PlanoraApp/app/bookings/compare.tsx` — platform comparison UI

Test: GET /api/v1/booking/options/{trip_id}/flight → returns platform deeplinks array.

---

### STEP 8 — Trip Vault
`[RECOMMENDED MODEL: Claude Sonnet 4.6]`

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Resource 1: Azure Blob Storage           ║
║ Step 1: portal.azure.com (already in     ║
║         planora-rg from Step 4 setup)   ║
║ Step 2: Create Resource → Storage        ║
║         Account → planoravault          ║
║ Step 3: Container: vault-documents       ║
║         Access: Private                 ║
║ Step 4: Access keys → Copy              ║
║         connection string →             ║
║         appsettings AzureStorage:       ║
║         ConnectionString                ║
║                                          ║
║ Resource 2: Azure AI Document            ║
║             Intelligence                ║
║ Step 5: Create Resource → Document       ║
║         Intelligence (Form Recognizer) ║
║ Step 6: Resource group: planora-rg       ║
║         Region: Central India            ║
║         Pricing: Free F0               ║
║ Step 7: Keys and Endpoint → Copy        ║
║         Key 1 + Endpoint →             ║
║         appsettings DocIntelligence:*  ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

Files:
- `PlanoraApp.API/Services/VaultService/VaultController.cs`
- `PlanoraApp.API/Services/VaultService/BlobStorageService.cs`
- `PlanoraApp.API/Services/VaultService/OcrParserService.cs` — Azure AI Document Intelligence
- `PlanoraApp/app/vault/index.tsx` — timeline sorted by travel_date
- `PlanoraApp/app/vault/upload.tsx` — PDF picker + camera + QR scanner

MMKV: write all vault items to `vault:{trip_id}` on fetch. Read MMKV first on load, refresh in background.

Test: upload PDF → OCR parses → timeline shows → accessible offline.

---

### STEP 9 — SafeTravel Shield
`[RECOMMENDED MODEL: Claude Opus 4.6]`

```
╔══════════════════════════════════════════╗
║         [CLOUD SETUP REQUIRED]           ║
╠══════════════════════════════════════════╣
║ Resource 1: Azure Communication Services ║
║ Step 1: portal.azure.com → Create        ║
║         Resource → Communication        ║
║         Services                        ║
║ Step 2: Resource group: planora-rg       ║
║         Resource name: planora-acs       ║
║         Region: Central India            ║
║ Step 3: Go to resource → Keys →         ║
║         Copy Primary Connection String  ║
║         → appsettings                  ║
║         AzureCommunicationServices:     ║
║         ConnectionString               ║
║ Step 4: Phone Numbers → Get a number    ║
║         (toll-free or local)           ║
║         Copy number →                  ║
║         AzureCommunicationServices:    ║
║         FromPhone                      ║
║                                          ║
║ Resource 2: Firebase (push only)         ║
║ Step 5: console.firebase.google.com      ║
║ Step 6: New project → Cloud Messaging    ║
║ Step 7: Add Android app →               ║
║         package: com.planora.app        ║
║         Download google-services.json  ║
║         → PlanoraApp/android/app/      ║
║ Step 8: Add iOS app →                   ║
║         bundle: com.planora.app         ║
║         Download GoogleService-Info.   ║
║         plist → PlanoraApp/ios/        ║
║ Step 9: Project settings →              ║
║         Cloud Messaging →              ║
║         Server key →                   ║
║         appsettings Firebase:ServerKey ║
╚══════════════════════════════════════════╝
⏸ PAUSED — Waiting for confirmation.
```

**Azure Communication Services SMS (replaces Twilio):**
```csharp
dotnet add package Azure.Communication.Sms

var client = new SmsClient(config["AzureCommunicationServices:ConnectionString"]);
SmsSendResult result = await client.SendAsync(
    from: config["AzureCommunicationServices:FromPhone"],
    to: recipientPhone,
    message: sosMessage
);
```

Files:
- `PlanoraApp.API/Services/SafetyService/SafetyController.cs`
- `PlanoraApp.API/Services/SafetyService/EmergencyContactsService.cs`
- `PlanoraApp.API/Services/SafetyService/SosService.cs` — Azure Communication Services SMS + Firebase push
- `PlanoraApp.API/Services/SafetyService/ProximityService.cs`
- `PlanoraApp/app/safety/shield.tsx`
- `PlanoraApp/app/safety/sos.tsx`
- `PlanoraApp/components/ui/SafetyFab.tsx` — rendered in root `_layout.tsx`, visible everywhere

SOS rules — strict:
- 3000ms LongPressGestureHandler minimum
- Disclaimer modal → biometric (expo-local-authentication) → POST /safety/sos
- Log every attempt regardless of outcome
- NEVER fire on single tap

Test: 3s hold → disclaimer → biometric → inner circle notified via SMS + push.

---

### STEP 10 — Finance + Tours + Polish
`[RECOMMENDED MODEL: Sonnet 4.6 for files, Gemini 2.5 Pro for codebase review]`

Finance:
- `PlanoraApp/app/finance/expenses.tsx`
- `PlanoraApp/app/finance/split.tsx`
- `PlanoraApp/app/finance/report.tsx`
- `PlanoraApp.API/Services/FinanceService/ExpenseController.cs`

Tours:
- `PlanoraApp/app/tours/search.tsx`
- `PlanoraApp/app/tours/package-detail.tsx`
- `PlanoraApp.API/Services/TourService/TourController.cs`

Polish checklist:
- [ ] Dark mode (Planora dark theme from theme.ts)
- [ ] Loading skeletons
- [ ] Error boundaries
- [ ] Offline banners (@react-native-community/netinfo)
- [ ] Empty states
- [ ] App icon (logo.jpg resized to 1024x1024)
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
  [ ] Google Cloud APIs (Places, Matrix, Maps)
  [ ] Azure Blob Storage — planoravault
  [ ] Azure AI Document Intelligence
  [ ] Azure Communication Services — planora-acs
  [ ] Firebase project (push notifications only)

App Status:
  [ ] PlanoraApp running (npx expo start)
  [ ] PlanoraApp.API running (dotnet run)
  [ ] Onboarding images loading correctly
  [ ] .env created + gitignored
  [ ] appsettings.Development.json created + gitignored
```
