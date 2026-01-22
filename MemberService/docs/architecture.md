# Architecture

## Overview

MemberService follows Clean Architecture principles with four layers:

- **API Layer**: Controllers, middleware, configuration
- **Application Layer**: Use cases, DTOs, validation, services
- **Domain Layer**: Entities, value objects, domain logic, interfaces
- **Infrastructure Layer**: External concerns (database, external APIs, file system)

## Key Design Decisions

### Authentication
- JWT tokens with HS256 algorithm
- Access tokens: 15 minutes
- Refresh tokens: 7 days
- BCrypt password hashing with work factor 12
- Phone number verification (6-digit code, 5 minutes validity, Redis storage)
- Email verification (6-digit code, 5 minutes validity, Redis storage)

### ID Generation
- Snowflake IDs for distributed uniqueness
- 64-bit integers for better performance than GUIDs

### Database
- **PostgreSQL 16** with EF Core Code-First
- Async operations throughout
- Repository pattern for data access
- Production: postgres:16 (standard)
- Testing: postgres:16-alpine

### Cache & Session
- **Redis 7** for verification codes
- TTL-based automatic expiration
- Production: redis:7-alpine
- Testing: redis:7-alpine

### Validation
- FluentValidation for input validation
- Domain validation in value objects
- Phone number: E.164 format validation
- Email: RFC 5322 format validation

### Notification Services
- Email: Gmail SMTP / AWS SES
- SMS: AWS SNS / AliCloud SMS
- Verification code service with rate limiting

### Testing
- TDD approach with xUnit
- Integration tests with Testcontainers (PostgreSQL + Redis)
- 231 tests, 100% passing
- Unit tests: Moq + FluentAssertions
- Integration tests: Real PostgreSQL and Redis containers

## Dependencies

```
API -> Application -> Domain <- Infrastructure
     |                ^
     v                |
Application -> Infrastructure
```

- API depends on Application and Infrastructure
- Application depends on Domain
- Infrastructure depends on Domain
- Domain has no dependencies