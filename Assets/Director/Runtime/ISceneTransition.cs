using System.Threading.Tasks;

namespace DarkNaku.Director {
    /// <summary>
    /// 씬 전환 시 시각적 효과(페이드 인/아웃 등)를 제공하기 위한 인터페이스.
    /// </summary>
    public interface ISceneTransition {
        /// <summary>
        /// 트랜지션 인 애니메이션 시작 전 준비 단계에서 호출됩니다.
        /// </summary>
        /// <param name="fromSceneName">이전 씬 이름. 첫 씬 진입 시 null.</param>
        /// <param name="toSceneName">진입할 씬 이름.</param>
        void PrepareTransitionIn(string fromSceneName, string toSceneName) {
        }

        /// <summary>
        /// 트랜지션 아웃 애니메이션 시작 전 준비 단계에서 호출됩니다.
        /// </summary>
        /// <param name="fromSceneName">현재 씬 이름.</param>
        /// <param name="toSceneName">전환할 대상 씬 이름.</param>
        void PrepareTransitionOut(string fromSceneName, string toSceneName) {
        }

        /// <summary>
        /// 트랜지션 인 애니메이션을 실행합니다.
        /// </summary>
        /// <param name="fromSceneName">이전 씬 이름. 첫 씬 진입 시 null.</param>
        /// <param name="toSceneName">진입할 씬 이름.</param>
        /// <returns>애니메이션 완료를 나타내는 Task.</returns>
        Task TransitionIn(string fromSceneName, string toSceneName) {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 트랜지션 아웃 애니메이션을 실행합니다.
        /// </summary>
        /// <param name="fromSceneName">현재 씬 이름.</param>
        /// <param name="toSceneName">전환할 대상 씬 이름.</param>
        /// <returns>애니메이션 완료를 나타내는 Task.</returns>
        Task TransitionOut(string fromSceneName, string toSceneName) {
            return Task.CompletedTask;
        }
    }
}
