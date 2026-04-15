# FULL PROCESS v2.0 — Planora
> **v1.0:** Full Azure consolidation. All services in one Azure portal under `planora-rg`.
> **v2.0:** Skyscanner + Kayak denied → AWIN/Booking.com confirmed as primary affiliate. GCP Free Plan active. Bookings Tab removed from nav → replaced by Vault (Tab 3). Three trip creation modes added. Spot Enrichment Agent added. Manual Builder and Edit Mode added. Build sequence updated to 11 steps.

---

## PART A — Fresh Azure Account Setup

### Step 1: Create the account
1. Open a **private/incognito browser window**
2. Go to **azure.microsoft.com/free**
3. Click "Start free"
4. Sign in with a **Gmail or Outlook address** — NOT your VIT email
5. Enter phone number for SMS verification
6. Enter a **debit/credit card** — identity verification only, NOT charged unless you manually upgrade
7. Complete signup → you get:
   - **$200 free credit** (valid 30 days)
   - **12 months free** on core services
   - **Always free** tier on 55+ services

### Step 2: Create Resource Group
1. portal.azure.com → search "Resource groups" → Create
2. Name: `planora-rg`
3. Region: `Central India`
4. Click Review + Create → Create

> Put ALL resources inside `planora-rg`.

### Step 3: Microsoft Entra External ID (needed at Build Step 2)
> ⚠ **Azure AD B2C is discontinued for new customers as of May 2025.** Entra External ID is the direct replacement.

1. Go to **entra.microsoft.com** (NOT portal.azure.com)
2. Navigate to **Entra ID → Overview → Manage tenants → Create**
3. Select **External** → Continue
4. Tenant name: `Planora`, Initial domain: `planoratravel`
5. Country: India → Review + Create (up to 30 minutes)
6. Switch to new tenant → **App registrations → New registration**
   - Name: `Planora`, Platform: Mobile and desktop, Redirect URI: `exp://localhost`
7. Copy **Application (client) ID** → save as `B2C_CLIENT_ID`
8. **User flows → New user flow → Sign up and sign in**
   - Name: `signupsignin` → `B2C_1_signupsignin`
9. Save Client ID → `.env` → `EXPO_PUBLIC_B2C_CLIENT_ID`
10. Save tenant domain `planoratravel` → `EXPO_PUBLIC_B2C_TENANT_NAME`

### Step 4: Azure Cosmos DB — MongoDB API (needed at Build Step 4)
1. Switch back to main subscription
2. portal.azure.com → Create Resource → "Azure Cosmos DB" → **Azure Cosmos DB for MongoDB** (NOT Core/SQL)
3. Resource group: `planora-rg`, Account name: `planora-cosmos`, Region: Central India
4. Capacity mode: Serverless
5. Go to resource → **Connection strings** → Copy Primary Connection String
6. Save → `appsettings.Development.json` → `CosmosDb:ConnectionString`
7. Data Explorer → New Database → `planora`
   - Create collections: `trips`, `itinerary_days`, `places_of_interest`

### Step 5: Azure Database for PostgreSQL (needed at Build Step 4)
1. portal.azure.com → Create Resource → "Azure Database for PostgreSQL Flexible Server"
2. Resource group: `planora-rg`, Server name: `planora-postgres`, Region: Central India
3. Workload type: Development (Burstable B1ms)
4. Admin username + password — **save both**
5. Networking: Allow public access + Add current client IP + Allow Azure services
6. Go to resource → Connection strings → copy ADO.NET format
7. Save → `appsettings.Development.json` → `PostgreSQL:ConnectionString`
8. Connect via pgAdmin/psql → run all `CREATE TABLE` statements from PROJECT_BIBLE Section 4

### Step 6: Azure Cache for Redis (needed at Build Step 5)
1. portal.azure.com → Create Resource → "Azure Cache for Redis"
2. Resource group: `planora-rg`, DNS name: `planora-cache`, Region: Central India
3. Cache SKU: Basic, C0
4. Go to resource → **Access keys** → Copy Primary connection string (StackExchange.Redis)
5. Save → `appsettings.Development.json` → `AzureRedis:ConnectionString`

