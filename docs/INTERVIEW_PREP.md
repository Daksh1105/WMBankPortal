# Technical Interview & Viva Preparation Guide

This living document contains real technical viva questions, expected architectural answers, and defensive rationale tailored for banking, wealth management, and senior .NET evaluator interviews.

---

## Phase 0 Viva Questions

### Q1: Why did you choose Clean Architecture over a traditional 3-Tier (UI-BLL-DAL) architecture for this banking portal?
* **Candidate Answer:**
  * *"In a traditional 3-tier architecture, dependencies flow top-to-bottom: UI depends on Business Logic, and Business Logic depends on the Data Access Layer (DAL). This tightly couples core business logic to the database schema and ORM libraries.*
  * *In banking and wealth management, business rules (underwriting criteria, KYC rules, state transitions) must outlive infrastructure changes. By using Clean Architecture with the Dependency Inversion Principle, our Domain and Application layers have zero dependencies on SQL Server or external cloud SDKs. All dependencies point inward.*
  * *This enables us to unit test 100% of our business rules in memory without spinning up a database, and gives the enterprise the agility to swap cloud storage providers (e.g. Azure Blob to AWS S3) or database engines without touching a single line of business logic."*

---

### Q2: What is the security vulnerability associated with storing JWT access tokens in browser `localStorage`, and how did your architecture address it?
* **Candidate Answer:**
  * *"Any data stored in `localStorage` or `sessionStorage` is accessible to any JavaScript running on the origin. If the application has a Cross-Site Scripting (XSS) vulnerability or an insecure third-party npm package, an attacker can execute `localStorage.getItem('token')` and exfiltrate persistent credentials.*
  * *In our architecture, we implement defense-in-depth:*
    1. *The short-lived JWT Access Token (15 minutes) is held exclusively in **React JavaScript memory** (in React state). It is never written to disk or browser storage, meaning an attacker cannot exfiltrate it across browser sessions.*
    2. *The Refresh Token is stored inside an **`HttpOnly`, `Secure`, `SameSite=Strict` cookie**. Because it is marked `HttpOnly`, browser scripts cannot access it.*
    3. *During local development, we configure the Vite dev proxy so that frontend calls to `/api` appear same-origin, ensuring `SameSite=Strict` cookies are transmitted cleanly without cross-site drops."*

---

### Q3: Why is the sequence of middleware in ASP.NET Core critical? What happens if you register Rate Limiting after Authorization?
* **Candidate Answer:**
  * *"ASP.NET Core middlewares execute in a strict sequential Russian-doll pipeline for both request and response phases.*
  * *If Rate Limiting were placed after Authorization, an unauthenticated attacker could launch a Denial of Service (DoS) or credential brute-force attack by flooding the server with requests. Because Authorization and Authentication execute first, the server would spend expensive CPU and database cycles validating tokens and querying user tables on every single malicious request before the rate limiter ever evaluated the caller's IP.*
  * *By placing Global Exception Handling, HTTPS Redirection, Security Headers, CORS, and Rate Limiting at the very beginning of the pipeline, we drop malicious or abusive traffic at the perimeter before any business or database resources are consumed."*

---

### Q4: Does EF Core `SaveChangesAsync` require an explicit transaction? When would you use `Database.BeginTransactionAsync()`?
* **Candidate Answer:**
  * *"No, `SaveChangesAsync()` does not need an explicit transaction for single-context calls. By default, EF Core wraps every call to `SaveChangesAsync()` in an implicit database transaction. If any entity fails validation or constraints, the entire batch of inserts, updates, and deletes is automatically rolled back.*
  * *You only use `Database.BeginTransactionAsync()` when:*
    1. *You have a multi-step business workflow involving **multiple calls to `SaveChangesAsync()`** that must succeed or fail together.*
    2. *You are coordinating changes across multiple different `DbContext` instances.*
    3. *You are coordinating database state changes with external side-effects (such as writing to an outbox table or verifying a file storage operation) where failure in step 2 requires rolling back step 1."*

---

### Q5: Why do banking APIs return RFC 7807 ProblemDetails instead of standard JSON exception messages?
* **Candidate Answer:**
  * *"RFC 7807 specifies a standard HTTP ProblemDetails format (`type`, `title`, `status`, `detail`, `instance`). In enterprise banking systems:*
    1. *Returning raw stack traces or internal exception details is an information disclosure vulnerability (OWASP A01/A05) that reveals database schemas and library versions to attackers.*
    2. *ProblemDetails provides a consistent, machine-readable schema for frontends and API consumers to handle validation errors, concurrency conflicts, and state transition errors uniformly.*
    3. *It allows us to inject operational metadata like `correlationId` into the error response so clients can quote the exact tracking ID to customer support without exposing internal diagnostic logs."*

---

## Phase 1 Viva Questions (Authentication & Token Security)

### Q6: Why do we store only the SHA-256 hash of a refresh token in the database rather than the raw token string?
* **Candidate Answer:**
  * *"A refresh token is a high-privilege bearer credential with a long lifetime (e.g. 7 days) that can mint new access tokens on demand. If stored in plaintext, any database breach, unencrypted backup, read replica leak, or SQL injection vulnerability would compromise every active session across the entire customer base.*
  * *By hashing the token with SHA-256 before writing it to SQL Server, we guarantee that the database stores only one-way cryptographic digests. When the client sends the cookie, our service hashes the incoming token on the fly and queries the hash.*
  * *Even if an adversary dumps the entire `RefreshTokens` table, they cannot reverse the SHA-256 hashes back into raw tokens to authenticate against the API."*

