-- AuctionService 資料庫初始化腳本
-- 此腳本用於手動初始化 PostgreSQL 資料庫
-- 適用於開發環境或手動部署情境

-- 建立資料庫 (如果不存在)
-- 注意: 此命令需要在 psql 外部執行，或由 DBA 手動執行
-- CREATE DATABASE auction_dev;

-- 連接到目標資料庫
-- \c auction_dev;

-- 設定搜尋路徑
SET search_path TO public;

-- 建立自訂類型 (如果需要)
-- DO $$ BEGIN
--     CREATE TYPE auction_status AS ENUM ('Pending', 'Active', 'Ended');
-- EXCEPTION
--     WHEN duplicate_object THEN null;
-- END $$;

-- 建立分頁函數 (用於效能最佳化)
CREATE OR REPLACE FUNCTION get_auctions_page(
    p_limit INTEGER DEFAULT 20,
    p_offset INTEGER DEFAULT 0,
    p_status_filter TEXT DEFAULT NULL,
    p_category_filter INTEGER DEFAULT NULL,
    p_search_term TEXT DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    name VARCHAR(200),
    description TEXT,
    starting_price DECIMAL(18,2),
    category_id INTEGER,
    category_name VARCHAR(50),
    status VARCHAR(20),
    start_time TIMESTAMP WITH TIME ZONE,
    end_time TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE,
    total_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    WITH filtered_auctions AS (
        SELECT
            a.id,
            a.name,
            a.description,
            a.starting_price,
            a.category_id,
            c.name as category_name,
            CASE
                WHEN NOW() < a.start_time THEN 'Pending'
                WHEN NOW() BETWEEN a.start_time AND a.end_time THEN 'Active'
                ELSE 'Ended'
            END as status,
            a.start_time,
            a.end_time,
            a.created_at
        FROM auctions a
        INNER JOIN categories c ON a.category_id = c.id
        WHERE
            (p_status_filter IS NULL OR
             CASE
                 WHEN NOW() < a.start_time THEN 'Pending'
                 WHEN NOW() BETWEEN a.start_time AND a.end_time THEN 'Active'
                 ELSE 'Ended'
             END = p_status_filter) AND
            (p_category_filter IS NULL OR a.category_id = p_category_filter) AND
            (p_search_term IS NULL OR
             a.name ILIKE '%' || p_search_term || '%' OR
             a.description ILIKE '%' || p_search_term || '%')
    ),
    total_count AS (
        SELECT COUNT(*) as count FROM filtered_auctions
    )
    SELECT
        fa.id,
        fa.name,
        fa.description,
        fa.starting_price,
        fa.category_id,
        fa.category_name,
        fa.status,
        fa.start_time,
        fa.end_time,
        fa.created_at,
        tc.count
    FROM filtered_auctions fa, total_count tc
    ORDER BY fa.end_time ASC, fa.created_at DESC
    LIMIT p_limit
    OFFSET p_offset;
END;
$$ LANGUAGE plpgsql;

-- 建立索引最佳化函數
CREATE OR REPLACE FUNCTION create_performance_indexes()
RETURNS VOID AS $$
BEGIN
    -- 檢查並建立索引
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'ix_auctions_end_time') THEN
        CREATE INDEX ix_auctions_end_time ON auctions (end_time);
        RAISE NOTICE 'Created index: ix_auctions_end_time';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'ix_auctions_category_id') THEN
        CREATE INDEX ix_auctions_category_id ON auctions (category_id);
        RAISE NOTICE 'Created index: ix_auctions_category_id';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'ix_auctions_user_id_created_at') THEN
        CREATE INDEX ix_auctions_user_id_created_at ON auctions (user_id, created_at DESC);
        RAISE NOTICE 'Created index: ix_auctions_user_id_created_at';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'ix_auctions_status_composite') THEN
        CREATE INDEX ix_auctions_status_composite ON auctions (start_time, end_time, category_id);
        RAISE NOTICE 'Created index: ix_auctions_status_composite';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'ix_follows_user_id') THEN
        CREATE INDEX ix_follows_user_id ON follows (user_id);
        RAISE NOTICE 'Created index: ix_follows_user_id';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'ix_follows_auction_id') THEN
        CREATE INDEX ix_follows_auction_id ON follows (auction_id);
        RAISE NOTICE 'Created index: ix_follows_auction_id';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'uq_follows_user_auction') THEN
        ALTER TABLE follows ADD CONSTRAINT uq_follows_user_auction UNIQUE (user_id, auction_id);
        RAISE NOTICE 'Created unique constraint: uq_follows_user_auction';
    END IF;

    RAISE NOTICE 'All performance indexes created successfully';
