-- =============================================
-- Database Quick Reference
-- Linkoja Microservice - PostgreSQL
-- =============================================

-- CONNECT TO DATABASE
-- psql -U postgres -d linkoja_db

-- =============================================
-- USEFUL QUERIES
-- =============================================

-- View all tables
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;

-- Check table row counts
SELECT 
    schemaname AS schema,
    tablename AS table,
    n_tup_ins - n_tup_del AS row_count
FROM pg_stat_user_tables
ORDER BY n_tup_ins - n_tup_del DESC;

-- View all indexes
SELECT 
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'public'
ORDER BY tablename, indexname;

-- =============================================
-- SAMPLE DATA QUERIES
-- =============================================

-- Get all users with their roles
SELECT "Id", "Email", "Name", "Role", "IsPhoneVerified", "CreatedAt"
FROM "Users"
ORDER BY "CreatedAt" DESC;

-- Get all verified businesses with owner info
SELECT 
    b."Id",
    b."Name" AS "BusinessName",
    b."Category",
    b."Status",
    u."Name" AS "OwnerName",
    u."Email" AS "OwnerEmail"
FROM "Businesses" b
JOIN "Users" u ON b."OwnerId" = u."Id"
WHERE b."Status" = 'verified'
ORDER BY b."CreatedAt" DESC;

-- Get businesses with review statistics
SELECT 
    b."Id",
    b."Name",
    COUNT(r."Id") AS "ReviewCount",
    ROUND(AVG(r."Rating")::numeric, 2) AS "AverageRating"
FROM "Businesses" b
LEFT JOIN "BusinessReviews" r ON b."Id" = r."BusinessId"
GROUP BY b."Id", b."Name"
ORDER BY "AverageRating" DESC NULLS LAST;

-- Get popular businesses (by followers)
SELECT 
    b."Id",
    b."Name",
    b."Category",
    COUNT(f."Id") AS "FollowerCount"
FROM "Businesses" b
LEFT JOIN "BusinessFollowers" f ON b."Id" = f."BusinessId"
GROUP BY b."Id", b."Name", b."Category"
ORDER BY "FollowerCount" DESC
LIMIT 10;

-- Get pending business approvals
SELECT 
    b."Id",
    b."Name",
    b."Category",
    u."Email" AS "OwnerEmail",
    b."CreatedAt"
FROM "Businesses" b
JOIN "Users" u ON b."OwnerId" = u."Id"
WHERE b."Status" = 'pending'
ORDER BY b."CreatedAt" ASC;

-- Get unread notifications for a user
SELECT "Id", "Type", "Title", "Message", "CreatedAt"
FROM "Notifications"
WHERE "UserId" = 1 AND "IsRead" = FALSE
ORDER BY "CreatedAt" DESC;

-- Get pending review reports
SELECT 
    rr."Id",
    rr."Reason",
    rr."Description",
    u."Email" AS "ReportedBy",
    br."Rating",
    br."Comment",
    b."Name" AS "BusinessName"
FROM "ReviewReports" rr
JOIN "Users" u ON rr."ReportedByUserId" = u."Id"
JOIN "BusinessReviews" br ON rr."ReviewId" = br."Id"
JOIN "Businesses" b ON br."BusinessId" = b."Id"
WHERE rr."Status" = 'pending'
ORDER BY rr."CreatedAt" DESC;

-- =============================================
-- MAINTENANCE QUERIES
-- =============================================

-- Delete expired OTP codes (run daily)
DELETE FROM "OtpVerifications" 
WHERE "ExpiresAt" < NOW() - INTERVAL '1 day';

-- Delete used password reset tokens (run weekly)
DELETE FROM "PasswordResetTokens" 
WHERE "IsUsed" = TRUE AND "CreatedAt" < NOW() - INTERVAL '30 days';

-- Archive old read notifications (run monthly)
DELETE FROM "Notifications" 
WHERE "IsRead" = TRUE AND "CreatedAt" < NOW() - INTERVAL '90 days';

-- Update statistics (run weekly)
VACUUM ANALYZE;

-- =============================================
-- ADMIN OPERATIONS
-- =============================================

