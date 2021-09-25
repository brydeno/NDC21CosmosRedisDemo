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
				builder
					.Connect(endPoint: cosmosSection["EndPoint"], cosmosSection["AuthKey"])
					.UseDatabase(databaseId: "ZombieApocalypse")
					.WithSharedThroughput(4000)
					.WithContainerConfig(c =>
					{
						c.AddContainer<City>(containerId: "Cities", partitionKeyPath: "/State");
					});
			});

			services.AddCosmosContext<ApocalypseCosmosContext>();
			services.AddTransient<IApocalypseRequestHandler, ApocalypseCosmosContext>(sp =>
					sp.GetRequiredService<ApocalypseCosmosContext>()
							.AddTelemetry(sp.GetRequiredService<TelemetryClient>())
							.AddLockService(sp.GetRequiredService<ILockService>()));

//			services.AddDbContextPool<AuctionsPlusSqlContext>(options =>
//			{
//				options.UseSqlServer(
//					configuration.GetConnectionString("AuctionsPlusSqlDB"),
//					b => b.MigrationsAssembly("AuctionsPlus.AuctionPlatform.Infrastructure"));
//			});

//			services.AddScoped<IAuctionsPlusSqlRepository, AuctionsPlusSqlRepository>();

//			// Separate User repository as per Rodel's comment on the story
//			services.AddScoped<IUserRepository, UserRepository>();

//			services.AddScoped<IDateTimeProvider, MachineDateTimeProvider>();

//			services.AddDbContextPool<AuctionsPlusLogSqlContext>(options =>
//			{
//				options.UseSqlServer(
//					configuration.GetConnectionString("AuctionsPlusLogSqlDB"),
//					b => b.MigrationsAssembly("AuctionsPlus.AuctionPlatform.Infrastructure"));

//#if DEBUG
//				// Most project shouldn't expose sensitive data, which is why we are
//				// limiting to be available only in DEBUG mode.
//				// If this is not, SQL "parameters" will be '?' instead of actual values.
//				options.EnableSensitiveDataLogging();
//#endif
//			});
			return services;
		}

		private static int ParseOrDefault(string value, int defaultValue)
		{
			return string.IsNullOrEmpty(value) ? defaultValue : int.Parse(value);
		}
	}
}
