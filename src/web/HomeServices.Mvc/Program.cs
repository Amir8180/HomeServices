using FluentValidation.AspNetCore;
using HomeServices.Application;
using HomeServices.Application.Contracts;
using HomeServices.Infrastructure;
using HomeServices.Infrastructure.Data;
using HomeServices.Infrastructure.Persistence.Seed;
using HomeServices.Mvc.Extensions;
using HomeServices.Mvc.Services;
using Serilog;

// ---------- Serilog bootstrap ----------
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
Log.Information("Starting HomeServices.Mvc");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// ---------- Application + Infrastructure ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---------- HTTP context + current user ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ---------- Authentication (cookie backed by JWT claims) ----------
builder.Services.AddHomeServicesAuth(builder.Configuration);

// (MvcServiceCollectionExtensions is the internal auth/principal helper above)

// ---------- MVC + FluentValidation ----------
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute>();
})
.AddFluentValidation(fv =>
{
    fv.RegisterValidatorsFromAssemblyContaining<HomeServices.Application.Validators.CreateCategoryDtoValidator>();
    fv.ImplicitlyValidateChildProperties = true;
});

// ---------- Migrate + Seed ----------
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    await DbInitializer.InitializeAsync(context, loggerFactory.CreateLogger("DbInitializer"));
}

// ---------- Pipeline ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ---------- Routes ----------
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Portfolio}/{action=Index}/{id?}");

app.Run();
