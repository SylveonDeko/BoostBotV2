using BoostBotV2.Db.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BoostBotV2.Db;

public class BoostContextFactory : IDesignTimeDbContextFactory<BoostContext>
{
    public BoostContext CreateDbContext(string[] args)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "BoostBot.db");
        var optionsBuilder = new DbContextOptionsBuilder<BoostContext>();
        var builder = new SqliteConnectionStringBuilder($"Data Source = {path}");
        builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);
        optionsBuilder.UseSqlite(builder.ToString());
        var ctx = new BoostContext(optionsBuilder.Options);
        ctx.Database.SetCommandTimeout(60);
        return ctx;
    }
}

public class BoostContext : DbContext
{
    public BoostContext(DbContextOptions<BoostContext> options) : base(options)
    {
    }
    
   public DbSet<GuildsAdded> GuildsAdded { get; set; }
   public DbSet<Blacklists> Blacklists { get; set; }
   public DbSet<Keys> Keys { get; set; }
   public DbSet<MemberFarmRegistry> MemberFarmRegistry { get; set; }
   public DbSet<NitroStock> NitroStock { get; set; }
   public DbSet<PrivateStock> PrivateStock { get; set; }
   public DbSet<RulesAgreed> RulesAgreed { get; set; }
}