### Step 7: Azure Service Bus (needed at Build Step 5)
1. portal.azure.com → Create Resource → "Service Bus"
2. Resource group: `planora-rg`, Namespace: `planora-bus`, Region: Central India, Pricing: Basic
3. Go to namespace → **Queues** → + Queue → Name: `ai-generation-jobs`
4. Shared access policies → `RootManageSharedAccessKey` → Copy Primary Connection String
5. Save → `appsettings.Development.json` → `AzureServiceBus:ConnectionString`

### Step 8: Azure Blob Storage (needed at Build Step 9)
1. portal.azure.com → Create Resource → Storage account
2. Resource group: `planora-rg`, Name: `planoravault`, Region: Central India
3. Go to resource → **Containers** → + Container → Name: `vault-documents`, Access: Private
4. Access keys → Copy Connection string (key1)
5. Save → `appsettings.Development.json` → `AzureStorage:ConnectionString`

### Step 9: Azure AI Document Intelligence (needed at Build Step 9)
1. Create Resource → "Document Intelligence"
2. Resource group: `planora-rg`, Region: Central India, Pricing: Free F0
3. Go to resource → **Keys and Endpoint** → Copy Key 1 + Endpoint

### Step 10: Azure Communication Services (needed at Build Step 10)
1. portal.azure.com → Create Resource → "Communication Services"
2. Resource group: `planora-rg`, Resource name: `planora-acs`, Region: Central India
3. Go to resource → **Keys** → Copy Primary Connection String
4. Save → `appsettings.Development.json` → `AzureCommunicationServices:ConnectionString`
5. Phone Numbers → Get a phone number → Copy → `AzureCommunicationServices:FromPhone`

### Step 11: That's it for Azure
Everything is now in one portal under `planora-rg`. Non-Azure services:
- **Google Cloud** (Places API, Maps, Distance Matrix — GCP Free Plan active)
- **Firebase** (push notifications only)
- **OpenAI API** (direct — simpler and cheaper than Azure OpenAI at this scale)
- **AWIN** (Booking.com affiliate programme — confirmed)

---

## PART B — Remaining Cloud Accounts

### Google Cloud APIs (Build Step 6)
> GCP Free Plan is active — APIs are working.

1. console.cloud.google.com → New project → Name: Planora
2. APIs & Services → Enable:
   - Places API (New)
   - Distance Matrix API
   - Maps SDK for Android
   - Maps SDK for iOS
3. Credentials → Create API Key → Copy key
4. Save → `appsettings.Development.json` → `Google:PlacesApiKey`
5. Save → `PlanoraApp/.env` → `EXPO_PUBLIC_GOOGLE_MAPS_KEY`

### Firebase (Build Step 10 — push notifications only)
1. console.firebase.google.com → New project → Name: Planora
2. Project settings → Cloud Messaging → enable
3. Add Android app → package name: `com.planora.app`
   - Download `google-services.json` → `PlanoraApp/android/app/`
4. Add iOS app → bundle ID: `com.planora.app`
   - Download `GoogleService-Info.plist` → `PlanoraApp/ios/PlanoraApp/`
5. Project settings → Cloud Messaging → Legacy server key → Copy
6. Save → `appsettings.Development.json` → `Firebase:ServerKey`

### AWIN / Booking.com Affiliate (needed for Build Step 7)
> Booking.com partnership is confirmed via AWIN.

1. Go to **awin.com** → Create publisher account
2. Once approved, go to **Publisher** → My Publishers → Search for Booking.com programme
3. Apply to join the Booking.com programme
4. Once approved, go to the programme → **Links** → find your `aid` parameter
5. Save → `appsettings.Development.json` → `Affiliates:BookingComAid`

> While awaiting affiliate approval, you can use Booking.com plain deeplinks without the `aid` param for local dev/testing. Add the `aid` before production.

---

## PART C — Keys Reference Card

