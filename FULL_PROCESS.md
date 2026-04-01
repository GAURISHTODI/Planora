# FULL PROCESS v1.0 — Planora
> **v1.0:** Full Azure consolidation. All services now live in one Azure portal under `planora-rg`. MongoDB Atlas → Cosmos DB (MongoDB API). Supabase → Azure PostgreSQL. Upstash → Azure Cache for Redis + Azure Service Bus. Twilio → Azure Communication Services. Firebase retained for push notifications only (Azure Notification Hubs calls FCM under the hood anyway — direct FCM is simpler).
> **v1.0:** Azure AD B2C discontinued for new customers (May 2025) → **Microsoft Entra External ID**. Tenant created at `entra.microsoft.com`, NOT `portal.azure.com`. All SDK/env keys identical. This was planned to add initially which is not a part of the plan now

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

> Put ALL resources inside `planora-rg`. One group = one bill, one delete to clean up everything.

### Step 3: Microsoft Entra External ID (needed at Build Step 2)
> ⚠ **Azure AD B2C is discontinued for new customers as of May 2025.** Microsoft Entra External ID is the direct replacement — same OIDC/PKCE flow, same token format, same env variable names. Tenant creation MUST be done at `entra.microsoft.com`.

1. Go to **entra.microsoft.com** (NOT portal.azure.com — B2C tenant creation will fail there)
2. Navigate to **Entra ID → Overview → Manage tenants → Create**
3. Select **External** → Continue
4. Tenant name: `Planora`
5. Initial domain: `planoratravel` (becomes `planoratravel.onmicrosoft.com`)
6. Country: India → Review + Create (takes up to 30 minutes)
7. **Switch to new tenant:** Settings icon (top right) → Directories + subscriptions → Switch next to Planora
8. In the external tenant → **App registrations → New registration**
   - Name: `Planora`
   - Platform: Mobile and desktop applications
   - Redirect URI: `exp://localhost`
   - Click Register
9. Copy **Application (client) ID** → save as `B2C_CLIENT_ID`
10. **User flows → New user flow → Sign up and sign in**
    - Name: `signupsignin` (becomes `B2C_1_signupsignin`)
    - Identity providers: Email signup + Google
    - Click Create
11. Save:
    - Client ID → `PlanoraApp/.env` → `EXPO_PUBLIC_B2C_CLIENT_ID`
    - Tenant domain `planoratravel` → `EXPO_PUBLIC_B2C_TENANT_NAME`

### Step 4: Azure Cosmos DB — MongoDB API (needed at Build Step 4)
> This replaces MongoDB Atlas. Uses the IDENTICAL MongoDB.Driver SDK — zero code changes.

1. **Switch back to main subscription** (not B2C tenant) via top-right directory switcher
2. portal.azure.com → Create Resource → search "Azure Cosmos DB" → Create
3. Select API: **Azure Cosmos DB for MongoDB** (NOT Core/SQL — this is critical)
4. Resource group: `planora-rg`
5. Account name: `planora-cosmos` (globally unique, lowercase)
6. Region: Central India
7. Capacity mode: Serverless (free for low volume)
8. Click Review + Create → Create (~5 min)
9. Go to resource → **Connection strings** (left menu)
10. Copy **Primary Connection String**
11. Save → `appsettings.Development.json` → `CosmosDb:ConnectionString`
12. Go to **Data Explorer** → New Database → `planora`
    - Create collections: `trips`, `itinerary_days`, `places_of_interest`

### Step 5: Azure Database for PostgreSQL (needed at Build Step 4)
> This replaces Supabase. Same Npgsql SDK.

1. portal.azure.com → Create Resource → "Azure Database for PostgreSQL Flexible Server"
2. Resource group: `planora-rg`
3. Server name: `planora-postgres`
4. Region: Central India
5. PostgreSQL version: 16
6. Workload type: Development (Burstable B1ms — cheapest)
7. Admin username + password — **save both**
8. Networking tab: Check "Allow public access" + "Add current client IP"
   - Also check: "Allow Azure services to connect"
9. Click Review + Create → Create (~3 min)
10. Go to resource → **Connection strings** → copy ADO.NET format
11. Replace `{your_password}` with your actual password
12. Save → `appsettings.Development.json` → `PostgreSQL:ConnectionString`
13. Connect via pgAdmin or psql → run all `CREATE TABLE` statements from PROJECT_BIBLE Section 4

### Step 6: Azure Cache for Redis (needed at Build Step 5)
> This replaces Upstash Redis. Same StackExchange.Redis SDK.