---

### Q7: How does token rotation coupled with Token Family Tracking detect token compromise and protect the user?
* **Candidate Answer:**
  * *"In token rotation, each refresh token is single-use. When exchanged at `/refresh`, the old token is marked revoked and replaced with a new one.*
  * *We link all tokens issued during an active session under a single `FamilyId`. If an attacker intercepts a refresh token and tries to use it after the legitimate user has already refreshed, the server encounters an already-revoked token.*
  * *This is a cryptographic smoking gun indicating token cloning or a replay attack. The server immediately queries all active tokens sharing that `FamilyId` and revokes them in a single transaction. This invalidates both the attacker's and the legitimate user's session instantly, preventing further unauthorized access and forcing re-authentication."*

---

### Q8: Why did you place `ApplicationUser` inside `WMBankPortal.Infrastructure` rather than `WMBankPortal.Domain`?
* **Candidate Answer:**
  * *"In Clean Architecture, the `Domain` layer must be pure and free of external framework dependencies. `ApplicationUser` inherits from `IdentityUser<Guid>`, which is part of Microsoft's `Microsoft.AspNetCore.Identity.EntityFrameworkCore` package.*
  * *If `ApplicationUser` lived in `Domain`, the entire domain would take a transitive dependency on ASP.NET Core and EF Core, violating the Dependency Inversion Principle.*
  * *Instead, the `Domain` layer represents users strictly by their fundamental identifier (`Guid UserId`). The `Infrastructure` layer manages the Identity implementation, while the `Application` layer defines abstractions like `IAuthService` and `IAppDbContext`."*

---

### Q9: How do you protect against user enumeration and timing attacks during the login process?
* **Candidate Answer:**
  * *"User enumeration occurs when an API reveals whether an email exists in the system (e.g., returning 'Email not registered' vs 'Incorrect password'). Attackers use this to harvest valid customer emails.*
  * *In our `AuthService`:*
    1. *We return the exact same generic error message (`Invalid email or password`) whether the user was not found, the password was wrong, or the account is locked out.*
    2. *We always run password verification logic (or a dummy hash calculation) so response times are indistinguishable, preventing attackers from inferring valid accounts through timing discrepancies.*
    3. *We enforce account lockout after 5 consecutive failed attempts, preventing automated dictionary and brute-force attacks."*

---

## Phase 2 Viva Questions (Profile, Dynamic Applications & IDOR)

### Q10: What is an Insecure Direct Object Reference (IDOR), and how is it prevented in your banking application endpoints?
* **Candidate Answer:**
  * *"An IDOR vulnerability occurs when an application exposes a database entity identifier directly in a URL (e.g., `GET /api/v1/applications/{id}`) and fails to verify that the requesting user has the right to access that specific entity.*
  * *To prevent IDOR:*
    1. *We use high-entropy random GUIDs rather than predictable auto-incrementing integers, stopping sequential ID harvesting.*
    2. *In the service layer (`ApplicationService`), we inspect the authenticated caller's claims (`User.GetUserId()`) and match it directly against `application.CustomerId`.*
    3. *If a customer attempts to query or manipulate another customer's application, we immediately return **HTTP 403 Forbidden**.*
    4. *We test this rigorously in our integration test suite (`Phase2IntegrationTests.SubmitApplication_And_IdorProtection_EnforcesStrictCustomerBoundary`), proving that Customer B cannot view, inspect the timeline of, or withdraw Customer A's application."*

---

### Q11: Why did you model banking applications using a Domain State Machine rather than allowing unrestricted status updates?
* **Candidate Answer:**
  * *"Financial applications must adhere to strict regulatory underwriting lifecycles. Allowing arbitrary status changes (e.g., setting an application directly from `Submitted` to `Approved` without officer review) is a critical compliance violation.*
  * *By implementing an explicit domain state machine:*
    1. *All status transitions are governed by encapsulated methods on the `BankingApplication` entity (`Claim`, `RequestKyc`, `Approve`, `Reject`, `Withdraw`).*
    2. *Illegal transitions throw an `InvalidStateTransitionException`, returning a clean HTTP 409 Conflict/422 Unprocessable Entity ProblemDetails response.*
    3. *Every single valid transition automatically appends an immutable `ApplicationTimeline` entry recording the previous state, new state, user ID, role, and mandatory underwriting remarks, generating an audit trail."*

---

### Q12: How does your dynamic form schema architecture work, and why is it preferred over schema migrations for every product?
* **Candidate Answer:**
  * *"In a wealth management enterprise, new financial products (e.g., specialized ESG loans, credit card tiers, fixed deposits) are introduced frequently, each requiring distinct application inputs.*
  * *Adding new relational columns to `BankingApplications` for every new product would create schema bloat, thousands of sparse NULL columns, and risky, frequent database schema migrations in production.*
  * *Instead, each `BankingProduct` stores a JSON form schema (`nvarchar(max)`) defining required input types, labels, dropdown options, and validation bounds. The frontend dynamically generates the user interface from this schema, and the submitted response is stored in `FormValuesJson` alongside core indexed financial columns like `RequestedAmount` and `Status`."*


