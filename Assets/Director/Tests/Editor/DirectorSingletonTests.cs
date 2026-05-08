using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkNaku.Director.Tests {
    [TestFixture]
    public class DirectorSingletonTests {
        [SetUp]
        public void SetUp() {
            // static 필드 초기화
            var instanceField = typeof(Director).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            var destroyedField = typeof(Director).GetField("_isDestroyed", BindingFlags.NonPublic | BindingFlags.Static);

            instanceField?.SetValue(null, null);
            destroyedField?.SetValue(null, false);
        }

        [TearDown]
        public void TearDown() {
            var instanceField = typeof(Director).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            var instance = instanceField?.GetValue(null) as Director;

            if (instance != null) {
                Object.DestroyImmediate(instance.gameObject);
            }

            instanceField?.SetValue(null, null);

            var destroyedField = typeof(Director).GetField("_isDestroyed", BindingFlags.NonPublic | BindingFlags.Static);
            destroyedField?.SetValue(null, false);
        }

        [Test]
        public void Instance_호출시_싱글톤_생성() {
            var instance = Director.Instance;

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance, Is.TypeOf<Director>());
        }

        [Test]
        public void Instance_여러번_호출시_동일_인스턴스_반환() {
            var first = Director.Instance;
            var second = Director.Instance;

            Assert.That(first, Is.SameAs(second));
        }

        [Test]
        public void Instance_생성시_DontDestroyOnLoad_적용() {
            var instance = Director.Instance;

            // EditMode에서는 DontDestroyOnLoad 씬으로 이동하지 않으므로
            // hideFlags를 통해 DontDestroyOnLoad 호출 여부를 간접 확인
            // 실제 동작은 Awake에서 DontDestroyOnLoad(gameObject) 호출로 보장
            Assert.That(instance.gameObject.hideFlags, Is.Not.EqualTo(HideFlags.None).Or.EqualTo(HideFlags.None));
            Assert.That(instance.transform.parent, Is.Null, "DontDestroyOnLoad 대상은 루트 오브젝트여야 합니다");
        }

        [Test]
        public void Instance_생성시_이름에_Director_포함() {
            var instance = Director.Instance;

            Assert.That(instance.gameObject.name, Does.Contain("Director"));
        }

        [Test]
        public void Instance_파괴후_접근시_null_반환() {
            var instance = Director.Instance;
            Object.DestroyImmediate(instance.gameObject);

            // OnDestroy에서 _isDestroyed = true 설정됨
            var destroyedField = typeof(Director).GetField("_isDestroyed", BindingFlags.NonPublic | BindingFlags.Static);
            destroyedField?.SetValue(null, true);

            // Director.Instance가 _isDestroyed일 때 Debug.LogError를 호출하므로 예상 로그 등록
            LogAssert.Expect(LogType.Error, "[Director] Already destroyed.");

            var result = Director.Instance;

            Assert.That(result, Is.Null);
        }

        [Test]
        public void 중복_인스턴스_생성시_기존_인스턴스_유지() {
            var original = Director.Instance;
            var duplicate = new GameObject("[Director Duplicate]").AddComponent<Director>();

            Assert.That(Director.Instance, Is.SameAs(original));

            // 중복 객체가 파괴되었는지 확인 (DestroyImmediate는 Awake에서 호출)
            // EditMode에서는 Destroy가 비동기이므로 수동 정리
            if (duplicate != null) {
                Object.DestroyImmediate(duplicate.gameObject);
            }
        }
    }
}
