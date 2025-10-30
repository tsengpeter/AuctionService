<!--
SYNC IMPACT REPORT
==================
Version Change: INITIAL → 1.0.1
Constitution Type: New Constitution (Initial Ratification)

Principles Defined:
- I. Backend-First Development (Architecture priority)
- II. Complete Frontend-Backend Separation (Decoupling)
- III. NX Monorepo Structure (Organization)
- IV. RESTful API Design (Backend standard)
- V. Test-First with 80% Coverage (NON-NEGOTIABLE)
- VI. Security & Authentication (Backend requirement)
- VII. Shared Libraries via NX (Frontend organization)
- VIII. API Contract Compliance (Integration standard)
- IX. Internationalization (i18n default: zh-TW)
- X. Performance & Optimization (Cross-cutting)

Added Sections:
+ Technology Stack Standards
+ User Roles & Permissions
+ Development Workflow & Phases
+ Quality Gates & Testing Standards
+ Deployment & CI/CD Requirements

Templates Requiring Updates:
✅ plan-template.md - Constitution Check section validated
✅ spec-template.md - Requirements alignment validated
✅ tasks-template.md - Task categorization validated

Version 1.0.1 Changes (PATCH):
- Changed frontend framework from Next.js to React (technology clarification)
- Updated deployment target from Vercel/Netlify to standard static hosting
- No principle changes, only technology stack refinement

Follow-up TODOs:
- None - All placeholders filled

-->

# AuctionService Constitution

## Core Principles

### I. Backend-First Development

**MUST** prioritize ASP.NET Core 8 backend API development before React frontend implementation.

**Rationale**: Backend APIs serve as the foundation and contract for all frontend implementations. Building backend-first ensures:
- Clear API contracts before frontend work begins
- No frontend rework due to backend changes
- Independent testing and validation of business logic
- Parallel frontend team ramp-up during backend development

**Requirements**:
- All backend APIs MUST be fully functional before frontend work starts
- All backend APIs MUST have passing tests (minimum 80% coverage)
- All backend APIs MUST be documented in OpenAPI/Swagger format
- Frontend development blocked until backend Phase 1 complete

### II. Complete Frontend-Backend Separation

**MUST** maintain complete decoupling between backend (ASP.NET Core 8) and frontend (React).

**Rationale**: Separation enables independent scaling, deployment, technology evolution, and team organization.

**Requirements**:
- Backend provides ONLY RESTful JSON APIs (no server-side rendering)
- Frontend consumes ONLY backend APIs (no direct database access)
- Backend and frontend deploy independently
- Communication exclusively via HTTP/HTTPS RESTful endpoints
- CORS policies explicitly configured on backend

### III. NX Monorepo Structure

**MUST** use NX workspace to manage multiple projects with shared libraries.

**Rationale**: NX provides computation caching, dependency graph analysis, and efficient builds for monorepos with multiple frontend projects.

**Requirements**:
- Backend projects are independent (not managed by NX)
- Frontend projects (web/mobile) organized under NX workspace
- Shared code MUST reside in `libs/` directory with clear purposes:
  - `libs/api`: API client and TypeScript types
  - `libs/hooks`: Shared React hooks
  - `libs/i18n`: Internationalization resources
  - `libs/redux`: Redux store and slices
  - `libs/styles`: SCSS toolkit and design tokens
  - `libs/ui`: Shared UI components (Atomic Design)
- NX affected commands used in CI/CD pipelines

### IV. RESTful API Design (NON-NEGOTIABLE)

**MUST** follow RESTful principles strictly for all backend endpoints.

**Rationale**: RESTful design ensures predictable, maintainable, and industry-standard APIs.

**Requirements**:
- All endpoints documented in OpenAPI/Swagger BEFORE implementation
- Standard HTTP verbs: GET (read), POST (create), PUT/PATCH (update), DELETE (remove)
- Proper HTTP status codes: 200 (OK), 201 (Created), 400 (Bad Request), 401 (Unauthorized), 403 (Forbidden), 404 (Not Found), 500 (Server Error)
- Resource-oriented URLs: `/api/auctions`, `/api/auctions/{id}`, `/api/bids`, `/api/follow`, `/api/mybids`, `/api/account`
- Pagination for list endpoints (page, pageSize parameters)
- Filtering and sorting via query parameters

