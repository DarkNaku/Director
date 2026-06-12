using System;
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
                UnityEngine.Object.DestroyImmediate(_director.gameObject);
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
        public void WithParam_디스패처_설정() {
            _director.WithParam(42);

            var dispatcher = GetDispatcher(_director);

            Assert.That(dispatcher, Is.Not.Null);
        }

        [Test]
        public void WithParam_정수_파라미터_제네릭_핸들러로_전달() {
            _director.WithParam(42);

            var handler = new TrackingIntHandler();
            InvokeDispatcher(_director, handler);

            Assert.That(handler.ReceivedValue, Is.EqualTo(42));
            Assert.That(handler.OnEnterCalled, Is.False);
        }

        [Test]
        public void WithParam_문자열_파라미터_제네릭_핸들러로_전달() {
            _director.WithParam("hello");

            var handler = new TrackingStringHandler();
            InvokeDispatcher(_director, handler);

            Assert.That(handler.ReceivedValue, Is.EqualTo("hello"));
        }

        [Test]
        public void WithParam_매칭_핸들러_없으면_파라미터_없는_OnEnter_호출() {
            _director.WithParam(42);

            var handler = new TrackingNoParamHandler();
            InvokeDispatcher(_director, handler);

            Assert.That(handler.OnEnterCalled, Is.True);
        }

        [Test]
        public void WithParam_커스텀_타입_파라미터_전달() {
            var payload = new TestData { Id = 1, Name = "Test" };
            _director.WithParam(payload);

            var handler = new TrackingCustomHandler();
            InvokeDispatcher(_director, handler);

            Assert.That(handler.ReceivedValue, Is.SameAs(payload));
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

            Assert.That(loadingField?.GetValue(_director), Is.EqualTo("Loading"));
            Assert.That((float)minTimeField?.GetValue(_director), Is.EqualTo(3f));

            var handler = new TrackingIntHandler();
            InvokeDispatcher(_director, handler);
            Assert.That(handler.ReceivedValue, Is.EqualTo(99));
        }

        private static Action<ISceneHandler> GetDispatcher(Director director) {
            var field = typeof(Director).GetField("_enterDispatcher", BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(director) as Action<ISceneHandler>;
        }

        private static void InvokeDispatcher(Director director, ISceneHandler handler) {
            var dispatcher = GetDispatcher(director);
            Assert.That(dispatcher, Is.Not.Null, "디스패처가 설정되어 있어야 합니다");
            dispatcher(handler);
        }

        private class TrackingIntHandler : ISceneHandler<int> {
            public int ReceivedValue { get; private set; }
            public bool OnEnterCalled { get; private set; }

            public void OnEnter(int param) {
                ReceivedValue = param;
            }

            public void OnEnter() {
                OnEnterCalled = true;
            }
        }

        private class TrackingStringHandler : ISceneHandler<string> {
            public string ReceivedValue { get; private set; }

            public void OnEnter(string param) {
                ReceivedValue = param;
            }
        }

        private class TrackingCustomHandler : ISceneHandler<TestData> {
            public TestData ReceivedValue { get; private set; }

            public void OnEnter(TestData param) {
                ReceivedValue = param;
            }
        }

        private class TrackingNoParamHandler : ISceneHandler {
            public bool OnEnterCalled { get; private set; }

            public void OnEnter() {
                OnEnterCalled = true;
            }
        }

        private class TestData {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
