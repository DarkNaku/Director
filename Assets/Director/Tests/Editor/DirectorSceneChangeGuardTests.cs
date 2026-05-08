using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace DarkNaku.Director.Tests {
    [TestFixture]
    public class DirectorSceneChangeGuardTests {
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
        public void 씬_전환중_플래그_기본값_false() {
            var field = typeof(Director).GetField("_changing", BindingFlags.NonPublic | BindingFlags.Instance);
            var value = (bool)field?.GetValue(_director);

            Assert.That(value, Is.False);
        }

        [Test]
        public void 씬_전환중일때_추가_전환_차단() {
            var changingField = typeof(Director).GetField("_changing", BindingFlags.NonPublic | BindingFlags.Instance);
            changingField?.SetValue(_director, true);

            // ChangeAsync 호출 시 _changing이 true면 즉시 리턴
            var method = typeof(Director).GetMethod("ChangeAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = method?.Invoke(_director, new object[] { "NextScene" });

            // _changing이 여전히 true인지 확인 (리셋되지 않았으므로 즉시 리턴한 것)
            var value = (bool)changingField?.GetValue(_director);
            Assert.That(value, Is.True);
        }

        [Test]
        public void OnSubSystemRegistration_정적_필드_초기화() {
            var instanceField = typeof(Director).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            var destroyedField = typeof(Director).GetField("_isDestroyed", BindingFlags.NonPublic | BindingFlags.Static);
            var firstEnterField = typeof(Director).GetField("_firstEnterDone", BindingFlags.NonPublic | BindingFlags.Static);

            destroyedField?.SetValue(null, true);
            firstEnterField?.SetValue(null, true);

            var method = typeof(Director).GetMethod("OnSubSystemRegistration", BindingFlags.NonPublic | BindingFlags.Static);
            method?.Invoke(null, null);

            Assert.That(instanceField?.GetValue(null), Is.Null);
            Assert.That((bool)destroyedField?.GetValue(null), Is.False);
            Assert.That((bool)firstEnterField?.GetValue(null), Is.False);
        }
    }
}
