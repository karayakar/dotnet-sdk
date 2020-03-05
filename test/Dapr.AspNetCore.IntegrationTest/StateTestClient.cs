﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapr.Client;
    using Grpc.Net.Client;

    internal class StateTestClient : DaprClientGrpc
    {
        public Dictionary<string, object> State { get; } = new Dictionary<string, object>();
        static GrpcChannel channel = GrpcChannel.ForAddress("http://localhost");

        /// <summary>
        /// Initializes a new instance of the <see cref="DaprClientGrpc"/> class.
        /// </summary>
        internal StateTestClient()
            :base(channel)
        { }

        public override ValueTask<TValue> GetStateAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(storeName))
            {
                throw new ArgumentException("The value cannot be null or empty.", nameof(storeName));
            }

            if (this.State.TryGetValue(key, out var obj))
            {
                return new ValueTask<TValue>((TValue)obj);
            }
            else
            {
                return new ValueTask<TValue>(default(TValue));
            }
        }

        public override ValueTask SaveStateAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(storeName))
            {
                throw new ArgumentException("The value cannot be null or empty.", nameof(storeName));
            }

            this.State[key] = value;
            return new ValueTask(Task.CompletedTask);
        }

        public override ValueTask DeleteStateAsync(string storeName, string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(storeName))
            {
                throw new ArgumentException("The value cannot be null or empty.", nameof(storeName));
            }

            this.State.Remove(key);
            return new ValueTask(Task.CompletedTask);
        }
    }
}
