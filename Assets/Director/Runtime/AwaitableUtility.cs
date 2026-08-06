using UnityEngine;

namespace DarkNaku.Director {
    /// <summary>
    /// Awaitable 관련 유틸리티. Task.CompletedTask에 대응하는 즉시 완료된 Awaitable을 제공합니다.
    /// </summary>
    public static class AwaitableUtility {
        /// <summary>
        /// 즉시 완료된 Awaitable을 반환합니다. <c>Task.CompletedTask</c>의 대체입니다.
        /// Awaitable은 한 번만 await할 수 있으므로 호출할 때마다 새 인스턴스를 생성합니다.
        /// </summary>
        public static Awaitable Completed {
            get {
                var source = new AwaitableCompletionSource();
                source.SetResult();
                return source.Awaitable;
            }
        }
    }
}
