using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using WebApplication3.Data;
using WebApplication3.Middlewares;
using WebApplication3.Models;
using WebApplication3.Repositories;
using WebApplication3.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);

    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Вставьте JWT токен вот так: Bearer {ваш_токен}",
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

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

SQLitePCL.Batteries.Init();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=mydatabase.db"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IWalletService, WalletService>();

builder.Services.AddScoped<IPasswordHasherService, BCryptPasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaClient",
        policy => policy
            .WithOrigins("http://127.0.0.1:5500", "http://localhost:5500")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();

    var serviceProvider = scope.ServiceProvider;
    await SeedAdmin(serviceProvider);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration(); 
});
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("SpaClient");
app.MapControllers();
// Простой тестовый endpoint
app.MapGet("/ping", () => "pong");
app.Run();

static async Task SeedAdmin(IServiceProvider serviceProvider)
{
    var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
    var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasherService>();

    var adminEmail = "admin@myapp.ru";
    var adminPassword = "AdminSecurePassword123!";

    var existingAdmin = await userRepository.FindByEmail(adminEmail);

    if (existingAdmin == null)
    {
        string passwordHash = passwordHasher.HashPassword(adminPassword);

        var admin = new User
        {
            Name = "Administrator",
            Email = adminEmail,
            PasswordHash = passwordHash,
            Role = "Admin"
        };

        await userRepository.AddUser(admin);
    }
}