```
KEY                        WHERE TO GET                      WHERE IT GOES
─────────────────────────────────────────────────────────────────────────────────
OpenAI API Key             platform.openai.com               appsettings → OpenAI:ApiKey
Cosmos DB conn str         Azure portal → Cosmos DB          appsettings → CosmosDb:ConnectionString
PostgreSQL conn str        Azure portal → PostgreSQL         appsettings → PostgreSQL:ConnectionString
Azure Redis conn str       Azure portal → Cache for Redis    appsettings → AzureRedis:ConnectionString
Azure Service Bus conn str Azure portal → Service Bus        appsettings → AzureServiceBus:ConnectionString
Azure Blob conn str        Azure portal → Storage acct      appsettings → AzureStorage:ConnectionString
Doc Intelligence Key       Azure portal → Doc Intel          appsettings → DocIntelligence:ApiKey
Doc Intelligence URL       Azure portal → Doc Intel          appsettings → DocIntelligence:Endpoint
Entra Client ID            entra.microsoft.com               .env → EXPO_PUBLIC_B2C_CLIENT_ID
Entra Tenant               entra.microsoft.com               .env → EXPO_PUBLIC_B2C_TENANT_NAME
ACS conn str               Azure portal → ACS                appsettings → AzureCommunicationServices:ConnectionString
ACS phone number           Azure portal → ACS                appsettings → AzureCommunicationServices:FromPhone
Google API Key             GCP console (Free Plan active)    appsettings → Google:PlacesApiKey
                                                             .env → EXPO_PUBLIC_GOOGLE_MAPS_KEY
Firebase Server Key        Firebase console                  appsettings → Firebase:ServerKey
Booking.com AID (AWIN)     awin.com → Booking.com programme  appsettings → Affiliates:BookingComAid
Agoda CID                  partners.agoda.com                appsettings → Affiliates:AgodaCid
Viator MCID                viator.com/partner                appsettings → Affiliates:ViatorMcid
GYG Partner ID             partner.getyourguide.com          appsettings → Affiliates:GygPartnerId
```

> ⚠ Skyscanner and Kayak partnership keys are NOT needed — both denied. Do not add them.

**All Azure keys** live in one place: portal.azure.com → Resource groups → `planora-rg`

---

## PART D — Machine Setup Checklist

```bash
node --version        # need 18+    → nodejs.org if missing
dotnet --version      # need 8.0+   → dot.net/download if missing
git --version         # need any    → git-scm.com if missing
```

```bash
npm install -g expo-cli eas-cli
```

---

## PART E — Planora Folder Setup

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

Create `.gitignore`:
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
git add PROJECT_BIBLE.md AGENT_INSTRUCTIONS.md BOOKING_ARCHITECTURE.md FULL_PROCESS.md .gitignore
git commit -m "Initial: project docs v2.0 — AWIN/Booking.com, 3 trip modes, no Bookings tab"
```

---

## PART F — Starting in Antigravity

### Session starter message — Step 1:
```
@PROJECT_BIBLE.md @AGENT_INSTRUCTIONS.md

Read both files fully before doing anything.

App name: Planora
Current build step: 1
Last completed: Nothing — fresh start
Infrastructure (ALL Azure):
  Document DB: Azure Cosmos DB (MongoDB API) — use MongoDB.Driver SDK
  SQL DB: Azure Database for PostgreSQL — use Npgsql SDK
  Cache: Azure Cache for Redis — use StackExchange.Redis
  Queue: Azure Service Bus — Azure Functions trigger binding
  SMS: Azure Communication Services
  Push: Firebase Cloud Messaging (only non-Azure besides Google and OpenAI)
  Affiliate: AWIN / Booking.com confirmed. Skyscanner and Kayak DENIED.

Bottom nav: Home, Trips, Vault, Safety, Profile — NO Bookings tab.
Pre-wizard screen: 3-card layout (AI Generate | Build Manually | Browse Packages)

The images folder is at: Planora/images/
Copy images to PlanoraApp/assets/images/ as part of Step 1.

Begin Step 1.
```

### For every session after the first:
```
@PROJECT_BIBLE.md @AGENT_INSTRUCTIONS.md

