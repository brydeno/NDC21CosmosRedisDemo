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

namespace ZombieHandler
{
    public class ZombieApocalypseFunctions
    {
        private readonly IApocalypseRequestHandler _apocalypseRequestHandler;
        public ZombieApocalypseFunctions(IApocalypseRequestHandler apocalypseRequestHandler)
        {
            _apocalypseRequestHandler = apocalypseRequestHandler;
        }
        
        //    public Task UpdateCurrentInformation(string cityName, int kangarooChange, int humanChange, int zombieChange);
//            public Task SetInformation(string cityName, int kangarooCount, int humanCount, int zombieCount);
        


        [FunctionName("GetCurrentInformation")]
        public async Task<IActionResult> GetCurrentInformation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req,
            ILogger log)
        {
            return new OkObjectResult(await _apocalypseRequestHandler.GetCurrentInformation());
        }

        [FunctionName("Calculate")]
        public async Task<IActionResult> Calculate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            await _apocalypseRequestHandler.Calculate();
            return new OkResult();
        }

        [FunctionName("UpdateCurrentInformation")]
        public async Task<IActionResult> UpdateCurrentInformation(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
        {
            ChangeCityDTO cityChange = await JsonSerializer.DeserializeAsync<ChangeCityDTO>(req.Body);
            await _apocalypseRequestHandler.UpdateCurrentInformation(cityChange.CityName,cityChange.KangarooChange,cityChange.HumanChange,cityChange.ZombieChange);
            return new OkResult();
        }

        [FunctionName("SetInformation")]
        public async Task<IActionResult> SetInformation(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
        {
            CityDTO city = await JsonSerializer.DeserializeAsync<CityDTO>(req.Body);
            await _apocalypseRequestHandler.SetInformation(city.CityName,city.KangarooCount,city.HumanCount,city.ZombieCount,city.State);
            return new OkResult();
        }
    }
}
