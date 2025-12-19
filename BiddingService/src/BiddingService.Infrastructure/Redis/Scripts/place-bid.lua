-- place-bid.lua
-- Atomic bid placement script for Redis
-- KEYS: [auction_key, highest_bid_key]
-- ARGV: [bid_id, bidder_id, amount, bid_at, bidder_id_hash]

local auction_key = KEYS[1]
local highest_bid_key = KEYS[2]
local bid_id = ARGV[1]
local bidder_id = ARGV[2]
local amount = ARGV[3]
local bid_at = ARGV[4]
local bidder_id_hash = ARGV[5]

-- Check if auction exists and get current highest bid
local current_highest = redis.call('HGET', highest_bid_key, 'amount')
if current_highest and tonumber(amount) <= tonumber(current_highest) then
    return 0 -- Bid too low
end

-- Store bid in auction sorted set (by amount, then time)
redis.call('ZADD', auction_key, amount, bid_id)

-- Update highest bid hash
redis.call('HSET', highest_bid_key,
    'bidId', bid_id,
    'bidderId', bidder_id,
    'bidderIdHash', bidder_id_hash,
    'amount', amount,
    'bidAt', bid_at)

return 1 -- Success