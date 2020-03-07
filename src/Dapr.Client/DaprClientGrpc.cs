﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapr.Client.Autogen.Grpc;
    using Google.Protobuf;
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using Grpc.Net.Client;
    using Autogenerated = Dapr.Client.Autogen.Grpc;

    /// <summary>
    /// A client for interacting with the Dapr endpoints.
    /// </summary>
    internal class DaprClientGrpc : DaprClient
    {
        private readonly Autogenerated.Dapr.DaprClient client;
        private readonly JsonSerializerOptions jsonSerializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DaprClientGrpc"/> class.
        /// </summary>
        /// <param name="channel">gRPC channel to create gRPC clients.</param>
        /// <param name="jsonSerializerOptions">Json serialization options.</param>
        internal DaprClientGrpc(GrpcChannel channel, JsonSerializerOptions jsonSerializerOptions = null)
        {
            this.jsonSerializerOptions = jsonSerializerOptions;
            this.client = new Autogenerated.Dapr.DaprClient(channel);
        }

        #region Publish Apis
        /// <inheritdoc/>
        public override Task PublishEventAsync<TRequest>(string topicName, TRequest publishContent, CancellationToken cancellationToken = default)
        {
            topicName.ThrowIfNullOrEmpty(nameof(topicName));
            publishContent.ThrowIfNull(nameof(publishContent));
            return MakePublishRequest(topicName, publishContent, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task PublishEventAsync(string topicName, CancellationToken cancellationToken = default)
        {
            topicName.ThrowIfNullOrEmpty(nameof(topicName));
            return MakePublishRequest(topicName, string.Empty, cancellationToken);
        }        

        private async Task MakePublishRequest<TRequest>(string topicName, TRequest publishContent, CancellationToken cancellationToken)
        {
            // Create PublishEventEnvelope
            var envelope = new Autogenerated.PublishEventEnvelope()
            {
                Topic = topicName,
            };

            if (publishContent != null)
            {
                envelope.Data = await ConvertToAnyAsync(publishContent, this.jsonSerializerOptions);
            }

            var callOptions = new CallOptions(cancellationToken: cancellationToken);
            await client.PublishEventAsync(envelope, callOptions);
        }
        #endregion

        #region InvokeBinding Apis
        public override async Task InvokeBindingAsync<TRequest>(
           string name,
           TRequest content,
           IReadOnlyDictionary<string, string> metadata = default,
           CancellationToken cancellationToken = default)
        {
            name.ThrowIfNullOrEmpty(nameof(name));

            var envelope = new Autogenerated.InvokeBindingEnvelope()
            {
                Name = name,
            };

            if (content != null)
            {
                envelope.Data = await ConvertToAnyAsync(content, this.jsonSerializerOptions);
            }

            if (metadata != null)
            {
                var d = metadata.ToDictionary(k => k.Key, k => k.Value);
                envelope.Metadata.Add(d);
            }

            var callOptions = new CallOptions(cancellationToken: cancellationToken);
            await client.InvokeBindingAsync(envelope, callOptions);
        }
        #endregion

        #region InvokeMethod Apis
        public override async Task InvokeMethodAsync(
           string serviceName,
           string methodName,
           IReadOnlyDictionary<string, string> metadata = default,
           CancellationToken cancellationToken = default)
        {
            serviceName.ThrowIfNullOrEmpty(nameof(serviceName));
            methodName.ThrowIfNullOrEmpty(nameof(methodName));

            _ = await this.MakeInvokeRequestAsync(serviceName, methodName, null, metadata, cancellationToken);
        }

        public override async Task InvokeMethodAsync<TRequest>(
           string serviceName,
           string methodName,
           TRequest data,
           IReadOnlyDictionary<string, string> metadata = default,
           CancellationToken cancellationToken = default)
        {
            serviceName.ThrowIfNullOrEmpty(nameof(serviceName));
            methodName.ThrowIfNullOrEmpty(nameof(methodName));

            Any serializedData = null;
            if (data != null)
            {
                serializedData = await ConvertToAnyAsync(data, this.jsonSerializerOptions);
            }

            _ = await this.MakeInvokeRequestAsync(serviceName, methodName, serializedData, metadata, cancellationToken);
        }

        public override async Task<TResponse> InvokeMethodAsync<TResponse>(
           string serviceName,
           string methodName,
           IReadOnlyDictionary<string, string> metadata = default,
           CancellationToken cancellationToken = default)
        {
            serviceName.ThrowIfNullOrEmpty(nameof(serviceName));
            methodName.ThrowIfNullOrEmpty(nameof(methodName));

            var response = await this.MakeInvokeRequestAsync(serviceName, methodName, null, metadata, cancellationToken);
            if (response.Data.Value.IsEmpty)
            {
                return default;
            }

            var responseData = response.Data.Value.ToStringUtf8();
            return JsonSerializer.Deserialize<TResponse>(responseData, this.jsonSerializerOptions);
        }

        public override async Task<TResponse> InvokeMethodAsync<TRequest, TResponse>(
            string serviceName,
            string methodName,
            TRequest data,
            IReadOnlyDictionary<string, string> metadata = default,
            CancellationToken cancellationToken = default)
        {
            serviceName.ThrowIfNullOrEmpty(nameof(serviceName));
            methodName.ThrowIfNullOrEmpty(nameof(methodName));

            Any serializedData = null;
            if (data != null)
            {
                serializedData = await ConvertToAnyAsync(data, this.jsonSerializerOptions);
            }

            var response = await this.MakeInvokeRequestAsync(serviceName, methodName, serializedData, metadata, cancellationToken);
            if (response.Data.Value.IsEmpty)
            {
                return default;
            }

            var responseData = response.Data.Value.ToStringUtf8();
            return JsonSerializer.Deserialize<TResponse>(responseData, this.jsonSerializerOptions);
        }

        private async Task<InvokeServiceResponseEnvelope> MakeInvokeRequestAsync(
            string serviceName,
            string methodName,
            Any data,
            IReadOnlyDictionary<string, string> metadata = default,
            CancellationToken cancellationToken = default)
        {
            var envelope = new Autogenerated.InvokeServiceEnvelope()
            {
                Id = serviceName,
                Method = methodName
            };

            if (data != null)
            {
                envelope.Data = data;
            }

            if (metadata != null)
            {
                var d = metadata.ToDictionary(k => k.Key, k => k.Value);
                envelope.Metadata.Add(d);
            }

            var callOptions = new CallOptions(cancellationToken: cancellationToken);
            return await client.InvokeServiceAsync(envelope, callOptions);
        }
        #endregion
        
        #region State Apis
        /// <inheritdoc/>
        public override async ValueTask<TValue> GetStateAsync<TValue>(string storeName, string key, ConsistencyMode? consistencyMode = default, CancellationToken cancellationToken = default)
        {
            storeName.ThrowIfNullOrEmpty(nameof(storeName));
            key.ThrowIfNullOrEmpty(nameof(key));

            var getStateEnvelope = new GetStateEnvelope()
            {
                StoreName = storeName,
                Key = key,
            };

            if (consistencyMode != null)
            {
                getStateEnvelope.Consistency = consistencyMode.ToString().ToLower();
            }

            var callOptions = new CallOptions(cancellationToken: cancellationToken);
            var response = await client.GetStateAsync(getStateEnvelope, callOptions);

            if (response.Data.Value.IsEmpty)
            {
                return default;
            }

            var responseData = response.Data.Value.ToStringUtf8();
            return JsonSerializer.Deserialize<TValue>(responseData, this.jsonSerializerOptions);
        }

        /// <inheritdoc/>
        public override async ValueTask<(TValue value, ETag eTag)> GetStateAndETagAsync<TValue>(string storeName, string key, ConsistencyMode? consistencyMode = default, CancellationToken cancellationToken = default)
        {
            storeName.ThrowIfNullOrEmpty(nameof(storeName));
            key.ThrowIfNullOrEmpty(nameof(key));

            var getStateEnvelope = new GetStateEnvelope()
            {
                StoreName = storeName,
                Key = key,
            };

            if (consistencyMode != null)
            {
                getStateEnvelope.Consistency = consistencyMode.ToString().ToLower();
            }

            var callOptions = new CallOptions(cancellationToken: cancellationToken);
            var response = await client.GetStateAsync(getStateEnvelope, callOptions);

            if (response.Data.Value.IsEmpty)
            {
                return (default(TValue), response.Etag);
            }

            var responseData = response.Data.Value.ToStringUtf8();
            var deserialized = JsonSerializer.Deserialize<TValue>(responseData, this.jsonSerializerOptions);
            return (deserialized, response.Etag);
        }

        /// <inheritdoc/>
        public override async ValueTask SaveStateAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            StateOptions stateOptions = default,
            IReadOnlyDictionary<string, string> metadata = default,
            CancellationToken cancellationToken = default)
        {
            storeName.ThrowIfNullOrEmpty(nameof(storeName));
            key.ThrowIfNullOrEmpty(nameof(key));

            await this.MakeSaveStateCallAsync<TValue>(
                storeName, 
                key, 
                value, 
                eTag: null,
                stateOptions, 
                metadata, 
                cancellationToken);
        }

        /// <inheritdoc/>
        public override async ValueTask<bool> TrySaveStateAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            ETag etag,            
            StateOptions stateOptions = default,
            IReadOnlyDictionary<string, string> metadata = default,
            CancellationToken cancellationToken = default)
        {
            storeName.ThrowIfNullOrEmpty(nameof(storeName));
            key.ThrowIfNullOrEmpty(nameof(key));

            try
            {
                await this.MakeSaveStateCallAsync<TValue>(storeName, key, value, etag, stateOptions, metadata, cancellationToken);
                return true;
            }
            catch (Exception)
            {
                // log?
            }

            return false;
        }

        internal async ValueTask MakeSaveStateCallAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            ETag eTag = default,
            StateOptions stateOptions = default,
            IReadOnlyDictionary<string, string> metadata = default,
            CancellationToken cancellationToken = default)
        {
            // Create PublishEventEnvelope
            var saveStateEnvelope = new Autogenerated.SaveStateEnvelope()
            {
                StoreName = storeName,
            };

            var stateRequest = new Autogenerated.StateRequest()
            {
                Key = key,
            };

            if (metadata != null)
            {
                var d = metadata.ToDictionary(k => k.Key, k => k.Value);
                stateRequest.Metadata.Add(d);
            }

            if(eTag != null)
            {
                stateRequest.Etag = eTag.Value;
            }

            if (stateOptions != null)
            {
                stateRequest.Options = ToAutoGeneratedStateRequestOptions(stateOptions);
            }

            if (value != null)
            {
                stateRequest.Value = await ConvertToAnyAsync(value, this.jsonSerializerOptions);
            }

            saveStateEnvelope.Requests.Add(stateRequest);
            var callOptions = new CallOptions(cancellationToken: cancellationToken);
            await client.SaveStateAsync(saveStateEnvelope, callOptions);
        }

        /// <inheritdoc/>
        public override async ValueTask DeleteStateAsync(
            string storeName,
            string key,
            StateOptions stateOptions = default,
            CancellationToken cancellationToken = default)
        {
            storeName.ThrowIfNullOrEmpty(nameof(storeName));
            key.ThrowIfNullOrEmpty(nameof(key));

            await this.MakeDeleteStateCallsync(
                storeName,
                key,
                etag: null,
                stateOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public override async ValueTask<bool> TryDeleteStateAsync(
            string storeName,
            string key,
            ETag etag = default,
            StateOptions stateOptions = default,
            CancellationToken cancellationToken = default)
        {
            storeName.ThrowIfNullOrEmpty(nameof(storeName));
            key.ThrowIfNullOrEmpty(nameof(key));

            try
            {
                await this.MakeDeleteStateCallsync(storeName, key, etag, stateOptions, cancellationToken);
                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }

        private async ValueTask MakeDeleteStateCallsync(
           string storeName,
           string key,
           ETag etag = default,
           StateOptions stateOptions = default,
           CancellationToken cancellationToken = default)
        {
            var deleteStateEnvelope = new DeleteStateEnvelope()
            {
                StoreName = storeName,
                Key = key,
            };

            if (etag != null)
            {
                deleteStateEnvelope.Etag = etag.Value;
            }

            if (stateOptions != null)
            {
                deleteStateEnvelope.Options = ToAutoGenratedStateOptions(stateOptions);
            }

            var callOptions = new CallOptions(cancellationToken: cancellationToken);
            await client.DeleteStateAsync(deleteStateEnvelope, callOptions);
        }
        #endregion

        private StateRequestOptions ToAutoGeneratedStateRequestOptions(StateOptions stateOptions)
        {
            var stateRequestOptions = new Autogenerated.StateRequestOptions();

            if (stateOptions.Consistency != null)
            {
                stateRequestOptions.Consistency = stateOptions.Consistency.Value.ToString().ToLower();
            }

            if (stateOptions.Concurrency != null)
            {
                if (stateOptions.Concurrency.Value.Equals(ConcurrencyMode.FirstWrite))
                {
                    stateRequestOptions.Consistency = "first-write";
                }

                if (stateOptions.Concurrency.Value.Equals(ConcurrencyMode.LastWrite))
                {
                    stateRequestOptions.Consistency = "last-write";
                }
            }

            if (stateOptions.RetryOptions != null)
            {
                if(stateOptions.RetryOptions.RetryMode != null)
                {
                    stateRequestOptions.RetryPolicy.Pattern = stateOptions.RetryOptions.RetryMode.Value.ToString().ToLower();
                }

                if(stateOptions.RetryOptions.RetryInterval != null)
                {
                    stateRequestOptions.RetryPolicy.Interval = Duration.FromTimeSpan(stateOptions.RetryOptions.RetryInterval.Value);
                }

                if(stateOptions.RetryOptions.RetryThreshold != null)
                {
                    stateRequestOptions.RetryPolicy.Threshold = stateOptions.RetryOptions.RetryThreshold.Value;
                }
            }

            return stateRequestOptions;
        }

        private Autogenerated.StateOptions ToAutoGenratedStateOptions(StateOptions stateOptions)
        {
            var stateRequestOptions = new Autogenerated.StateOptions();

            if (stateOptions.Consistency != null)
            {
                stateRequestOptions.Consistency = stateOptions.Consistency.Value.ToString().ToLower();
            }

            if (stateOptions.Concurrency != null)
            {
                if (stateOptions.Concurrency.Value.Equals(ConcurrencyMode.FirstWrite))
                {
                    stateRequestOptions.Consistency = "first-write";
                }

                if (stateOptions.Concurrency.Value.Equals(ConcurrencyMode.LastWrite))
                {
                    stateRequestOptions.Consistency = "last-write";
                }
            }

            if (stateOptions.RetryOptions != null)
            {
                if (stateOptions.RetryOptions.RetryMode != null)
                {
                    stateRequestOptions.RetryPolicy.Pattern = stateOptions.RetryOptions.RetryMode.Value.ToString().ToLower();
                }

                if (stateOptions.RetryOptions.RetryInterval != null)
                {
                    stateRequestOptions.RetryPolicy.Interval = Duration.FromTimeSpan(stateOptions.RetryOptions.RetryInterval.Value);
                }

                if (stateOptions.RetryOptions.RetryThreshold != null)
                {
                    stateRequestOptions.RetryPolicy.Threshold = stateOptions.RetryOptions.RetryThreshold.Value;
                }
            }

            return stateRequestOptions;
        }

        private static async Task<Any> ConvertToAnyAsync<T>(T data, JsonSerializerOptions options = null)
        {
            using var stream = new MemoryStream();

            if (data != null)
            {
                await JsonSerializer.SerializeAsync(stream, data, options);
            }

            await stream.FlushAsync();

            // set the position to beginning of stream.
            stream.Seek(0, SeekOrigin.Begin);

            return new Any
            {
                Value = await ByteString.FromStreamAsync(stream)
            };
        }
    }
}
