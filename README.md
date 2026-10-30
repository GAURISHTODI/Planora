# Planora — AI Engineering Project

> **Planora is an AI-powered travel-planning application designed around a multi-agent itinerary-generation workflow.** Its AI layer researches places, optimizes day-wise routes, merges package constraints, estimates budgets, personalizes points of interest (POIs), and enriches individual spots. The mobile app and cloud platform provide the product surface around that workflow.

**Primary lens:** AI engineering — agent orchestration, LLM integration, tool/API use, structured outputs, workflow reliability, data grounding, and evaluation.  
**Important status note:** This README describes the architecture and implementation plan documented for Planora. It does **not** imply every component is already implemented or deployed.

---

## Table of Contents

- [AI Engineer Overview](#ai-engineer-overview)
- [AI Capabilities](#ai-capabilities)
- [Agent Workflow](#agent-workflow)
- [AI Engineering Design](#ai-engineering-design)
- [Technology Stack](#technology-stack)
- [AI Request Lifecycle](#ai-request-lifecycle)
- [Data and Tool Integrations](#data-and-tool-integrations)
- [Reliability, Safety, and Evaluation](#reliability-safety-and-evaluation)
- [Repository Documentation](#repository-documentation)
- [Build Roadmap](#build-roadmap)
- [Local Development](#local-development)
- [Configuration](#configuration)
- [Engineering References](#engineering-references)
- [Project Scope](#project-scope)

---

## AI Engineer Overview

Planora is designed to solve a practical orchestration problem: converting a traveler's preferences and constraints into a useful, structured, day-by-day itinerary rather than returning an unstructured LLM response.

The AI system combines language-model reasoning with external place and routing data, deterministic application logic, and persisted trip state.

### AI engineering areas

| Area | Planora application |
|---|---|
| LLM integration | OpenAI models are used for POI personalization and spot enrichment. |
| Agent orchestration | A Semantic Kernel-based orchestrator coordinates the documented itinerary-generation stages. |
| Tool/API use | Google Places and Distance Matrix provide place and route context. |
| Structured domain output | Generated results are represented as trips, days, POIs, estimates, and booking metadata. |
| Hybrid AI + deterministic logic | The design combines LLM enrichment with route optimization, local placement logic, and deeplink construction. |
| Asynchronous processing | AI generation is submitted as a job through Azure Service Bus, with progress/status exposed to the client. |
| Persistence | Cosmos DB stores trip-oriented documents; PostgreSQL supports relational data such as packages, expenses, and safety records. |
| Product integration | Generated itineraries are consumed by an Expo / React Native application and can be edited or enriched spot by spot. |

### Why this is an AI engineering project

The focus is not simply calling an LLM. The documented architecture needs to coordinate multiple stages, ground responses in external data, preserve structured state, handle long-running work, and produce outputs that the application can render and users can edit.

---

## AI Capabilities

### 1. AI itinerary generation

Inputs include trip preferences and constraints collected through the trip wizard. The planned workflow generates a trip containing days and POIs, with route grouping, package constraints, budget estimates, and personalized information.

### 2. Place research and enrichment

The system uses Google Places data for place details such as names, coordinates, photos, and websites. The LLM adds user-facing summaries and selected travel metadata, such as visit duration, entry-fee estimates, and ticket links when available.

### 3. Day-wise route planning

The architecture uses Distance Matrix data and a route-optimization stage to cluster places into day plans. This is a hybrid design: external routing data and application-side optimization complement LLM-generated content.

### 4. Budget-aware planning

A Budget Agent is part of the documented generation chain and estimates trip costs. The application also plans a finance layer for recording expenses, splitting costs, and comparing spending against a trip budget.

### 5. Spot enrichment for editing and manual planning

A single place can be enriched independently of full itinerary generation. The documented endpoint accepts a place ID, trip ID, and day number, retrieves place context, generates enrichment metadata, builds relevant links, and determines a placement within the trip.

### 6. Choiceable vs. non-choiceable POIs

Every POI is assigned a product-level class:

- **Choiceable:** users need to compare alternatives, such as hotels, flights, cabs, trains, or car rentals.
- **Non-choiceable:** the destination itself is fixed, such as a monument, museum, park, or specific attraction.

This classification affects downstream rendering and link generation. It should be treated as a domain decision with explicit rules, not merely free-form model text.

---

## Agent Workflow

The documented full-generation workflow is:

```text
Trip wizard inputs
        |
        v
Create PENDING trip + enqueue generation job
        |
        v
Semantic Kernel orchestrator builds/coordinates the workflow
        |
        v
Research Agent
  - Retrieves candidate places using Google Places
  - Scores/recommends POIs
  - Assigns the POI class
        |
        v
Route Optimiser
  - Uses Distance Matrix data
  - Groups places into day-wise routes
        |
        v
Tour Package Injector
  - Merges fixed/locked package blocks when applicable
        |
        v
Budget Agent
  - Produces cost estimates
        |
        v
Day Assembly
  - Places activities into time slots
        |
        v
Personalisation Agent
  - Uses the LLM to create summaries and enrich POI details
        |
        v
DeeplinkBuilderService
  - Creates platform links for choiceable items
  - Adds a direct ticket link when applicable
        |
        v
Persist trip + mark job complete + notify client
```

The source documentation specifies an approximately 9 a.m.–10 p.m. planning window for day assembly. Actual scheduling must still account for opening hours, travel duration, user constraints, API coverage, and validation rules.

### Spot-enrichment workflow

```text
POST /api/v1/ai/enrich-spot
        |
        v
Fetch place details from Google Places
        |
        v
LLM enrichment
  - ai_summary
  - poi_class
  - entry_fee
  - ticket_url
  - visit_duration
        |
        v
DeeplinkBuilderService
        |
        v
Local placement algorithm
        |
        v
Update trip document and return enriched POI
```

### Responsibilities by component

| Component | Responsibility | Engineering note |
|---|---|---|
| Research Agent | Find and score candidate POIs | Ground place candidates in Places API data. |
| Route Optimiser | Group POIs into day routes | Use route/distance data and deterministic constraints. |
| Tour Package Injector | Merge fixed package blocks | Preserve locked activities and package constraints. |
| Budget Agent | Estimate trip costs | Make assumptions and currency explicit in the output. |
| Day Assembly | Assign day/time positions | Validate durations, travel gaps, and time windows. |
| Personalisation Agent | Create summaries and enrichment metadata | Keep outputs schema-constrained and grounded. |
| DeeplinkBuilderService | Build external booking/ticket URLs | Deterministic URL construction; no affiliate integration is required by the current design. |
| Orchestrator | Coordinate stages and job state | Track failures, retries, progress, and final status. |

> These are the responsibilities described in the project documentation. The table should not be read as evidence that each agent has already been implemented as an independently deployed service.

---

## AI Engineering Design

### 1. Use LLMs for language tasks; use deterministic code for invariants

The documented design assigns summarization and POI enrichment to the LLM, while routing data retrieval, local placement, and deeplink construction use APIs or application logic.

A useful implementation boundary is:

- **LLM-suited:** personalized summaries, natural-language interpretation, metadata extraction where source context is available.
- **Tool/API-suited:** place lookup, coordinates, distance/duration data, source URLs.
- **Deterministic-code-suited:** URL construction, schema checks, IDs, persistence, booking-state transitions, and hard scheduling constraints.

This separation improves debuggability and reduces reliance on free-form model output for business-critical behavior.

### 2. Structured outputs and validation

The AI layer should return typed domain objects rather than asking the mobile client to interpret arbitrary prose. Validate at the API boundary:

- Required fields and valid enum values, including `poi_class`.
- Numeric bounds and currency/unit consistency for estimates.
- Valid URLs before exposing external links.
- Valid trip/day/POI references before persisting updates.
- Time-window and route constraints before marking a plan complete.
- Missing or uncertain source information without inventing details.

The source documents describe fields such as `ai_summary`, `poi_class`, `entry_fee`, `ticket_url`, `visit_duration`, `suggested_options`, and `browse_links`; the final implementation should define their formal schemas and nullability.

### 3. Grounding and provenance

Use place IDs and returned Places API details as the canonical grounding context for place identity and coordinates. Treat model-generated descriptions and estimates as derived content, not authoritative facts. Where a value cannot be verified, return an explicit unknown/estimate rather than fabricating certainty.

### 4. Asynchronous orchestration

Full itinerary generation is designed as a background job:

1. Create a trip in a pending state.
2. Enqueue an AI-generation job.
3. Run the orchestrated workflow.
4. Persist intermediate/final state and progress.
5. Expose job status to the client.
6. Mark the trip complete or failed with actionable error information.

This avoids holding a mobile API request open for the entire multi-stage generation process.

### 5. Separate generation from enrichment

Full trip generation and one-spot enrichment are different workflows. Keeping them separate enables the app's manual builder and edit mode to reuse the same enrichment capabilities without regenerating an entire itinerary.

---

## Technology Stack

The following stack is specified in the project documentation. Check the actual repository and deployment configuration before treating every item as active.

### AI and application layer

| Layer | Technologies / services | Purpose |
|---|---|---|
| LLM integration | OpenAI API | POI summaries and enrichment |
| Agent orchestration | Microsoft Semantic Kernel | Coordinate AI workflow components |
| Backend API | .NET 8 / C# | API contracts, orchestration, domain logic |
| Place data | Google Places API | Place identity and details |
| Routing data | Google Distance Matrix API | Distance and travel-time context |
| Mobile client | Expo, React Native, TypeScript | Cross-platform trip planning interface |

### Cloud and persistence

| Layer | Technology | Intended role |
|---|---|---|
| Document data | Azure Cosmos DB (MongoDB API) | Trip, day, and POI-oriented data |
| Relational data | Azure Database for PostgreSQL | Packages, expenses, safety-related records |
| Cache | Azure Cache for Redis | Caching / supporting fast access |
| Job queue | Azure Service Bus | Background AI-generation jobs |
| File storage | Azure Blob Storage | Vault documents |
| Document extraction | Azure AI Document Intelligence | Extract information from uploaded tickets/documents |
| Authentication | Microsoft Entra External ID | External-user authentication |
| Notifications | Firebase Cloud Messaging; Azure Communication Services in the documented plan | Push and communication workflows |

---

## AI Request Lifecycle

### Full generation endpoint

The documented API contract is:

```http
POST /api/v1/ai/generate
```

Returns a `job_id` for tracking the generation workflow.

### Job status

```http
GET /api/v1/ai/jobs/{id}/status
```

The project documentation describes status updates containing fields such as:

```json
{
  "agent": "current-stage",
  "progress_pct": 40,
  "message": "Human-readable progress"
}
```

This is an illustrative shape based on the documented contract, not a guarantee of the final implemented response schema. The documentation refers to server-sent events (SSE) for progress.

### Spot enrichment endpoint

```http
POST /api/v1/ai/enrich-spot
Content-Type: application/json
```

The documented inputs are a `place_id`, `trip_id`, and `day_number`. Confirm exact property names and validation rules against the implemented DTOs.

### Trip data model

The architecture stores trip-oriented data including days and POIs. AI-generation jobs are documented with fields such as:

- `job_id`
- `trip_id`
- `status`
- `current_agent`
- `progress_pct`
- `error_log`
- `started_at`
- `completed_at`

These fields support observability and recovery for long-running generation tasks.

---

## Data and Tool Integrations

### External tools

- **Google Places API:** place lookup and details.
- **Google Distance Matrix API:** route distance and duration context.
- **OpenAI API:** LLM-powered personalization and spot enrichment.
- **Azure Service Bus:** decouples client requests from the long-running generation workflow.
- **Azure Cosmos DB:** persists trip documents and generated POIs.

### Booking-link strategy

Planora is designed as a planning and facilitation layer, not a booking engine. It does not process payments. The documented v3 design uses ordinary platform deeplinks without requiring affiliate approval or affiliate IDs.

- Choiceable POIs can expose a short list of suggested platform links plus a browse-more list.
- Non-choiceable POIs show destination information and, where applicable, one direct ticket link.
- Deeplinks are built during generation/enrichment so tapping a link does not require another AI call.

The link builder should use deterministic templates, URL encoding, and tests for supported platforms. It should not imply that a link is a live price quote or a confirmed reservation.

---

## Reliability, Safety, and Evaluation

The supplied project documents define the architecture and workflow, but they do not provide a completed benchmark suite or measured model-evaluation results. These are recommended AI-engineering acceptance criteria for implementation.

### Quality checks

| Check | What to verify |
|---|---|
| Schema validity | Every generated POI conforms to the agreed DTO/schema. |
| Groundedness | Names, coordinates, hours, and place identity agree with tool/API sources where available. |
| Itinerary feasibility | Opening hours, visit durations, route travel times, and day boundaries are respected. |
| Budget transparency | Estimates state assumptions; missing prices are not represented as confirmed prices. |
| POI classification | Choiceable/non-choiceable classification follows explicit domain rules. |
| URL correctness | Generated links use supported templates and valid encoding. |
| Partial failure | A failed enrichment step does not silently corrupt or erase a saved trip. |
| Repeatability | Same input and tool data can be replayed in tests; nondeterministic LLM output is handled explicitly. |

### Suggested evaluation dataset

Create a small, versioned test set containing different trip lengths, budgets, traveler preferences, destinations, package constraints, and sparse-data cases. For each case, store expected invariants rather than requiring exact natural-language output.

Useful metrics include:

- **Schema pass rate**
- **POI grounding / place-ID match rate**
- **Schedule constraint violation rate**
- **Route feasibility rate**
- **Budget-estimate coverage**
- **Invalid-link rate**
- **End-to-end generation success rate**
- **Latency and token/cost per completed itinerary**
- **Agent-stage failure and retry rate**

These are recommendations for the engineering test plan, not claimed results.

### Operational guardrails

- Keep API keys and connection strings in environment-specific secret configuration; never commit them.
- Apply timeouts, bounded retries, and clear failure states around external APIs and LLM calls.
- Log stage-level status, latency, token usage, and sanitized error details.
- Avoid logging access tokens, uploaded document contents, or unnecessary personal data.
- Treat model output and user-supplied text as untrusted input.
- Validate URLs and all data before persistence or client rendering.
- Use explicit limits for request size, output length, and generation cost.

---

## Repository Documentation

The project source documents are the primary reference for implementation decisions:

| Document | Use it for |
|---|---|
| [`PROJECT_BIBLE.md`](PROJECT_BIBLE.md) | Product scope, core concepts, architecture, data model, API contracts, and build sequence |
| [`AGENT_INSTRUCTIONS.md`](AGENT_INSTRUCTIONS.md) | Step-by-step implementation instructions and conventions for coding sessions |
| [`BOOKING_ARCHITECTURE.md`](BOOKING_ARCHITECTURE.md) | Choiceable/non-choiceable POI behavior and deeplink rules |
| [`FULL_PROCESS.md`](FULL_PROCESS.md) | Cloud resource setup, configuration, and end-to-end development workflow |
| `README.md` | AI-engineering overview and entry point into the project |

Keep these documents in the repository if you want the links above to work. If filenames or folder layout differ, update the relative links.

---

## Build Roadmap

The source documentation specifies this 11-step implementation sequence:

1. Scaffold the Expo app, navigation, onboarding, and design system.
2. Configure Microsoft Entra External ID authentication.
3. Implement the pre-wizard and trip wizard UI.
4. Build the .NET API and database foundations.
5. Integrate Semantic Kernel, the orchestrator, spot enrichment, and deeplink builder.
6. Implement the complete agent chain and Google API integrations.
7. Render Choiceable and Non-Choiceable trip cards.
8. Implement the manual builder and edit mode.
9. Build the trip-aware Vault and document processing.
10. Implement SafeTravel Shield features.
11. Complete finance, package browsing, and product polish.

**AI engineering priority:** Steps 4–6 establish the core AI workflow. Build and test a thin vertical slice early: one request, one place-grounded enrichment, a typed response, persisted output, and observable errors. Then expand to the full multi-stage chain.

---

## Local Development

The exact solution/project paths and commands depend on the repository layout. For the mobile application, the project documentation specifies Expo.

```bash
# From the mobile app directory
npm install
npx expo start
```

For the .NET backend, use the solution/project files in the repository and the .NET 8 SDK. Typical commands, if a solution file exists at the repository root:

```bash
dotnet restore
dotnet build
dotnet test
```

Do not assume the API or all cloud resources are runnable locally until their projects, configuration, and service dependencies have been created.

---

## Configuration

The project documentation refers to local backend configuration such as `appsettings.Development.json` and mobile environment configuration such as `.env`.

Typical configuration categories include:

```text
OpenAI:ApiKey
Google:PlacesApiKey
Google:MapsApiKey
AzureCosmos:ConnectionString
PostgreSQL:ConnectionString
AzureRedis:ConnectionString
AzureServiceBus:ConnectionString
AzureStorage:ConnectionString
DocIntelligence:Endpoint
DocIntelligence:ApiKey
```

These are **illustrative configuration categories**, not a guarantee that the exact key names match the current code. Check the settings classes and configuration binding in the implementation before adding values.

- Keep local secret files out of version control.
- Provide a safe `.env.example` or non-secret configuration template.
- Prefer managed identity or federated credentials where supported for deployed workloads.
- Restrict API keys by service and environment.
- Rotate any credential that is accidentally committed or exposed.

---

## Engineering References

These are useful official references for implementing the technologies specified in the project documents:

- [Microsoft Semantic Kernel documentation](https://learn.microsoft.com/en-us/semantic-kernel/overview/)
- [Semantic Kernel for .NET](https://learn.microsoft.com/en-us/semantic-kernel/get-started/quick-start-guide)
- [OpenAI API documentation](https://platform.openai.com/docs/overview)
- [OpenAI API best practices](https://platform.openai.com/docs/guides/production-best-practices)
- [.NET documentation](https://learn.microsoft.com/en-us/dotnet/)
- [ASP.NET Core web APIs](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Azure Service Bus documentation](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview)
- [Azure Cosmos DB for MongoDB documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/mongodb/introduction)
- [Google Places API documentation](https://developers.google.com/maps/documentation/places/web-service)
- [Google Distance Matrix API documentation](https://developers.google.com/maps/documentation/distance-matrix/overview)
- [Expo documentation](https://docs.expo.dev/)

Use official docs as the source of truth for current SDK versions, API behavior, pricing, and service availability.

---

## Project Scope

Planora combines AI-assisted itinerary generation with travel organization and safety features. The AI-engineering core is the pipeline that transforms trip constraints into grounded, structured, editable trip data using LLM enrichment, external tools, deterministic application logic, and asynchronous orchestration.

**Current-documentation caveat:** The project source describes a target architecture and an ordered build plan. Update this README as components are implemented, tested, and deployed. Only mark a capability as production-ready when there is code, configuration, and evidence to support that status.
