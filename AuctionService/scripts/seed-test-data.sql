-- Test Data Seed Script for Load Testing
-- 此腳本創建負載測試所需的測試數據

-- 插入測試分類
INSERT INTO "Categories" ("Name") VALUES
('Electronics'),
('Collectibles'),
('Art')
ON CONFLICT DO NOTHING;

-- 插入測試拍賣品
INSERT INTO "Auctions" ("Id", "Name", "Description", "StartingPrice", "CategoryId", "UserId", "StartTime", "EndTime", "CreatedAt", "UpdatedAt") VALUES
-- 基本測試拍賣
('11111111-1111-1111-1111-111111111111'::uuid, 'iPhone 15 Pro', 'Latest iPhone model with 256GB storage', 30000.00, 1, 'test-seller-1', NOW() - INTERVAL '1 hour', NOW() + INTERVAL '7 days', NOW(), NOW()),
('22222222-2222-2222-2222-222222222222'::uuid, 'Vintage Watch', 'Rare vintage watch from 1960s', 50000.00, 2, 'test-seller-1', NOW() - INTERVAL '2 hours', NOW() + INTERVAL '5 days', NOW(), NOW()),
('33333333-3333-3333-3333-333333333333'::uuid, 'Modern Art Painting', 'Contemporary art piece by local artist', 15000.00, 3, 'test-seller-2', NOW() - INTERVAL '30 minutes', NOW() + INTERVAL '10 days', NOW(), NOW()),

-- 熱門拍賣測試數據
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid, 'Hot Item - Limited Edition', 'Super popular item for load testing', 10000.00, 1, 'test-seller-1', NOW() - INTERVAL '3 hours', NOW() + INTERVAL '2 days', NOW(), NOW()),

-- 額外測試拍賣品（用於列表查詢）
('44444444-4444-4444-4444-444444444444'::uuid, 'MacBook Pro M3', 'Latest MacBook with 16GB RAM', 45000.00, 1, 'test-seller-2', NOW() - INTERVAL '1 day', NOW() + INTERVAL '6 days', NOW(), NOW()),
('55555555-5555-5555-5555-555555555555'::uuid, 'Rare Coin Collection', 'Valuable coin collection from multiple countries', 80000.00, 2, 'test-seller-3', NOW() - INTERVAL '2 hours', NOW() + INTERVAL '8 days', NOW(), NOW()),
('66666666-6666-6666-6666-666666666666'::uuid, 'Sony Camera', 'Professional mirrorless camera', 35000.00, 1, 'test-seller-3', NOW() - INTERVAL '4 hours', NOW() + INTERVAL '3 days', NOW(), NOW()),
('77777777-7777-7777-7777-777777777777'::uuid, 'Antique Furniture', 'Victorian era furniture set', 120000.00, 2, 'test-seller-1', NOW() - INTERVAL '6 hours', NOW() + INTERVAL '12 days', NOW(), NOW()),
('88888888-8888-8888-8888-888888888888'::uuid, 'Digital Art NFT', 'Unique digital artwork', 25000.00, 3, 'test-seller-2', NOW() - INTERVAL '30 minutes', NOW() + INTERVAL '4 days', NOW(), NOW()),
('99999999-9999-9999-9999-999999999999'::uuid, 'Gaming Console', 'Latest generation gaming console', 18000.00, 1, 'test-seller-3', NOW() - INTERVAL '2 hours', NOW() + INTERVAL '9 days', NOW(), NOW()),
('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'::uuid, 'Designer Watch', 'Luxury designer watch', 95000.00, 2, 'test-seller-1', NOW() - INTERVAL '5 hours', NOW() + INTERVAL '15 days', NOW(), NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 顯示插入結果統計
DO $$
DECLARE
    auction_count INTEGER;
    category_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO auction_count FROM "Auctions";
    SELECT COUNT(*) INTO category_count FROM "Categories";
    
    RAISE NOTICE '✓ Test data seeding completed:';
    RAISE NOTICE '  - Categories: %', category_count;
    RAISE NOTICE '  - Auctions: %', auction_count;
END $$;
