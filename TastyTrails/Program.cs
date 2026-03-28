using TastyTrails.Configurations;
using TastyTrails.Services;
using Neo4j.Driver;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Bson;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- MONGO PODEŠAVANJA ---
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoSettings"));
// OSTAVI SAMO OVU JEDNU LINIJU ZA MONGO:
builder.Services.AddSingleton<MongoService>();

// --- NEO4J PODEŠAVANJA ---
builder.Services.AddScoped<INeo4jService, Neo4jService>();
builder.Services.Configure<Neo4jSettings>(builder.Configuration.GetSection("Neo4jSettings"));
builder.Services.AddSingleton<IDriver>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<Neo4jSettings>>().Value;
    return GraphDatabase.Driver(settings.Uri, AuthTokens.Basic(settings.User, settings.Password));
});

builder.Services.AddScoped<AuthService>();

// --- JWT PODEŠAVANJA ---
var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>() ?? throw new Exception("JwtSettings section missing!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey!))
    };
});

// --- CORS PODEŠAVANJA ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// --- ISPRAVAN REDOSLED MIDDLEWARE-A ---

// 1. Prvo redirekcija
app.UseHttpsRedirection();

// 2. OBAVEZNO CORS pre Auth
app.UseCors("AllowFrontend");

// 3. Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4. Autentikacija pa Autorizacija (SAMO JEDNOM!)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();