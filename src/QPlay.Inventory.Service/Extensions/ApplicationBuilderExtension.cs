using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace QPlay.Inventory.Service.Extensions;

public static class ApplicationBuilderExtension
{
    private const string AllowedOriginSetting = "AllowedOrigin";

    public static IApplicationBuilder ConfigureCors(
        this IApplicationBuilder app,
        IConfiguration configuration
    )
    {
        app.UseCors(
            policyBuilder =>
                policyBuilder
                    .WithOrigins(configuration[AllowedOriginSetting])
                    .AllowAnyHeader()
                    .AllowAnyMethod()
        );
        return app;
    }
}