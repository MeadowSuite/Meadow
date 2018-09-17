using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Meadow.Core.Utils
{
    public static class TypeConversion
    {
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