1. portal.azure.com → Create Resource → "Azure Cache for Redis"
2. Resource group: `planora-rg`
3. DNS name: `planora-cache`
4. Region: Central India
5. Cache SKU: Basic, Cache size: C0 (250MB — free tier equivalent)
6. Click Review + Create → Create (~5 min)
7. Go to resource → **Access keys** (left menu)
8. Copy **Primary connection string (StackExchange.Redis)**
9. Save → `appsettings.Development.json` → `AzureRedis:ConnectionString`

### Step 7: Azure Service Bus (needed at Build Step 5)
> This replaces Upstash QStash. Azure Functions trigger bindings connect directly — no HTTP layer.

1. portal.azure.com → Create Resource → "Service Bus"
2. Resource group: `planora-rg`
3. Namespace name: `planora-bus`
4. Region: Central India
5. Pricing tier: Basic
6. Click Review + Create → Create
7. Go to namespace → **Queues** → + Queue
   - Name: `ai-generation-jobs`
   - Max size: 1 GB, Max delivery count: 5
8. Go to **Shared access policies** → `RootManageSharedAccessKey`
9. Copy **Primary Connection String**
10. Save → `appsettings.Development.json` → `AzureServiceBus:ConnectionString`

### Step 8: Azure Blob Storage (needed at Build Step 8)
1. portal.azure.com → Create Resource → Storage account
2. Resource group: `planora-rg`
3. Name: `planoravault` (globally unique, lowercase)
4. Region: Central India
5. Performance: Standard, Redundancy: LRS
6. Click Review + Create → Create
7. Go to resource → **Containers** → + Container
   - Name: `vault-documents`
   - Access level: Private
8. Go to **Access keys** → Copy **Connection string** (key1)
9. Save → `appsettings.Development.json` → `AzureStorage:ConnectionString`

### Step 9: Azure AI Document Intelligence for OCR (needed at Build Step 8)
1. Create Resource → search "Document Intelligence" (formerly Form Recognizer)
2. Resource group: `planora-rg`
3. Region: Central India
4. Pricing: Free F0 tier
5. Click Review + Create
6. Go to resource → **Keys and Endpoint**
7. Copy Key 1 + Endpoint → save for Step 8

### Step 10: Azure Communication Services (needed at Build Step 9)
> This replaces Twilio.

1. portal.azure.com → Create Resource → "Communication Services"
2. Resource group: `planora-rg`
3. Resource name: `planora-acs`
4. Region: Central India (data location)
5. Click Review + Create → Create
6. Go to resource → **Keys** (left menu)
7. Copy **Primary Connection String**
8. Save → `appsettings.Development.json` → `AzureCommunicationServices:ConnectionString`
9. Go to **Phone numbers** → Get a phone number (SMS capable)
10. Copy number → `AzureCommunicationServices:FromPhone`

### Step 11: That's it for Azure
Everything is now in one portal under `planora-rg`. The only non-Azure services are:
- **Google Cloud** (Places API, Maps, Distance Matrix — no Azure equivalent)
- **Firebase** (push notifications — simpler than Azure Notification Hubs for mobile)
- **OpenAI API** (called directly — Azure OpenAI is also available on fresh accounts but direct is cheaper at this scale)

---

## PART B — Remaining Cloud Accounts (set up when agent prompts)

### Google Cloud APIs (Build Step 6)
1. console.cloud.google.com → New project → Name: Planora
2. APIs & Services → Enable APIs:
   - Places API (New)
   - Distance Matrix API
   - Maps SDK for Android
   - Maps SDK for iOS
3. Credentials → Create API Key → Copy key
4. Restrict key: Application restrictions → Android + iOS apps → add bundle ID later
5. Save → `appsettings.Development.json` → `Google:PlacesApiKey`
6. Save → `PlanoraApp/.env` → `EXPO_PUBLIC_GOOGLE_MAPS_KEY`

### Firebase (Build Step 9 — push notifications only)
1. console.firebase.google.com → New project → Name: Planora
2. Project settings → Cloud Messaging → enable
3. Add Android app → package name: `com.planora.app`
   - Download `google-services.json` → place in `PlanoraApp/android/app/`
4. Add iOS app → bundle ID: `com.planora.app`
   - Download `GoogleService-Info.plist` → place in `PlanoraApp/ios/PlanoraApp/`
5. Project settings → Cloud Messaging → Legacy server key → Copy
6. Save → `appsettings.Development.json` → `Firebase:ServerKey`

---

## PART C — Keys Reference Card

