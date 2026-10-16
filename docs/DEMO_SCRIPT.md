# Assessment Presentation & Demonstration Script

This script provides a step-by-step presentation narrative for demonstrating the Wealth Management Banking Customer Portal & AI Support Assistant to hiring managers and technical evaluators.

---

## 1. Introduction (2 Minutes)
* **Objective:** Establish the business context, security imperatives, and architectural philosophy.
* **Talking Points:**
  * *"Hello everyone. Today I'm presenting an enterprise banking customer portal and AI support-agent assistant built for a wealth management banking environment."*
  * *"In wealth management, systems must withstand strict regulatory compliance, protect customer PII, prevent unauthorized document access, and assist support agents with generative AI while maintaining absolute human-in-the-loop control."*
  * *"To achieve this, I architected the backend using .NET 10 Clean Architecture with strict inward dependency inversion, paired with a React 19 + TypeScript + Vite frontend."*

---

## 2. Architecture & Security Walkthrough (3 Minutes)
* **Talking Points:**
  * **Perimeter Defense:** Show the strictly ordered middleware pipeline (`ProblemDetails`, HTTPS, Security Headers, CORS, Rate Limiting, Correlation ID).
  * **Dual Pipeline Security:** Explain why file upload validation (magic bytes, double-extension blocking, private storage, SAS tokens) is completely decoupled from the AI text sanitization pipeline (database entity substitution, regex ordering for Aadhaar/Cards/Accounts, prompt boundary framing).
  * **Session Hardening:** Explain the in-memory access token + HttpOnly SameSite=Strict refresh cookie architecture with token family reuse detection.

---

## 3. Live Functional Demonstration (10 Minutes)
*(Will be expanded with step-by-step clicks as each functional phase is implemented: Customer Onboarding -> Product Application -> KYC Upload -> Officer Review & Claim -> Approval Notification -> Support Ticketing & AI Co-Pilot Assistant).*
