using System.Threading.Tasks;
using NUnit.Framework;

namespace DarkNaku.Director.Tests {
    [TestFixture]
    public class InterfaceDefaultTests {
        private class MockSceneHandler : ISceneHandler { }
        private class MockGenericSceneHandler : ISceneHandler<int> { }
        private class MockSceneTransition : ISceneTransition { }
        private class MockLoadingProgress : ILoadingProgress {
            public float LastProgress { get; private set; }
            public void OnProgress(float progress) => LastProgress = progress;
        }

        [Test]
        public void ISceneHandler_OnEnter_기본구현_CompletedTask_반환() {
            ISceneHandler handler = new MockSceneHandler();
            var task = handler.OnEnter();

            Assert.That(task, Is.EqualTo(Task.CompletedTask));
        }

        [Test]
        public void ISceneHandler_OnExit_기본구현_CompletedTask_반환() {
            ISceneHandler handler = new MockSceneHandler();
            var task = handler.OnExit();

            Assert.That(task, Is.EqualTo(Task.CompletedTask));
        }

        [Test]
        public void ISceneHandler_라이프사이클_콜백_기본구현_예외없음() {
            ISceneHandler handler = new MockSceneHandler();

            Assert.DoesNotThrow(() => {
                handler.OnBeforeTransitionIn();
                handler.OnAfterTransitionIn();
                handler.OnBeforeTransitionOut();
                handler.OnAfterTransitionOut();
            });
        }

        [Test]
        public void ISceneHandler_제네릭_OnEnter_기본구현_CompletedTask_반환() {
            ISceneHandler<int> handler = new MockGenericSceneHandler();
            var task = handler.OnEnter(42);

            Assert.That(task, Is.EqualTo(Task.CompletedTask));
        }

        [Test]
        public void ISceneHandler_제네릭_ISceneHandler_상속() {
            ISceneHandler<int> handler = new MockGenericSceneHandler();

            Assert.That(handler, Is.InstanceOf<ISceneHandler>());
        }

        [Test]
        public void ISceneTransition_TransitionIn_기본구현_CompletedTask_반환() {
            ISceneTransition transition = new MockSceneTransition();
            var task = transition.TransitionIn("from", "to");

            Assert.That(task, Is.EqualTo(Task.CompletedTask));
        }

        [Test]
        public void ISceneTransition_TransitionOut_기본구현_CompletedTask_반환() {
            ISceneTransition transition = new MockSceneTransition();
            var task = transition.TransitionOut("from", "to");

            Assert.That(task, Is.EqualTo(Task.CompletedTask));
        }

        [Test]
        public void ISceneTransition_Prepare_기본구현_예외없음() {
            ISceneTransition transition = new MockSceneTransition();

            Assert.DoesNotThrow(() => {
                transition.PrepareTransitionIn("from", "to");
                transition.PrepareTransitionOut("from", "to");
            });
        }

        [Test]
        public void ILoadingProgress_OnProgress_값_전달() {
            var progress = new MockLoadingProgress();
            progress.OnProgress(0.5f);

            Assert.That(progress.LastProgress, Is.EqualTo(0.5f));
        }

        [Test]
        public void ILoadingProgress_OnProgress_완료값_전달() {
            var progress = new MockLoadingProgress();
            progress.OnProgress(1f);

            Assert.That(progress.LastProgress, Is.EqualTo(1f));
        }
    }
}