### V. Test-First with 80% Coverage (NON-NEGOTIABLE)

**MUST** write tests before implementation with minimum 80% code coverage.

**Rationale**: Test-first development catches bugs early, serves as living documentation, and ensures maintainability.

**Requirements**:
- **Backend (xUnit)**:
  - Unit tests for services and business logic
  - Integration tests for database operations
  - API endpoint tests for all controllers
  - Minimum 80% coverage across all backend projects
  - All tests MUST pass before merging
- **Frontend (Jest + Playwright)**:
  - Jest for unit tests (80% coverage target)
  - Playwright for E2E tests (70% coverage target)
  - Component tests for shared UI libraries
- Red-Green-Refactor cycle: Write test → Test fails → Implement → Test passes → Refactor

### VI. Security & Authentication

**MUST** implement comprehensive security following OWASP guidelines.

**Rationale**: Auction platforms handle sensitive user data and financial transactions requiring robust security.

**Requirements**:
- JWT authentication for all protected endpoints
- CORS policies explicitly configured (whitelist allowed origins)
- Rate limiting on API endpoints (prevent DDoS/brute force)
- Input validation on all endpoints (prevent injection attacks)
- XSS protection (sanitize user inputs, set security headers)
- CSRF protection (anti-forgery tokens)
- HTTPS enforced in production
- Password hashing with industry-standard algorithms (bcrypt/Argon2)
- Logging integration with Sentry for security events

### VII. Shared Libraries via NX

**MUST** organize all shared frontend code in NX libraries under `libs/` directory.

**Rationale**: Shared libraries promote code reuse, consistency, and maintainability across multiple frontend applications.

**Requirements**:
- Each library MUST have a clear, single purpose
- Libraries MUST be independently testable
- Follow Atomic Design for UI components (atoms, molecules, organisms, templates, pages)
- TypeScript strict mode enabled for all libraries
- ESLint + Prettier enforced on all library code
- No circular dependencies between libraries

### VIII. API Contract Compliance

**MUST** strictly adhere to API contracts defined by backend OpenAPI specification.

**Rationale**: Contract compliance prevents integration bugs and enables independent development.

**Requirements**:
- Backend MUST provide complete OpenAPI specification before frontend development
- Frontend MUST consume APIs strictly according to OpenAPI contract
- TypeScript types auto-generated from OpenAPI spec (using tools like openapi-typescript)
- Contract tests verify backend implementation matches specification
- Breaking changes require MAJOR version bump and migration plan
- No frontend workarounds for backend contract violations

### IX. Internationalization (i18n)

**MUST** support multiple languages with Traditional Chinese (zh-TW) as default.

**Rationale**: i18n enables global reach and serves primary Taiwanese user base.

**Requirements**:
- Default language: Traditional Chinese (zh-TW)
- All user-facing text externalized to i18n resource files
- Backend error messages localized based on Accept-Language header
- Frontend uses i18n library in `libs/i18n` for translations
- Currency formatting and date/time localization
- RTL (Right-to-Left) support deferred to future requirement

### X. Performance & Optimization

**MUST** implement performance optimizations for both backend and frontend.

**Rationale**: Auction platforms require real-time responsiveness and handle high traffic during bidding.

**Requirements**:
- **Backend**:
  - Pagination on all list endpoints
  - Database query optimization (indexes, query analysis)
  - Caching strategy (in-memory, distributed cache for hot data)
  - Response time SLA: p95 < 200ms for read endpoints, p95 < 500ms for write endpoints
- **Frontend**:
  - Code splitting and lazy loading
  - Image optimization (WebP format, responsive images)
  - Bundle size monitoring (max 300KB initial load)
  - Lighthouse score targets: Performance > 90, Accessibility > 90

## Technology Stack Standards

### Backend Technology Stack (NON-NEGOTIABLE)

