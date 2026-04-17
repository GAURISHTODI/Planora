# FULL PROCESS v3.0 — Planora
> **v1.0:** Full Azure consolidation under `planora-rg`. Microsoft Entra External ID for auth.
> **v2.0:** Skyscanner + Kayak denied. Three trip creation modes. Edit Mode. Vault replaces Bookings tab.
> **v3.0:** No affiliate IDs anywhere — pure deeplinks. Choiceable vs Non-Choiceable booking model. Vault is trip-and-POI-aware with booking_status per spot. New design system: White + Black + #0E2038/#213A5C/#90A4B0, Oswald + Playfair Display + Bebas Neue. Dark theme abolished. 11-step build.

---

## PART A — Fresh Azure Account Setup

### Step 1: Create the account
1. Open a **private/incognito browser**
2. Go to **azure.microsoft.com/free** → Start free
3. Sign in with Gmail or Outlook — NOT your VIT email
4. Enter phone + debit/credit card (identity verification only)
5. You get: $200 credit (30 days) + 12 months free on core services + always-free tier

### Step 2: Create Resource Group
1. portal.azure.com → search "Resource groups" → Create
2. Name: `planora-rg`, Region: `Central India`
3. Review + Create → Create

> Put ALL resources inside `planora-rg`.

### Step 3: Microsoft Entra External ID (needed at Build Step 2)
> ⚠ Azure AD B2C discontinued for new customers May 2025. Entra External ID is the replacement.

1. **entra.microsoft.com** (NOT portal.azure.com)
2. Entra ID → Overview → Manage tenants → Create → External
3. Tenant name: `Planora`, Domain: `planoratravel` → `planoratravel.onmicrosoft.com`
4. Country: India → Review + Create (up to 30 min)
5. Switch to new tenant → App registrations → New registration
   - Name: `Planora`, Platform: Mobile and desktop, Redirect URI: `exp://localhost`
6. Copy Application (client) ID → `.env` → `EXPO_PUBLIC_B2C_CLIENT_ID`
7. User flows → New → Sign up and sign in → name: `signupsignin` → `B2C_1_signupsignin`
8. Save tenant domain `planoratravel` → `.env` → `EXPO_PUBLIC_B2C_TENANT_NAME`

### Step 4: Azure Cosmos DB — MongoDB API (needed at Build Step 4)
1. portal.azure.com → Create Resource → Azure Cosmos DB
2. **Select: Azure Cosmos DB for MongoDB** (NOT Core/SQL — critical)
3. Resource group: `planora-rg`, Account: `planora-cosmos`, Region: Central India
4. Capacity: Serverless
5. Resource → Connection strings → Copy Primary Connection String
6. Save → `appsettings.Development.json` → `CosmosDb:ConnectionString`
7. Data Explorer → New Database → `planora`
   - Collections: `trips`, `itinerary_days`, `places_of_interest`

### Step 5: Azure Database for PostgreSQL (needed at Build Step 4)
1. portal.azure.com → Create → Azure Database for PostgreSQL Flexible Server
2. Resource group: `planora-rg`, Server: `planora-postgres`, Region: Central India
3. Workload: Development (Burstable B1ms)
4. Admin username + password → **save both**
5. Networking: Allow public access + current client IP + Allow Azure services
6. Connection strings → ADO.NET format → replace `{password}`
7. Save → `appsettings.Development.json` → `PostgreSQL:ConnectionString`
8. Connect via pgAdmin/psql → run all CREATE TABLE statements from PROJECT_BIBLE Section 6

### Step 6: Azure Cache for Redis (needed at Build Step 5)
1. portal.azure.com → Create → Azure Cache for Redis
2. Resource group: `planora-rg`, DNS: `planora-cache`, Region: Central India, SKU: Basic C0
3. Resource → Access keys → Copy Primary connection string (StackExchange.Redis)
4. Save → `appsettings.Development.json` → `AzureRedis:ConnectionString`

