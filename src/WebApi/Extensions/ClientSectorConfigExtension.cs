using Microsoft.Extensions.Options;
using WebApi.Options;

namespace WebApi.Extensions;

public static class ClientSectorConfigExtension
{
    public static IServiceCollection AddClientSectorConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ClientSectorOptions>(configuration.GetSection("ClientSector"));

        services.AddSingleton<ISet<string>>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<ClientSectorOptions>>().Value;
            var list = opts?.AllowedClientSectors ?? [];
            return new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
        });

        return services;
    }
}
