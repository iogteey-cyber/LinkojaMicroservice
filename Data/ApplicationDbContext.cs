using Microsoft.EntityFrameworkCore;
using LinkojaMicroservice.Models;

namespace LinkojaMicroservice.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Business> Businesses { get; set; }
        public DbSet<BusinessReview> BusinessReviews { get; set; }
        public DbSet<BusinessFollower> BusinessFollowers { get; set; }
        public DbSet<BusinessPost> BusinessPosts { get; set; }
        public DbSet<BusinessCategory> BusinessCategories { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<OtpVerification> OtpVerifications { get; set; }
        public DbSet<ReviewReport> ReviewReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
            });

            // Configure Business entity
            modelBuilder.Entity<Business>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey(e => e.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure BusinessReview entity
            modelBuilder.Entity<BusinessReview>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Business)
                    .WithMany(b => b.Reviews)
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure BusinessFollower entity
            modelBuilder.Entity<BusinessFollower>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Business)
                    .WithMany(b => b.Followers)
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.BusinessId, e.UserId }).IsUnique();
            });

            // Configure BusinessPost entity
            modelBuilder.Entity<BusinessPost>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Business)
                    .WithMany(b => b.Posts)
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure BusinessCategory entity
            modelBuilder.Entity<BusinessCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Business)
                    .WithMany(b => b.BusinessCategories)
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure PasswordResetToken entity
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.Token).IsUnique();
            });

            // Configure Notification entity
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.RelatedBusiness)
                    .WithMany()
                    .HasForeignKey(e => e.RelatedBusinessId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure OtpVerification entity
            modelBuilder.Entity<OtpVerification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PhoneNumber);
            });

            // Configure ReviewReport entity
            modelBuilder.Entity<ReviewReport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Review)
                    .WithMany()
                    .HasForeignKey(e => e.ReviewId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ReportedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ReportedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
