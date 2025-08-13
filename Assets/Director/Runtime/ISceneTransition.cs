using System.Threading.Tasks;

namespace DarkNaku.Director {
    public interface ISceneTransition {
        void PrepareTransitionIn(string fromSceneName, string toSceneName) {
        }

        void PrepareTransitionOut(string fromSceneName, string toSceneName) {
        }

        Task TransitionIn(string fromSceneName, string toSceneName) {
            return Task.CompletedTask;
        }

        Task TransitionOut(string fromSceneName, string toSceneName) {
            return Task.CompletedTask;
        }
    }
}