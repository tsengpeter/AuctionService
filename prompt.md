/speckit.constitution
Create principles for a backend-first auction platform with frontend-backend separation:

## 1. Architecture & Development Priority

1.1 Backend-First Development: Prioritize ASP.NET Core 8 backend API development before React.js frontend implementation. All APIs must be fully functional, tested, and documented before frontend work begins.

1.2 NX Monorepo Structure: Use NX workspace to manage multiple projects with shared libraries. Backend is independent, frontend projects (web/mobile) share common libraries (libs/).

1.3 Complete Separation: Backend (ASP.NET Core 8 + PostgreSQL) and Frontend (React.js SPA) are completely decoupled. Backend provides only RESTful JSON APIs.

## 2. Backend Principles (Priority Phase)

2.1 Technology Stack: ASP.NET Core 8 (C#) + PostgreSQL + Entity Framework Core. Include JWT authentication, input validation, and comprehensive error handling.

2.2 API Design: Follow RESTful principles strictly. All endpoints documented in OpenAPI/Swagger format before implementation. Include endpoints: /api/auctions, /api/bid, /api/follow, /api/mybids, /api/account.

2.3 Database Schema: Design PostgreSQL schema upfront with tables for Users, Auctions, Bids, FollowedItems. Use EF Core migrations for all schema changes.

2.4 Security: Implement JWT authentication, CORS policies, rate limiting, XSS/CSRF protection following OWASP guidelines.

2.5 Testing Standards: Minimum 80% test coverage using xUnit. Include unit tests, integration tests, and API endpoint tests. All tests must pass before frontend development.

2.6 Error Handling: Standardized error responses with proper HTTP status codes (200, 201, 400, 403, 404). Implement logging with Sentry integration.

## 3. Frontend Principles (Secondary Phase)

3.1 Technology Stack: React.js + TypeScript + React Router + Ant Design + SCSS + Redux Toolkit. Build as Single Page Application (SPA). Implement RWD and accessibility (a11y).

3.2 Build Tool: Use Vite for fast development and optimized production builds.

3.3 NX Shared Libraries: Organize shared code in libs/ directory:
   - libs/api: API client and types
   - libs/hooks: Shared React hooks
   - libs/i18n: Internationalization (default: zh-TW)
   - libs/redux: Redux store and slices
   - libs/styles: SCSS toolkit
   - libs/ui: Shared UI components (Atomic Design)

3.4 Routing Structure: Use React Router for client-side routing. Implement routes: /auctions, /auctions/:id, /mybids, /follow, /history, /account

3.5 State Management: Use Redux Toolkit for global state. API calls using Fetch or Axios with proper error handling.

3.6 Testing Standards: Jest + React Testing Library for unit tests (80% coverage), Playwright for E2E tests (70% coverage).

3.7 Code Quality: ESLint + Prettier enforced on all commits.

## 4. Shared Principles

4.1 API Contract: Backend must provide complete OpenAPI specification. Frontend consumes APIs strictly according to this contract.

4.2 Internationalization: Support multiple languages with i18n, default to zh-TW (Traditional Chinese).

4.3 Performance: Backend implements pagination, caching, and query optimization. Frontend implements code splitting, lazy loading, and image optimization.

4.4 CI/CD: Use GitHub Actions with NX affected commands. Separate pipelines for backend (Azure App Service) and frontend (Vercel/Netlify/Static hosting).

4.5 Docker Support: All services containerized with Docker for consistent deployment.

4.6 Monitoring: Implement logging and error tracking with Sentry for both frontend and backend.

## 5. User Roles & Permissions

5.1 Regular Users: Can browse, track, bid on items, and manage personal accounts.

5.2 Administrators: Can manage auction items, user accounts, with backend admin endpoints.

## 6. Development Workflow

6.1 Phase 1: Backend development with complete API documentation and testing.

6.2 Phase 2: Frontend development consuming stable backend APIs.

6.3 Git Flow: Use Git flow branching strategy with feature branches.

6.4 Code Review: All changes require code review before merging.


/speckit.specify

Build an online auction platform backend API that enables users to browse, bid on, and track auction items.

Core Features (Backend API Focus):

1. Auction Management
   - Retrieve all auction items with category filtering and keyword search
   - Get detailed item information including description, current bid, end time
   - Display bid history for each item

2. Bidding System
   - Allow users to place bids with amount validation
   - Track bid status (leading, outbid)
   - Maintain complete bid history

3. Item Tracking
   - Users can add/remove items to/from watchlist
   - Retrieve user's tracked items

4. User Management
   - User authentication (JWT)
   - Profile management (name, email)
   - Password change functionality

5. Bid Records
   - Retrieve user's bid history
   - Filter records by time
   - Show current status of each bid

Focus on backend API implementation first. All APIs must follow the specifications in section 6 (API 規格). Frontend will be developed after backend APIs are stable and fully documented.


/speckit.plan

Phase 1 - Backend API Development (CURRENT PRIORITY):

Location: /apps/backend or /backend
Technology Stack:
- ASP.NET Core 8 (C#)
- PostgreSQL database
- Entity Framework Core for ORM
- JWT for authentication
- Swagger/OpenAPI for API documentation
- xUnit for testing
- Sentry for logging

Database Schema:
- Users: Id, Username, Email, PasswordHash, CreatedAt
- Auctions: Id, Title, Description, StartingPrice, CurrentBid, EndTime, CategoryId, Status
- Bids: Id, AuctionId, UserId, Amount, BidTime
- FollowedItems: Id, UserId, AuctionId, FollowedAt
- Categories: Id, Name

API Endpoints (RESTful):
- GET /api/auctions (with ?category=&search=)
- GET /api/auctions/{id}
- POST /api/bid
- GET /api/mybids
- GET /api/follow
- POST /api/follow
- DELETE /api/follow/{itemId}
- GET /api/history
- GET /api/account
- PUT /api/account
- POST /api/account/password

Testing:
- Unit tests for all services and controllers
- Integration tests for API endpoints
- Target: 80% code coverage

Deployment: Azure App Service (or equivalent) with Docker

---

Phase 2 - Frontend Development (FUTURE):

Will be implemented using NX monorepo after backend is complete:

apps/
├── web/              # React.js web app (Vite + React + TypeScript)
├── mobile/           # React.js mobile app (Vite + React + TypeScript)
└── backend/          # ASP.NET Core API

libs/
├── api/              # API client & types (generated from OpenAPI)
├── hooks/            # Shared React hooks
├── i18n/             # Internationalization (react-i18next)
├── redux/            # Redux store & slices
├── styles/           # SCSS toolkit
└── ui/               # Shared UI components (Atomic Design)

Frontend Routes (React Router v6):
- / or /auctions          # Auction list page
- /auctions/:id           # Auction detail page
- /mybids                 # My bids page
- /follow                 # Tracked items page
- /history                # Bid history page
- /account                # Account settings page

Technology Stack:
- React.js 18+ with TypeScript
- Vite for build tool (fast HMR and optimized builds)
- React Router v6 for client-side routing
- Ant Design for UI components
- Redux Toolkit for state management
- SCSS for styling
- Axios or Fetch for API calls
- React Testing Library + Jest for unit tests
- Playwright for E2E tests

Project Structure (per app):
src/
├── pages/            # Page components
├── components/       # Feature components
├── layouts/          # Layout components
├── routes/           # Router configuration
├── store/            # Redux store setup
├── services/         # API service layer
├── hooks/            # Custom hooks
├── utils/            # Utility functions
├── assets/           # Static assets
└── App.tsx           # Root component

Build & Bundle:
- Code splitting with React.lazy and Suspense
- Tree shaking for unused code elimination
- Asset optimization (images, fonts)
- Production build target: ES2020

Testing:
- Jest + React Testing Library (unit tests, 80% coverage)
- Playwright (E2E tests, 70% coverage)
- MSW (Mock Service Worker) for API mocking in tests

Deployment: 
- Vercel / Netlify for static hosting
- Or AWS S3 + CloudFront
- Environment variables for API endpoints

Frontend development will begin only after:
1. All backend APIs are implemented
2. OpenAPI documentation is complete
3. Backend tests pass with 80%+ coverage
4. Backend is deployed and stable


/speckit.tasks

/speckit.analyze Save your analyze report to `analyze-01.md`

/speckit.implement