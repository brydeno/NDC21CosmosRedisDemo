using Locking;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace Infrastructure
{

    static public class LockServiceExtensions
    {
		public static IServiceCollection AddLockService(this IServiceCollection services, Action<RedisLockServiceBuilder> configure = null)
        {
			services.AddTransient(provider =>
			{
				var config = provider.GetRequiredService<IConfiguration>();
				RedisLockServiceBuilder builder = new RedisLockServiceBuilder()
					.ReadConfiguration(config);
				builder = builder.
						AddLogging(provider.GetRequiredService<ILoggerFactory>(),provider.GetRequiredService<ILogger<LockService>>()).
						AddTelemetry(provider.GetRequiredService<TelemetryClient>());

				configure?.Invoke(builder);

				return builder;
			});

			services.AddSingleton<ILockService>(provider => provider.GetRequiredService<RedisLockServiceBuilder>().Build());

			return services;
		}
	}
	public class RedisLockServiceBuilder
	{
		private RedisConnectionSettings _connectionSettings = null;
		private ILoggerFactory _loggerFactory = null;
		private ILogger<LockService> _logger = null;
		private TimeSpan _expiry = TimeSpan.FromMilliseconds(100);
		private TimeSpan _wait = TimeSpan.FromMilliseconds(100);
		private TimeSpan _retry = TimeSpan.FromMilliseconds(100);
		private TelemetryClient _telemetryClient = null;

		public RedisLockServiceBuilder AddLogging(ILoggerFactory loggerFactory, ILogger<LockService> logger)
        {
			_loggerFactory = loggerFactory;
			_logger = logger;
			return this;
        }
		public RedisLockServiceBuilder AddTelemetry(TelemetryClient telemetryClient)
		{
			_telemetryClient = telemetryClient;
			return this;
		}

		public RedisLockServiceBuilder ReadConfiguration(IConfiguration config)
		{
			_connectionSettings = new RedisConnectionSettings(config);
			return this;
		}

		public RedisLockServiceBuilder Connect(string connectionString)
		{
			_connectionSettings = new RedisConnectionSettings(connectionString);
			return this;
		}


		public RedisLockServiceBuilder Connect(RedisConnectionSettings connSettings)
		{
			_connectionSettings = connSettings;
			return this;
		}
		public void WithConfig(TimeSpan expiry, TimeSpan wait, TimeSpan retry)
		{
			_expiry = expiry;
			_wait = wait;
			_retry = retry;
		}

		public LockService Build()
		{
			ConfigurationOptions redisConfig = ConfigurationOptions.Parse(_connectionSettings.ConnectionString);

			var connection = ConnectionMultiplexer.Connect(redisConfig);
			var multiplexers = new List<RedLockMultiplexer>
			{
				connection
			};

			return new LockService(RedLockFactory.Create(multiplexers, _loggerFactory), _expiry, _wait, _retry, _logger, _telemetryClient);
		}

    }

	public class RedisLockConfig
	{
		public int ExpiryMilliseconds { get; set; }
		public int WaitMilliseconds { get; set; }
		public int RetryMilliseconds { get; set; }
	}

	public class RedisConnectionSettings
	{
		public RedisConnectionSettings(IConfiguration config)
		{
			ConnectionString = config["redisConnection:connectionString"];
		}

		public RedisConnectionSettings(string connectionString)
		{
			ConnectionString = connectionString;
		}

		public string ConnectionString { get; set; }
	}
}