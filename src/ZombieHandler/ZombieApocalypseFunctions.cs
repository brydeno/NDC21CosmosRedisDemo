using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Infrastructure;
using System.Linq;
using System.Collections.Generic;

namespace ZombieHandler
{
    public class ZombieApocalypseFunctions
    {
        private readonly IApocalypseRequestHandler _cosmos;
        private readonly IApocalypseRequestHandler _sql;
        public ZombieApocalypseFunctions(ICosmosRequestHandler cosmos, ISQLRequestHandler sql)
        {
            _sql = sql;
            _cosmos = cosmos;
        }


        public IApocalypseRequestHandler GetRequestHandler(HttpRequest req)
        {
            var handler = req.Headers["Handler"].FirstOrDefault();
            if (!string.IsNullOrEmpty(handler) && handler.Equals("SQL"))
            {
                return _sql;
            }
            return _cosmos;
        }

        [FunctionName("GetCurrentInformation")]
        public async Task<IActionResult> GetCurrentInformation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req,
            ILogger log)
        {
            return new OkObjectResult(await GetRequestHandler(req).GetCurrentInformation());
        }

        [FunctionName("Calculate")]
        public async Task<IActionResult> Calculate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            await GetRequestHandler(req).Calculate();
            return new OkResult();
        }

        [FunctionName("CalculateSQL")]
        public async Task<IActionResult> CalculateSQL(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            await _sql.Calculate();
            return new OkResult();
        }

        [FunctionName("CalculateCosmos")]
        public async Task<IActionResult> CalculateCosmos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            await _cosmos.Calculate();
            return new OkResult();
        }
        [FunctionName("UpdateCurrentInformation")]
        public async Task<IActionResult> UpdateCurrentInformation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            ChangeCityDTO cityChange = await JsonSerializer.DeserializeAsync<ChangeCityDTO>(req.Body);
            await GetRequestHandler(req).UpdateCurrentInformation(cityChange.CityName,cityChange.KangarooChange,cityChange.HumanChange,cityChange.ZombieChange);
            return new OkResult();
        }

        [FunctionName("SetInformation")]
        public async Task<IActionResult> SetInformation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            var random = new Random();
            IEnumerable<CitiesDTO> cities = await JsonSerializer.DeserializeAsync<IEnumerable<CitiesDTO>>(req.Body);
            foreach (var city in cities)
            {
                int population = int.Parse(city.Population);
                int kp = (int)Math.Round(population * (0.8 + (random.NextDouble() * 0.4)));
                int zp = (int)Math.Round(population * (0.8 + (random.NextDouble() * 0.4)));
                await _cosmos.SetInformation(city.City, kp, population, zp, city.State);
                await _sql.SetInformation(city.City, kp, population, zp, city.State);
            }
            return new OkResult();
        }
    }
}
