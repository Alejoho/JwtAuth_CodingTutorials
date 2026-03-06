using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using WebApiAuthentication.Authentication;
using WebApiAuthentication.DataAccess.Context;
using WebApiAuthentication.DataAccess.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ReviewContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
        ValidIssuer = builder.Configuration["Jwt:ValidIssuer"],
        ValidAudiences = builder.Configuration.GetSection("Jwt:ValidAudiences").Get<string[]>(),
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("Secret not configured")))
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

var app = builder.Build();

{
    PopulateDb();
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

app.Run();

void PopulateDb()
{
    using var scope = app.Services.CreateScope();

    using var db = scope.ServiceProvider.GetRequiredService<ReviewContext>();

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
