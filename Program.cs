using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StockBridge.API.Auth;
using StockBridge.API.Data;
using StockBridge.API.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Render (ve genel olarak Docker/PaaS) konteyneri dinamik bir PORT env degiskeni ile
// çalismayi bekler; yerel gelistirmede (launchSettings.json) bu degisken olmadigi
// icin normal davranis degismez.
var containerPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(containerPort))
{
    // TLS, platform (Render/Railway) tarafinda edge'de sonlandirilir; konteynere
    // sadece duz HTTP ile dahili trafik yönlendirilir, bu yuzden burada http kullanimi kasitlidir.
    builder.WebHost.UseUrls($"http://0.0.0.0:{containerPort}"); // NOSONAR
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7029")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Neon (veya baska bir bulut Postgres) baglanti bilgisi DATABASE_URL env degiskeni
// (postgres://user:pass@host:port/db) olarak Render'a manuel eklenir; bu deger,
// appsettings.json'daki ConnectionStrings:DefaultConnection degerini
// (yerel/Docker Compose gelistirme icin) gecersiz kilar.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    connectionString = BuildNpgsqlConnectionString(databaseUrl);
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IErpService, MockIfsErpService>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IGroqService, GroqService>();

// JWT auth sat�r�ndan �NCE
builder.Services.AddScoped<IClaimsTransformation, ZitadelRoleTransformer>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://stockbridge-8rhlij.eu1.zitadel.cloud";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://stockbridge-8rhlij.eu1.zitadel.cloud",
            ValidateAudience = true,
            ValidAudiences = new[] { "373940220195845278", "373939994760364801" },
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // ZitadelRoleTransformer, ham "urn:zitadel:..." rol claim'ini ClaimTypes.Role
            // olarak yeniden ekliyor; [Authorize(Roles = "...")] bu claim tipini bekler.
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"JWT HATA: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddHostedService<ErpSyncBackgroundJob>();

var app = builder.Build();

// Konteyner ortaminda (Docker Compose / Railway) elle "dotnet ef database update"
// calistirmak pratik degil; bu yuzden bekleyen migration'lar her baslangicta
// otomatik uygulanir. Migrate() idempotenttir, zaten uygulanmis migration'lari atlar.
using (var migrationScope = app.Services.CreateScope())
{
    var db = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Railway gibi PaaS'larda TLS, uygulamanin onundeki edge proxy'de sonlanir ve konteynere
// duz HTTP olarak iletilir. Bu middleware olmadan UseHttpsRedirection() sonsuz yonlendirme
// dongusune girer, cunku Kestrel istegi hep HTTP olarak gorur.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Bulut Postgres saglayicilarinin (Neon, Railway, Render Postgres...) verdigi
// DATABASE_URL (postgres://user:pass@host:port/dbname) formatini Npgsql'in
// bekledigi anahtar=deger baglanti dizesine cevirir.
static string BuildNpgsqlConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
    var database = uri.AbsolutePath.TrimStart('/');
    var port = uri.Port == -1 ? 5432 : uri.Port;

    // Neon gibi saglayicilar SSL olmadan baglantiyi reddeder; Require ile aciktan zorunlu kiliyoruz.
    return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}