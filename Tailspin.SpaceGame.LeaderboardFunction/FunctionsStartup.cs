using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;

[assembly: FunctionsStartup(typeof(TailSpin.SpaceGame.LeaderboardFunction.Startup))]
namespace TailSpin.SpaceGame.LeaderboardFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // https://github.com/Azure/azure-functions-host/issues/4517#issuecomment-497940595
            var actual_root = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot")  // local_root
                    ?? (Environment.GetEnvironmentVariable("HOME") == null
                        ? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                        : $"{Environment.GetEnvironmentVariable("HOME")}/site/wwwroot"); // azure_root
            
            builder.Services.AddSingleton<IDocumentDBRepository<Score>>(new LocalDocumentDBRepository<Score>(Path.Combine(actual_root, @"SampleData/scores.json")));
            builder.Services.AddSingleton<IDocumentDBRepository<Profile>>(new LocalDocumentDBRepository<Profile>(Path.Combine(actual_root, @"SampleData/profiles.json")));

        }
    }
}
