using System.Threading.Tasks;

namespace DarkNaku.Director {
    public interface ISceneTransition {
        Task TransitionIn(string fromSceneName, string toSceneName);
        Task TransitionOut(string fromSceneName, string toSceneName);
    }
}