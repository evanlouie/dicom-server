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

namespace Microsoft.Health.Dicom.Functions
{
    public static class NoOpOrchestration
    {
        [FunctionName("NoOpOrchestration")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            int count = 10;
            for (int i = 1; i < count; i++)
            {
                await context.CallActivityAsync("NoOpOrchestration_Activity", i);
                await context.CreateTimer(context.CurrentUtcDateTime.AddMinutes(3), CancellationToken.None);
            }
        }

        [FunctionName("NoOpOrchestration_Activity")]
        public static void SayHello([ActivityTrigger] int i, ILogger log)
        {
            log.LogInformation("NoOpOrchestration_Activity count {Count}", i);
        }

        [FunctionName("NoOpOrchestration_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            if (starter == null)
                throw new ArgumentNullException(nameof(starter));

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("NoOpOrchestration", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
