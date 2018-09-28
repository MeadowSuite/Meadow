using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.UnitTestTemplate
{
    /// <summary>
    /// Warning: careful usage of this class is required to avoid deadlocks. 
    /// Concurrent usage of the synchronous and async methods can cause a deadlock.
    /// </summary>
    public class AsyncObjectPool<TItem> : IDisposable where TItem : class
    {
        readonly SemaphoreSlim _semaphore;
        readonly List<TItem> _items;
        readonly Func<Task<TItem>> _createItem;

        public AsyncObjectPool(Func<Task<TItem>> createItem)
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _items = new List<TItem>();
            _createItem = createItem;
        }

        public async Task<TItem> Get()
        {
            await _semaphore.WaitAsync();
            try
            {
                TItem item;
                if (_items.Count > 0)
                {
                    item = _items[0];
                    _items.RemoveAt(0);
                }
                else
                {
                    item = await _createItem();
                }

                return item;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task PutAsync(TItem item)
        {
            await _semaphore.WaitAsync();
            try
            {
                _items.Add(item);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Put(TItem item)
        {
            _semaphore.Wait();
            try
            {
                _items.Add(item);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<TItem[]> GetItemsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return _items.ToArray();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public TItem[] GetItems()
        {
            _semaphore.Wait();
            try
            {
                return _items.ToArray();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> HasItemsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return _items.Count > 0;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public bool HasItems()
        {
            _semaphore.Wait();
            try
            {
                return _items.Count > 0;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _semaphore.Wait();
            _semaphore.Dispose();
            foreach (var item in _items)
            {
                if (item is IDisposable disposableItem)
                {
                    disposableItem.Dispose();
                }
            }

            _items.Clear();
        }
    }
}
