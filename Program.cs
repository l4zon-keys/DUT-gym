using System.Globalization;
using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// HTML number/date inputs always send period-decimal, invariant-format values
// regardless of the browser's locale. Without this, decimal model binding (and
// ToString("0.00") formatting) follows the server OS's culture - on a machine
// where that culture uses a comma decimal separator, posting "79.5" silently
// binds to null instead of throwing, which is a nasty silent-data-loss bug.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession();

// Configure DbContext directly using builder.Configuration and matching "DefaultConnection"
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<GymCapacityService>();
builder.Services.AddScoped<AttendanceStreakService>();
builder.Services.AddScoped<AttendanceReportService>();
builder.Services.AddScoped<GamificationService>();
builder.Services.AddScoped<FriendshipService>();
builder.Services.AddScoped<PredictiveCapacityService>();
builder.Services.AddScoped<CapacitySlotService>();

// Set Email:Provider to "Log" (default, no credentials needed) or "Smtp" (real
// sending - fill in Email:Smtp:* first) in appsettings.json to switch.
var emailProvider = builder.Configuration["Email:Provider"] ?? "Log";
if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddScoped<IEmailSender, LogEmailSender>();
}
builder.Services.AddHostedService<TrainerReminderBackgroundService>();

var app = builder.Build();

// Dev-only seeded admin so there's always a known way in locally without manual
// SQL. Gated to Development so this known password never exists on a real
// deployment (Azure, etc.) - only ever runs against your own LocalDB.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    db.Database.Migrate();

    const string devAdminEmail = "admin@dut.ac.za";
    if (!db.Users.Any(u => u.Email == devAdminEmail))
    {
        var hasher = new PasswordHasher<User>();
        var admin = new User
        {
            EmpName = "Admin",
            Gender = "Male",
            StudentNumber = "00000000",
            Email = devAdminEmail,
            Role = EmailRoleHelper.AdminRole
        };
        admin.Password = hasher.HashPassword(admin, "Admin123!");
        db.Users.Add(admin);
        db.SaveChanges();
    }

    // Gamification catalog - reference data needed in every environment, not a
    // dev-only convenience, so it's seeded unconditionally (idempotent) rather
    // than gated to Development like the admin account above.
    if (!db.Badges.Any())
    {
        db.Badges.AddRange(
            new Badge { Name = "Early Bird", Description = "Checked in before 7am.", IconClass = "bi-sunrise", Code = "early_bird" },
            new Badge { Name = "5-Day Streak", Description = "Attended 5 different days in one week.", IconClass = "bi-fire", Code = "five_day_streak" },
            new Badge { Name = "Zumba Fanatic", Description = "Booked 5 Zumba classes.", IconClass = "bi-music-note-beamed", Code = "zumba_fanatic" }
        );
        db.SaveChanges();
    }

    if (!db.Rewards.Any())
    {
        db.Rewards.AddRange(
            new Reward { Name = "10% Discount Code", Description = "10% off your next membership renewal.", IconClass = "bi-tag", RequiredXp = 200 },
            new Reward { Name = "Certificate of Achievement", Description = "Recognition for consistent gym attendance.", IconClass = "bi-patch-check", RequiredXp = 400 },
            new Reward { Name = "Free PT Session", Description = "One complimentary personal training session.", IconClass = "bi-person-check", RequiredXp = 700 },
            new Reward { Name = "Gym Merch", Description = "A branded DUT Gym t-shirt or water bottle.", IconClass = "bi-bag-heart", RequiredXp = 1000 }
        );
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();