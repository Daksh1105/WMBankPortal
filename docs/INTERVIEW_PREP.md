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
