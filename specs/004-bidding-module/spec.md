# Feature Specification: Bidding Module

**Feature Branch**: `004-bidding-module`  
**Created**: 2026-04-26  
**Status**: Draft  
**Input**: User description: "實作 Bidding 模組：使用者對 Active 狀態的拍賣商品出價（出價金額必須大於目前最高出價，否則回 422；商品非 Active 狀態回 409），查詢特定商品的出價歷史（依出價時間降序、分頁），查詢我的出價清單（顯示每筆出價的狀態：leading 我是目前最高出價者 / outbid 已被超越 / won 得標 / lost 未得標），schema: bidding，table: bids"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Place a Bid on an Active Auction (Priority: P1)

A logged-in user browses an active auction and places a bid higher than the current highest bid. The system records the bid and the user becomes the current leading bidder. Other bidders who were previously leading are notified that they have been outbid.

**Why this priority**: Placing a bid is the core action of the entire bidding system. Without it, no other bidding functionality has value. Delivery of this single story constitutes a usable MVP.

**Independent Test**: Can be fully tested by submitting a valid bid on an active auction item and verifying the bid is recorded and the bidder is shown as "leading".

**Acceptance Scenarios**:

1. **Given** a logged-in user and an active auction with a starting price of 100, **When** the user submits a bid of 150, **Then** the bid is accepted, the user's bid status is "leading", and the current highest bid is 150.
2. **Given** a logged-in user and an active auction with a current highest bid of 200, **When** the user submits a bid of 200 or less, **Then** the bid is rejected with a clear message indicating the required minimum amount.
3. **Given** a logged-in user and an auction that has already ended, **When** the user submits a bid, **Then** the bid is rejected with a message indicating the auction is no longer accepting bids.
4. **Given** a logged-in user and an auction that has not yet started or has been cancelled, **When** the user submits a bid, **Then** the bid is rejected with a message indicating the auction is not currently active.
5. **Given** an unauthenticated visitor, **When** they attempt to submit a bid, **Then** the request is rejected and the user is prompted to log in.
6. **Given** the owner of an auction, **When** they attempt to bid on their own listing, **Then** the bid is rejected with a message indicating owners cannot bid on their own items.

---

### User Story 2 - View Bid History for an Auction (Priority: P2)

Any visitor (logged-in or not) can view the complete list of bids placed on a specific auction item, ordered from the most recent bid down to the earliest, with pagination support for items with many bids.

**Why this priority**: Bid history provides price transparency and builds trust, encouraging more confident bidding. It can be implemented and demonstrated independently of My Bids.

**Independent Test**: Can be fully tested by viewing the bid history of an auction with multiple bids and verifying the correct order, bid amounts, and pagination behaviour.

**Acceptance Scenarios**:

1. **Given** an auction with multiple bids, **When** a visitor requests the bid history, **Then** all bids are returned ordered by bid time from newest to oldest, each showing the bidder identifier and amount.
2. **Given** an auction with more bids than the page size, **When** a visitor requests page 2, **Then** the correct subset of bids is returned along with information about total count and available pages.
3. **Given** an auction with no bids, **When** a visitor requests the bid history, **Then** an empty list is returned without an error.
4. **Given** a non-existent auction ID, **When** a visitor requests the bid history, **Then** an appropriate "not found" response is returned.

---

### User Story 3 - View My Bids (Priority: P3)

A logged-in user can see all bids they have ever placed, with the current status of each bid clearly shown: whether they are currently the leading bidder, have been outbid, have won, or have lost the auction.

**Why this priority**: Gives buyers visibility into their bidding activity and outcomes. This relies on bid status tracking established in P1 and outcome processing after auctions end.

**Independent Test**: Can be fully tested by placing bids as a user and verifying the list reflects correct statuses across active and ended auctions.

**Acceptance Scenarios**:

1. **Given** a logged-in user who has placed bids across multiple auctions, **When** they request their bid list, **Then** each entry shows the auction reference, bid amount, and current status.
2. **Given** a user who is currently the highest bidder on an active auction, **When** they view their bids, **Then** that bid's status is "leading".
3. **Given** a user whose bid was surpassed by another bidder on an active auction, **When** they view their bids, **Then** that bid's status is "outbid".
4. **Given** a user who placed the highest bid on an auction that has now ended, **When** they view their bids, **Then** that bid's status is "won".
5. **Given** a user who placed a bid on an auction that has now ended but they were not the highest bidder, **When** they view their bids, **Then** that bid's status is "lost".
6. **Given** a logged-in user who has never placed any bids, **When** they request their bid list, **Then** an empty list is returned without an error.

