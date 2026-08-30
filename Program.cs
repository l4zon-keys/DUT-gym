using System.Globalization;
using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
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

// Set PaymentGateway:Provider to "Mock" or "PayFast" in appsettings.json to switch.
var paymentProvider = builder.Configuration["PaymentGateway:Provider"] ?? "Mock";
if (string.Equals(paymentProvider, "PayFast", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IPaymentGateway, PayFastGateway>();
}
else
{
    builder.Services.AddScoped<IPaymentGateway, MockPaymentGateway>();
}
builder.Services.AddScoped<GymCapacityService>();
builder.Services.AddScoped<AttendanceStreakService>();
builder.Services.AddScoped<AttendanceReportService>();

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