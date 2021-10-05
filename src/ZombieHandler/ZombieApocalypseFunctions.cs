using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
            ILogger log)
        {
            return new OkObjectResult(await _apocalypseRequestHandler.GetCurrentInformation());
        }

        [FunctionName("Calculate")]
        public async Task<IActionResult> Calculate(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
        {
            await _apocalypseRequestHandler.Calculate();
            return new OkResult();
        }
    }
}
