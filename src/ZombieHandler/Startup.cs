using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Infrastructure;
using System.Reflection;


[assembly: FunctionsStartup(typeof(ZombieHandler.Startup))]
namespace ZombieHandler
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var sp = builder.Services.BuildServiceProvider();
            var configuration = sp.GetRequiredService<IConfiguration>();
            builder.Services.AddInfrastructure(configuration);
        }
    }
}
