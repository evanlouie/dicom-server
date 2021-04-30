// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Functions.Preemption.Configurations;

namespace Microsoft.Health.Dicom.Functions
{
    public class BusyOrchestration
    {
        private readonly PreemptionConfiguration _configuration;

        public BusyOrchestration(IOptions<PreemptionConfiguration> options)
            => _configuration = options?.Value ?? throw new ArgumentNullException(nameof(options));

        [FunctionName("BusyOrchestration")]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (log == null)
                throw new ArgumentNullException(nameof(log));

            int count = 10;
            for (int i = 1; i < count; i++)
            {
                await context.
                await PauseIfRequestedAsync(context, log);

                await context.CallActivityAsync("BusyOrchestration_Activity", i);
                await context.CreateTimer(context.CurrentUtcDateTime.AddMinutes(1), CancellationToken.None);
            }
        }

        private async Task PauseIfRequestedAsync(IDurableOrchestrationContext context, ILogger log)
        {
            DateTime pauseTime, resumeTime;
            try
            {
                pauseTime = await context.WaitForExternalEvent<DateTime>(_configuration.PauseEventName, TimeSpan.FromSeconds(1));
                log.LogError("Paused as of '{Time}'", pauseTime);
            }
            catch (TimeoutException)
            {
                log.LogInformation("No pause signal");
                return;
            }

            // Wait for the resume signal
            do
            {
                string str = await context.WaitForExternalEvent<string>(_configuration.ResumeEventName);
                resumeTime = DateTime.Parse(str);
            } while (pauseTime > resumeTime);

            log.LogInformation("Resuming orchestration!");
        }

        [FunctionName("BusyOrchestration_Activity")]
        public static void SayHello([ActivityTrigger] int i, ILogger log)
        {
            log.LogInformation("BusyOrchestration_Activity count {Count}", i);
        }

        [FunctionName("BusyOrchestration_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            if (starter == null)
                throw new ArgumentNullException(nameof(starter));

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("BusyOrchestration", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
