# Architectural Decision Records (ADRs)

This document tracks all significant architectural decisions, their business drivers, trade-offs, and rejected alternatives for the Wealth Management Banking Customer Portal.

---

## ADR-001: Clean Architecture with Inward Dependency Inversion
* **Status:** Accepted
* **Context:** Banking enterprise systems must remain decoupled from specific databases, cloud vendors, and UI frameworks to ensure long-term maintainability and compliance.
* **Decision:** Implement Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Api`).
  * `Domain`: Pure C# entities and invariants. ZERO NuGet dependencies. Users referenced by `Guid` only.
  * `Application`: Use case handlers, DTOs, FluentValidation rules, and abstractions.
  * `Infrastructure`: EF Core, SQL Server, ASP.NET Identity (`ApplicationUser`), Cloud SDKs.
  * `Api`: ASP.NET Core Web API controllers, middleware, and SignalR hubs.
* **Consequences & Trade-offs:** Requires more files and mapping overhead than a simple 3-tier CRUD app, but provides absolute testability and isolation of financial business rules.

---

## ADR-002: In-Memory JWT Access Tokens & HttpOnly SameSite=Strict Refresh Cookie with Vite Proxy
* **Status:** Accepted
* **Context:** Storing JWTs in `localStorage` exposes banking customers to credential theft via Cross-Site Scripting (XSS).
* **Decision:**
  * **Access Token:** Maintained exclusively in React JavaScript memory (lost on page close; refreshed silently via interceptor).
  * **Refresh Token:** Stored in an `HttpOnly`, `Secure`, `SameSite=Strict` cookie scoped to `/api/v1/auth`.
  * **Same-Site Local Requirement:** `SameSite=Strict` cookies are dropped by browsers if the API is hosted on a different origin. During local development, the Vite dev server proxies `/api` requests to ASP.NET Core (`http://localhost:5202`), preserving the same origin. In Azure production, Azure Static Web Apps linked backends or reverse proxies maintain the same domain.
* **Consequences & Trade-offs:** Requires automatic token refresh interceptor in Axios, but completely eliminates persistent XSS credential theft.

---

## ADR-003: Plain Handler/Service Pattern & Explicit Manual Mapping
* **Status:** Accepted
* **Context:** MediatR and AutoMapper introduced commercial licensing changes, reflection overhead, and indirection.
* **Decision:** Use explicit service/handler classes (e.g. `ApplicationService`, `AuthService`) and explicit extension mapping methods (`ApplicationMappingExtensions.ToDto()`).
* **Consequences & Trade-offs:** Developers write explicit mapping code, but debugging is straightforward (F12 navigation works directly), startup time is instant, and there are zero licensing liabilities.

---

## ADR-004: SignalR Real-Time Hub & Azure SignalR Service Scale-Out
* **Status:** Accepted
* **Context:** Status updates and ticket messages require real-time in-app delivery.
* **Decision:** Implement SignalR `NotificationHub` at `/hubs/notifications`. Because browser WebSocket connections cannot send custom HTTP headers during handshake, JWT authentication is passed via query string (`?access_token=...`) and validated in ASP.NET Core's `JwtBearerEvents.OnMessageReceived`.
* **Scale-Out Note (Known Enterprise Limitation):** When running multiple App Service instances behind a load balancer, standard in-memory SignalR hubs cannot broadcast across instances without a backplane. In cloud production, **Azure SignalR Service** or a **Redis Backplane** must be attached to manage cross-instance connection state.

---

## ADR-005: Default Mock AI Assistant & Mock Sentiment Analysis
* **Status:** Accepted
* **Context:** The pre-joining assessment must run and demonstrate full functionality out-of-the-box without requiring evaluator Azure OpenAI approval or cloud credentials.
* **Decision:** Default `IAiAssistantService` to `MockAiAssistantService` with deterministic, realistic wealth management responses. Provide configurable swap to live `AzureOpenAiService` via `appsettings.json`. Sentiment analysis and urgency scoring are handled within this abstraction rather than mandating Azure AI Language.
* **Consequences & Trade-offs:** Zero cost and zero cloud setup for evaluators running locally. Live Azure OpenAI can be toggled by configuring keys in `dotnet user-secrets`.

---

## ADR-006: EF Core SaveChanges Transactionality vs. Explicit Transactions
* **Status:** Accepted
* **Context:** In banking systems, data integrity across status updates, audit logs, and timelines is non-negotiable.
* **Decision:** Recognize that a single `SaveChangesAsync()` call in EF Core executes inside an implicit database transaction. An explicit transaction (`using var tx = await _context.Database.BeginTransactionAsync()`) is used only when:
  1. Multiple `SaveChangesAsync()` calls must succeed or fail together across multiple services.
  2. Co-ordinating database operations with external steps (e.g. outbox pattern, document re-upload verification).
* **Consequences & Trade-offs:** Avoids redundant nested transaction overhead while guaranteeing atomicity for complex business workflows.
