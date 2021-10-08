using AzureGems.CosmosDB;
using AzureGems.Repository.CosmosDB;
using Domain;
using Locking;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
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
					.WithConfig(expiry: TimeSpan.FromMilliseconds(ParseOrDefault(redisSection["expiry"], 1000)),
								wait: TimeSpan.FromMilliseconds(ParseOrDefault(redisSection["wait"], 100)),
								retry: TimeSpan.FromMilliseconds(ParseOrDefault(redisSection["retry"], 10)));
			});

			services.AddCosmosDb(builder =>
			{
				var cosmosSection = configuration.GetSection("CosmosDb");
				string authKey = "";
				string serviceEndPoint = "";

				// Use this generic builder to parse the connection string
				DbConnectionStringBuilder strBuilder = new DbConnectionStringBuilder
				{
					ConnectionString = cosmosSection["Account"]
				};

				if (strBuilder.TryGetValue("AccountKey", out object key))
				{
					authKey = key.ToString();
				}

				if (strBuilder.TryGetValue("AccountEndpoint", out object uri))
				{
					serviceEndPoint = uri.ToString();
				}

				builder
					.Connect(endPoint: cosmosSection["Endpoint"], cosmosSection["AuthKey"])
					.UseDatabase(databaseId: "ZombieApocalypse")
					.WithSharedThroughput(400)
					.WithContainerConfig(c =>
					{
						c.AddContainer<City>(containerId: "Cities", partitionKeyPath: "/State");
					});
			});

			services.AddCosmosContext<ApocalypseCosmosContext>();
			services.AddTransient<ICosmosRequestHandler, ApocalypseCosmosContext>(sp =>
					sp.GetRequiredService<ApocalypseCosmosContext>()
							.AddTelemetry(sp.GetRequiredService<TelemetryClient>())
							.AddLockService(sp.GetRequiredService<ILockService>()));

			services.AddTransient<ISQLRequestHandler, ApocalypseSQLContext>(sp =>
					sp.GetRequiredService<ApocalypseSQLContext>()
							.AddTelemetry(sp.GetRequiredService<TelemetryClient>()));

			return services;
		}

		private static int ParseOrDefault(string value, int defaultValue)
		{
			return string.IsNullOrEmpty(value) ? defaultValue : int.Parse(value);
		}
	}
}
