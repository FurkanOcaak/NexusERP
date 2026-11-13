using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexusERP.DataAccess.Interceptors;
using NexusERP.DataAccess.Persistence;

namespace NexusERP.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection ??= new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NexusERPDatabase"] = "Server=(localdb)\\MSSQLLocalDB;Database=NexusERP_Test;Trusted_Connection=True;"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<NexusERPDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<NexusERPDbContext>>();
            services.RemoveAll<NexusERPDbContext>();
            services.RemoveAll<DbConnection>();

            services.AddSingleton<DbConnection>(_connection);
            services.AddSingleton<AuditSaveChangesInterceptor>();

            services.AddDbContext<NexusERPDbContext>((serviceProvider, options) =>
            {
                var connection = serviceProvider.GetRequiredService<DbConnection>();

                options.UseSqlite(connection);
                options.AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
            });

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NexusERPDbContext>();

            if (!string.Equals(
                    dbContext.Database.ProviderName,
                    "Microsoft.EntityFrameworkCore.Sqlite",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Test database provider must be SQLite. Actual provider: {dbContext.Database.ProviderName ?? "<null>"}");
            }

            dbContext.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
        }

        base.Dispose(disposing);
    }
}
