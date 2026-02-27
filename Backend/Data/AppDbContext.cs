using HelApp.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace HelApp.Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
}
