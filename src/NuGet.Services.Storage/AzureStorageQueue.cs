﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace NuGet.Services.Storage
{
    public class AzureStorageQueue<T> : StorageQueue<T>
    {
        private CloudQueue _queue;

        private TimeSpan _visibilityTimeout = TimeSpan.FromMinutes(5);

        public AzureStorageQueue(CloudStorageAccount account, string queueName, IStorageQueueMessageSerializer<T> serializer)
            : base(serializer)
        {
            _queue = account.CreateCloudQueueClient().GetQueueReference(queueName);
            _queue.CreateIfNotExists();
        }

        public override Task OnAdd(IStorageQueueMessage message, CancellationToken token)
        {
            CloudQueueMessage cloudMessage;

            if (message is AzureStorageQueueMessage)
            {
                cloudMessage = ((AzureStorageQueueMessage)message).Message;
            }
            else
            {
                var azureMessage = new AzureStorageQueueMessage(message.Contents);
                cloudMessage = azureMessage.Message;
            }
            
            return _queue.AddMessageAsync(cloudMessage, token);
        }

        public override async Task<IStorageQueueMessage> OnGetNext(CancellationToken token)
        {
            var nextMessage = await _queue.GetMessageAsync(
                visibilityTimeout: _visibilityTimeout, 
                options: null, 
                operationContext: null, 
                cancellationToken: token);

            if (nextMessage == null)
            {
                return null;
            }

            return new AzureStorageQueueMessage(nextMessage);
        }

        public override Task OnRemove(IStorageQueueMessage message, CancellationToken token)
        {
            if (!(message is AzureStorageQueueMessage))
            {
                throw new ArgumentException("This message was not returned from this queue!", nameof(message));
            }

            return _queue.DeleteMessageAsync((message as AzureStorageQueueMessage).Message, token);
        }
    }
}