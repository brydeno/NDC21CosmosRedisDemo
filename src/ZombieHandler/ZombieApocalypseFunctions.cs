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

        //    public Task UpdateCurrentInformation(string cityName, int kangarooChange, int humanChange, int zombieChange);
        //            public Task SetInformation(string cityName, int kangarooCount, int humanCount, int zombieCount);

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

        [FunctionName("UpdateCurrentInformation")]
        public async Task<IActionResult> UpdateCurrentInformation(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
        {
            ChangeCityDTO cityChange = await JsonSerializer.DeserializeAsync<ChangeCityDTO>(req.Body);
            await GetRequestHandler(req).UpdateCurrentInformation(cityChange.CityName,cityChange.KangarooChange,cityChange.HumanChange,cityChange.ZombieChange);
            return new OkResult();
        }

        [FunctionName("SetInformation")]
        public async Task<IActionResult> SetInformation(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
        {
            CityDTO city = await JsonSerializer.DeserializeAsync<CityDTO>(req.Body);
            await GetRequestHandler(req).SetInformation(city.CityName,city.KangarooCount,city.HumanCount,city.ZombieCount,city.State);
            return new OkResult();
        }
    }
}