### Step 7: Azure Service Bus (needed at Build Step 5)
1. portal.azure.com → Create → Service Bus
2. Resource group: `planora-rg`, Namespace: `planora-bus`, Region: Central India, Pricing: Basic
3. Namespace → Queues → + Queue → Name: `ai-generation-jobs`, Max size: 1GB, Max delivery: 5
4. Shared access policies → `RootManageSharedAccessKey` → Copy Primary Connection String
5. Save → `appsettings.Development.json` → `AzureServiceBus:ConnectionString`

### Step 8: Azure Blob Storage (needed at Build Step 9)
1. portal.azure.com → Create → Storage account
2. Resource group: `planora-rg`, Name: `planoravault`, Region: Central India, Performance: Standard, LRS
3. Containers → + Container → Name: `vault-documents`, Access: Private
4. Access keys → Copy Connection string (key1)
5. Save → `appsettings.Development.json` → `AzureStorage:ConnectionString`

### Step 9: Azure AI Document Intelligence (needed at Build Step 9)
1. Create Resource → Document Intelligence (formerly Form Recognizer)
2. Resource group: `planora-rg`, Region: Central India, Pricing: Free F0
3. Keys and Endpoint → Copy Key 1 + Endpoint
4. Save → `appsettings.Development.json` → `DocIntelligence:ApiKey` + `DocIntelligence:Endpoint`

### Step 10: Azure Communication Services (needed at Build Step 10)
1. portal.azure.com → Create → Communication Services
2. Resource group: `planora-rg`, Name: `planora-acs`, Region: Central India
3. Keys → Copy Primary Connection String
4. Save → `appsettings.Development.json` → `AzureCommunicationServices:ConnectionString`
5. Phone Numbers → Get a phone number → Copy → `AzureCommunicationServices:FromPhone`

### Step 11: Azure is complete
Everything in portal.azure.com under `planora-rg`. Non-Azure:
- **Google Cloud** (Places, Maps, Distance Matrix — GCP Free Plan active)
- **Firebase** (push only)
- **OpenAI API** (direct — cheaper and simpler at this scale)

---

## PART B — Remaining Accounts

### Google Cloud APIs (Build Step 6)
> GCP Free Plan is active.

1. console.cloud.google.com → New project → Name: Planora
2. APIs & Services → Enable:
   - Places API (New)
   - Distance Matrix API
   - Maps SDK for Android
   - Maps SDK for iOS
3. Credentials → Create API Key → Copy
4. Save → `appsettings.Development.json` → `Google:PlacesApiKey`
5. Save → `PlanoraApp/.env` → `EXPO_PUBLIC_GOOGLE_MAPS_KEY`

### Firebase (Build Step 10 — push only)
1. console.firebase.google.com → New project → Planora
2. Project settings → Cloud Messaging → enable
3. Add Android app → package: `com.planora.app` → download `google-services.json` → `PlanoraApp/android/app/`
4. Add iOS app → bundle: `com.planora.app` → download `GoogleService-Info.plist` → `PlanoraApp/ios/PlanoraApp/`
5. Project settings → Cloud Messaging → Legacy server key → Copy
6. Save → `appsettings.Development.json` → `Firebase:ServerKey`

### Google Fonts (Build Step 1 — Expo)
No account needed. Installed via npm:
```bash
npx expo install @expo-google-fonts/oswald @expo-google-fonts/playfair-display @expo-google-fonts/bebas-neue expo-font
```

---

## PART C — Keys Reference Card

