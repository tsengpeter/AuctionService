# Architecture Documentation

## Overview

The BiddingService is a high-performance microservice designed to handle auction bidding operations. It follows Clean Architecture principles to ensure maintainability, testability, and scalability.

## Clean Architecture

The service is organized into four main layers:

```
┌─────────────────────────────────────┐
│           BiddingService.Api        │  ← Web API, Controllers, Middleware
├─────────────────────────────────────┤
│          BiddingService.Core        │  ← Domain Logic, Entities, Interfaces
├─────────────────────────────────────┤
│     BiddingService.Infrastructure   │  ← Data Access, External Services
├─────────────────────────────────────┤
│        BiddingService.Shared        │  ← Common Utilities, Constants
└─────────────────────────────────────┘
```

### Layer Responsibilities

#### API Layer (`BiddingService.Api`)
- ASP.NET Core Web API controllers
- Request/response models (DTOs)
- Middleware configuration
- Authentication/authorization (planned)
- API documentation (Swagger/OpenAPI)

#### Core Layer (`BiddingService.Core`)
- Domain entities (`Bid`, `AuctionInfo`)
- Business logic services (`BiddingService`)
- Interfaces for external dependencies
- Value objects (`BidAmount`, `BidId`)
- Custom exceptions
- DTOs for requests and responses

#### Infrastructure Layer (`BiddingService.Infrastructure`)
- Data access implementations (`BidRepository`, `RedisRepository`)
- External service clients (`AuctionServiceClient`)
- Background services (`RedisSyncWorker`, `RedisHealthCheckService`)
- Database migrations and configuration
- Caching implementations
- Encryption services

#### Shared Layer (`BiddingService.Shared`)
- Common utilities and helpers
- Constants and enumerations
- Extension methods
- Logging configurations

## Key Design Patterns

### Repository Pattern
- `IBidRepository`: Abstracts data access for bids
- `IRedisRepository`: Abstracts Redis operations
- Enables easy testing and dependency injection

### Service Layer Pattern
- `IBiddingService`: Contains business logic
- Orchestrates operations across repositories
- Handles validation and error management

### Dependency Injection
- All dependencies are injected through constructors
- Enables loose coupling and testability
- Configuration in `Program.cs`

### Background Services
- `RedisSyncWorker`: Handles Redis-to-PostgreSQL synchronization
- `RedisHealthCheckService`: Monitors Redis connectivity
- Implements `IHostedService` for lifecycle management

## Data Flow

### Bid Submission Flow
```
1. API Controller receives POST /api/bids
2. BiddingService validates auction and bid
3. RedisRepository places bid atomically (Lua script)
4. BidRepository saves to PostgreSQL (async)
5. Response returned to client
```

### Bid Query Flow
```
1. API Controller receives GET request
2. BiddingService orchestrates data retrieval
3. RedisRepository provides fast access for hot data
4. BidRepository provides complete historical data
5. AuctionServiceClient provides auction metadata
6. Aggregated response returned
```

## Caching Strategy

### Redis Usage
- **Primary Cache**: Bid data with atomic operations via Lua scripts
- **Priority Caching**: Highest bids cached with fallback mechanisms
- **Dead Letter Queue**: Failed sync operations queued for retry
- **Batch Operations**: Auction data cached with TTL

### Cache Invalidation
- No explicit invalidation - data is immutable
- TTL-based expiration for stale data
- Background sync ensures eventual consistency

## Database Design

### PostgreSQL Schema
```sql
-- Bids table with encrypted sensitive fields
CREATE TABLE bids (
    bid_id BIGINT PRIMARY KEY,
    auction_id BIGINT NOT NULL,
    bidder_id_encrypted BYTEA NOT NULL,
    bidder_id_hash VARCHAR(64) NOT NULL,
    amount_encrypted BYTEA NOT NULL,
    bid_at TIMESTAMP NOT NULL,
    synced_from_redis BOOLEAN DEFAULT FALSE
);

-- Indexes for performance
CREATE INDEX idx_bids_auction_id ON bids(auction_id);
CREATE INDEX idx_bids_bidder_hash ON bids(bidder_id_hash);
CREATE INDEX idx_bids_bid_at ON bids(bid_at);
```

### Encryption Strategy
- AES-256-GCM for sensitive fields (`bidder_id`, `amount`)
- Separate encryption keys for different data types
- Secure key management via environment variables

## Performance Considerations

### Response Time Targets
- Bid submission: <100ms
- Highest bid query: <50ms
- Bid history: <200ms (with pagination)
- Auction stats: <300ms (with aggregation)

### Optimization Techniques
- Redis Lua scripting for atomic operations
- Database indexing on query fields
- Asynchronous processing for non-critical paths
- Connection pooling for database and Redis
- Background synchronization to reduce latency

## Error Handling

### Exception Hierarchy
- `AuctionNotFoundException`: Auction doesn't exist
- `AuctionNotActiveException`: Auction is not active
- `BidAmountTooLowException`: Bid is below requirements
- `DuplicateBidException`: User already bid on auction
- `BidNotFoundException`: Specific bid not found

### Error Response Format
```json
{
  "error": {
    "code": "BID_AMOUNT_TOO_LOW",
    "message": "Bid amount 50.00 is too low. Current highest bid is 100.00",
    "details": {
      "currentHighestBid": 100.00,
      "bidAmount": 50.00
    }
  }
}
```

## Monitoring and Observability

### Health Checks
- Redis connectivity
- Database connectivity
- External service availability

### Logging
- Structured logging with Serilog
- Correlation IDs for request tracing
- Different log levels (Debug, Info, Warning, Error)

### Metrics (Planned)
- Bid request count and latency
- Cache hit/miss ratios
- Database query performance
- Background job success/failure rates

## Security Considerations

### Data Protection
- Encryption of PII (bidder IDs, amounts)
- Input validation and sanitization
- SQL injection prevention via EF Core
- XSS protection via proper encoding

### Authentication (Planned)
- JWT token validation
- Role-based access control
- API key authentication for service-to-service calls

## Testing Strategy

### Unit Tests
- Test individual components in isolation
- Mock external dependencies
- Cover business logic and validation

### Integration Tests
- Test with real databases (Testcontainers)
- Verify component interactions
- Test external service contracts

### Test Coverage Goals
- Core business logic: >90%
- API endpoints: >80%
- Infrastructure components: >70%

## Deployment Architecture

### Containerization
- Multi-stage Docker builds
- Minimal runtime images
- Health check configurations

### Environment Configuration
- Environment-specific settings
- Secret management
- Configuration validation

### Scaling Considerations
- Horizontal scaling with Redis clustering
- Database read replicas for queries
- Background job partitioning

## Future Enhancements

### Planned Features
- Real-time bid notifications (WebSocket/SignalR)
- Bid auto-increment functionality
- Advanced auction statistics
- Machine learning-based price predictions
- Multi-region deployment support

### Technical Debt
- Authentication implementation
- Comprehensive integration tests
- Performance benchmarking
- API documentation automation