---

### Edge Cases

- What happens when two users place the same bid amount at nearly the same time? (The bid recorded first chronologically wins; the second is rejected as it is not strictly greater.)
- What happens when the bid amount is zero or negative? (The bid is rejected as invalid regardless of auction state.)
- What happens if a bid is placed at the exact moment an auction ends? (Bids placed after the auction close time are rejected, even if the close event has not yet been fully processed.)
- What happens when an auction ends and has no bids? (No winner is assigned; all fields remain empty/null.)
- Can a user have multiple bids on the same auction? (Yes; a user may bid multiple times, but only their most recent standing and the overall highest bid matter.)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Authenticated users MUST be able to place a bid on an active auction item by submitting a bid amount.
- **FR-002**: A submitted bid amount MUST be strictly greater than the current highest bid on that auction; bids equal to or less than the current highest bid MUST be rejected with a clear reason.
- **FR-003**: When no previous bids exist on an auction, the starting price defined on the auction listing acts as the minimum threshold; any bid amount MUST be greater than the starting price.
- **FR-004**: Bids placed on an auction item that is not in Active status MUST be rejected with a clear reason indicating the auction cannot accept bids.
- **FR-005**: The system MUST prevent users from placing bids on auction items they own.
- **FR-006**: Any visitor MUST be able to retrieve the bid history for a specific auction item, ordered by bid time descending, with pagination.
- **FR-007**: Authenticated users MUST be able to retrieve a list of all bids they have placed, each showing the auction item reference, bid amount, bid time, and bid status.
- **FR-008**: Bid status MUST reflect the bidder's current standing: "leading" (currently the highest bidder on an active auction), "outbid" (surpassed by another bidder on an active auction), "won" (highest bidder after auction ended), or "lost" (not the highest bidder after auction ended).
- **FR-009**: When an auction ends, the system MUST automatically update all associated bids — the highest bidder's status becomes "won" and all other bidders' statuses become "lost".
- **FR-010**: The system MUST ensure bid amount is a positive value; bids with a zero or negative amount MUST be rejected.

### Key Entities

- **Bid**: Represents a single bid placed by a user on an auction item. Attributes: unique identifier, reference to auction item (by ID), reference to bidder (by user ID), bid amount, bid timestamp, current bid status (leading / outbid / won / lost).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A valid bid is accepted and recorded within 2 seconds of submission under normal load.
- **SC-002**: Invalid bids (insufficient amount, non-active auction, owner bidding) are rejected immediately with a clear, user-friendly explanation — not a generic error.
- **SC-003**: Bid history for any auction is accurate and reflects all placed bids with no omissions; visitors can retrieve the full history through pagination.
- **SC-004**: A user can always see the correct status (leading / outbid / won / lost) for every bid they have placed, with no stale or incorrect statuses.
- **SC-005**: When an auction ends, all associated bid statuses are updated correctly — 100% of winning bids become "won" and 100% of non-winning bids become "lost" — with no manual intervention.
- **SC-006**: The bidding system handles concurrent bid submissions gracefully; no two bids for the same amount on the same auction are both accepted simultaneously.

## Assumptions

- Bid amounts are denominated in a single currency; multi-currency support is out of scope.
- The starting price (minimum bid threshold when no bids exist) is owned by the Auction module and is accessible to this feature at bid-placement time.
- Bid history is publicly accessible; no authentication is required to view bids on an auction.
- A user may place multiple bids on the same auction (raising their own bid is allowed as long as the new amount exceeds the current highest bid).
- The auction ending process (which triggers the won/lost status update) is initiated by the existing `AuctionWonEvent` domain event published by the Auction module.
- Bidder identity shown in public bid history displays a non-sensitive identifier (e.g., username or masked ID), not a private user reference.

## Dependencies

- **Auction Module**: Provides auction item state (Active / Ended) and starting price, referenced by auction ID.
- **Member Module**: Provides bidder identity by user ID for display purposes.
- **`AuctionWonEvent`**: Domain event published by the Auction module when an auction ends, consumed here to finalize bid statuses.