```
KEY                         WHERE TO GET                       WHERE IT GOES
────────────────────────────────────────────────────────────────────────────────────
OpenAI API Key              platform.openai.com                appsettings → OpenAI:ApiKey
Cosmos DB conn str          Azure portal → Cosmos DB           appsettings → CosmosDb:ConnectionString
PostgreSQL conn str         Azure portal → PostgreSQL          appsettings → PostgreSQL:ConnectionString
Azure Redis conn str        Azure portal → Cache for Redis     appsettings → AzureRedis:ConnectionString
Azure Service Bus conn str  Azure portal → Service Bus         appsettings → AzureServiceBus:ConnectionString
Azure Blob conn str         Azure portal → Storage acct       appsettings → AzureStorage:ConnectionString
Doc Intelligence Key        Azure portal → Doc Intel           appsettings → DocIntelligence:ApiKey
Doc Intelligence URL        Azure portal → Doc Intel           appsettings → DocIntelligence:Endpoint
Entra Client ID             entra.microsoft.com                .env → EXPO_PUBLIC_B2C_CLIENT_ID
Entra Tenant                entra.microsoft.com                .env → EXPO_PUBLIC_B2C_TENANT_NAME
ACS conn str                Azure portal → ACS                 appsettings → AzureCommunicationServices:ConnectionString
ACS phone                   Azure portal → ACS                 appsettings → AzureCommunicationServices:FromPhone
Google API Key              GCP console (Free Plan active)     appsettings → Google:PlacesApiKey
                                                               .env → EXPO_PUBLIC_GOOGLE_MAPS_KEY
Firebase Server Key         Firebase console                   appsettings → Firebase:ServerKey
```

> ⚠ NO affiliate keys. No Skyscanner. No Kayak. No Booking.com AID. No Viator MCID. Pure deeplinks need none of these.

**All Azure keys:** portal.azure.com → Resource groups → `planora-rg`

---

## PART D — Machine Setup

```bash
node --version        # need 18+  → nodejs.org
dotnet --version      # need 8.0+ → dot.net/download
git --version         # need any  → git-scm.com

npm install -g expo-cli eas-cli
```

---

## PART E — Folder Setup

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
```

`.gitignore`:
```
.env
.env.local
appsettings.Development.json
appsettings.Production.json
google-services.json
GoogleService-Info.plist
*.user
.DS_Store
node_modules/
bin/
obj/
.expo/
```

Init git:
```bash
cd Planora
git init
git add .
git commit -m "Initial: project docs v3.0 — deeplinks only, Choiceable model, new design system"
```

---

## PART F — Antigravity Session Guide

### Step 1 starter message:
```
@PROJECT_BIBLE.md @AGENT_INSTRUCTIONS.md

Read both files fully before doing anything.

App name: Planora
Current build step: 1
Last completed: Nothing — fresh start

Infrastructure:
  Document DB: Azure Cosmos DB (MongoDB API) — MongoDB.Driver SDK
  SQL DB: Azure PostgreSQL — Npgsql SDK
  Cache: Azure Cache for Redis — StackExchange.Redis
  Queue: Azure Service Bus — Azure Functions trigger binding
  SMS: Azure Communication Services
  Push: Firebase Cloud Messaging
  Affiliates: NONE — pure deeplinks only

Design system:
  Palette: White + Black + #0E2038 + #213A5C + #90A4B0 ONLY
  Fonts: Bebas Neue (headings, ALL CAPS) + Playfair Display italic (destinations) + Oswald (everything else)
  NO dark backgrounds. NO purple. NO old palette.

Bottom nav: Home, Trips, Vault, Safety, Profile — NO Bookings tab
Pre-wizard: 3-card screen (AI Generate | Build Manually | Browse Packages)
Booking: Choiceable items get inline deeplinks + Browse button. Non-Choiceable get info card + one ticket link.

Images at: Planora/images/ → copy to PlanoraApp/assets/images/

Begin Step 1.
```

### Every session after first:
```
@PROJECT_BIBLE.md @AGENT_INSTRUCTIONS.md

Current step: [N]
Last completed: [what was finished and committed]
[any notes]

Continue.
```

---

## PART G — Build Order with Cloud Timing

```
Step  What                               Do BEFORE starting
──────────────────────────────────────────────────────────────────────────────
1     Scaffold + Onboarding + Design     Nothing (images + fonts only)
      System (fonts, theme, components)
