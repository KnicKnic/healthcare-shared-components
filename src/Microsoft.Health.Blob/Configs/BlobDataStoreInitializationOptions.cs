// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Blob.Configs
{
    public class BlobDataStoreInitializationOptions
    {
        public TimeSpan MinimumRetryTime { get; set; } = TimeSpan.Zero;

        public TimeSpan MinimumDelayBetweenInvocations { get; set; } = TimeSpan.Zero;
    }
}
