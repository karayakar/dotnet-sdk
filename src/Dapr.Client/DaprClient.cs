// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines methods for clients interacting with the Dapr endpoints.
    /// </summary>
    public abstract class DaprClient
    {
        /// <summary>
        /// Gets a client for interacting with the Dapr publish endpoints.
        /// </summary>
        public abstract PublishClient Publish { get; }

        /// <summary>
        /// Gets a client for interacting with the Dapr state management endpoints.
        /// </summary>
        public abstract StateClient State { get; }
    }
}
