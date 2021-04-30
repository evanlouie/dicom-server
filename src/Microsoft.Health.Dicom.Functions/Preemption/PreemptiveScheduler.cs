// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Functions.Preemption.Configurations;

namespace Microsoft.Health.Dicom.Functions.Preemption
{
    public class PreemptiveScheduler
    {
        private readonly IPreemptionStore _store;
        private readonly PreemptionConfiguration _configuration;

        public PreemptiveScheduler(IPreemptionStore store, IOptions<PreemptionConfiguration> options)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _configuration = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        [FunctionName("PreemptiveScheduler")]
        public async Task Run(
            [TimerTrigger("*/10 * * * * *")]TimerInfo myTimer,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log,
            CancellationToken cancellationToken = default)
        {
            if (myTimer == null)
                throw new ArgumentNullException(nameof(myTimer));

            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (log == null)
                throw new ArgumentNullException(nameof(log));

            DateTime currentTime = DateTime.UtcNow;
            log.LogInformation("Running Preemptive Scheduler for time '{CurrentTime}'", currentTime);
            if (myTimer.IsPastDue)
                log.LogWarning("Timer is running behind");

            // Determine what's running
            var lowPriority = new SortedList<DateTime, DurableOrchestrationStatus>();
            (TimeSpan Delay, DurableOrchestrationStatus InstanceStatus) max = (TimeSpan.Zero, null);

            await foreach (DurableOrchestrationStatus status in EnumerateRunningInstancesAsync(client, cancellationToken))
            {
                if (_configuration.LowPriority.Contains(status.Name, StringComparer.OrdinalIgnoreCase))
                {
                    lowPriority.Add(status.CreatedTime, status);
                }
                else if (_configuration.HighPriority.Contains(status.Name, StringComparer.OrdinalIgnoreCase))
                {
                    TimeSpan idle = currentTime - status.LastUpdatedTime;
                    if (idle > max.Delay)
                        max = (idle, status);
                }
            }

            // If a high-priority orchestration has been delayed, we'll alert a low-priority orchestration to pause
            if (max.InstanceStatus != null)
            {
                log.LogInformation("Orchestration '{Name}' instance '{InstanceId}' has not made progress in its execution for '{Delay}'. Low-priority orchestration will be preempted.",
                    max.InstanceStatus.Name,
                    max.InstanceStatus.InstanceId,
                    max.Delay);

                await TryPauseAsync(client, lowPriority, currentTime, log);
            }
            else // Otherwise, alert any orchestration that they can resume
            {
                await TryResumeAsync(client, currentTime, log);
            }
        }

        private static async IAsyncEnumerable<DurableOrchestrationStatus> EnumerateRunningInstancesAsync(
            IDurableOrchestrationClient client,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var batch = new OrchestrationStatusQueryResult { ContinuationToken = null };
            do
            {
                batch = await client.ListInstancesAsync(
                    new OrchestrationStatusQueryCondition
                    {
                        ContinuationToken = batch.ContinuationToken,
                        CreatedTimeFrom   = DateTime.MinValue,
                        CreatedTimeTo     = DateTime.MaxValue,
                        RuntimeStatus     = new List<OrchestrationRuntimeStatus> { OrchestrationRuntimeStatus.Running, OrchestrationRuntimeStatus.Pending },
                        ShowInput         = false,
                    },
                    cancellationToken);

                foreach (DurableOrchestrationStatus status in batch.DurableOrchestrationState)
                    yield return status;
            } while (batch.ContinuationToken != null);
        }

        private async Task<bool> TryPauseAsync(IDurableOrchestrationClient client, SortedList<DateTime, DurableOrchestrationStatus> candidates, DateTime currentTime, ILogger log)
        {
            IReadOnlyCollection<(string Name, string InstanceId)> alreadyPaused = await _store.GetPausedAsync();
            var toBePaused = new List<(string Name, string InstanceId)> ();

            for (int i = 0; i < candidates.Count && toBePaused.Count < _configuration.Step; i++)
            {
                var candidate = (candidates.Values[i].Name, candidates.Values[i].InstanceId);
                if (!alreadyPaused.Contains(candidate) && await _store.PauseAsync(candidate, currentTime))
                    toBePaused.Add(candidate);
            }

            // Send events
            await Task.WhenAll(toBePaused.Select(x => client.RaiseEventAsync(x.InstanceId, _configuration.PauseEventName, currentTime)));

            if (toBePaused.Count > 0)
            {
                log.LogInformation("Paused the following orchestrations: {Instances}", toBePaused.Select(x => $"({x.Name}, {x.InstanceId})"));
                return true;
            }
            else
            {
                log.LogWarning("Could not pause any additional orchestrations.");
                return false;
            }
        }

        private async Task<bool> TryResumeAsync(IDurableOrchestrationClient client, DateTime currentTime, ILogger log)
        {
            IReadOnlyCollection<(string Name, string InstanceId)> resumed =  await _store.ResumeAsync(_configuration.Step);

            // Send events
            await Task.WhenAll(resumed.Select(x => client.RaiseEventAsync(x.InstanceId, _configuration.ResumeEventName, currentTime)));

            if (resumed.Count > 0)
            {
                log.LogInformation("Resumed the following orchestrations: {Instances}", resumed.Select(x => $"({x.Name}, {x.InstanceId})"));
                return true;
            }
            else
            {
                log.LogInformation("No orchestration instances are currently paused");
                return false;
            }
        }
    }
}
