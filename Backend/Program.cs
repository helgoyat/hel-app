using HelApp.Backend.Data;
using HelApp.Backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var dbFolder = Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dbFolder);
var defaultConnection = $"Data Source={Path.Combine(dbFolder, "helapp.db")}";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? defaultConnection;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];

if (string.IsNullOrWhiteSpace(auth0Domain) || string.IsNullOrWhiteSpace(auth0Audience))
{
    throw new InvalidOperationException("Auth0 configuration is missing. Set Auth0:Domain and Auth0:Audience in appsettings or environment variables.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Domain}/";
        options.Audience = auth0Audience;
    });

static bool HasPermission(ClaimsPrincipal user, string permission)
{
    if (user.FindAll("permissions").Any(claim => claim.Value == permission))
    {
        return true;
    }

    var scopeValue = user.FindFirst("scope")?.Value;

    if (string.IsNullOrWhiteSpace(scopeValue))
    {
        return false;
    }

    return scopeValue
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Any(scope => scope == permission);
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadTodos", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasPermission(context.User, "read:todos")));

    options.AddPolicy("WriteTodos", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasPermission(context.User, "write:todos")));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { message = "Backend is running" }));

app.MapGet("/api/todos", async (AppDbContext db) =>
    await db.TodoItems
        .OrderBy(item => item.Id)
        .ToListAsync())
    .RequireAuthorization("ReadTodos");

app.MapPost("/api/todos", async (AppDbContext db, TodoItem input) =>
{
    if (string.IsNullOrWhiteSpace(input.Title))
    {
        return Results.BadRequest(new { message = "Title is required" });
    }

    var todo = new TodoItem
    {
        Title = input.Title.Trim(),
        IsDone = input.IsDone
    };

    db.TodoItems.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/api/todos/{todo.Id}", todo);
})
    .RequireAuthorization("WriteTodos");

app.MapPut("/api/todos/{id:int}", async (AppDbContext db, int id, TodoItem input) =>
{
    var todo = await db.TodoItems.FindAsync(id);

    if (todo is null)
    {
        return Results.NotFound();
    }

    if (string.IsNullOrWhiteSpace(input.Title))
    {
        return Results.BadRequest(new { message = "Title is required" });
    }

    todo.Title = input.Title.Trim();
    todo.IsDone = input.IsDone;

    await db.SaveChangesAsync();

    return Results.Ok(todo);
})
    .RequireAuthorization("WriteTodos");

app.MapDelete("/api/todos/{id:int}", async (AppDbContext db, int id) =>
{
    var todo = await db.TodoItems.FindAsync(id);

    if (todo is null)
    {
        return Results.NotFound();
    }

    db.TodoItems.Remove(todo);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
    .RequireAuthorization("WriteTodos");

app.Run();
