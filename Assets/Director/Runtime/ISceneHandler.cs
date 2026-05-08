using System.Threading.Tasks;

namespace DarkNaku.Director {
    /// <summary>
    /// 씬 라이프사이클 콜백을 정의하는 인터페이스.
    /// 씬의 루트 GameObject에 부착된 컴포넌트에서 구현합니다.
    /// </summary>
    public interface ISceneHandler {
        /// <summary>
        /// 트랜지션 인 애니메이션 시작 직전에 호출됩니다.
        /// </summary>
        void OnBeforeTransitionIn() {
        }

        /// <summary>
        /// 씬 진입 시 호출됩니다.
        /// </summary>
        /// <returns>초기화 완료를 나타내는 Task.</returns>
        Task OnEnter() {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 트랜지션 인 애니메이션 완료 직후에 호출됩니다.
        /// </summary>
        void OnAfterTransitionIn() {
        }

        /// <summary>
        /// 트랜지션 아웃 애니메이션 시작 직전에 호출됩니다.
        /// </summary>
        void OnBeforeTransitionOut() {
        }

        /// <summary>
        /// 씬 퇴장 시 호출됩니다.
        /// </summary>
        /// <returns>정리 완료를 나타내는 Task.</returns>
        Task OnExit() {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 트랜지션 아웃 애니메이션 완료 직후에 호출됩니다.
        /// </summary>
        void OnAfterTransitionOut() {
        }
    }

    /// <summary>
    /// 타입 파라미터를 받을 수 있는 씬 핸들러 인터페이스.
    /// <see cref="Director.WithParam{T}"/>으로 전달된 파라미터를 수신합니다.
    /// </summary>
    /// <typeparam name="T">수신할 파라미터의 타입.</typeparam>
    public interface ISceneHandler<in T> : ISceneHandler {
        /// <summary>
        /// 타입 파라미터와 함께 씬 진입 시 호출됩니다.
        /// </summary>
        /// <param name="param">전달받은 파라미터.</param>
        /// <returns>초기화 완료를 나타내는 Task.</returns>
        Task OnEnter(T param) {
            return Task.CompletedTask;
        }
    }
}
