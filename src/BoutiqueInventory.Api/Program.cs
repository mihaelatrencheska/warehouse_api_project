using System.Text;
using System.Text.Json.Serialization;
using BoutiqueInventory.Api.Auth;
using BoutiqueInventory.Api.Filters;
using BoutiqueInventory.Api.Hubs;
using BoutiqueInventory.Api.Middleware;
using BoutiqueInventory.Api.Notifications;
using BoutiqueInventory.Application;
using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Infrastructure;
using BoutiqueInventory.Infrastructure.Data;
using BoutiqueInventory.Infrastructure.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var dataDir = Path.Combine(builder.Environment.ContentRootPath, "Data");
    Directory.CreateDirectory(dataDir);

    builder.Host.UseSerilog((context, _, cfg) => cfg
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/boutique-.log", rollingInterval: RollingInterval.Day));

    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
    builder.Services.Configure<BoutiqueAuthOptions>(builder.Configuration.GetSection(BoutiqueAuthOptions.SectionName));
    builder.Services.AddSingleton<TokenService>();

    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Auth:Jwt configuration is required.");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    builder.Services
        .AddControllers(o => o.Filters.Add<ValidationFilter>())
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    builder.Services.AddSignalR();
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddInfrastructureLayer(builder.Configuration);
    builder.Services.AddApplicationLayer();

    builder.Services.AddScoped<SignalRExpirationNotifier>();
    builder.Services.AddScoped<IExpirationNotifier>(sp => new CompositeExpirationNotifier(
    [
        sp.GetRequiredService<EmailExpirationNotifier>(),
        sp.GetRequiredService<WebhookExpirationNotifier>(),
        sp.GetRequiredService<SignalRExpirationNotifier>()
    ]));

    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "Boutique Inventory & Warehouse API";
            document.Info.Version = "v1";
            document.Info.Description =
                "Clean-architecture REST API for managing boutique warehouses, sections, products, categories, " +
                "search and expiration alerts. Authenticate via POST /api/auth/login.";
            return Task.CompletedTask;
        });
    });

    builder.Services.AddCors(options => options.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()));

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var searchIndex = scope.ServiceProvider.GetRequiredService<IProductSearchIndex>();
            await DbSeeder.MigrateAndSeedAsync(db, searchIndex);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database migration or seed failed.");
            throw;
        }
    }

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    app.UseStaticFiles();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference("/scalar", options => options
            .WithTitle("Boutique Inventory API")
            .WithTheme(ScalarTheme.BluePlanet)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<AlertsHub>("/hubs/alerts");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
