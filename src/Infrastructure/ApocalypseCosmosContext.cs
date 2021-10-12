using AzureGems.Repository.Abstractions;
using Domain;
using Locking;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure
{
	public class ApocalypseCosmosContext : ICosmosRequestHandler
	{
		CosmosClient _cosmosClient;

		private TelemetryClient _telemetryClient;
		private ILockService _lockService;
		private readonly Container _container;

		public ApocalypseCosmosContext(ILockService lockService, TelemetryClient telemetryClient, IConfiguration configuration)
		{
			_lockService = lockService;
			_telemetryClient = telemetryClient;
			var cosmosSection = configuration.GetSection("CosmosDb");
			_cosmosClient = new CosmosClient(cosmosSection["Account"]);
			_container = _cosmosClient.GetDatabase("ZombieApocalypse").GetContainer("Cities");
		}

		private City CosmosCityToCity(CosmosCity c)
		{
			if (c == null)
				return null;
			return new City
			{
				Name = c.Name,
				State = c.State,
				HumanCount = c.HumanCount,
				KangarooCount = c.KangarooCount,
				ZombieCount = c.ZombieCount
			};
		}

		private CosmosCity CityToCosmosCity(City c)
		{
			return new CosmosCity
			{
				Name = c.Name,
				State = c.State,
				HumanCount = c.HumanCount,
				KangarooCount = c.KangarooCount,
				ZombieCount = c.ZombieCount
			};

		}

		private async Task<City> GetCity(string name)
        {
			using var dependency = new Dependency(_telemetryClient, "Cosmos", "GetCity", $"{name}");
			var query = _container.GetItemQueryIterator<CosmosCity>(_container.GetItemLinqQueryable<CosmosCity>().Where(c => name.Equals(c.Name)).ToQueryDefinition());
			var iterator = await query.ReadNextAsync();
			return CosmosCityToCity(iterator.FirstOrDefault());
		}

		private async Task<IEnumerable<City>> GetCities()
		{
			using var dependency = new Dependency(_telemetryClient, "Cosmos", "GetCities", $"ALL");
			var results = new List<CosmosCity>();
			var query = _container.GetItemQueryIterator<CosmosCity>();
			while (query.HasMoreResults)
			{
				FeedResponse<CosmosCity> feedResponse = await query.ReadNextAsync();
				results.AddRange(feedResponse);				
			}
			return results.Select(x => CosmosCityToCity(x));
		}

		public async Task UpdateCity(City city, LockToken lockToken)
		{
			using var dependency = new Dependency(_telemetryClient, "Cosmos", "UpdateCity", $"{city.Name}");
			// First make sure that the city is locked for writing
			if ((lockToken.GetId() == city.Name))
			{
				await _container.UpsertItemAsync(CityToCosmosCity(city));
			}
			else
			{
				throw new LockingException($"Attempting to write to City {city.Name} with a lock for {lockToken.GetId()}");
			}
		}
		
		public async Task<IEnumerable<City>> GetCurrentInformation()
		{
			return await GetCities();
		}

		public async Task UpdateCurrentInformation(string cityName, int kangarooChange, int humanChange, int zombieChange)
		{
			using var dependency = new Dependency(_telemetryClient, "Cosmos", "UpdateCurrentInformation", $"{cityName}");
			await _lockService.PerformWhileLocked(cityName, async (lockToken) =>
			{
				// So all of this code executes with the lock active.
				// The locktoken is used to confirm we have the write thing locked when we update.
				var city = await GetCity(cityName);
				city.KangarooCount += kangarooChange;
				city.HumanCount += humanChange;
				city.ZombieCount += zombieChange;
				await UpdateCity(city, lockToken);
			});
		}

		public async Task SetInformation(string cityName, int kangarooCount, int humanCount, int zombieCount, string state)
        {
			using var dependency = new Dependency(_telemetryClient, "Cosmos", "SetInformation", $"{cityName}");
			await _lockService.PerformWhileLocked(cityName, async (lockToken) =>
			{
				// So all of this code executes with the lock active.
				// The locktoken is used to confirm we have the write thing locked when we update.
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
				await UpdateCity(city, lockToken);
			});

		}

		public async Task Calculate()
        {
			using var dependency = new Dependency(_telemetryClient, "Cosmos", "Calculate", $"All");
			var tasks = new List<Task>();
			foreach (var city in await GetCities())
            {
				tasks.Add(CalculateCity(city.Name));
            }
			await Task.WhenAll(tasks);
		}

		public async Task CalculateCity(string cityName)
		{
			using var dependency = new Dependency(_telemetryClient, "Cosmos", "CalculateCity", $"{cityName}");
			await _lockService.PerformWhileLocked(cityName, async (lockToken) =>
			{
				// So all of this code executes with the lock active.
				// The locktoken is used to confirm we have the write thing locked when we update.
				var city = await GetCity(cityName);
				city.Calculate();
				await UpdateCity(city, lockToken);
			});
		}
	}
}