Current step: [N]
Last completed: [what you finished and committed]
[any notes about what happened last session]

Continue.
```

---

## PART G — Build Order with Cloud Timing

```
Step  What                              Do this BEFORE starting step
─────────────────────────────────────────────────────────────────────────
1     Scaffold + Onboarding             Nothing (images ready)
2     Auth (Entra External ID)          Part A Step 3 — entra.microsoft.com
3     Wizard UI + 3-card pre-wizard     Nothing
4     .NET + Cosmos DB + PgSQL          Part A Steps 4–5
5     Semantic Kernel + Orchestrator    OpenAI key + Part A Steps 6–7
      + Spot Enrichment Agent
6     Agent chain (6 agents)            Google Cloud APIs (Part B)
7     Booking Gateway (contextual)      Azure Redis already done in Step 5
                                        AWIN Booking.com AID (Part B)
8     Manual Builder + Edit Mode        Nothing new
9     Trip Vault (replaces Bookings)    Part A Steps 8–9 (Blob + Doc Intelligence)
10    SafeTravel Shield                 Part A Step 10 (ACS) + Firebase (Part B)
11    Finance + Package Browse + Polish Nothing new
```

**Do NOW before first session:**
- Azure fresh account + `planora-rg` (Part A Steps 1–2)
- Microsoft Entra External ID at `entra.microsoft.com` (Part A Step 3)
- Azure Cosmos DB MongoDB API (Part A Step 4)
- Azure PostgreSQL + run schema SQL (Part A Step 5)

---

## PART H — Quick Reference Card

**Session start:** `@PROJECT_BIBLE.md @AGENT_INSTRUCTIONS.md — Step [N]. Last done: [X]. Continue.`

**When agent pauses for cloud setup:**
1. Follow the printed steps (all in portal.azure.com → planora-rg)
2. Copy the connection string / key
3. Paste into `appsettings.Development.json` or `.env`
4. Tell agent: "done" → it continues

**Model switching:**
- Step starts → Opus 4.6 (plan) → Sonnet 4.6 (write)
- Error → Sonnet 4.6 (paste error only)
- Cloud confusion → Gemini 2.5 Pro
- Full code review → Gemini 2.5 Pro

**Commit after every step:**
```bash
git add .
git commit -m "Step X complete: [description]"
```

**One-stop Azure dashboard:** portal.azure.com → Resource groups → `planora-rg`

---

## PART I — Key Architecture Decisions Explained

### Why no Bookings tab?
The original Bookings tab was solving a problem that already has many better-built solutions (MakeMyTrip, Booking.com, etc.). Planora's value is in the planning intelligence, safety layer, and vault — not in duplicating a booking interface. Booking options now surface contextually per POI inside the trip. Confirmed bookings consolidate in the Vault. This keeps the UI focused and the codebase simpler.

### Why three trip creation modes?
- **AI Generate** — core differentiator, full magic, one shot
- **Manual Builder** — for users who know exactly what they want, or importing a package they bought elsewhere. Token-efficient: ~800 tokens per spot vs ~10,000 for a full trip.
- **Package Browse** — for users who prefer a curated tour. Redirects to Booking.com (AWIN) + other platforms. No complex scraping needed.

### Why AWIN/Booking.com over Skyscanner/Kayak?
Skyscanner and Kayak declined the affiliate application. Booking.com via AWIN is confirmed and covers both hotels AND tours/activities — giving Planora affiliate coverage across two major booking categories from a single approved partnership.

### Why is Edit Mode purely local for deletes?
Deleting a spot doesn't require AI intelligence. The user knows what they want to remove. Running a GPT-4o call to "approve" a deletion would waste tokens and add latency. Delete is a PATCH to Cosmos DB and a local state update.

### Why a local placement algorithm for spot adds?
The algorithm is simple: find the day whose POI cluster centroid is geographically closest to the new spot, and if that day isn't full, insert there. This is deterministic, free, and fast. It produces sensible results for 95% of cases. AI is only needed for the enrichment of the spot's data — not for figuring out which day it belongs to.