2     Auth (Entra External ID)           Part A Step 3 (entra.microsoft.com)
3     Pre-wizard + Wizard UI             Nothing
4     .NET API + Cosmos DB + PostgreSQL  Part A Steps 4–5
5     Semantic Kernel + Orchestrator +   OpenAI key + Part A Steps 6–7
      SpotEnrichmentAgent +              (Redis + Service Bus)
      DeeplinkBuilderService
6     Full agent chain (6 agents)        Google Cloud APIs (Part B)
7     Trip Card UI — Choiceable /        Nothing new
      Non-Choiceable + BrowseSheet
8     Manual Builder + Edit Mode         Nothing new
9     Trip Vault (trip+POI-aware)        Part A Steps 8–9 (Blob + Doc Intel)
10    SafeTravel Shield                  Part A Step 10 (ACS) + Firebase (Part B)
11    Finance + Package Browse + Polish  Nothing new
```

---

## PART H — Quick Reference

**Session start:** `@PROJECT_BIBLE.md @AGENT_INSTRUCTIONS.md — Step [N]. Last done: [X]. Continue.`

**Cloud setup:** Follow printed steps → copy key → paste into `appsettings.Development.json` or `.env` → tell agent "done"

**Model:**
- Planning → Opus 4.6
- Writing code → Sonnet 4.6
- Cloud issue → Gemini 2.5 Pro
- Full review → Gemini 2.5 Pro

**Commit:** `git add . && git commit -m "Step X complete: [description]"`

**Azure dashboard:** portal.azure.com → Resource groups → `planora-rg`

---

## PART I — Architecture Decision Log

### Why no affiliate IDs?
Affiliate programmes (Skyscanner denied, Kayak denied, Booking.com requires traffic to approve) create a hard dependency that blocks the app from working. Plain deeplinks to Booking.com, Agoda, MakeMyTrip etc. work on day one without any approval. Once the app has traffic, affiliate parameters can be added to the existing URLs without changing any architecture — they're just extra query params.

### Why Choiceable vs Non-Choiceable?
A trip to Jaipur has two kinds of spots: Hawa Mahal (you're going there — no choice needed) and the hotel (you need to pick one). Building a "compare options" UI for Hawa Mahal is meaningless. Building an info card with visit time, entry fee, and a ticket link for a hotel is also meaningless. The split creates the right UX for each: comparison UI for things you choose, info card for things you attend.

### Why 2–3 suggested options inline + Browse button?
Showing all 8 hotel platforms at once is overwhelming. Showing 2–3 trusted names inline handles 90% of users. The Browse button gives power users access to everything without cluttering the default view.

### Why deeplinks built at generation time?
If deeplinks were built at tap time, any latency or backend outage would delay the user's ability to open a booking platform. Pre-building them at generation time means the booking flow is instant at tap time — the URL is already on the POI document. `Linking.openURL()` is all that happens at tap.

### Why is the Vault trip-and-POI-aware?
A generic file dump makes users hunt for documents. Organising by trip → day → POI means the user opens their Jaipur trip, sees Day 1, sees Hawa Mahal marked "Ticket uploaded" and Hotel Night 1 marked "Booking ref added." The context is immediate. It also enables accurate booking_status per spot, check-in reminders per spot, and calendar sync per spot.

### Why white/navy over dark theme?
Travel is aspirational. Light, clean, editorial design communicates quality and confidence. Dark apps feel like developer tools. The Booking.com and Airbnb apps are white for a reason. The new palette is premium without being cold.

### Why Bebas Neue for headlines?
It reads like a travel magazine. ALL CAPS + condensed geometric + bold presence = destination feeling. Paired with Playfair Display italic for destination names, the combination is editorial and warm — distinct from every other travel app using plain sans-serif stacks.
