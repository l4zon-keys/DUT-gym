using LoginFormASPCore6.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Services
{
    // Minimal friend connections, feeding the gamification "friends" leaderboard.
    public class FriendshipService
    {
        private readonly MyDbContext db;

        public FriendshipService(MyDbContext db)
        {
            this.db = db;
        }

        public async Task<(bool Ok, string? Error)> SendRequestAsync(int userId, int friendUserId)
        {
            if (userId == friendUserId)
            {
                return (false, "You can't add yourself as a friend.");
            }

            var friendExists = await db.Users.AnyAsync(u => u.Id == friendUserId);
            if (!friendExists)
            {
                return (false, "That user doesn't exist.");
            }

            var existing = await db.Friendships.FirstOrDefaultAsync(f =>
                (f.UserId == userId && f.FriendUserId == friendUserId) ||
                (f.UserId == friendUserId && f.FriendUserId == userId));
            if (existing != null)
            {
                return (false, existing.Status == FriendshipStatus.Accepted
                    ? "You're already friends."
                    : "A friend request is already pending.");
            }

            db.Friendships.Add(new Friendship { UserId = userId, FriendUserId = friendUserId });
            await db.SaveChangesAsync();
            return (true, null);
        }

        public async Task AcceptRequestAsync(int userId, int requestId)
        {
            var request = await db.Friendships.FirstOrDefaultAsync(f =>
                f.Id == requestId && f.FriendUserId == userId && f.Status == FriendshipStatus.Pending);
            if (request == null) return;

            request.Status = FriendshipStatus.Accepted;
            await db.SaveChangesAsync();
        }

        public async Task RemoveAsync(int userId, int friendshipId)
        {
            var friendship = await db.Friendships.FirstOrDefaultAsync(f =>
                f.Id == friendshipId && (f.UserId == userId || f.FriendUserId == userId));
            if (friendship == null) return;

            db.Friendships.Remove(friendship);
            await db.SaveChangesAsync();
        }

        public async Task<List<Friendship>> GetAcceptedFriendsAsync(int userId)
            => await db.Friendships
                .Include(f => f.User)
                .Include(f => f.FriendUser)
                .Where(f => f.Status == FriendshipStatus.Accepted && (f.UserId == userId || f.FriendUserId == userId))
                .ToListAsync();

        public async Task<List<Friendship>> GetPendingIncomingRequestsAsync(int userId)
            => await db.Friendships
                .Include(f => f.User)
                .Where(f => f.FriendUserId == userId && f.Status == FriendshipStatus.Pending)
                .ToListAsync();
    }
}
