// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Functions.Preemption
{
    public class InMemoryPreemptionStore : IPreemptionStore
    {
        private readonly SortedList<DateTime, (string Name, string InstanceId)> _data = new SortedList<DateTime, (string Name, string InstanceId)>();

        public Task<IReadOnlyCollection<(string Name, string InstanceId)>> GetPausedAsync()
            => Task.FromResult<IReadOnlyCollection<(string Name, string InstanceId)>>(_data.Values.ToHashSet()); // Copy

        public Task<bool> PauseAsync((string Name, string InstanceId) orchestrationInstance, DateTime currentTime)
        {
            if (string.IsNullOrWhiteSpace(orchestrationInstance.Name))
                throw new ArgumentException("Invalid function name", nameof(orchestrationInstance));

            if (string.IsNullOrWhiteSpace(orchestrationInstance.InstanceId))
                throw new ArgumentException("Invalid instance id", nameof(orchestrationInstance));

            if (_data.ContainsValue(orchestrationInstance))
                return Task.FromResult(false);

            _data.Add(currentTime, orchestrationInstance);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<(string Name, string InstanceId)>> ResumeAsync(int count)
        {
            var removed = new List<(string Name, string InstanceId)>();
            for (int i = count; i > 0 && _data.Count > 0; i--)
            {
                removed.Add(_data.Values[_data.Count - 1]);
                _data.RemoveAt(_data.Count - 1);
            }

            return Task.FromResult<IReadOnlyCollection<(string Name, string InstanceId)>>(removed);
        }
    }
}