- **Framework**: ASP.NET Core 8 (C#)
- **Database**: PostgreSQL (latest stable)
- **ORM**: Entity Framework Core
- **Authentication**: JWT tokens
- **API Documentation**: OpenAPI/Swagger
- **Testing**: xUnit
- **Logging**: Sentry integration
- **Containerization**: Docker

### Frontend Technology Stack (NON-NEGOTIABLE)

- **Framework**: React (latest stable) with React Router
- **Language**: TypeScript (strict mode)
- **UI Library**: Ant Design
- **Styling**: SCSS
- **State Management**: Redux Toolkit
- **HTTP Client**: Fetch API
- **Testing**: Jest (unit), Playwright (E2E)
- **Code Quality**: ESLint + Prettier
- **Monorepo**: NX
- **Build Tool**: Vite or Webpack

## User Roles & Permissions

### Regular Users

**Capabilities**:
- Browse auction items (unauthenticated)
- Create account and authenticate
- Track/follow auction items
- Place bids on active auctions
- View personal bid history
- Manage account settings

### Administrators

**Capabilities** (all Regular User capabilities plus):
- Create/update/delete auction items via backend admin endpoints
- Manage user accounts (suspend, delete)
- View system-wide analytics and reports
- Access audit logs

**Security**: Admin role enforced via JWT claims; admin endpoints protected by role-based authorization.

## Development Workflow & Phases

### Phase 1: Backend Development (BLOCKING)

**Goal**: Deliver complete, tested, documented backend APIs.

**Deliverables**:
- PostgreSQL database schema with EF Core migrations
- All API endpoints implemented and tested
- OpenAPI/Swagger documentation complete
- JWT authentication functional
- 80% test coverage achieved
- Backend deployed to staging environment

**Exit Criteria**: All backend tests passing, API documentation approved.

### Phase 2: Frontend Development (DEPENDENT)

**Goal**: Build user interface consuming stable backend APIs.

**Prerequisites**: Phase 1 complete.

**Deliverables**:
- NX workspace configured with shared libraries
- React application with routing (React Router)
- Frontend pages implemented per routing structure
- Redux state management integrated
- API client generated from OpenAPI spec
- 80% Jest test coverage, 70% E2E coverage
- Frontend deployed to staging environment

**Exit Criteria**: All frontend tests passing, user acceptance testing complete.

## Quality Gates & Testing Standards

### Code Review (MANDATORY)

- All changes MUST go through pull request review
- Minimum one approving review required
- Automated checks MUST pass:
  - All tests passing (backend xUnit, frontend Jest/Playwright)
  - Code coverage thresholds met (80% backend, 80% Jest, 70% E2E)
  - ESLint/Prettier passing (frontend)
  - Build successful
  - No security vulnerabilities detected

### Git Flow Branching Strategy

- `master`: Production-ready code
- `develop`: Integration branch for features
- `feature/###-feature-name`: Feature branches (### = issue number)
- `hotfix/###-description`: Emergency production fixes
- `release/vX.Y.Z`: Release preparation branches

## Deployment & CI/CD Requirements

### CI/CD Pipelines (GitHub Actions)

**Backend Pipeline**:
- Triggered on push to `develop`, `master`, or `feature/*` branches
- Steps: Restore → Build → Test → Code coverage → Docker build → Deploy to Azure App Service (staging/production)

**Frontend Pipeline**:
- Triggered on push to `develop`, `master`, or `feature/*` branches
- Uses NX affected commands (only build/test changed projects)
- Steps: Install → NX affected:lint → NX affected:test → NX affected:build → Deploy to static hosting (staging/production)

### Deployment Targets

- **Backend**: Azure App Service (containerized ASP.NET Core)
- **Frontend**: Static hosting (Azure Static Web Apps, Netlify, or similar)
- **Database**: Azure Database for PostgreSQL
- **Monitoring**: Sentry for error tracking (backend + frontend)

## Governance

This constitution supersedes all other development practices. All pull requests and code reviews MUST verify compliance with these principles.

### Amendment Process

1. Proposed amendments MUST be documented with rationale
2. Impact analysis on existing code and templates MUST be performed
3. Approval required from technical lead or architecture team
4. Version bump according to semantic versioning:
   - **MAJOR**: Backward-incompatible governance changes or principle removals
   - **MINOR**: New principles added or materially expanded guidance
   - **PATCH**: Clarifications, wording fixes, non-semantic refinements
5. Migration plan required for breaking changes
6. Sync updates to all templates in `.specify/templates/`

### Compliance Review

- All feature specifications MUST reference applicable principles
- Implementation plans MUST include "Constitution Check" section verifying compliance
- Complexity or deviations MUST be explicitly justified in plan documentation

**Version**: 1.0.1 | **Ratified**: 2025-10-30 | **Last Amended**: 2025-10-30
