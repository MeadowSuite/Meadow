using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Meadow.Cli
{
    public class ContractTypeHolder : IDynamicMetaObjectProvider
    {
        readonly Type[] _items;

        public ContractTypeHolder(Type[] types)
        {
            _items = types;
        }

        object GetEntry(string name)
        {
            return _items.Single(t => t.Name == name);
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new TestDynamicMetaObject(parameter, this);
        }

        class TestDynamicMetaObject : DynamicMetaObject
        {
            public TestDynamicMetaObject(Expression expression, ContractTypeHolder value) 
                : base(expression, BindingRestrictions.Empty, value)
            {
            }

            public override IEnumerable<string> GetDynamicMemberNames()
            {
                foreach (var t in (Value as ContractTypeHolder)._items)
                {
                    yield return t.Name;
                }
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                if (!(Value as ContractTypeHolder)._items.Any(t => t.Name == binder.Name))
                {
                    return base.BindGetMember(binder);
                }

                Expression<Func<ContractTypeHolder, string, object>> getEntryExpression = (obj, p) => obj.GetEntry(p);
                MethodInfo getEntryMethod = (getEntryExpression.Body as MethodCallExpression).Method;
                Expression[] parameters = new Expression[] { Expression.Constant(binder.Name) };
                MethodCallExpression getEntryCallExpression = Expression.Call(Expression.Convert(Expression, LimitType), getEntryMethod, parameters);
                BindingRestrictions bindingTypeRestriction = BindingRestrictions.GetTypeRestriction(Expression, LimitType);
                DynamicMetaObject getDictionaryEntry = new DynamicMetaObject(getEntryCallExpression, bindingTypeRestriction);
                return getDictionaryEntry;
            }
        }
    }
}
