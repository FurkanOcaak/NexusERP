
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusERP.DataAccess.Interceptors;
using NexusERP.DataAccess.Persistence;

namespace NexusERP.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("NexusERPDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "NexusERP database connection string is not configured.");
        }

        services.AddSingleton<AuditSaveChangesInterceptor>();

        services.AddDbContext<NexusERPDbContext>(
            (serviceProvider, options) =>
            {
                options.UseSqlServer(connectionString);

                options.AddInterceptors(
                    serviceProvider.GetRequiredService<
                        AuditSaveChangesInterceptor>());
            });

        return services;
    }
}
