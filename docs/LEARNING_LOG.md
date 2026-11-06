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

### 4. Next Step: Phase 1 Preview (Completed)
In Phase 1, we configured ASP.NET Identity, refresh token family rotation, account lockout, and frontend auth state.

---

## Phase 1: Authentication, ASP.NET Core Identity & Token Security

### 1. What We Built
* **ASP.NET Core Identity with Guid Keys**: Extended `IdentityUser<Guid>` and `IdentityRole<Guid>` strictly inside `Infrastructure/Identity`, preserving Clean Architecture boundaries so `Domain` has zero identity framework dependencies.
* **Cryptographically Hashed Refresh Tokens**: Domain entity `RefreshToken` with raw tokens generated via `RandomNumberGenerator`, hashed with SHA-256 before storage in SQL Server.
* **Token Family Tracking & Compromise Detection**: Each login session creates a `FamilyId`. If an already-rotated or revoked token is replayed (indicating credential theft), all active tokens across that entire family are instantly revoked.
* **Lockout Protection**: Configured 5 failed login attempts resulting in an automatic 15-minute lockout via ASP.NET Core Identity's `UserManager.AccessFailedAsync`.
* **Cookie-Based Security**: Refresh tokens are issued exclusively via `HttpOnly`, `SameSite=Strict`, `Secure` cookies scoped to `/api/v1/auth`.
* **Silent Concurrent Refresh in React**: Frontend `apiClient` Axios interceptor catches `401 Unauthorized`, queues simultaneous outgoing requests, silently requests a new JWT via `/api/v1/auth/refresh`, and replays queued requests without user disruption.
* **Automated Database Migration & Demo Seeding**: `AppDbContext` automatically applies pending migrations and seeds 4 role accounts (`customer`, `officer`, `agent`, `admin`) on development startup.
* **Unit & Integration Test Coverage**: Added tests for FluentValidation rules, `RefreshToken` domain logic, registration, login, account lockout after 5 attempts, and cookie rotation.

---

### 2. Deep-Dive Concepts & Real-World Analogies

#### A. Storing Hashes Instead of Plaintext Tokens
* **The Concept:** A refresh token is a bearer credential equivalent in power to a password for the duration of its lifespan (e.g. 7 days). If an attacker manages to obtain a database dump or read replica backup through SQL injection or insider threat, having plaintext refresh tokens allows them to impersonate every active user on the banking portal.
* **The Solution:** We generate a high-entropy 64-character cryptographic token (`RandomNumberGenerator.GetBytes(64)`), compute its SHA-256 hash, and store *only the hash* in the database. When the client sends back the raw token cookie, we hash it on the fly and look up the hash. If the database is compromised, the attacker only gets irreversible hashes.
* **The Analogy:** Think of a **high-end hotel coat check**. The attendant gives you a numbered claim ticket. In the logbook behind the counter, the hotel writes down a secret barcode that can only be matched by scanning your physical ticket. If a thief breaks in and steals the logbook, they cannot produce a counterfeit claim ticket to steal your coat.

#### B. Refresh Token Rotation & Token Family Reuse Detection
* **The Concept:** Token rotation means that every time a refresh token is used to obtain a new access token, the old refresh token is marked as `Revoked` and replaced with a newly generated refresh token.
* **What Happens During a Replay Attack?**
  1. An attacker intercepts Refresh Token #1 from a user.
  2. The legitimate user's browser uses Refresh Token #1 to get Refresh Token #2. Refresh Token #1 is now revoked.
  3. The attacker later attempts to present Refresh Token #1 to the `/refresh` endpoint.
  4. The system detects that Refresh Token #1 has *already been revoked*. This is an unambiguous signal that a token has been cloned or leaked!
  5. The server looks up the `FamilyId` of the token and **instantly revokes all tokens** belonging to that family, terminating the attacker's and the user's active session and forcing re-authentication.
* **The Analogy:** Think of a **relay race baton**. When Runner A hands the baton to Runner B, Baton A is destroyed. If someone suddenly shows up at the finish line carrying Baton A, the race marshals know someone stole a discarded baton. The entire team is immediately disqualified.

#### C. Account Lockout Mechanics & Timing Attack Prevention
* **The Concept:** Automated bots attempt brute-force dictionary attacks against banking login endpoints. Without lockout protection, a distributed botnet can test thousands of passwords per second.
* **The Implementation:** We enable Identity's `DefaultLockoutTimeSpan = 15 minutes` and `MaxFailedAccessAttempts = 5`. Furthermore, when a login fails due to an invalid password or non-existent user, we return an identical generic error message (`Invalid credentials or account locked`) to prevent **user enumeration** (where an attacker probes to see which emails are registered).

#### D. Frontend Silent Concurrent Refresh Queue
* **The Concept:** In modern SPAs, a page load might trigger 5 API calls in parallel (e.g. `getProfile`, `getApplications`, `getNotifications`, `getAccounts`, `getTickets`). If the access token has expired, all 5 requests fail with `401 Unauthorized` simultaneously.
* **The Problem:** If the frontend blindly attempts to refresh for each 401, 5 concurrent `/refresh` requests hit the backend. Due to token rotation, only the first request succeeds; the other 4 will trigger token reuse detection and lock the user out!
* **The Solution:** We implement a **promise queue** in `frontend/src/api/client.ts`. The first 401 sets `isRefreshing = true` and fires a single `/refresh` request. The remaining 4 requests are placed into a pending promise queue. Once the refresh completes, the queue is resolved with the new access token, and all original requests are replayed seamlessly.

---

### 3. Jargon Buster (Terms You Must Know)

| Jargon | Plain-English Definition |
| :--- | :--- |
| **PBKDF2 / Argon2** | Cryptographic key derivation functions that use salt, iterations, and memory-hardness to hash passwords so they cannot be cracked with GPU rainbow tables. |
| **ClaimsPrincipal** | The .NET runtime object representing the authenticated user, containing claims (Subject `sub`, Email, Roles, etc.) extracted from the decrypted JWT payload. |
| **Token Rotation** | The practice of issuing a brand-new refresh token every time one is exchanged, invalidating the previous one. |
| **Token Family** | A lineage identifier (`FamilyId`) linking all refresh tokens issued within a single authenticated session across consecutive rotations. |
| **SameSite=Strict** | A browser cookie attribute instructing the browser never to include the cookie in cross-site requests, completely preventing Cross-Site Request Forgery (CSRF). |
| **HttpOnly Flag** | A browser cookie attribute preventing client-side JavaScript (`document.cookie`) from reading or modifying the cookie, neutralizing XSS credential theft. |

---

### 4. Next Step: Phase 2 Preview
In Phase 2, we will configure:
* **Customer Profile Management**: 1:1 relationship with `AspNetUsers`, personal information, address, and demographics.
* **Banking Product Catalog**: Predefined financial products (Savings, Credit Card, Personal Loan, Fixed Deposit) with configurable dynamic JSON schemas.
* **Dynamic Product Applications**: Strongly-typed `BankingApplication` entity with concurrency `RowVersion`, collision-safe `ApplicationNumber` generation (`APP-yyyyMMdd-XXXXXXXX`), and dynamic form response validation.
* **Resource-Based Authorization**: Ensuring customers can only query and manipulate their own applications (IDOR defense).
* **React Dynamic Form Engine**: Dynamically rendering UI form controls based on the product's JSON schema with client-side validation.
