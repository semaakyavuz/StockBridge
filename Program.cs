using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StockBridge.API.Auth;
using StockBridge.API.Data;
using StockBridge.API.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IErpService, MockIfsErpService>();
builder.Services.AddHttpClient();

// JWT auth satýrýndan ÖNCE
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
            RoleClaimType = "urn:zitadel:iam:org:project:roles"
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();