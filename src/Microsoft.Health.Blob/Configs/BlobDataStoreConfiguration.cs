﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Blob.Configs
{
    public class BlobDataStoreConfiguration
    {
        public string ConnectionString { get; set; }

        public BlobDataStoreAuthenticationType AuthenticationType { get; set; } = BlobDataStoreAuthenticationType.ConnectionString;

        public BlobDataStoreInitializationOptions InitializationOptions { get; } = new BlobDataStoreInitializationOptions();
        
        public BlobDataStoreRequestOptions RequestOptions { get; } = new BlobDataStoreRequestOptions();
    }
}
