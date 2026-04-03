using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Dodaj ovo za OpenApiInfo i Security shemu
using MongoDB.Bson;
using Neo4j.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using TastyTrails.Configurations;
using TastyTrails.Services;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- POPRAVLJEN SWAGGER DA IMA AUTHORIZE DUGME ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TastyTrails API", Version = "v1" });

    // Definisanje Bearer Security Sheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Unesite JWT token ovako: Bearer {vaš_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// --- MONGO PODEŠAVANJA ---
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoSettings"));
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
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, //
        ValidateAudience = true, //
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

app.UseHttpsRedirection();

// CORS mora pre Auth
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();