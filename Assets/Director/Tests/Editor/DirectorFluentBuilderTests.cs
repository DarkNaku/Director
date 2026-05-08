using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace DarkNaku.Director.Tests {
    [TestFixture]
    public class DirectorFluentBuilderTests {
        private Director _director;

        [SetUp]
        public void SetUp() {
            var instanceField = typeof(Director).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            var destroyedField = typeof(Director).GetField("_isDestroyed", BindingFlags.NonPublic | BindingFlags.Static);

            instanceField?.SetValue(null, null);
            destroyedField?.SetValue(null, false);

            _director = Director.Instance;
        }

        [TearDown]
        public void TearDown() {
            if (_director != null) {
                Object.DestroyImmediate(_director.gameObject);
            }

            var instanceField = typeof(Director).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);

            var destroyedField = typeof(Director).GetField("_isDestroyed", BindingFlags.NonPublic | BindingFlags.Static);
            destroyedField?.SetValue(null, false);
        }

        [Test]
        public void WithLoading_로딩씬_이름_설정() {
            _director.WithLoading("LoadingScene");

            var field = typeof(Director).GetField("_loadingScene", BindingFlags.NonPublic | BindingFlags.Instance);
            var value = field?.GetValue(_director) as string;

            Assert.That(value, Is.EqualTo("LoadingScene"));
        }

        [Test]
        public void WithLoading_자기자신_반환() {
            var result = _director.WithLoading("LoadingScene");

            Assert.That(result, Is.SameAs(_director));
        }

        [Test]
        public void SetMinLoadingTime_최소_로딩_시간_설정() {
            _director.SetMinLoadingTime(2.5f);

            var field = typeof(Director).GetField("_minLoadingTime", BindingFlags.NonPublic | BindingFlags.Instance);
            var value = (float)field?.GetValue(_director);

            Assert.That(value, Is.EqualTo(2.5f));
        }

        [Test]
        public void SetMinLoadingTime_자기자신_반환() {
            var result = _director.SetMinLoadingTime(1f);

            Assert.That(result, Is.SameAs(_director));
        }

        [Test]
        public void WithParam_파라미터_타입과_값_설정() {
            _director.WithParam(42);

            var paramField = typeof(Director).GetField("_param", BindingFlags.NonPublic | BindingFlags.Instance);
            var param = paramField?.GetValue(_director);

            Assert.That(param, Is.Not.Null);

            var paramInnerType = param.GetType();
            var typeField = paramInnerType.GetField("Type");
            var valueField = paramInnerType.GetField("Value");

            Assert.That(typeField?.GetValue(param), Is.EqualTo(typeof(int)));
            Assert.That(valueField?.GetValue(param), Is.EqualTo(42));
        }

        [Test]
        public void WithParam_문자열_파라미터_설정() {
            _director.WithParam("hello");

            var paramField = typeof(Director).GetField("_param", BindingFlags.NonPublic | BindingFlags.Instance);
            var param = paramField?.GetValue(_director);
            var paramInnerType = param.GetType();

            Assert.That(paramInnerType.GetField("Type")?.GetValue(param), Is.EqualTo(typeof(string)));
            Assert.That(paramInnerType.GetField("Value")?.GetValue(param), Is.EqualTo("hello"));
        }

        [Test]
        public void WithParam_자기자신_반환() {
            var result = _director.WithParam(100);

            Assert.That(result, Is.SameAs(_director));
        }

        [Test]
        public void 플루언트_체이닝_동작() {
            var result = _director
                .WithLoading("Loading")
                .SetMinLoadingTime(3f)
                .WithParam(99);

            Assert.That(result, Is.SameAs(_director));

            var loadingField = typeof(Director).GetField("_loadingScene", BindingFlags.NonPublic | BindingFlags.Instance);
            var minTimeField = typeof(Director).GetField("_minLoadingTime", BindingFlags.NonPublic | BindingFlags.Instance);
            var paramField = typeof(Director).GetField("_param", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(loadingField?.GetValue(_director), Is.EqualTo("Loading"));
            Assert.That((float)minTimeField?.GetValue(_director), Is.EqualTo(3f));
            Assert.That(paramField?.GetValue(_director), Is.Not.Null);
        }

        [Test]
        public void WithParam_커스텀_타입_설정() {
            var data = new TestData { Id = 1, Name = "Test" };
            _director.WithParam(data);

            var paramField = typeof(Director).GetField("_param", BindingFlags.NonPublic | BindingFlags.Instance);
            var param = paramField?.GetValue(_director);
            var paramInnerType = param.GetType();

            Assert.That(paramInnerType.GetField("Type")?.GetValue(param), Is.EqualTo(typeof(TestData)));

            var value = paramInnerType.GetField("Value")?.GetValue(param) as TestData;
            Assert.That(value?.Id, Is.EqualTo(1));
            Assert.That(value?.Name, Is.EqualTo("Test"));
        }

        private class TestData {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
