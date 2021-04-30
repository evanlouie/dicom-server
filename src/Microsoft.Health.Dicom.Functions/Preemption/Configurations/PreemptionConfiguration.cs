// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Functions.Preemption.Configurations
{
    public class PreemptionConfiguration
    {
        [Required]
        public string ResumeEventName { get; set; }

        [Required]
        public string PauseEventName { get; set; }

        [Required]
        public IReadOnlyCollection<string> HighPriority { get; set; }

        [Required]
        public IReadOnlyCollection<string> LowPriority { get; set; }

        public PreemptivePreference PreemptivePreference { get; set; }

        [Range(1, int.MaxValue)]
        public int Step { get; set; } = 1;

        [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(15);

        [Required]
        public string SqlConnectionString { get; set; }
    }
}
