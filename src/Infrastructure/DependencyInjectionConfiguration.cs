using Domain;
using Locking;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace Infrastructure
{
    public static class DependencyInjectionConfiguration
    {
		public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
		{
			services.AddTransient<ILoggerFactory, LoggerFactory>();
			services.AddLockService(builder =>
			{
				var redisSection = configuration.GetSection("Redis");
				builder
					.Connect(connectionString: redisSection["connectString"])
					.WithConfig(expiry: TimeSpan.FromMilliseconds(ParseOrDefault(redisSection["expiry"], 500)),
								wait: TimeSpan.FromMilliseconds(ParseOrDefault(redisSection["wait"], 50)),
								retry: TimeSpan.FromMilliseconds(ParseOrDefault(redisSection["retry"], 10)));
			});

			var sqlSection = configuration.GetSection("SQL");
			services.AddDbContextPool<ApocalypseSQLContext>(options =>
			{
				options.UseSqlServer(
					sqlSection["connectString"],
					b => b.MigrationsAssembly("Infrastructure"));

			});

			services.AddTransient<ISQLRequestHandler, ApocalypseSQLContext>(sp =>
					sp.GetRequiredService<ApocalypseSQLContext>()
							.AddTelemetry(sp.GetRequiredService<TelemetryClient>()));
			
			services.AddSingleton((s) => {
				var cosmosSection = configuration.GetSection("CosmosDb");
				return new CosmosClient(cosmosSection["Account"]);
			});
			services.AddTransient<ICosmosRequestHandler, ApocalypseCosmosContext>();

			return services;
		}

		private static int ParseOrDefault(string value, int defaultValue)
		{
			return string.IsNullOrEmpty(value) ? defaultValue : int.Parse(value);
		}
	}
}
