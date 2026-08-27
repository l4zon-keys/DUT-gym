using LoginFormASPCore6.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Services
{
    public enum CapacityLevel
    {
        Light,
        Moderate,
        Heavy
    }

    public class CapacityStatus
    {
        public int CurrentOccupancy { get; set; }
        public int Threshold { get; set; }
        public CapacityLevel Level { get; set; }
    }

    public class GymCapacityService
    {
        private readonly MyDbContext context;
        private readonly IConfiguration configuration;

        public GymCapacityService(MyDbContext context, IConfiguration configuration)
        {
            this.context = context;
            this.configuration = configuration;
        }

        public async Task<CapacityStatus> GetCurrentStatusAsync()
        {
            var occupancy = await context.CheckIns.CountAsync(c => c.CheckOutTime == null);
            var threshold = configuration.GetValue<int?>("GymCapacity:Threshold") ?? 100;
            var moderateAt = configuration.GetValue<double?>("GymCapacity:ModerateAt") ?? 0.5;
            var heavyAt = configuration.GetValue<double?>("GymCapacity:HeavyAt") ?? 0.8;

            return new CapacityStatus
            {
                CurrentOccupancy = occupancy,
                Threshold = threshold,
                Level = CalculateLevel(occupancy, threshold, moderateAt, heavyAt)
            };
        }

        // Pure and separated out so the threshold math is unit-testable without a DbContext.
        public static CapacityLevel CalculateLevel(int occupancy, int threshold, double moderateAt, double heavyAt)
        {
            var ratio = threshold <= 0 ? 0 : (double)occupancy / threshold;
            return ratio >= heavyAt ? CapacityLevel.Heavy
                : ratio >= moderateAt ? CapacityLevel.Moderate
                : CapacityLevel.Light;
        }
    }
}
