using System;
using System.Collections.Concurrent;

namespace Meadow.Core.Utils
{
    public class AutoObjectPool<T> where T : class, new()
    {
        static readonly AutoObjectPool<T> Instance = new AutoObjectPool<T>();

        static ConcurrentBag<T> _objects;

        public AutoObjectPool()
        {
            _objects = new ConcurrentBag<T>();
        }

        class DisposableCallback : IDisposable
        {
            readonly Action _onDispose;

            public DisposableCallback(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                _onDispose();
            }
        }

        public static IDisposable Get(out T item)
        {
            if (!_objects.TryTake(out var localItem))
            {
                localItem = new T();
            }

            item = localItem;
            return new DisposableCallback(() => Put(localItem));
        }

        static void Put(T item)
        {
            _objects.Add(item);
        }
    }

}