-- Create new admin user
INSERT INTO "Users" ("Email", "PasswordHash", "Name", "Role", "AuthProvider")
VALUES ('nenadmin@linkoja.com', '$2a$11$hash_here', 'New Admin', 'admin', 'local');

-- Promote user to admin
UPDATE "Users" 
SET "Role" = 'admin' 
WHERE "Email" = 'user@example.com';

-- Approve business
UPDATE "Businesses" 
SET "Status" = 'verified', "UpdatedAt" = NOW() 
WHERE "Id" = 1;

-- Reject business
UPDATE "Businesses" 
SET "Status" = 'rejected', "UpdatedAt" = NOW() 
WHERE "Id" = 2;

-- Delete spam review
DELETE FROM "BusinessReviews" 
WHERE "Id" = 123;

-- Resolve report
UPDATE "ReviewReports" 
SET "Status" = 'resolved', "ResolvedAt" = NOW() 
WHERE "Id" = 1;

-- =============================================
-- ANALYTICS QUERIES
-- =============================================

-- Platform statistics
SELECT 
    (SELECT COUNT(*) FROM "Users") AS "TotalUsers",
    (SELECT COUNT(*) FROM "Users" WHERE "Role" = 'business_owner') AS "BusinessOwners",
    (SELECT COUNT(*) FROM "Businesses") AS "TotalBusinesses",
    (SELECT COUNT(*) FROM "Businesses" WHERE "Status" = 'verified') AS "VerifiedBusinesses",
    (SELECT COUNT(*) FROM "BusinessReviews") AS "TotalReviews",
    (SELECT COUNT(*) FROM "BusinessFollowers") AS "TotalFollows",
    (SELECT COUNT(*) FROM "BusinessPosts") AS "TotalPosts";

-- Business growth over time (last 30 days)
SELECT 
    DATE("CreatedAt") AS "Date",
    COUNT(*) AS "NewBusinesses"
FROM "Businesses"
WHERE "CreatedAt" >= NOW() - INTERVAL '30 days'
GROUP BY DATE("CreatedAt")
ORDER BY "Date" DESC;

-- User registration trend (last 7 days)
SELECT 
    DATE("CreatedAt") AS "Date",
    COUNT(*) AS "NewUsers"
FROM "Users"
WHERE "CreatedAt" >= NOW() - INTERVAL '7 days'
GROUP BY DATE("CreatedAt")
ORDER BY "Date" DESC;

-- Top categories by business count
SELECT 
    "Category",
    COUNT(*) AS "BusinessCount"
FROM "Businesses"
WHERE "Status" = 'verified'
GROUP BY "Category"
ORDER BY "BusinessCount" DESC
LIMIT 10;

-- Most active reviewers
SELECT 
    u."Id",
    u."Name",
    u."Email",
    COUNT(r."Id") AS "ReviewCount"
FROM "Users" u
JOIN "BusinessReviews" r ON u."Id" = r."UserId"
GROUP BY u."Id", u."Name", u."Email"
ORDER BY "ReviewCount" DESC
LIMIT 10;

-- =============================================
-- PERFORMANCE MONITORING
-- =============================================

-- Check table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- Check index usage
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan AS index_scans,
    idx_tup_read AS tuples_read,
    idx_tup_fetch AS tuples_fetched
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
ORDER BY idx_scan DESC;

-- Find slow queries (requires pg_stat_statements extension)
-- SELECT query, calls, mean_exec_time, total_exec_time
-- FROM pg_stat_statements
-- ORDER BY mean_exec_time DESC
-- LIMIT 10;

-- =============================================
-- BACKUP & RESTORE COMMANDS (run in shell)
-- =============================================

-- Backup database
-- pg_dump -U postgres -d linkoja_db -F c -b -v -f backup_$(date +%Y%m%d).backup

-- Restore database
-- pg_restore -U postgres -d linkoja_db -v backup_YYYYMMDD.backup

-- Export table to CSV
-- psql -U postgres -d linkoja_db -c "COPY \"Users\" TO '/tmp/users.csv' CSV HEADER"

-- Import CSV to table
-- psql -U postgres -d linkoja_db -c "COPY \"Users\" FROM '/tmp/users.csv' CSV HEADER"