END;
$$ LANGUAGE plpgsql;

-- 建立資料清理函數
CREATE OR REPLACE FUNCTION cleanup_old_data()
RETURNS VOID AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    -- 刪除 1 年前結束的商品 (保留資料完整性)
    DELETE FROM follows
    WHERE auction_id IN (
        SELECT id FROM auctions
        WHERE end_time < NOW() - INTERVAL '1 year'
    );

    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RAISE NOTICE 'Cleaned up % old follow records', deleted_count;

    -- 記錄清理操作
    INSERT INTO cleanup_log (operation, table_name, records_affected, executed_at)
    VALUES ('cleanup_old_follows', 'follows', deleted_count, NOW());

    RAISE NOTICE 'Data cleanup completed successfully';
END;
$$ LANGUAGE plpgsql;

-- 建立清理記錄表 (如果不存在)
CREATE TABLE IF NOT EXISTS cleanup_log (
    id SERIAL PRIMARY KEY,
    operation VARCHAR(100) NOT NULL,
    table_name VARCHAR(50) NOT NULL,
    records_affected INTEGER NOT NULL,
    executed_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- 建立資料庫統計函數
CREATE OR REPLACE FUNCTION get_database_stats()
RETURNS TABLE (
    table_name TEXT,
    record_count BIGINT,
    table_size TEXT,
    index_size TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        t.table_name::TEXT,
        c.reltuples::BIGINT as record_count,
        pg_size_pretty(pg_total_relation_size(t.table_name::regclass)) as table_size,
        pg_size_pretty(pg_indexes_size(t.table_name::regclass)) as index_size
    FROM information_schema.tables t
    JOIN pg_class c ON c.relname = t.table_name
    WHERE t.table_schema = 'public'
      AND t.table_type = 'BASE TABLE'
      AND c.relkind = 'r'
    ORDER BY c.reltuples DESC;
END;
$$ LANGUAGE plpgsql;

-- 建立健康檢查函數
CREATE OR REPLACE FUNCTION health_check()
RETURNS TABLE (
    check_name TEXT,
    status TEXT,
    details TEXT,
    checked_at TIMESTAMP WITH TIME ZONE
) AS $$
DECLARE
    auction_count BIGINT;
    category_count BIGINT;
    db_size TEXT;
BEGIN
    -- 檢查基本資料表
    SELECT COUNT(*) INTO auction_count FROM auctions;
    SELECT COUNT(*) INTO category_count FROM categories;
    SELECT pg_size_pretty(pg_database_size(current_database())) INTO db_size;

    -- 回傳健康檢查結果
    RETURN QUERY VALUES
        ('database_connection', 'healthy', 'Database connection successful', NOW()),
        ('auctions_table', CASE WHEN auction_count >= 0 THEN 'healthy' ELSE 'unhealthy' END,
         format('Auctions table accessible, %s records', auction_count), NOW()),
        ('categories_table', CASE WHEN category_count >= 0 THEN 'healthy' ELSE 'unhealthy' END,
         format('Categories table accessible, %s records', category_count), NOW()),
        ('database_size', 'info', format('Database size: %s', db_size), NOW());

END;
$$ LANGUAGE plpgsql;

-- 建立效能監控函數
CREATE OR REPLACE FUNCTION get_query_performance_stats()
RETURNS TABLE (
    query TEXT,
    calls BIGINT,
    total_time DOUBLE PRECISION,
    mean_time DOUBLE PRECISION,
    rows_affected BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        LEFT(query, 100) as query,
        calls,
        total_time,
        mean_time,
        rows
    FROM pg_stat_statements
    WHERE query NOT LIKE '%pg_stat_statements%'
    ORDER BY total_time DESC
    LIMIT 10;
END;
$$ LANGUAGE plpgsql;

-- 建立備份準備函數
CREATE OR REPLACE FUNCTION prepare_for_backup()
RETURNS VOID AS $$
BEGIN
    -- 記錄備份開始
    INSERT INTO backup_log (operation, status, started_at)
    VALUES ('backup_prepare', 'started', NOW());

    -- 確保所有交易完成
    CHECKPOINT;

    -- 記錄備份準備完成
    UPDATE backup_log
    SET status = 'prepared', completed_at = NOW()
    WHERE operation = 'backup_prepare'
      AND completed_at IS NULL;

    RAISE NOTICE 'Database prepared for backup';
END;
$$ LANGUAGE plpgsql;

-- 建立備份記錄表
CREATE TABLE IF NOT EXISTS backup_log (
    id SERIAL PRIMARY KEY,
    operation VARCHAR(50) NOT NULL,
    status VARCHAR(20) NOT NULL,
    started_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    completed_at TIMESTAMP WITH TIME ZONE,
    details TEXT
);

-- 建立維護函數
CREATE OR REPLACE FUNCTION perform_maintenance()
RETURNS VOID AS $$
BEGIN
    -- 更新統計資訊
    ANALYZE;

    -- 清理死元組
    VACUUM;

    -- 記錄維護操作
    INSERT INTO maintenance_log (operation, status, executed_at)
    VALUES ('full_maintenance', 'completed', NOW());

    RAISE NOTICE 'Database maintenance completed';
END;
$$ LANGUAGE plpgsql;

-- 建立維護記錄表
CREATE TABLE IF NOT EXISTS maintenance_log (
    id SERIAL PRIMARY KEY,
    operation VARCHAR(50) NOT NULL,
    status VARCHAR(20) NOT NULL,
    executed_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    details TEXT
);

-- 設定權限 (範例 - 根據實際需求調整)
-- GRANT USAGE ON SCHEMA public TO auction_user;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO auction_user;
-- GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO auction_user;

-- 建立初始效能索引
SELECT create_performance_indexes();

-- 插入範例資料 (僅供開發測試使用)
-- 注意: 生產環境應該透過 EF Core Migrations 插入種子資料

/*
-- 範例分類資料
INSERT INTO categories (name, display_order, is_active, created_at) VALUES
('電子產品', 1, true, NOW()),
('家具', 2, true, NOW()),
('收藏品', 3, true, NOW()),
('藝術品', 4, true, NOW()),
('服飾配件', 5, true, NOW()),
('書籍', 6, true, NOW()),
('運動用品', 7, true, NOW()),
('其他', 8, true, NOW())
ON CONFLICT DO NOTHING;

-- 範例回應代碼資料
INSERT INTO response_codes (code, name, message_zh_tw, message_en, category, severity) VALUES
('SUCCESS', '成功', '操作成功', 'Operation successful', 'SUCCESS', 'Info'),
('AUCTION_NOT_FOUND', '商品不存在', '找不到指定的商品', 'Auction not found', 'NOT_FOUND', 'Warning'),
('VALIDATION_ERROR', '驗證錯誤', '請求資料驗證失敗', 'Validation error', 'VALIDATION', 'Warning'),
('UNAUTHORIZED', '未授權', '未授權的訪問', 'Unauthorized access', 'AUTH', 'Error')
ON CONFLICT (code) DO NOTHING;
*/

-- 顯示初始化完成訊息
DO $$
BEGIN
    RAISE NOTICE 'AuctionService database initialization completed successfully';
    RAISE NOTICE 'Available functions:';
    RAISE NOTICE '  - get_auctions_page(limit, offset, status, category, search)';
    RAISE NOTICE '  - get_database_stats()';
    RAISE NOTICE '  - health_check()';
    RAISE NOTICE '  - perform_maintenance()';
    RAISE NOTICE '  - cleanup_old_data()';
END $$;