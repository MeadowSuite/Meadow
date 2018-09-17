using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Meadow.Core.Utils
{
    public static class TypeAttributeCache<TType, TAttribute> where TAttribute : Attribute
    {
        public static readonly TAttribute Attribute;

        static TypeAttributeCache()
        {
            Attribute = typeof(TType).GetCustomAttribute<TAttribute>();
        }
    }
}
