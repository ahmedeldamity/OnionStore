﻿using Hangfire;
using HangfireBasicAuthenticationFilter;
using Microsoft.Extensions.Options;
using Service.ConfigurationData;

namespace API.ServicesExtension;
public static class HangfireConfigurationsExtension
{
	public static IServiceCollection AddHangfireServices(this IServiceCollection services)
	{
        var serviceProvider = services.BuildServiceProvider();

        var databaseConnections = serviceProvider.GetRequiredService<IOptions<DatabaseConnections>>().Value;

        // Add Hangfire Services
        services.AddHangfire(x => x.UseSqlServerStorage(databaseConnections.IdentityConnection));

        // Register Hangfire Services
        services.AddHangfireServer();

        return services;
	}

    public static WebApplication UseHangfireDashboard(this WebApplication app, IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();

        var hangfireData = serviceProvider.GetRequiredService<IOptions<HangfireSettingsData>>().Value;

        app.UseHangfireDashboard(hangfireData.DashboardUrl, new DashboardOptions()
        {
            Authorization =
            [
                new HangfireCustomBasicAuthenticationFilter
                {
                    User  = hangfireData.UserName,
                    Pass  = hangfireData.Password
                }
            ],
            DashboardTitle = hangfireData.ServerName,

        });

        return app;
    }
}