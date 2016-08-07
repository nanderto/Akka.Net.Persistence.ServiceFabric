﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akka.Persistence.ServiceFabric.Tests.Mocks
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Data.Notifications;

    public class MockReliableDictionary<TKey, TValue> : IReliableDictionary<TKey, TValue>
        where TKey : IComparable<TKey>, IEquatable<TKey>
    {
        private ConcurrentDictionary<TKey, TValue> dictionary = new ConcurrentDictionary<TKey, TValue>();

        public event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>> DictionaryChanged;

        public Uri Name { get; set; }

        public Func<IReliableDictionary<TKey, TValue>, NotifyDictionaryRebuildEventArgs<TKey, TValue>, Task> RebuildNotificationAsyncCallback
        {
            set { throw new NotImplementedException(); }
        }

        public Task AddAsync(ITransaction tx, TKey key, TValue value)
        {
            if (!this.dictionary.TryAdd(key, value))
            {
                throw new InvalidOperationException("key already exists: " + key.ToString());
            }


            return Task.FromResult(true);
        }

        public Task AddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (!this.dictionary.TryAdd(key, value))
            {
                throw new InvalidOperationException("key already exists: " + key.ToString());
            }

            return Task.FromResult(true);
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            return Task.FromResult(this.dictionary.AddOrUpdate(key, addValueFactory, updateValueFactory));
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            return Task.FromResult(this.dictionary.AddOrUpdate(key, addValue, updateValueFactory));
        }

        public Task<TValue> AddOrUpdateAsync(
            ITransaction tx, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(this.dictionary.AddOrUpdate(key, addValueFactory, updateValueFactory));
        }

        public Task<TValue> AddOrUpdateAsync(
            ITransaction tx, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.dictionary.AddOrUpdate(key, addValue, updateValueFactory));
        }

        public Task ClearAsync()
        {
            this.dictionary.Clear();

            return Task.FromResult(true);
        }

        public Task ClearAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            this.dictionary.Clear();

            return Task.FromResult(true);
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key)
        {
            return Task.FromResult(this.dictionary.ContainsKey(key));
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, LockMode lockMode)
        {
            return Task.FromResult(this.dictionary.ContainsKey(key));
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.dictionary.ContainsKey(key));
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, LockMode lockMode, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.dictionary.ContainsKey(key));
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key)
        {
            TValue value;
            bool result = this.dictionary.TryGetValue(key, out value);

            return Task.FromResult(new ConditionalValue<TValue>(result, value));
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, LockMode lockMode)
        {
            TValue value;
            bool result = this.dictionary.TryGetValue(key, out value);

            return Task.FromResult(new ConditionalValue<TValue>(result, value));
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
        {
            TValue value;
            bool result = this.dictionary.TryGetValue(key, out value);

            return Task.FromResult(new ConditionalValue<TValue>(result, value));
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(
            ITransaction tx, TKey key, LockMode lockMode, TimeSpan timeout, CancellationToken cancellationToken)
        {
            TValue value;
            bool result = this.dictionary.TryGetValue(key, out value);

            return Task.FromResult(new ConditionalValue<TValue>(result, value));
        }

        public Task SetAsync(ITransaction tx, TKey key, TValue value)
        {
            this.dictionary[key] = value;

            return Task.FromResult(true);
        }

        public Task SetAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            this.dictionary[key] = value;

            return Task.FromResult(true);
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, Func<TKey, TValue> valueFactory)
        {
            return Task.FromResult(this.dictionary.GetOrAdd(key, valueFactory));
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, TValue value)
        {
            return Task.FromResult(this.dictionary.GetOrAdd(key, value));
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, Func<TKey, TValue> valueFactory, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.dictionary.GetOrAdd(key, valueFactory));
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.dictionary.GetOrAdd(key, value));
        }

        public Task<bool> TryAddAsync(ITransaction tx, TKey key, TValue value)
        {
            return Task.FromResult(this.dictionary.TryAdd(key, value));
        }

        public Task<bool> TryAddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.dictionary.TryAdd(key, value));
        }

        public Task<ConditionalValue<TValue>> TryRemoveAsync(ITransaction tx, TKey key)
        {
            TValue outValue;
            return Task.FromResult(new ConditionalValue<TValue>(this.dictionary.TryRemove(key, out outValue), outValue));
        }

        public Task<ConditionalValue<TValue>> TryRemoveAsync(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.TryRemoveAsync(tx, key);
        }

        public Task<bool> TryUpdateAsync(ITransaction tx, TKey key, TValue newValue, TValue comparisonValue)
        {
            return Task.FromResult(this.dictionary.TryUpdate(key, newValue, comparisonValue));
        }

        public Task<bool> TryUpdateAsync(
            ITransaction tx, TKey key, TValue newValue, TValue comparisonValue, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.dictionary.TryUpdate(key, newValue, comparisonValue));
        }

        public Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(ITransaction txn)
        {
            return Task.FromResult<IAsyncEnumerable<KeyValuePair<TKey, TValue>>>(new MockAsyncEnumerable<KeyValuePair<TKey, TValue>>(this.dictionary));
        }

        public Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(ITransaction txn, EnumerationMode enumerationMode)
        {
            return Task.FromResult<IAsyncEnumerable<KeyValuePair<TKey, TValue>>>(
                new MockAsyncEnumerable<KeyValuePair<TKey, TValue>>(
                    enumerationMode == EnumerationMode.Unordered
                        ? (IEnumerable<KeyValuePair<TKey, TValue>>)this.dictionary
                        : this.dictionary.OrderBy(x => x.Key)));
        }

        public Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(
            ITransaction txn, Func<TKey, bool> filter, EnumerationMode enumerationMode)
        {
            return Task.FromResult<IAsyncEnumerable<KeyValuePair<TKey, TValue>>>(
                new MockAsyncEnumerable<KeyValuePair<TKey, TValue>>(
                    enumerationMode == EnumerationMode.Unordered
                        ? this.dictionary.Where(x => filter(x.Key))
                        : this.dictionary.Where(x => filter(x.Key)).OrderBy(x => x.Key)));
        }

        public Task<long> GetCountAsync(ITransaction tx)
        {
            return Task.FromResult((long)this.dictionary.Count);
        }

        public Task<long> GetCountAsync()
        {
            return Task.FromResult((long)this.dictionary.Count);
        }
    }
}
