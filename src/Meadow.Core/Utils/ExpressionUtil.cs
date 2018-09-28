using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Meadow.Core.Utils
{
    public static class ExpressionUtil
    {
        public static Action<TVal> GetSetter<TVal>(Expression<Func<TVal>> exp)
        {
            var propertyGetExpression = exp.Body as MemberExpression;

            // Expression is for an instance member
            if (propertyGetExpression?.Expression is MemberExpression fieldOnClosureExpression && fieldOnClosureExpression != null)
            {
                var closureClassExpression = fieldOnClosureExpression.Expression as ConstantExpression;
                var closureClassInstance = closureClassExpression.Value;

                var closureFieldInfo = fieldOnClosureExpression.Member as FieldInfo;
                var closureFieldValue = closureFieldInfo.GetValue(closureClassInstance);

                switch (propertyGetExpression.Member)
                {
                    case PropertyInfo propInfo:
                        return v => propInfo.SetValue(closureFieldValue, v);
                    case FieldInfo fieldInfo:
                        return v => fieldInfo.SetValue(closureFieldValue, v);
                }
            }

            // Expression is for a static member
            else
            {
                var memberInfo = GetMember(exp);
                switch (memberInfo)
                {
                    case FieldInfo fieldInfo:
                        return v => fieldInfo.SetValue(null, v);
                    case PropertyInfo propertyInfo:
                        return v => propertyInfo.SetValue(null, v);
                }
            }

            throw new Exception($"Unsupported expression {exp}");
        }
        
        public static MemberInfo GetMember(LambdaExpression exp)
        {
            if (exp.Body is MemberExpression memberExp)
            {
                return memberExp.Member;
            }
            else if (exp.Body is UnaryExpression unaryExp)
            {
                if (unaryExp.Operand is MemberExpression unaryMemExp)
                {
                    return unaryMemExp.Member;
                }
                else if (unaryExp.Operand is ConstantExpression constExp)
                {
                    if (constExp.Type.IsEnum)
                    {
                        return constExp.Type.GetField(constExp.Value.ToString());
                    }
                }
            }
            else if (exp.Body is ConstantExpression constExp)
            {
                if (constExp.Type.IsEnum)
                {
                    return constExp.Type.GetField(constExp.Value.ToString());
                }
            }

            throw new NotImplementedException("Member expression inspection for not implemented for expression: " + exp);
        }

    }
}
