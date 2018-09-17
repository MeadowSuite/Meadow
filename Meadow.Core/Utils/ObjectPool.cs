using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Meadow.Core.Utils
{
    public class ObjectPool<T>
    {
        private ConcurrentBag<T> _objects;
        private Func<T> _objectGenerator;

        public ObjectPool(Func<T> objectGenerator)
        {
            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator;
        }

        public T Get()
        {
            if (_objects.TryTake(out var item))
            {
                return item;
            }

            return _objectGenerator();
        }

        public void Put(T item)
        {
            _objects.Add(item);
        }
    }

}
