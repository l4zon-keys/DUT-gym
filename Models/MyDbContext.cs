using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LoginFormASPCore6.Models
{
    public partial class MyDbContext : DbContext
    {
        public MyDbContext()
        {
        }

        public MyDbContext(DbContextOptions<MyDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<MembershipPlan> MembershipPlans { get; set; } = null!;
        public virtual DbSet<Venue> Venues { get; set; } = null!;
        public virtual DbSet<Session> Sessions { get; set; } = null!;
        public virtual DbSet<Membership> Memberships { get; set; } = null!;
        public virtual DbSet<Payment> Payments { get; set; } = null!;
        public virtual DbSet<CheckIn> CheckIns { get; set; } = null!;
        public virtual DbSet<TrainerRequest> TrainerRequests { get; set; } = null!;
        public virtual DbSet<TrainerSession> TrainerSessions { get; set; } = null!;
        public virtual DbSet<WorkoutPlan> WorkoutPlans { get; set; } = null!;
        public virtual DbSet<FitnessGoal> FitnessGoals { get; set; } = null!;
        public virtual DbSet<ProgressLog> ProgressLogs { get; set; } = null!;
        public virtual DbSet<SessionBooking> SessionBookings { get; set; } = null!;
        public virtual DbSet<Equipment> Equipment { get; set; } = null!;
        public virtual DbSet<XpEvent> XpEvents { get; set; } = null!;
        public virtual DbSet<Badge> Badges { get; set; } = null!;
        public virtual DbSet<UserBadge> UserBadges { get; set; } = null!;
        public virtual DbSet<Challenge> Challenges { get; set; } = null!;
        public virtual DbSet<ChallengeParticipant> ChallengeParticipants { get; set; } = null!;
        public virtual DbSet<Reward> Rewards { get; set; } = null!;
        public virtual DbSet<UserReward> UserRewards { get; set; } = null!;
        public virtual DbSet<Friendship> Friendships { get; set; } = null!;
        public virtual DbSet<CapacitySlot> CapacitySlots { get; set; } = null!;
        public virtual DbSet<CapacitySlotBooking> CapacitySlotBookings { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {

            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.Property(e => e.StudentNumber)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.HasIndex(e => e.StudentNumber)
                    .IsUnique();

                entity.Property(e => e.EmpName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Gender)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValue(EmailRoleHelper.UnknownRole);

                entity.Property(e => e.ApprovalStatus).HasConversion<string>().HasMaxLength(20);
            });

            modelBuilder.Entity<MembershipPlan>(entity =>
            {
                entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            });

            modelBuilder.Entity<Session>(entity =>
            {
                entity.HasOne(e => e.Venue)
                    .WithMany()
                    .HasForeignKey(e => e.VenueId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Instructor)
                    .WithMany()
                    .HasForeignKey(e => e.InstructorUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Membership>(entity =>
            {
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.PersonalTrainerOption).HasConversion<string>().HasMaxLength(20);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Plan)
                    .WithMany()
                    .HasForeignKey(e => e.PlanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ReviewedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Method).HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

                entity.HasOne(e => e.Membership)
                    .WithMany(m => m.Payments)
                    .HasForeignKey(e => e.MembershipId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ConfirmedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ConfirmedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CheckIn>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CheckedInByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CheckedInByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CheckedOutByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CheckedOutByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TrainerRequest>(entity =>
            {
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Trainer)
                    .WithMany()
                    .HasForeignKey(e => e.TrainerUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TrainerSession>(entity =>
            {
                entity.HasOne(e => e.Trainer)
                    .WithMany()
                    .HasForeignKey(e => e.TrainerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkoutPlan>(entity =>
            {
                entity.HasOne(e => e.Trainer)
                    .WithMany()
                    .HasForeignKey(e => e.TrainerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FitnessGoal>(entity =>
            {
                entity.Property(e => e.GoalType).HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.StartingWeightKg).HasColumnType("decimal(5,2)");
                entity.Property(e => e.TargetWeightKg).HasColumnType("decimal(5,2)");
                entity.Property(e => e.ActivityLevel).HasConversion<string>().HasMaxLength(20);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProgressLog>(entity =>
            {
                entity.Property(e => e.WeightKg).HasColumnType("decimal(5,2)");

                entity.HasOne(e => e.FitnessGoal)
                    .WithMany(g => g.ProgressLogs)
                    .HasForeignKey(e => e.FitnessGoalId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SessionBooking>(entity =>
            {
                entity.HasOne(e => e.Session)
                    .WithMany()
                    .HasForeignKey(e => e.SessionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.Severity).HasConversion<string>().HasMaxLength(20);

                entity.HasOne(e => e.ReportedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ReportedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<XpEvent>(entity =>
            {
                entity.Property(e => e.Reason).HasConversion<string>().HasMaxLength(30);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Badge>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
            });

            modelBuilder.Entity<UserBadge>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.BadgeId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Badge)
                    .WithMany()
                    .HasForeignKey(e => e.BadgeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Challenge>(entity =>
            {
                entity.Property(e => e.Metric).HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.TargetValue).HasColumnType("decimal(10,2)");

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ChallengeParticipant>(entity =>
            {
                entity.Property(e => e.CurrentValue).HasColumnType("decimal(10,2)");
                entity.HasIndex(e => new { e.ChallengeId, e.UserId }).IsUnique();

                entity.HasOne(e => e.Challenge)
                    .WithMany()
                    .HasForeignKey(e => e.ChallengeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserReward>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.RewardId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Reward)
                    .WithMany()
                    .HasForeignKey(e => e.RewardId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Friendship>(entity =>
            {
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
                entity.HasIndex(e => new { e.UserId, e.FriendUserId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.FriendUser)
                    .WithMany()
                    .HasForeignKey(e => e.FriendUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Equipment>(entity =>
            {
                // Serial number/venue are required at registration (server-validated
                // in EquipmentController), but kept nullable at the DB level since
                // equipment registered before this field existed won't have one.
                entity.Property(e => e.SerialNumber).IsRequired(false);
                entity.Property(e => e.VenueId).IsRequired(false);

                entity.HasIndex(e => e.SerialNumber)
                    .IsUnique()
                    .HasFilter("[SerialNumber] IS NOT NULL");

                entity.HasIndex(e => e.QrCode)
                    .IsUnique()
                    .HasFilter("[QrCode] IS NOT NULL");

                entity.HasOne(e => e.Venue)
                    .WithMany()
                    .HasForeignKey(e => e.VenueId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CapacitySlotBooking>(entity =>
            {
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

                entity.HasOne(e => e.CapacitySlot)
                    .WithMany()
                    .HasForeignKey(e => e.CapacitySlotId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}