using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DarkNaku.Director.Tests {
    [TestFixture]
    public class SceneHandlerImplementationTests {
        private class TrackingHandler : ISceneHandler {
            public bool EnterCalled { get; private set; }
            public bool ExitCalled { get; private set; }
            public bool BeforeTransitionInCalled { get; private set; }
            public bool AfterTransitionInCalled { get; private set; }
            public bool BeforeTransitionOutCalled { get; private set; }
            public bool AfterTransitionOutCalled { get; private set; }

            public Task OnEnter() {
                EnterCalled = true;
                return Task.CompletedTask;
            }

            public Task OnExit() {
                ExitCalled = true;
                return Task.CompletedTask;
            }

            public void OnBeforeTransitionIn() => BeforeTransitionInCalled = true;
            public void OnAfterTransitionIn() => AfterTransitionInCalled = true;
            public void OnBeforeTransitionOut() => BeforeTransitionOutCalled = true;
            public void OnAfterTransitionOut() => AfterTransitionOutCalled = true;
        }

        private class TrackingGenericHandler : ISceneHandler<int> {
            public bool GenericEnterCalled { get; private set; }
            public int ReceivedParam { get; private set; }

            public Task OnEnter(int param) {
                GenericEnterCalled = true;
                ReceivedParam = param;
                return Task.CompletedTask;
            }
        }

        private class MultiInterfaceHandler : ISceneHandler<int>, ISceneHandler<string> {
            public int ReceivedInt { get; private set; }
            public string ReceivedString { get; private set; }

            public Task OnEnter(int param) {
                ReceivedInt = param;
                return Task.CompletedTask;
            }

            public Task OnEnter(string param) {
                ReceivedString = param;
                return Task.CompletedTask;
            }
        }

        [Test]
        public void TrackingHandler_모든_라이프사이클_콜백_호출_추적() {
            var handler = new TrackingHandler();

            handler.OnBeforeTransitionIn();
            handler.OnEnter();
            handler.OnAfterTransitionIn();
            handler.OnBeforeTransitionOut();
            handler.OnExit();
            handler.OnAfterTransitionOut();

            Assert.That(handler.BeforeTransitionInCalled, Is.True);
            Assert.That(handler.EnterCalled, Is.True);
            Assert.That(handler.AfterTransitionInCalled, Is.True);
            Assert.That(handler.BeforeTransitionOutCalled, Is.True);
            Assert.That(handler.ExitCalled, Is.True);
            Assert.That(handler.AfterTransitionOutCalled, Is.True);
        }

        [Test]
        public void GenericHandler_파라미터_전달_확인() {
            var handler = new TrackingGenericHandler();
            handler.OnEnter(42);

            Assert.That(handler.GenericEnterCalled, Is.True);
            Assert.That(handler.ReceivedParam, Is.EqualTo(42));
        }

        [Test]
        public void GenericHandler_리플렉션으로_OnEnter_호출() {
            var handler = new TrackingGenericHandler();
            var interfaceType = handler.GetType()
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ISceneHandler<>) &&
                    i.GetGenericArguments()[0] == typeof(int));

            Assert.That(interfaceType, Is.Not.Null);

            var method = interfaceType?.GetMethod("OnEnter");
            var task = method?.Invoke(handler, new object[] { 99 });

            Assert.That(task, Is.InstanceOf<Task>());
            Assert.That(handler.GenericEnterCalled, Is.True);
            Assert.That(handler.ReceivedParam, Is.EqualTo(99));
        }

        [Test]
        public void MultiInterfaceHandler_타입별_OnEnter_구분_호출() {
            var handler = new MultiInterfaceHandler();

            // int 인터페이스 찾기
            var intInterface = handler.GetType()
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ISceneHandler<>) &&
                    i.GetGenericArguments()[0].IsAssignableFrom(typeof(int)));

            var intMethod = intInterface?.GetMethod("OnEnter");
            intMethod?.Invoke(handler, new object[] { 42 });

            Assert.That(handler.ReceivedInt, Is.EqualTo(42));
            Assert.That(handler.ReceivedString, Is.Null);

            // string 인터페이스 찾기
            var stringInterface = handler.GetType()
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ISceneHandler<>) &&
                    i.GetGenericArguments()[0].IsAssignableFrom(typeof(string)));

            var stringMethod = stringInterface?.GetMethod("OnEnter");
            stringMethod?.Invoke(handler, new object[] { "hello" });

            Assert.That(handler.ReceivedString, Is.EqualTo("hello"));
        }

        [Test]
        public void IsAssignableFrom_타입_호환성_검증() {
            // Director의 리플렉션 로직과 동일한 패턴 테스트
            var paramType = typeof(int);

            var handler = new TrackingGenericHandler();
            var match = handler.GetType()
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ISceneHandler<>) &&
                    i.GetGenericArguments()[0].IsAssignableFrom(paramType));

            Assert.That(match, Is.Not.Null, "int 파라미터에 대응하는 ISceneHandler<int> 인터페이스를 찾아야 합니다");
        }

        [Test]
        public void IsAssignableFrom_비호환_타입_null_반환() {
            var paramType = typeof(string);

            var handler = new TrackingGenericHandler(); // ISceneHandler<int>만 구현
            var match = handler.GetType()
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ISceneHandler<>) &&
                    i.GetGenericArguments()[0].IsAssignableFrom(paramType));

            Assert.That(match, Is.Null, "string은 ISceneHandler<int>와 매칭되지 않아야 합니다");
        }
    }
}
