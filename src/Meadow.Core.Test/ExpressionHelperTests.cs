using Meadow.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Meadow.Core.Test
{
    public class ExpressionHelperTests
    {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
        class ClassExample
        {
            public static string StaticProperty { get; set; }
            public static string StaticField;

            public string InstanceProperty { get; set; }
            public string InstanceField;
        }
#pragma warning restore CS0649

        const string TEST_VAL = "123abc";

        [Fact]
        public void StaticClassProperty()
        {
            ExpressionUtil.GetSetter(() => ClassExample.StaticProperty)(TEST_VAL);
            Assert.Equal(TEST_VAL, ClassExample.StaticProperty);
        }

        [Fact]
        public void StaticClassField()
        {
            ExpressionUtil.GetSetter(() => ClassExample.StaticField)(TEST_VAL);
            Assert.Equal(TEST_VAL, ClassExample.StaticField);
        }

        [Fact]
        public void InstanceClassProperty()
        {
            var obj = new ClassExample();
            ExpressionUtil.GetSetter(() => obj.InstanceProperty)(TEST_VAL);
            Assert.Equal(TEST_VAL, obj.InstanceProperty);
        }

        [Fact]
        public void InstanceClassField()
        {
            var obj = new ClassExample();
            ExpressionUtil.GetSetter(() => obj.InstanceField)(TEST_VAL);
            Assert.Equal(TEST_VAL, obj.InstanceField);
        }
    }
}
