using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Meadow.Core.Utils
{
    public static class AttributeHelper
    {
        public static TAttribute GetAttribute<TAttribute>(LambdaExpression exp) where TAttribute : Attribute
        {
            var memberInfo = ExpressionUtil.GetMember(exp);
            return memberInfo.GetCustomAttribute<TAttribute>();
        }

        public static TAttribute GetAttribute<TAttribute>(Expression<Func<object>> exp) where TAttribute : Attribute
        {
            return GetAttribute<TAttribute>((LambdaExpression)exp);
        }

        public static TAttribute GetAttribute<TAttribute, TVal>(Expression<Func<TVal>> exp) where TAttribute : Attribute
        {
            return GetAttribute<TAttribute>(exp);
        }

        public static TVal GetDefault<TVal>(Expression<Func<TVal>> exp)
        {
            var attr = GetAttribute<DefaultValueAttribute>(exp);
            return TypeConversion.ConvertValue<TVal>(attr.Value);
        }

        public static TVal GetDefault<TVal>(Expression<Func<TVal?>> exp) where TVal : struct
        {
            var attr = GetAttribute<DefaultValueAttribute>(exp);
            return TypeConversion.ConvertValue<TVal>(attr.Value);
        }



    }
}
