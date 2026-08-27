using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
using Microsoft.EntityFrameworkCore;

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