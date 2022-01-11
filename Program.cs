using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MinimalApiAuth;
using MinimalApiAuth.Models;
using MinimalApiAuth.Repositories;
using MinimalApiAuth.Services;

var builder = WebApplication.CreateBuilder(args);

var key = Encoding.ASCII.GetBytes(Settings.Secret);
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("Admin", policy => policy.RequireRole("manager"));
        options.AddPolicy("Employee", policy => policy.RequireRole("employee"));
    }
);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", (User model) =>
{
    var user = UserRepository.Get(model.Username, model.Password);

    if(user == null)
    {
        return Results.NotFound(new { message = "Invalid username or password" });
    }

    var token = TokenService.GenerateToken(user);
    user.Password = string.Empty;
    
    return Results.Ok(
        new {
            user = user,
            token = token
        }
    );
});

app.MapGet("/anonymous", () => { Results.Ok("anonymous"); }).AllowAnonymous();

app.MapGet("/autenticated", (ClaimsPrincipal user) => { 
    Results.Ok(new { message = $"Autenticated as {user.Identity.Name}" });
}).RequireAuthorization();

app.MapGet("/authorizationAdmin", (ClaimsPrincipal user) => { 
    Results.Ok(new { message = $"Autenticated as {user.Identity.Name}" });
}).RequireAuthorization("Admin");

app.MapGet("/authorizationEmployee", (ClaimsPrincipal user) => { 
    Results.Ok(new { message = $"Autenticated as {user.Identity.Name}" });
}).RequireAuthorization("Employee");

app.Run();
