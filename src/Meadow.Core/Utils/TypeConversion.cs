using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Meadow.Core.Utils
{
    public static class TypeConversion
    {
        static Lazy<MethodInfo> _convertMethod = new Lazy<MethodInfo>(() => typeof(TypeConversion).GetMethod(nameof(ConvertValue), new[] { typeof(object) }));

        public static object ConvertValue(Type toType, object fromObj)
        {
            var genericConvertMethod = _convertMethod.Value.MakeGenericMethod(toType);
            var result = genericConvertMethod.Invoke(null, new[] { fromObj });
            return result;
        }

        public static TVal ConvertValue<TVal>(object fromObj)
        {
            if (fromObj is TVal valResult)
            {
                return valResult;
            }
            else if (fromObj == null)
            {
                return default;
            }
            else
            {
                var converter = TypeDescriptor.GetConverter(typeof(TVal));
                if (converter.CanConvertTo(typeof(TVal)) && converter.CanConvertFrom(fromObj.GetType()))
                {
                    var convertedResult = (TVal)converter.ConvertFrom(fromObj);
                    return convertedResult;
                }
                else
                {
                    dynamic dynValue = fromObj;
                    return (TVal)dynValue;
                }
            }
        }
    }
}
