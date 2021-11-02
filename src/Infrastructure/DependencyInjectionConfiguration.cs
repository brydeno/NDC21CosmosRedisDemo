using Domain;
using Locking;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
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
			services.AddSingleton((s) =>
			{
				var redisSection = configuration.GetSection("Redis");
				ConfigurationOptions redisConfig = ConfigurationOptions.Parse(redisSection["connectString"]);

				var connection = ConnectionMultiplexer.Connect(redisConfig);
				var multiplexers = new List<RedLockMultiplexer>
				{
					connection
				};

				return RedLockFactory.Create(multiplexers, s.GetRequiredService<ILoggerFactory>());
			}
			);
			services.AddTransient<ILockService,LockService>();

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
				return new CosmosClient(cosmosSection["Account"], new CosmosClientOptions
                {
					ConnectionMode = ConnectionMode.Direct
                });
			});
			services.AddTransient<ICosmosRequestHandler, ApocalypseCosmosContext>();

			return services;
		}		
	}
}
