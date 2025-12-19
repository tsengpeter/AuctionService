# API Guide

## Overview

The BiddingService provides RESTful APIs for managing auction bids. All endpoints return JSON responses and use standard HTTP status codes.

## Base URL
```
http://localhost:5000/api
```

## Authentication

**Note**: Authentication is not yet implemented. All endpoints currently use a placeholder bidder ID.

## Common Response Formats

### Success Response
```json
{
  "bidId": 1234567890123456789,
  "auctionId": 1,
  "bidderIdHash": "a1b2c3d4...",
  "amount": 150.00,
  "bidAt": "2024-01-15T10:30:00Z"
}
```

### Error Response
```json
{
  "error": {
    "code": "AUCTION_NOT_FOUND",
    "message": "Auction with ID 999 not found",
    "details": null
  }
}
```

## Endpoints

### 1. Submit Bid

Submit a new bid for an auction.

**Endpoint**: `POST /api/bids`

**Request Body**:
```json
{
  "auctionId": 1,
  "amount": 150.00
}
```

**Response**: `201 Created`
```json
{
  "bidId": 1234567890123456789,
  "auctionId": 1,
  "bidderIdHash": "a1b2c3d4...",
  "amount": 150.00,
  "bidAt": "2024-01-15T10:30:00Z"
}
```

**Error Codes**:
- `400 Bad Request`: Invalid request data
- `404 Not Found`: Auction not found
- `409 Conflict`: Auction not active or bid amount too low

### 2. Get Bid by ID

Retrieve a specific bid by its ID.

**Endpoint**: `GET /api/bids/{bidId}`

**Response**: `200 OK`
```json
{
  "bidId": 1234567890123456789,
  "auctionId": 1,
  "bidderIdHash": "a1b2c3d4...",
  "amount": 150.00,
  "bidAt": "2024-01-15T10:30:00Z"
}
```

**Error Codes**:
- `404 Not Found`: Bid not found

### 3. Get Bid History

Retrieve bidding history for a specific auction.

**Endpoint**: `GET /api/bids/history/{auctionId}`

**Query Parameters**:
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 50, max: 100)

**Response**: `200 OK`
```json
{
  "auctionId": 1,
  "bids": [
    {
      "bidId": 1234567890123456789,
      "auctionId": 1,
      "bidderIdHash": "a1b2c3d4...",
      "amount": 150.00,
      "bidAt": "2024-01-15T10:30:00Z"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 50,
    "totalItems": 25,
    "totalPages": 1,
    "hasNext": false,
    "hasPrevious": false
  }
}
```

### 4. Get My Bids

Retrieve all bids placed by the current user.

**Endpoint**: `GET /api/bids/my-bids`

**Query Parameters**:
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 50, max: 100)

**Response**: `200 OK`
```json
{
  "bids": [
    {
      "bidId": 1234567890123456789,
      "auctionId": 1,
      "bidderIdHash": "a1b2c3d4...",
      "amount": 150.00,
      "bidAt": "2024-01-15T10:30:00Z"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 50,
    "totalItems": 5,
    "totalPages": 1,
    "hasNext": false,
    "hasPrevious": false
  }
}
```

### 5. Get Highest Bid

Retrieve the current highest bid for an auction.

**Endpoint**: `GET /api/bids/highest/{auctionId}`

**Response**: `200 OK`
```json
{
  "auctionId": 1,
  "highestBid": {
    "bidId": 1234567890123456789,
    "auctionId": 1,
    "bidderIdHash": "a1b2c3d4...",
    "amount": 200.00,
    "bidAt": "2024-01-15T10:45:00Z"
  }
}
```

**Note**: Returns `null` for `highestBid` if no bids exist.

### 6. Get Auction Statistics

Retrieve comprehensive statistics for an auction.

**Endpoint**: `GET /api/bids/auctions/{auctionId}/stats`

**Response**: `200 OK`
```json
{
  "auctionId": 1,
  "totalBids": 25,
  "uniqueBidders": 8,
  "startingPrice": 100.00,
  "currentHighestBid": 250.00,
  "averageBidAmount": 175.50,
  "priceGrowthRate": 150.00,
  "firstBidAt": "2024-01-15T09:00:00Z",
  "lastBidAt": "2024-01-15T11:30:00Z",
  "bidsInLastHour": 5,
  "bidsInLast24Hours": 25
}
```

## Rate Limiting

**Note**: Rate limiting is not yet implemented. Consider implementing based on:
- IP address
- User ID (when authentication is added)
- Auction ID

## Pagination

Endpoints that return lists support pagination:

- Default page size: 50 items
- Maximum page size: 100 items
- Use `page` and `pageSize` query parameters

## Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `AUCTION_NOT_FOUND` | 404 | Auction does not exist |
| `AUCTION_NOT_ACTIVE` | 409 | Auction is not currently active |
| `BID_AMOUNT_TOO_LOW` | 409 | Bid amount is below minimum requirements |
| `DUPLICATE_BID` | 409 | User has already bid on this auction |
| `BID_NOT_FOUND` | 404 | Specific bid does not exist |
| `VALIDATION_ERROR` | 400 | Request data validation failed |

## Examples

### Submit a Bid
```bash
curl -X POST http://localhost:5000/api/bids \
  -H "Content-Type: application/json" \
  -d '{
    "auctionId": 1,
    "amount": 150.00
  }'
```

### Get Auction Statistics
```bash
curl http://localhost:5000/api/bids/auctions/1/stats
```

### Get Bid History with Pagination
```bash
curl "http://localhost:5000/api/bids/history/1?page=1&pageSize=20"
```

## SDKs and Libraries

**Note**: No official SDKs are available yet. Use standard HTTP clients.

### JavaScript (fetch)
```javascript
const response = await fetch('/api/bids', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    auctionId: 1,
    amount: 150.00
  })
});

const result = await response.json();
```

### C# (HttpClient)
```csharp
using var client = new HttpClient();
client.BaseAddress = new Uri("http://localhost:5000");

var bidRequest = new { auctionId = 1, amount = 150.00m };
var response = await client.PostAsJsonAsync("/api/bids", bidRequest);
var bid = await response.Content.ReadFromJsonAsync<BidResponse>();
```

## Webhooks

**Note**: Webhooks are not yet implemented. Future versions may include:
- Bid placed notifications
- Auction ending soon alerts
- Outbid notifications

## Versioning

The API uses URL versioning. Current version is v1 (implicit in base path).

Future versions will be available at:
- `/api/v2/bids`
- `/api/v3/bids`