// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Functions.Preemption
{
    public interface IPreemptionStore
    {
        Task<bool> PauseAsync((string Name, string InstanceId) orchestrationInstance, DateTime currentTime);

        Task<IReadOnlyCollection<(string Name, string InstanceId)>> ResumeAsync(int count);

        Task<IReadOnlyCollection<(string Name, string InstanceId)>> GetPausedAsync();
    }
}
