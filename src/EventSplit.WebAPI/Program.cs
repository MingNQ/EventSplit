using System.Text;
using EventSplit.Application.Interfaces;
using EventSplit.Application.Services;
using EventSplit.Infrastructure.Data;
using EventSplit.Infrastructure.Services;
using EventSplit.WebAPI.Hubs;
using EventSplit.WebAPI.Jobs;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using EmailService = EventSplit.Infrastructure.Services.EmailService;

var builder = WebApplication.CreateBuilder(args);

// --- Database ---
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Convert PostgreSQL URI format (from Render) to ADO.NET format (for Npgsql)
static string ConvertConnectionString(string? connStr)
{
    if (string.IsNullOrEmpty(connStr)) 
        throw new InvalidOperationException("Connection string 'Default' is not configured.");
    
    if (connStr.StartsWith("postgres://") || connStr.StartsWith("postgresql://"))
    {
        var uri = new Uri(connStr);
        var userInfo = uri.UserInfo.Split(':');
        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
    }
    return connStr;
}

var connectionString = ConvertConnectionString(builder.Configuration.GetConnectionString("Default"));
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// --- Services ---
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<OverduePaymentJob>();

// --- JWT Authentication ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "EventSplitSuperSecretKey12345678!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "EventSplit",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EventSplit",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        // Allow SignalR to use JWT from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/payment"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// --- Hangfire ---
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

// --- SignalR ---
builder.Services.AddSignalR();

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "https://event-split.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// --- Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Auto migrate ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// --- Middleware ---
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");

// --- Routes ---
app.MapControllers();
app.MapHub<PaymentHub>("/hubs/payment");

// --- Recurring Job: Check overdue payments every 5 minutes ---
RecurringJob.AddOrUpdate<OverduePaymentJob>(
    "mark-overdue-payments",
    job => job.ExecuteAsync(),
    "*/5 * * * *");

app.Run();
