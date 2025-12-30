-- place-bid.lua
-- Atomic bid placement script for Redis
-- KEYS: [auction_key, highest_bid_key, pending_bids_key, bid_info_key]
-- ARGV: [bid_id, bidder_id, amount, bid_at, bidder_id_hash, ttl, auction_id, bid_info_json]

local auction_key = KEYS[1]
local highest_bid_key = KEYS[2]
local pending_bids_key = KEYS[3]
local bid_info_key = KEYS[4]

local bid_id = ARGV[1]
local bidder_id = ARGV[2]
local amount = ARGV[3]
local bid_at = ARGV[4]
local bidder_id_hash = ARGV[5]
local ttl = tonumber(ARGV[6])
local auction_id = ARGV[7]
local bid_info_json = ARGV[8]

-- Check if auction exists and get current highest bid
local current_highest = redis.call('HGET', highest_bid_key, 'amount')
if current_highest and tonumber(amount) <= tonumber(current_highest) then
    return 0 -- Bid too low
end

-- Store bid in auction sorted set (by amount, then time)
-- Member format: {bidId}:{timestamp}:{bidderId}
local member = bid_id .. ":" .. bid_at .. ":" .. bidder_id
redis.call('ZADD', auction_key, amount, member)
if ttl > 0 then
    redis.call('EXPIRE', auction_key, ttl)
end

-- Update highest bid hash
redis.call('HSET', highest_bid_key,
    'bidId', bid_id,
    'bidderId', bidder_id,
    'bidderIdHash', bidder_id_hash,
    'amount', amount,
    'bidAt', bid_at)
if ttl > 0 then
    redis.call('EXPIRE', highest_bid_key, ttl)
end

-- Store full bid info for worker
redis.call('SET', bid_info_key, bid_info_json)
if ttl > 0 then
    redis.call('EXPIRE', bid_info_key, ttl)
end

-- Add to pending bids for sync worker
-- Store "bidId:auctionId" to help worker find the bid info or know which auction it belongs to
redis.call('SADD', pending_bids_key, bid_id .. ":" .. auction_id)

return 1 -- Success
