<!--
Sync Impact Report:
- Version change: Initial → 1.0.0
- New principles established: Code Quality First, Test-Driven Development, User Experience Consistency, Performance Requirements
- Added sections: Quality Standards, Development Workflow
- Templates requiring updates:
  ✅ plan-template.md - Constitution Check section aligned
  ✅ spec-template.md - Requirements and acceptance criteria aligned with quality standards
  ✅ tasks-template.md - Task categorization reflects testing, quality, and performance principles
- Follow-up TODOs: None
-->

# AuctionService Constitution

## Core Principles

### I. Code Quality First

Every line of code MUST meet professional standards before merge:

- Code MUST be clean, readable, and self-documenting with meaningful names
- Complex logic MUST include explanatory comments describing the "why"
- SOLID principles MUST be applied: Single Responsibility, Open/Closed, Liskov Substitution,
  Interface Segregation, Dependency Inversion
- Code duplication MUST be eliminated through appropriate abstraction
- Dependencies MUST be injected, not instantiated directly
- Business logic MUST be separated from infrastructure concerns

**Rationale**: High code quality reduces technical debt, improves maintainability, and
minimizes defects. Quality is non-negotiable as it directly impacts long-term velocity and
system reliability.

### II. Test-Driven Development (NON-NEGOTIABLE)

All features MUST follow the Red-Green-Refactor cycle:

- Tests MUST be written before implementation begins
- All tests MUST fail initially (Red phase)
- Implementation proceeds only to make tests pass (Green phase)
- Code MUST be refactored for quality while maintaining passing tests (Refactor phase)
- Unit test coverage MUST exceed 80% for business logic
- Integration tests MUST validate service contracts and cross-service communication
- All tests MUST be automated and run in CI/CD pipeline

**Rationale**: TDD ensures testability by design, provides living documentation, enables
confident refactoring, and catches regressions early. This discipline is foundational to
system reliability.

### III. User Experience Consistency

All API contracts and behaviors MUST provide predictable, consistent experiences:

- API responses MUST follow consistent structure and naming conventions
- Error messages MUST be clear, actionable, and user-friendly
- HTTP status codes MUST accurately reflect operation outcomes
- Validation messages MUST specify exactly what is wrong and how to fix it
- API versioning MUST maintain backward compatibility within major versions
- Breaking changes MUST be avoided when possible; when necessary, they require major version bump

**Rationale**: Consistency reduces integration effort, minimizes confusion, and improves
developer experience for API consumers. Predictable behavior builds trust and reduces
support overhead.

### IV. Performance Requirements

System performance MUST meet defined standards under specified load:

- API endpoints MUST respond within 200ms at p95 under normal load
- Database queries MUST be optimized with appropriate indexes
- N+1 query problems MUST be eliminated through eager loading or batching
- Resource-intensive operations MUST be executed asynchronously
- Response payload size MUST be minimized through pagination and selective field loading
- Performance testing MUST be conducted for critical paths before production deployment

**Rationale**: Performance directly impacts user satisfaction and system scalability.
Proactive performance engineering prevents costly retroactive optimization and ensures
consistent service quality.

### V. Observability and Monitoring

System behavior MUST be transparent and traceable in production:

- Structured logging MUST be implemented at appropriate levels (Debug, Info, Warning, Error)
- All external calls (database, HTTP, message queue) MUST be logged with duration
- Correlation IDs MUST be propagated across service boundaries for request tracing
- Critical operations MUST emit metrics for monitoring and alerting
- Exceptions MUST be logged with full context (stack trace, input parameters, state)
- Health check endpoints MUST accurately reflect service readiness and liveness

**Rationale**: Production issues are inevitable; observability enables rapid diagnosis and
resolution. Comprehensive logging and monitoring reduce Mean Time To Resolution (MTTR) and
improve system reliability.

## Quality Standards

### Code Review Requirements

- All code MUST be peer-reviewed before merge
- Reviewers MUST verify adherence to all Core Principles
- Automated checks (linting, tests, coverage) MUST pass before human review
- Review feedback MUST be addressed before approval
- At least one approval required from a team member familiar with the affected domain

### Testing Gates

- Unit tests MUST pass with minimum 80% code coverage
- Integration tests MUST pass for all affected service contracts
- Performance tests MUST pass for endpoints with defined SLAs
- No critical or high-severity static analysis warnings allowed
- Security scans MUST show no high-risk vulnerabilities

### Documentation Requirements

- Public APIs MUST have XML documentation comments
- Complex algorithms MUST include inline comments explaining approach
- README files MUST be updated when new features affect setup or usage
- Breaking changes MUST be documented in CHANGELOG with migration guidance

## Development Workflow

### Feature Development Process

1. Feature specification created and reviewed (see spec-template.md)
2. Implementation plan developed with constitution compliance check (see plan-template.md)
3. Tests written following TDD approach
4. Implementation completed to pass tests
5. Code review conducted verifying all principles
6. Automated quality gates pass
7. Feature merged and deployed

### Branching and Versioning

- Feature branches MUST be created from main branch
- Branch naming convention: `###-feature-name` where ### is issue/ticket number
- Commits MUST be atomic and have descriptive messages
- Version format: MAJOR.MINOR.PATCH following semantic versioning
- Version bumps: MAJOR (breaking changes), MINOR (new features), PATCH (bug fixes)

### Complexity Management

- Complexity MUST be justified in plan.md when Constitution Check identifies violations
- Approved complexity MUST include mitigation strategy and technical debt tracking
- Technical debt MUST be reviewed quarterly and prioritized for resolution

## Governance

This constitution supersedes all other development practices and guidelines. All team members
MUST comply with these principles in their work.

### Amendment Process

- Amendments require documented proposal with justification
- Proposal MUST be reviewed by all team members
- Approval requires consensus or majority vote
- Approved amendments MUST include migration plan for existing code
- Constitution version MUST be incremented following semantic versioning

### Compliance and Enforcement

- All pull requests MUST verify constitution compliance in PR template
- Constitution violations MUST be addressed before merge approval
- Recurring violations indicate need for tooling, training, or process improvement
- Constitution review MUST occur at start of each project phase

### Version History

**Version**: 1.0.0 | **Ratified**: 2025-11-04 | **Last Amended**: 2025-11-04
