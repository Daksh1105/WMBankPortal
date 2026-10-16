# Junior-to-Architect Learning Journal

Welcome to your cumulative learning log! This document records the core concepts, real-world engineering analogies, and architectural rationale introduced during each phase.

---

## Phase 0: Solution Setup, Core Tooling & Baseline Security

### 1. What We Built
* **Clean Architecture Solution (.NET 10)**: Created four segregated layers (`Domain`, `Application`, `Infrastructure`, `Api`) plus two testing projects (`UnitTests`, `IntegrationTests`).
* **Frontend Scaffold (React 19 + TypeScript + Vite + Tailwind CSS v4)**: Configured with `@tailwindcss/vite` without legacy config files.
* **Vite Development Reverse Proxy**: Configured Vite dev server on port `5173` to proxy `/api` and `/hubs` to ASP.NET Core (`http://localhost:5202`).
* **Docker Compose Infrastructure**: Configured SQL Server 2022 and Azurite blob storage with persistent volumes.
* **Baseline Security Middleware Pipeline**: Strictly ordered middleware stack handling RFC 7807 `ProblemDetails`, HTTPS redirection, OWASP security headers, CORS, sliding-window rate limiting, and correlation IDs.
* **Functional Domain Primitives**: Created `Result`, `Result<T>`, `Error`, `PagedResult<T>`, and `DomainException`.

---

### 2. Deep-Dive Concepts & Real-World Analogies

#### A. Clean Architecture & The Inward Dependency Rule
* **The Concept:** High-level policy (banking rules, validation) should never depend on low-level implementation details (SQL queries, third-party libraries, cloud APIs). Instead, both depend on abstractions.
* **The Analogy:** Think of the **Domain** as the gold vault inside a bank, and the **Infrastructure/Api** as the exterior doors and courier trucks. The gold vault exists whether the courier drives a Ford or a Mercedes. If you change delivery trucks (e.g. switch SQL Server to Azure SQL or change blob storage), the vault remains untouched.
* **Rule of Thumb:** If a class in `Domain` has a `using Microsoft.Data.SqlClient;` or `using Azure.Storage;`, the boundary has been breached.

#### B. The ASP.NET Core Middleware Pipeline Order
* **The Concept:** Middleware components are executed in a sequential Russian-doll pipeline. An incoming HTTP request flows down through every registered middleware, reaches the endpoint, and the response flows back up through the same layers in reverse order.
* **The Analogy:** Think of an **airport security checkpoint**:
  1. *Global Exception Handler* is the paramedic stationed right outside the airport. If anyone faints at any point, they catch them before leaving.
  2. *Security Headers & HTTPS* is the passport verification booth ensuring official identity and tamper-proof documents.
  3. *Rate Limiting* is the queue crowd-control barrier preventing a mob from overwhelming the gates.
  4. *Authentication* verifies your boarding pass.
  5. *Authorization* checks if your boarding pass allows you into the First Class Lounge.
* **Why Order Matters:** If you place Rate Limiting *after* Authentication, an unauthenticated attacker could mount a denial-of-service attack that floods your database with credential lookups before the rate limiter ever triggers!

#### C. In-Memory JWTs & SameSite=Strict Cookies with Vite Proxy
* **The Concept:**
  * Storing tokens in browser `localStorage` leaves them permanently readable by any JavaScript script running on the page. If a third-party dependency is compromised (XSS), an attacker can extract the token.
  * To prevent this, the access token is kept in **React memory only**, while the refresh token is stored in an **`HttpOnly` cookie** that JavaScript cannot access.
  * `SameSite=Strict` tells the browser: *"Never attach this cookie to any request that originates from another website."*
* **The Vite Proxy Necessity:**
  * During local development, the React SPA runs on `http://localhost:5173` and the backend on `http://localhost:5202`. To the browser, different ports represent different origins!
  * By configuring the Vite proxy, frontend requests go to `http://localhost:5173/api/...`. The browser considers this a same-origin request and includes the `SameSite=Strict` cookie without hesitation. Vite then transparently forwards the request to port `5202`.

#### D. EF Core `SaveChangesAsync` Implicit Transactionality
* **The Concept:** Junior developers often wrap every database call in `BeginTransactionAsync()`. However, calling `await _context.SaveChangesAsync()` in EF Core already wraps all tracked entity additions, updates, and deletions in a single atomic database transaction automatically.
* **When an Explicit Transaction IS Needed:**
  * You need an explicit transaction (`using var tx = await _context.Database.BeginTransactionAsync()`) only when your workflow requires **multiple distinct calls** to `SaveChangesAsync()`, or when coordinating a database change with an external side-effect (like an outbox message or file upload confirmation) where you must roll back if the second step fails.

---

### 3. Jargon Buster (Terms You Must Know)

| Jargon | Plain-English Definition |
| :--- | :--- |
| **Dependency Inversion Principle (DIP)** | The fifth SOLID principle: High-level modules should not depend on low-level modules; both should depend on abstractions (interfaces). |
| **ProblemDetails (RFC 7807)** | An IETF standard specification for formatting HTTP machine-readable error responses in REST APIs (`type`, `title`, `status`, `detail`, `instance`). |
| **Railway-Oriented Programming (Result Pattern)** | An error-handling style where methods return a container object (`Result<T>`) indicating success or failure with an error object, avoiding throwing expensive exceptions for expected business outcomes. |
| **Sliding Window Rate Limiter** | A rate limiting algorithm that divides a time window into segments, smoothing out traffic bursts by tracking request frequency over rolling intervals. |
| **Correlation ID (`X-Correlation-ID`)** | A unique GUID attached to an HTTP request that is propagated through all logging statements and microservices to trace the exact lifecycle of a single user action. |

---

### 4. Next Step: Phase 1 Preview
In Phase 1, we will configure:
* ASP.NET Identity with real SQL Server tables (`AspNetUsers`, `AspNetRoles`).
* Refresh token hashing with token family reuse detection.
* Account lockout rules (5 failed attempts = 15-minute lock).
* In-memory access token handling on the React frontend.
* Integration test suite testing token issuance and rate limiting.