```
KEY                        WHERE TO GET                   WHERE IT GOES
──────────────────────────────────────────────────────────────────────────────────
OpenAI API Key             platform.openai.com            appsettings → OpenAI:ApiKey
Cosmos DB conn str         Azure portal → Cosmos DB       appsettings → CosmosDb:ConnectionString
PostgreSQL conn str        Azure portal → PostgreSQL      appsettings → PostgreSQL:ConnectionString
Azure Redis conn str       Azure portal → Cache for Redis appsettings → AzureRedis:ConnectionString
Azure Service Bus conn str Azure portal → Service Bus     appsettings → AzureServiceBus:ConnectionString
Azure Blob conn str        Azure portal → Storage acct   appsettings → AzureStorage:ConnectionString
Doc Intelligence Key       Azure portal → Doc Intel       appsettings → DocIntelligence:ApiKey
Doc Intelligence URL       Azure portal → Doc Intel       appsettings → DocIntelligence:Endpoint
Azure B2C Client ID        Azure B2C tenant               .env → EXPO_PUBLIC_B2C_CLIENT_ID
Azure B2C Tenant           Azure B2C tenant               .env → EXPO_PUBLIC_B2C_TENANT_NAME
ACS conn str               Azure portal → ACS             appsettings → AzureCommunicationServices:ConnectionString
ACS phone number           Azure portal → ACS             appsettings → AzureCommunicationServices:FromPhone
Google API Key             GCP console                    appsettings → Google:PlacesApiKey
                                                          .env → EXPO_PUBLIC_GOOGLE_MAPS_KEY
Firebase Server Key        Firebase console               appsettings → Firebase:ServerKey
```

**All Azure keys** live in one place: portal.azure.com → Resource groups → planora-rg

---

## PART D — Machine Setup Checklist

```bash
# Run these to verify your machine is ready:
node --version        # need 18+    → nodejs.org if missing
dotnet --version      # need 8.0+   → dot.net/download if missing
git --version         # need any    → git-scm.com if missing
```

Install Expo CLI:
```bash
npm install -g expo-cli eas-cli
```

---

## PART E — Planora Folder Setup (do once)

Your structure should be exactly:
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
  FULL_PROCESS.md
  .gitignore
```

Create `.gitignore` at root NOW:
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
git add PROJECT_BIBLE.md AGENT_INSTRUCTIONS.md FULL_PROCESS.md .gitignore
git commit -m "Initial: project docs v3 — full Azure consolidation"
```

---

## PART F — Starting in Antigravity (full guide)

### What Antigravity is and how it works
Antigravity is a standalone AI-powered IDE (built on VSCode). It has an agent panel on the left side. You give it instructions, it reads your files, writes code, and tells you what commands to run. You run the commands in the terminal panel inside Antigravity.

### First time setup in Antigravity
1. Open Antigravity
2. File → Open Folder → select your `Planora/` root folder
3. You will see your files in the left sidebar
4. Open the agent panel (left side, chat icon)
5. Model dropdown → select **Claude Sonnet 4.6** (for Step 1)

### How to reference files in Antigravity
Type `@filename` to attach a file to your message. The agent reads the full contents.

Example: `@PROJECT_BIBLE.md @AGENT_INSTRUCTIONS.md`

### Session starter message — copy this exactly for Step 1:
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
  Push: Firebase Cloud Messaging (only non-Azure service besides Google APIs)

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
Step  What                        Do this BEFORE starting step
──────────────────────────────────────────────────────────────────────
1     Scaffold + Onboarding       Nothing (just your images ready)
2     Auth (Entra External ID)     Part A Step 3 — entra.microsoft.com ← DO NOW
3     Wizard UI                   Nothing
4     .NET + Cosmos DB + PgSQL    Part A Steps 4–5 ← DO THESE NOW
5     Semantic Kernel             OpenAI key + Part A Steps 6–7 (Redis + Service Bus)
6     Agent chain                 Google Cloud APIs (Part B)
7     Booking Gateway             Azure Redis already done in Step 5
8     Trip Vault                  Part A Steps 8–9 (Blob + Doc Intelligence)
9     Safety Shield               Part A Step 10 (ACS) + Firebase (Part B)
10    Finance + Polish            Nothing new
```

**Do NOW before first session:**
- Azure fresh account + `planora-rg` resource group (Part A Steps 1–2)
- Microsoft Entra External ID tenant at `entra.microsoft.com` (Part A Step 3)
- Azure Cosmos DB MongoDB API (Part A Step 4)
- Azure PostgreSQL + run schema SQL (Part A Step 5)

---

## PART H — Quick Reference Card

**Session start:** `@PROJECT_BIBLE.md @AGENT_INSTRUCTIONS.md — Step [N]. Last done: [X]. Continue.`

**When agent pauses for cloud setup:**
1. Follow the printed steps exactly (all in portal.azure.com → planora-rg)
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
All services, all keys, all logs in one place.
