﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to orchestrate the storing of the DICOM instance entry.
    /// </summary>
    public interface IStoreOrchestrator
    {
        /// <summary>
        /// Asynchronously orchestrate the storing of a DICOM instance entry.
        /// </summary>
        /// <param name="dicomInstanceEntry">The DICOM instance entry to store.</param>
        /// <param name="customTagEntries">The custom tag entries.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous orchestration of the storing operation.</returns>
        Task StoreDicomInstanceEntryAsync(
            IDicomInstanceEntry dicomInstanceEntry,
            IList<CustomTagEntry> customTagEntries,
            CancellationToken cancellationToken = default);
    }
}
