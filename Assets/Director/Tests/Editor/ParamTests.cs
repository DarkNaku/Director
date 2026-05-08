using System;
using System.Reflection;
using NUnit.Framework;

namespace DarkNaku.Director.Tests {
    [TestFixture]
    public class ParamTests {
        private Type _paramType;
        private ConstructorInfo _constructor;

        [SetUp]
        public void SetUp() {
            _paramType = typeof(Director).GetNestedType("Param", BindingFlags.NonPublic);
            _constructor = _paramType?.GetConstructor(new[] { typeof(Type), typeof(object) });
        }

        [Test]
        public void Param_클래스_존재_확인() {
            Assert.That(_paramType, Is.Not.Null, "Director 내부에 Param 클래스가 존재해야 합니다");
        }

        [Test]
        public void Param_int_타입과_값_저장() {
            var param = _constructor?.Invoke(new object[] { typeof(int), 42 });

            var type = _paramType.GetField("Type")?.GetValue(param);
            var value = _paramType.GetField("Value")?.GetValue(param);

            Assert.That(type, Is.EqualTo(typeof(int)));
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void Param_string_타입과_값_저장() {
            var param = _constructor?.Invoke(new object[] { typeof(string), "test" });

            var type = _paramType.GetField("Type")?.GetValue(param);
            var value = _paramType.GetField("Value")?.GetValue(param);

            Assert.That(type, Is.EqualTo(typeof(string)));
            Assert.That(value, Is.EqualTo("test"));
        }

        [Test]
        public void Param_null_값_저장() {
            var param = _constructor?.Invoke(new object[] { typeof(string), null });

            var type = _paramType.GetField("Type")?.GetValue(param);
            var value = _paramType.GetField("Value")?.GetValue(param);

            Assert.That(type, Is.EqualTo(typeof(string)));
            Assert.That(value, Is.Null);
        }

        [Test]
        public void Param_float_타입과_값_저장() {
            var param = _constructor?.Invoke(new object[] { typeof(float), 3.14f });

            var type = _paramType.GetField("Type")?.GetValue(param);
            var value = _paramType.GetField("Value")?.GetValue(param);

            Assert.That(type, Is.EqualTo(typeof(float)));
            Assert.That(value, Is.EqualTo(3.14f));
        }

        [Test]
        public void Param_커스텀_클래스_저장() {
            var data = new TestPayload { Score = 100 };
            var param = _constructor?.Invoke(new object[] { typeof(TestPayload), data });

            var type = _paramType.GetField("Type")?.GetValue(param);
            var value = _paramType.GetField("Value")?.GetValue(param) as TestPayload;

            Assert.That(type, Is.EqualTo(typeof(TestPayload)));
            Assert.That(value, Is.Not.Null);
            Assert.That(value?.Score, Is.EqualTo(100));
        }

        private class TestPayload {
            public int Score { get; set; }
        }
    }
}
