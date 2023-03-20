using DurableComedy.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.ContainerInstance.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DurableComedy
{
    public class Orchestrator
    {
        private readonly ILogger<Orchestrator> _logger;

        public Orchestrator(ILogger<Orchestrator> log)
        {
            _logger = log;
        }

        [FunctionName(Function.Start)]
        [OpenApiOperation(operationId: "Run", tags: new[] { "Orchestrator" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "containerImage", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "mcr.microsoft.com/azuredocs/aci-helloworld")]
        [OpenApiParameter(name: "instanceID", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "DurableComedyInstanceID")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Success")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, [OrchestrationClient] IDurableOrchestrationClient starter)
        {
            _logger.LogInformation("HTTP trigger function processing request.");

            string containerImage = req.Query["containerImage"];
            string instanceID = req.Query["instanceID"];

            string instanceId = await starter.StartNewAsync(Function.RunOrchestrator, instanceID, containerImage);
            _logger.LogInformation("Orchestration Process Started");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(Function.RunOrchestrator)]
        public async Task<List<string>> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var result = new List<string>();
            var containerImage = context.GetInput<string>();
            Console.WriteLine($"Image Name: {containerImage}");

            var ipAddress = await context.CallActivityAsync<string>(Function.OrchestrateCG, (Constants.ContainerGroupName, containerImage, context.InstanceId));
            
            //Return Boolean, this will be invoked from the ACI container once its done with its job
            await context.WaitForExternalEvent(Function.JobFinished, TimeSpan.FromMinutes(3));
            
            //This activition function delete the ACI group once its done with its job
            await context.CallActivityAsync<string>(Function.DeleteCG, Constants.ContainerGroupName);

            return result;
        }

        [FunctionName(Function.OrchestrateCG)]
        public static string CreateAciGroup([ActivityTrigger] Tuple<string, string, string> args, ILogger log)
        {
            var azure = Authenticate();
            return CreateContainerGroup(azure, EnvironmentVariables.ResourceGroupName, args.Item1, args.Item2, args.Item3);
        }


        private static string CreateContainerGroup(IAzure azure, string resourceGroupName, string containerGroupName, string containerImage, string instanceId)
        {
            Console.WriteLine($"\nCreating container group '{containerGroupName}'...");

            // Get the resource group's region
            IResourceGroup resGroup = azure.ResourceGroups.GetByName(resourceGroupName);
            Region azureRegion = resGroup.Region;

            // Create the container group
            
            Task.Run(() => 
                azure.ContainerGroups.Define(containerGroupName)
                    .WithRegion(azureRegion)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithLinux()
                    .WithPublicImageRegistryOnly()
                    //.WithPrivateImageRegistry(EnvironmentVariables.Server, EnvironmentVariables.Username, EnvironmentVariables.Password)
                    .WithoutVolume()
                    .DefineContainerInstance(containerGroupName)
                        .WithImage(containerImage)
                        .WithExternalTcpPort(80)
                        .WithCpuCoreCount(1.0)
                        .WithMemorySizeInGB(1)
                        .WithEnvironmentVariable("instance", instanceId)
                        .Attach()
                    .WithDnsPrefix(containerGroupName)
                    .CreateAsync()
            );
            IContainerGroup containerGroup = null;
            while (containerGroup == null)
            {
                containerGroup = azure.ContainerGroups.GetByResourceGroup(resourceGroupName, containerGroupName);
                Console.Write(".");
                SdkContext.DelayProvider.Delay(1000);
            }
            // Poll until the container group is running
            while (containerGroup.State != "Running")
            {
                Console.Write($"\nContainer group state: {containerGroup.Refresh().State}");
                Thread.Sleep(1000);
            }

            Console.WriteLine($"Container group '{containerGroup.Name}' will be reachable at http://{containerGroup.Fqdn} once DNS has propagated.");
            return containerGroup.IPAddress;
        }

        [FunctionName(Function.DeleteCG)]
        public static string RunDelete([ActivityTrigger] string name, ILogger log)
        {
            var azure = Authenticate();
            DeleteContainerGroup(azure, Constants.ResourceGroupName, name);
            return $"Container Group {name} Deleted";
        }

        private static void DeleteContainerGroup(IAzure azure, string resourceGroupName, string containerGroupName)
        {
            IContainerGroup containerGroup = null;
            while (containerGroup == null)
            {
                containerGroup = azure.ContainerGroups.GetByResourceGroup(resourceGroupName, containerGroupName);
                SdkContext.DelayProvider.Delay(1000);
            }
            Console.WriteLine($"Deleting container group '{containerGroupName}'...");
            azure.ContainerGroups.DeleteById(containerGroup.Id);
        }


        private static IAzure Authenticate()
        {
            var creds = new AzureCredentialsFactory().FromServicePrincipal(EnvironmentVariables.Client, EnvironmentVariables.Key, EnvironmentVariables.Tenant, AzureEnvironment.AzureGlobalCloud);
            return Microsoft.Azure.Management.Fluent.Azure.Authenticate(creds).WithSubscription(EnvironmentVariables.SubscriptionId);
        }
    }
}
