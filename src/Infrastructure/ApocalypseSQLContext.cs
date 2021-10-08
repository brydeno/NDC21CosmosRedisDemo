using Domain;
using Locking;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Infrastructure
{


	public class ApocalypseSQLContext : DbContext, ISQLRequestHandler
	{
        public ApocalypseSQLContext(DbContextOptions<ApocalypseSQLContext> options) : base(options)
        {
        }

        public virtual DbSet<City> Cities { get; set; }

		private TelemetryClient _telemetryClient;

		public ApocalypseSQLContext AddTelemetry(TelemetryClient telemetryClient)
		{
			_telemetryClient = telemetryClient;
			return this;
		}

		private async Task<City> GetCity(string name)
		{
			using var dependency = new Dependency(_telemetryClient, "SQL", "GetCity", $"{name}");
			return await Cities.FindAsync(name);
		}

		private async Task<IEnumerable<City>> GetCities()
		{
			using var dependency = new Dependency(_telemetryClient, "SQL", "GetCities", $"ALL");
			return await Cities.ToListAsync();
		}

		public async Task<IEnumerable<City>> GetCurrentInformation()
		{
			return await GetCities();
		}

		public async Task UpdateCurrentInformation(string cityName, int kangarooChange, int humanChange, int zombieChange)
		{
			using var dependency = new Dependency(_telemetryClient, "SQL", "UpdateCurrentInformation", $"{cityName}");

			var trans = await Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
			try
            {
				var city = await GetCity(cityName);
				city.KangarooCount += kangarooChange;
				city.HumanCount += humanChange;
				city.ZombieCount += zombieChange;
				await SaveChangesAsync();
				await trans.CommitAsync();
            }
			catch
            {
				await trans.RollbackAsync();
            }
		}

		public async Task SetInformation(string cityName, int kangarooCount, int humanCount, int zombieCount, string state)
		{
			using var dependency = new Dependency(_telemetryClient, "SQL", "SetInformation", $"{cityName}");
			var trans = await Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
			try
			{
				var city = await GetCity(cityName);
				if (city == null)
				{
					city = new City
					{
						Name = cityName,
						State = state
					};
				}
				city.KangarooCount = kangarooCount;
				city.HumanCount = humanCount;
				city.ZombieCount = zombieCount;
				await SaveChangesAsync();
				await trans.CommitAsync();
			}
			catch
			{
				await trans.RollbackAsync();
			}
		}

		public async Task Calculate()
		{
			using var dependency = new Dependency(_telemetryClient, "SQL", "Calculate", $"All");
			var tasks = new List<Task>();
			foreach (var city in await GetCities())
			{
				tasks.Add(CalculateCity(city.Name));
			}
			await Task.WhenAll(tasks);
		}

		public async Task CalculateCity(string cityName)
		{
			using var dependency = new Dependency(_telemetryClient, "SQL", "CalculateCity", $"{cityName}");
			var trans = await Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
			try
			{
				var city = await GetCity(cityName);
				city.Calculate();
				await SaveChangesAsync();
				await trans.CommitAsync();
			}
			catch
			{
				await trans.RollbackAsync();
			}
		}
	}
}
