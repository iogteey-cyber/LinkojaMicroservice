-- =============================================
-- Linkoja Microservice Database Schema
-- Database: PostgreSQL
-- Description: Complete database schema for Linkoja platform
-- =============================================

-- Drop tables if they exist (in reverse order of dependencies)
DROP TABLE IF EXISTS "ReviewReports" CASCADE;
DROP TABLE IF EXISTS "OtpVerifications" CASCADE;
DROP TABLE IF EXISTS "Notifications" CASCADE;
DROP TABLE IF EXISTS "PasswordResetTokens" CASCADE;
DROP TABLE IF EXISTS "BusinessCategories" CASCADE;
DROP TABLE IF EXISTS "BusinessPosts" CASCADE;
DROP TABLE IF EXISTS "BusinessFollowers" CASCADE;
DROP TABLE IF EXISTS "BusinessReviews" CASCADE;
DROP TABLE IF EXISTS "Businesses" CASCADE;
DROP TABLE IF EXISTS "Users" CASCADE;

-- =============================================
-- Table: Users
-- Description: Stores user accounts (customers, business owners, admins)
-- =============================================
CREATE TABLE "Users" (
    "Id" SERIAL PRIMARY KEY,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "Phone" VARCHAR(20),
    "IsPhoneVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "PasswordHash" VARCHAR(255) NOT NULL,
    "Name" VARCHAR(255),
    "Role" VARCHAR(50) NOT NULL DEFAULT 'user', -- 'user', 'business_owner', 'admin'
    "AuthProvider" VARCHAR(50) DEFAULT 'local', -- 'local', 'google', 'facebook', 'apple'
    "SocialId" VARCHAR(255), -- ID from social provider
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "idx_users_email" ON "Users"("Email");
CREATE INDEX "idx_users_role" ON "Users"("Role");
CREATE INDEX "idx_users_social" ON "Users"("AuthProvider", "SocialId");

-- =============================================
-- Table: Businesses
-- Description: Stores business profiles
-- =============================================
CREATE TABLE "Businesses" (
    "Id" SERIAL PRIMARY KEY,
    "OwnerId" INTEGER NOT NULL,
    "Name" VARCHAR(255) NOT NULL,
    "LogoUrl" VARCHAR(500),
    "CoverPhotoUrl" VARCHAR(500),
    "Description" TEXT,
    "Category" VARCHAR(100),
    "Address" TEXT,
    "Latitude" DOUBLE PRECISION,
    "Longitude" DOUBLE PRECISION,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'pending', -- 'pending', 'verified', 'rejected'
    "VerificationDocUrl" VARCHAR(500),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "fk_business_owner" FOREIGN KEY ("OwnerId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

CREATE INDEX "idx_businesses_owner" ON "Businesses"("OwnerId");
CREATE INDEX "idx_businesses_status" ON "Businesses"("Status");
CREATE INDEX "idx_businesses_category" ON "Businesses"("Category");
CREATE INDEX "idx_businesses_location" ON "Businesses"("Latitude", "Longitude");

-- =============================================
-- Table: BusinessReviews
-- Description: Stores customer reviews for businesses
-- =============================================
CREATE TABLE "BusinessReviews" (
    "Id" SERIAL PRIMARY KEY,
    "BusinessId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Rating" INTEGER NOT NULL CHECK ("Rating" >= 1 AND "Rating" <= 5),
    "Comment" TEXT,
    "PhotoUrl" VARCHAR(500), -- Optional photo with review
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "fk_review_business" FOREIGN KEY ("BusinessId") REFERENCES "Businesses"("Id") ON DELETE CASCADE,
    CONSTRAINT "fk_review_user" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "unique_user_business_review" UNIQUE ("BusinessId", "UserId")
);

CREATE INDEX "idx_reviews_business" ON "BusinessReviews"("BusinessId");
CREATE INDEX "idx_reviews_user" ON "BusinessReviews"("UserId");
CREATE INDEX "idx_reviews_rating" ON "BusinessReviews"("Rating");

-- =============================================
-- Table: BusinessFollowers
-- Description: Tracks which users follow which businesses
-- =============================================
CREATE TABLE "BusinessFollowers" (
    "Id" SERIAL PRIMARY KEY,
    "BusinessId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "FollowedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "fk_follower_business" FOREIGN KEY ("BusinessId") REFERENCES "Businesses"("Id") ON DELETE CASCADE,
    CONSTRAINT "fk_follower_user" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "unique_follower" UNIQUE ("BusinessId", "UserId")
);

CREATE INDEX "idx_followers_business" ON "BusinessFollowers"("BusinessId");
CREATE INDEX "idx_followers_user" ON "BusinessFollowers"("UserId");

-- =============================================
-- Table: BusinessPosts
-- Description: Stores posts/updates created by businesses
-- =============================================
CREATE TABLE "BusinessPosts" (
    "Id" SERIAL PRIMARY KEY,
    "BusinessId" INTEGER NOT NULL,
    "Content" TEXT NOT NULL,
    "ImageUrl" VARCHAR(500),
    "VideoUrl" VARCHAR(500),
    "Likes" INTEGER NOT NULL DEFAULT 0,
    "Comments" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "fk_post_business" FOREIGN KEY ("BusinessId") REFERENCES "Businesses"("Id") ON DELETE CASCADE
);

CREATE INDEX "idx_posts_business" ON "BusinessPosts"("BusinessId");
CREATE INDEX "idx_posts_created" ON "BusinessPosts"("CreatedAt" DESC);

-- =============================================
-- Table: BusinessCategories
-- Description: Stores category tags for businesses (many-to-many relationship)
-- =============================================
CREATE TABLE "BusinessCategories" (
    "Id" SERIAL PRIMARY KEY,
    "BusinessId" INTEGER NOT NULL,
    "CategoryName" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "fk_category_business" FOREIGN KEY ("BusinessId") REFERENCES "Businesses"("Id") ON DELETE CASCADE
);

CREATE INDEX "idx_categories_business" ON "BusinessCategories"("BusinessId");
CREATE INDEX "idx_categories_name" ON "BusinessCategories"("CategoryName");

-- =============================================
-- Table: PasswordResetTokens
-- Description: Stores password reset tokens for users
-- =============================================
CREATE TABLE "PasswordResetTokens" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Token" VARCHAR(255) NOT NULL UNIQUE,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "IsUsed" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "fk_reset_token_user" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

CREATE INDEX "idx_reset_tokens_user" ON "PasswordResetTokens"("UserId");
CREATE INDEX "idx_reset_tokens_token" ON "PasswordResetTokens"("Token");
CREATE INDEX "idx_reset_tokens_expiry" ON "PasswordResetTokens"("ExpiresAt");

-- =============================================
-- Table: Notifications
-- Description: Stores in-app notifications for users
-- =============================================
CREATE TABLE "Notifications" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Type" VARCHAR(50) NOT NULL, -- 'follower', 'review', 'approval', 'comment'
    "Title" VARCHAR(255) NOT NULL,
    "Message" TEXT NOT NULL,
    "RelatedBusinessId" INTEGER,
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "fk_notification_user" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "fk_notification_business" FOREIGN KEY ("RelatedBusinessId") REFERENCES "Businesses"("Id") ON DELETE SET NULL
);

CREATE INDEX "idx_notifications_user" ON "Notifications"("UserId");
CREATE INDEX "idx_notifications_read" ON "Notifications"("IsRead");
CREATE INDEX "idx_notifications_created" ON "Notifications"("CreatedAt" DESC);

-- =============================================
-- Table: OtpVerifications
-- Description: Stores OTP codes for phone number verification
-- =============================================
CREATE TABLE "OtpVerifications" (
    "Id" SERIAL PRIMARY KEY,
    "PhoneNumber" VARCHAR(20) NOT NULL,
    "OtpCode" VARCHAR(10) NOT NULL,
    "IsVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "AttemptCount" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "idx_otp_phone" ON "OtpVerifications"("PhoneNumber");
CREATE INDEX "idx_otp_expiry" ON "OtpVerifications"("ExpiresAt");

-- =============================================
-- Table: ReviewReports
-- Description: Stores abuse reports for reviews
-- =============================================
CREATE TABLE "ReviewReports" (
    "Id" SERIAL PRIMARY KEY,
    "ReviewId" INTEGER NOT NULL,
    "ReportedByUserId" INTEGER NOT NULL,
    "Reason" VARCHAR(50) NOT NULL, -- 'spam', 'inappropriate', 'fake', 'offensive'
    "Description" TEXT,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'pending', -- 'pending', 'reviewed', 'resolved', 'dismissed'
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ResolvedAt" TIMESTAMP,
    CONSTRAINT "fk_report_review" FOREIGN KEY ("ReviewId") REFERENCES "BusinessReviews"("Id") ON DELETE CASCADE,
    CONSTRAINT "fk_report_user" FOREIGN KEY ("ReportedByUserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "unique_user_review_report" UNIQUE ("ReviewId", "ReportedByUserId")
);

CREATE INDEX "idx_reports_review" ON "ReviewReports"("ReviewId");
CREATE INDEX "idx_reports_user" ON "ReviewReports"("ReportedByUserId");
CREATE INDEX "idx_reports_status" ON "ReviewReports"("Status");

-- =============================================
-- Insert Sample Admin User (Password: Admin123!)
-- Note: Replace this hash with actual BCrypt hash in production
-- =============================================
INSERT INTO "Users" ("Email", "PasswordHash", "Name", "Role", "IsPhoneVerified", "AuthProvider")
VALUES (
    'admin@linkoja.com',
    '$2a$11$xYzV8UWQ0kK6YqX5PzJE7.9bQZGJF5LJV2W2qK5YqX5PzJE7.9bQZG', -- BCrypt hash for 'Admin123!'
    'System Administrator',
    'admin',
    true,
    'local'
);

-- =============================================
-- Database Schema Complete
-- =============================================
-- Total Tables: 11
-- - Users
-- - Businesses
-- - BusinessReviews
-- - BusinessFollowers
-- - BusinessPosts
-- - BusinessCategories
-- - PasswordResetTokens
-- - Notifications
-- - OtpVerifications
-- - ReviewReports
-- =============================================
