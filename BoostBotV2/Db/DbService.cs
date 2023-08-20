using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoostBotV2.Db;

public class DbService
{
    private readonly DbContextOptions<BoostContext> _migrateOptions;
    private readonly DbContextOptions<BoostContext> _options;

    public DbService()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "BoostBot.db");
        var builder = new SqliteConnectionStringBuilder($"Data Source={path}");
        builder.DataSource = builder.DataSource = Path.Combine(AppContext.BaseDirectory, builder.DataSource);

        var optionsBuilder = new DbContextOptionsBuilder<BoostContext>();
        optionsBuilder.UseSqlite(builder.ToString());
        _options = optionsBuilder.Options;

        optionsBuilder = new DbContextOptionsBuilder<BoostContext>();
        optionsBuilder.UseSqlite(builder.ToString());
        _migrateOptions = optionsBuilder.Options;
    }

    public async void Setup()
    {
        await using var context = new BoostContext(_options);
        var toApply = (await context.Database.GetPendingMigrationsAsync()).ToList();
        if (toApply.Any())
        {
            var mContext = new BoostContext(_migrateOptions);
            await mContext.Database.MigrateAsync();
            await mContext.SaveChangesAsync();
            await mContext.DisposeAsync();
        }

        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL");
        await context.SaveChangesAsync();
    }

    private BoostContext GetDbContextInternal()
    {
        var context = new BoostContext(_options);
        var conn = context.Database.GetDbConnection();
        conn.OpenAsync();
        using var com = conn.CreateCommand();
        com.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=OFF;";
        com.ExecuteNonQueryAsync();

        return context;
    }

    public BoostContext GetDbContext() => GetDbContextInternal();
}