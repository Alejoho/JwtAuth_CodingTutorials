using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using WebApiAuthentication.Authentication;
using WebApiAuthentication.DataAccess.Context;
using WebApiAuthentication.DataAccess.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ReviewContext>(opts =>
{
    opts.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        opts =>
        {
            // These 2 lines are for trying to avoid a transient error when connecting
            // to a dockered Sql Server instance
            opts.CommandTimeout(5);
            opts.EnableRetryOnFailure(10, TimeSpan.FromSeconds(3), [-2147467259]);

        });
});

using var loggerFactory = LoggerFactory.Create(
    b => b.SetMinimumLevel(LogLevel.Trace).AddConsole());

builder.Services.AddIdentity<LibraryUser, IdentityRole>(
    opts => opts.User.AllowedUserNameCharacters += " ")
    .AddEntityFrameworkStores<ReviewContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IReviewRepository, SqlServerRepository>();

builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(opts =>
{
    opts.SaveToken = true;
    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = builder.Configuration["Jwt:ValidIssuer"],
        ValidAudiences = builder.Configuration.GetSection("Jwt:ValidAudiences").Get<string[]>(),
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("Secret not configured")))
    };
    opts.Events = new JwtBearerEvents
    {
        OnChallenge = ctx => LogAttempt(ctx.Request.Headers, "OnChallenge", ctx.Request.Path),
        OnTokenValidated = ctx => LogAttempt(ctx.Request.Headers, "OnTokenValidated", ctx.Request.Path),
        OnAuthenticationFailed = ctx => LogAttempt(ctx.Request.Headers, "OnAuthenticationFailed", ctx.Request.Path),
        OnForbidden = ctx => LogAttempt(ctx.Request.Headers, "OnForbidden", ctx.Request.Path),
        OnMessageReceived = ctx => LogAttempt(ctx.Request.Headers, "OnMessageReceived", ctx.Request.Path)
        // See the order of these events at the end of the file
    };
});

builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

{
    InitializeDb();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("");
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

void InitializeDb()
{
    using var scope = app.Services.CreateScope();

    using var db = scope.ServiceProvider.GetRequiredService<ReviewContext>();

    db.Database.Migrate();

    SeedIfEmpty(db);
}

void SeedIfEmpty(ReviewContext db)
{
    if (db.BookReviews.Any())
    {
        return;
    }

    db.BookReviews.Add(new() { Title = "Dr No", Rating = 4 });
    db.BookReviews.Add(new() { Title = "Goldfinger", Rating = 3 });
    db.BookReviews.Add(new() { Title = "From Russia with Love", Rating = 1 });
    db.BookReviews.Add(new() { Title = "Moonraker", Rating = 4 });
    db.BookReviews.Add(new() { Title = "Dr No", Rating = 5 });
    db.BookReviews.Add(new() { Title = "Moonraker", Rating = 2 });
    db.BookReviews.Add(new() { Title = "Dr No", Rating = 2 });
    db.BookReviews.Add(new() { Title = "From Russia with Love", Rating = 5 });
    db.BookReviews.Add(new() { Title = "From Russia with Love", Rating = 3 });

    db.SaveChanges();
}


Task LogAttempt(IHeaderDictionary headers, string eventType, string path)
{
    var logger = loggerFactory.CreateLogger<Program>();

    var authorizationHeader = headers["Authorization"].FirstOrDefault();

    if (authorizationHeader is null
        || authorizationHeader is "Bearer"
        || authorizationHeader is "Bearer ")
    {
        logger.LogInformation($"{eventType}. JWT not present");
    }
    else
    {
        string jwtString = authorizationHeader.Substring("Bearer ".Length);

        var jwt = new JwtSecurityToken(jwtString);

        logger.LogInformation("""
            {0}. 
            Path: {1}
            System time: {2}.
            Expiration: {3}. 
            """,
            eventType,
            path,
            DateTime.UtcNow.ToLongTimeString(),
            jwt.ValidTo.ToLongTimeString());
    }

    return Task.CompletedTask;
}

/*
 
1-OnMessageReceived

If auth failed becuase of token expired

    2-AuthenticationFailed

    3-OnChallenge
 
If auth succeded

    2-OnTokenValidated

If auth failed because of bad token



 */
