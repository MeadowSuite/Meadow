using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Meadow.Core.Utils
{
    public static class EnumExtensions
    {
        static ConcurrentDictionary<(Type, Enum), string> _cache = new ConcurrentDictionary<(Type, Enum), string>();

        // TODO: implement feature to specific the attribute type as generic param

#if LANG_7_3

        public static string GetMemberValue<TEnum>(this TEnum enumMember) where TEnum : struct, Enum
        {
            var enumType = typeof(TEnum);
            var val = _cache.GetOrAdd((enumType, enumMember), key =>
            {
                var field = enumType.GetField(enumMember.ToString());
                var memberAttr = field.GetCustomAttribute<EnumMemberAttribute>();
                return memberAttr.Value;
            });
            return val;
        }

        public static TEnum[] GetValues<TEnum>() where TEnum : struct, Enum
        {
            return (TEnum[])Enum.GetValues(typeof(TEnum));
        }
        
#else
        public static string GetMemberValue<TEnum>(this TEnum enumMember) where TEnum : struct
        {
            var enumType = typeof(TEnum);
            var val = _cache.GetOrAdd((enumType, enumMember as Enum), key =>
            {
                var field = enumType.GetField(enumMember.ToString());
                var memberAttr = field.GetCustomAttribute<EnumMemberAttribute>();
                return memberAttr.Value;
            });
            return val;
        }

        public static TEnum[] GetValues<TEnum>() where TEnum : struct
        {
            return (TEnum[])Enum.GetValues(typeof(TEnum));
        }
#endif

    }
}
