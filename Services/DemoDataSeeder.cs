using LoginFormASPCore6.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Services
{
    public static class DemoDataSeeder
    {
        private const string DemoPassword = "Demo123!";

        public static async Task SeedReferenceDataAsync(MyDbContext db)
        {
            await SeedBadgesAsync(db);
            await SeedRewardsAsync(db);
        }

        public static async Task SeedShowcaseDataAsync(MyDbContext db)
        {
            await SeedReferenceDataAsync(db);

            var hasher = new PasswordHasher<User>();
            var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == EmailRoleHelper.AdminRole)
                ?? await EnsureUserAsync(db, hasher, "Demo Admin", "90000001", "demo.admin@dut.ac.za", EmailRoleHelper.AdminRole, null);

            var plan = await EnsurePlanAsync(db);
            var students = new[]
            {
                await EnsureUserAsync(db, hasher, "Ayanda Mkhize", "23010001", "ayanda.demo@dut4life.ac.za", EmailRoleHelper.StudentRole, null),
                await EnsureUserAsync(db, hasher, "Sneh Ntuli", "23010002", "sneh.demo@dut4life.ac.za", EmailRoleHelper.StudentRole, null),
                await EnsureUserAsync(db, hasher, "Thando Dlamini", "23010003", "thando.demo@dut4life.ac.za", EmailRoleHelper.StudentRole, null),
                await EnsureUserAsync(db, hasher, "Nandi Cele", "23010004", "nandi.demo@dut4life.ac.za", EmailRoleHelper.StudentRole, null),
                await EnsureUserAsync(db, hasher, "Musa Naidoo", "23010005", "musa.demo@dut4life.ac.za", EmailRoleHelper.StudentRole, null)
            };

            var now = DateTime.UtcNow;
            await EnsureMembershipAsync(db, students[0], plan, admin, "Steve Biko Campus", MembershipStatus.Active, now.AddMonths(-5), PaymentMethod.Card, plan.Price);
            await EnsureMembershipAsync(db, students[1], plan, admin, "Ritson Campus", MembershipStatus.Active, now.AddMonths(-3), PaymentMethod.Eft, plan.Price);
            await EnsureMembershipAsync(db, students[2], plan, admin, "ML Sultan Campus", MembershipStatus.Active, now.AddMonths(-1), PaymentMethod.MobileMoney, plan.Price);
            await EnsureMembershipAsync(db, students[3], plan, admin, "City Campus", MembershipStatus.Pending, now.AddDays(-7), PaymentMethod.Cash, plan.Price);
            await EnsureMembershipAsync(db, students[4], plan, admin, "Steve Biko Campus", MembershipStatus.Expired, now.AddMonths(-8), PaymentMethod.Card, plan.Price);

            await SeedCheckInsAsync(db, students, admin, now);
            await SeedXpAsync(db, students, now);
            await SeedBadgesForUsersAsync(db, students, now);
            await SeedRewardsForUsersAsync(db, students, now);
            await SeedFriendshipsAsync(db, students);
            await SeedChallengesAsync(db, admin, students, now);

            await db.SaveChangesAsync();
        }

        private static async Task SeedBadgesAsync(MyDbContext db)
        {
            var badges = new[]
            {
                new Badge { Name = "Early Bird", Description = "Checked in before 7am.", IconClass = "bi-sunrise", Code = "early_bird" },
                new Badge { Name = "5-Day Streak", Description = "Attended 5 different days in one week.", IconClass = "bi-fire", Code = "five_day_streak" },
                new Badge { Name = "Zumba Fanatic", Description = "Booked 5 Zumba classes.", IconClass = "bi-music-note-beamed", Code = "zumba_fanatic" }
            };

            foreach (var badge in badges)
            {
                if (!await db.Badges.AnyAsync(b => b.Code == badge.Code))
                {
                    db.Badges.Add(badge);
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task SeedRewardsAsync(MyDbContext db)
        {
            var rewards = new[]
            {
                new Reward { Name = "10% Discount Code", Description = "10% off your next membership renewal.", IconClass = "bi-tag", RequiredXp = 200 },
                new Reward { Name = "Certificate of Achievement", Description = "Recognition for consistent gym attendance.", IconClass = "bi-patch-check", RequiredXp = 400 },
                new Reward { Name = "Free PT Session", Description = "One complimentary personal training session.", IconClass = "bi-person-check", RequiredXp = 700 },
                new Reward { Name = "Gym Merch", Description = "A branded DUT Gym t-shirt or water bottle.", IconClass = "bi-bag-heart", RequiredXp = 1000 }
            };

            foreach (var reward in rewards)
            {
                if (!await db.Rewards.AnyAsync(r => r.Name == reward.Name))
                {
                    db.Rewards.Add(reward);
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task<User> EnsureUserAsync(MyDbContext db, PasswordHasher<User> hasher, string name, string studentNumber, string email, string role, ApprovalStatus? approvalStatus)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null) return user;

            user = new User
            {
                EmpName = name,
                Gender = "Female",
                StudentNumber = studentNumber,
                Email = email,
                Role = role,
                ApprovalStatus = approvalStatus
            };
            user.Password = hasher.HashPassword(user, DemoPassword);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }

        private static async Task<MembershipPlan> EnsurePlanAsync(MyDbContext db)
        {
            var plan = await db.MembershipPlans.FirstOrDefaultAsync(p => p.Name == "Demo Semester Access");
            if (plan != null) return plan;

            plan = new MembershipPlan
            {
                Name = "Demo Semester Access",
                Description = "Showcase membership plan for demo analytics.",
                Price = 350m,
                DurationMonths = 6,
                IsActive = true
            };
            db.MembershipPlans.Add(plan);
            await db.SaveChangesAsync();
            return plan;
        }

        private static async Task EnsureMembershipAsync(MyDbContext db, User user, MembershipPlan plan, User admin, string campus, MembershipStatus status, DateTime appliedAt, PaymentMethod method, decimal amount)
        {
            if (await db.Memberships.AnyAsync(m => m.UserId == user.Id && m.PlanId == plan.Id)) return;

            var membership = new Membership
            {
                UserId = user.Id,
                PlanId = plan.Id,
                Campus = campus,
                EmergencyContactName = "Demo Contact",
                EmergencyContactPhone = "0710000000",
                MedicalConsentAccepted = true,
                Status = status,
                AppliedAt = appliedAt,
                StartDate = status == MembershipStatus.Pending ? null : appliedAt.Date,
                ExpiryDate = status == MembershipStatus.Expired ? appliedAt.Date.AddMonths(6) : appliedAt.Date.AddMonths(plan.DurationMonths),
                ReviewedByUserId = status == MembershipStatus.Pending ? null : admin.Id,
                ReviewedAt = status == MembershipStatus.Pending ? null : appliedAt.AddDays(1),
                StatusNote = status == MembershipStatus.Expired ? "Demo expired membership for status analytics." : null
            };

            db.Memberships.Add(membership);
            await db.SaveChangesAsync();

            db.Payments.Add(new Payment
            {
                MembershipId = membership.Id,
                Method = method,
                Amount = amount,
                Status = status == MembershipStatus.Pending ? PaymentStatus.Pending : PaymentStatus.Paid,
                Reference = $"DEMO{user.StudentNumber}",
                SubmittedAt = appliedAt.AddHours(2),
                PaidAt = status == MembershipStatus.Pending ? null : appliedAt.AddHours(3),
                ConfirmedByUserId = status == MembershipStatus.Pending ? null : admin.Id
            });
            await db.SaveChangesAsync();
        }

        private static async Task SeedCheckInsAsync(MyDbContext db, User[] students, User admin, DateTime now)
        {
            if (await db.CheckIns.AnyAsync(c => students.Select(s => s.Id).Contains(c.UserId))) return;

            var patterns = new[]
            {
                new { User = students[0], Months = 6, VisitsPerMonth = 9, Hour = 6 },
                new { User = students[1], Months = 4, VisitsPerMonth = 6, Hour = 17 },
                new { User = students[2], Months = 2, VisitsPerMonth = 4, Hour = 12 },
                new { User = students[3], Months = 1, VisitsPerMonth = 2, Hour = 8 },
                new { User = students[4], Months = 5, VisitsPerMonth = 3, Hour = 18 }
            };

            foreach (var pattern in patterns)
            {
                for (var monthOffset = pattern.Months - 1; monthOffset >= 0; monthOffset--)
                {
                    var month = new DateTime(now.Year, now.Month, 1).AddMonths(-monthOffset);
                    for (var i = 0; i < pattern.VisitsPerMonth; i++)
                    {
                        var day = Math.Min(2 + i * 3, DateTime.DaysInMonth(month.Year, month.Month));
                        var checkIn = new DateTime(month.Year, month.Month, day, pattern.Hour + i % 3, 15, 0, DateTimeKind.Utc);
                        db.CheckIns.Add(new CheckIn
                        {
                            UserId = pattern.User.Id,
                            CheckedInByUserId = admin.Id,
                            CheckedOutByUserId = admin.Id,
                            CheckInTime = checkIn,
                            CheckOutTime = checkIn.AddMinutes(55 + (i % 4) * 15)
                        });
                    }
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task SeedXpAsync(MyDbContext db, User[] students, DateTime now)
        {
            var xpTotals = new[] { 1160, 465, 180, 65, 260 };
            for (var i = 0; i < students.Length; i++)
            {
                var sourceId = -9000 - i;
                if (await db.XpEvents.AnyAsync(e => e.UserId == students[i].Id && e.SourceId == sourceId)) continue;

                db.XpEvents.Add(new XpEvent
                {
                    UserId = students[i].Id,
                    Points = xpTotals[i],
                    Reason = XpReason.ChallengeCompleted,
                    SourceId = sourceId,
                    CreatedAt = now.AddDays(-i * 4)
                });
            }

            await db.SaveChangesAsync();
        }

        private static async Task SeedBadgesForUsersAsync(MyDbContext db, User[] students, DateTime now)
        {
            var badgeMap = await db.Badges.ToDictionaryAsync(b => b.Code);
            await EnsureBadgeAsync(db, students[0], badgeMap["early_bird"], now.AddMonths(-4));
            await EnsureBadgeAsync(db, students[0], badgeMap["five_day_streak"], now.AddMonths(-2));
            await EnsureBadgeAsync(db, students[0], badgeMap["zumba_fanatic"], now.AddDays(-12));
            await EnsureBadgeAsync(db, students[1], badgeMap["early_bird"], now.AddMonths(-1));
            await EnsureBadgeAsync(db, students[1], badgeMap["five_day_streak"], now.AddDays(-9));
            await EnsureBadgeAsync(db, students[2], badgeMap["early_bird"], now.AddDays(-7));
            await EnsureBadgeAsync(db, students[4], badgeMap["zumba_fanatic"], now.AddMonths(-3));
        }

        private static async Task EnsureBadgeAsync(MyDbContext db, User user, Badge badge, DateTime earnedAt)
        {
            if (await db.UserBadges.AnyAsync(ub => ub.UserId == user.Id && ub.BadgeId == badge.Id)) return;
            db.UserBadges.Add(new UserBadge { UserId = user.Id, BadgeId = badge.Id, EarnedAt = earnedAt });
        }

        private static async Task SeedRewardsForUsersAsync(MyDbContext db, User[] students, DateTime now)
        {
            var rewards = await db.Rewards.OrderBy(r => r.RequiredXp).ToListAsync();
            var rewardCounts = new[] { 4, 2, 0, 0, 1 };
            for (var i = 0; i < students.Length; i++)
            {
                foreach (var reward in rewards.Take(rewardCounts[i]))
                {
                    if (await db.UserRewards.AnyAsync(ur => ur.UserId == students[i].Id && ur.RewardId == reward.Id)) continue;
                    db.UserRewards.Add(new UserReward { UserId = students[i].Id, RewardId = reward.Id, EarnedAt = now.AddDays(-reward.RequiredXp / 50) });
                }
            }
        }

        private static async Task SeedFriendshipsAsync(MyDbContext db, User[] students)
        {
            await EnsureFriendshipAsync(db, students[0], students[1]);
            await EnsureFriendshipAsync(db, students[0], students[2]);
            await EnsureFriendshipAsync(db, students[1], students[4]);
        }

        private static async Task EnsureFriendshipAsync(MyDbContext db, User user, User friend)
        {
            if (await db.Friendships.AnyAsync(f =>
                (f.UserId == user.Id && f.FriendUserId == friend.Id) ||
                (f.UserId == friend.Id && f.FriendUserId == user.Id))) return;

            db.Friendships.Add(new Friendship
            {
                UserId = user.Id,
                FriendUserId = friend.Id,
                Status = FriendshipStatus.Accepted
            });
        }

        private static async Task SeedChallengesAsync(MyDbContext db, User admin, User[] students, DateTime now)
        {
            var challenge = await db.Challenges.FirstOrDefaultAsync(c => c.Title == "October Consistency Sprint");
            if (challenge == null)
            {
                challenge = new Challenge
                {
                    Title = "October Consistency Sprint",
                    Description = "Check in 12 times this month.",
                    Metric = ChallengeMetric.CheckInCount,
                    TargetValue = 12,
                    StartDate = new DateTime(now.Year, now.Month, 1),
                    EndDate = new DateTime(now.Year, now.Month, 1).AddMonths(1).AddDays(-1),
                    CreatedByUserId = admin.Id
                };
                db.Challenges.Add(challenge);
                await db.SaveChangesAsync();
            }

            var progress = new[] { 12m, 8m, 4m };
            for (var i = 0; i < progress.Length; i++)
            {
                if (await db.ChallengeParticipants.AnyAsync(p => p.ChallengeId == challenge.Id && p.UserId == students[i].Id)) continue;
                db.ChallengeParticipants.Add(new ChallengeParticipant
                {
                    ChallengeId = challenge.Id,
                    UserId = students[i].Id,
                    CurrentValue = progress[i],
                    Completed = progress[i] >= challenge.TargetValue,
                    CompletedAt = progress[i] >= challenge.TargetValue ? now.AddDays(-2) : null
                });
            }
        }
    }
}
