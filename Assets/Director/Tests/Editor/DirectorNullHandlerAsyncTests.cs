using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace DarkNaku.Director.Tests {
    /// <summary>
    /// 씬 핸들러가 null이거나 비동기 처리가 지연될 때 Director의 진입 흐름이
    /// 예외 없이 정상 완료되는지 검증합니다.
    /// `await handler?.ProcessOnEnterScene()` 형태가 핸들러 null 시 NullReferenceException으로
    /// 전환을 멈추던 회귀 버그에 대한 가드입니다.
    /// </summary>
    [TestFixture]
    public class DirectorNullHandlerAsyncTests {
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
        public void EnterSceneAsync_핸들러_null이어도_예외없이_완료() {
            // 회귀 테스트: 수정 전에는 await null로 NullReferenceException이 발생해
            // Task가 faulted 상태가 되고 전환이 멈췄다.
            var task = InvokeEnterSceneAsync(null);

            Assert.DoesNotThrow(() => task.Wait());
            Assert.That(task.IsFaulted, Is.False);
            Assert.That(task.IsCompletedSuccessfully, Is.True);
        }

        [Test]
        public void EnterSceneAsync_핸들러_있으면_OnEnterScene과_ProcessOnEnterScene_모두_호출() {
            var handler = new TrackingHandler();

            var task = InvokeEnterSceneAsync(handler);
            task.Wait();

            Assert.That(handler.OnEnterSceneCalled, Is.True);
            Assert.That(handler.ProcessOnEnterSceneCalled, Is.True);
            Assert.That(task.IsCompletedSuccessfully, Is.True);
        }

        [Test]
        public void EnterSceneAsync_ProcessOnEnterScene_완료까지_대기() {
            var handler = new DeferredHandler();

            var task = InvokeEnterSceneAsync(handler);

            // ProcessOnEnterScene이 미완료 Task를 반환했으므로 아직 대기 중이어야 한다.
            Assert.That(task.IsCompleted, Is.False);

            handler.Complete();
            task.Wait();

            Assert.That(task.IsCompletedSuccessfully, Is.True);
        }

        private Task InvokeEnterSceneAsync(ISceneHandler handler) {
            var method = typeof(Director).GetMethod(
                "EnterSceneAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(method, Is.Not.Null, "EnterSceneAsync 메서드를 찾을 수 없습니다");

            return (Task)method.Invoke(_director, new object[] { handler });
        }

        private class TrackingHandler : ISceneHandler {
            public bool OnEnterSceneCalled { get; private set; }
            public bool ProcessOnEnterSceneCalled { get; private set; }

            public void OnEnterScene() {
                OnEnterSceneCalled = true;
            }

            public Task ProcessOnEnterScene() {
                ProcessOnEnterSceneCalled = true;
                return Task.CompletedTask;
            }
        }

        private class DeferredHandler : ISceneHandler {
            private readonly TaskCompletionSource<bool> _tcs = new();

            public Task ProcessOnEnterScene() {
                return _tcs.Task;
            }

            public void Complete() {
                _tcs.SetResult(true);
            }
        }
    }
}
