using Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ServiceDefaults;

public static class ServiceDefaults
{
    public static void AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddMySqlDbContext<RefNotesContext>(connectionName: "main");   
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
    }
}
