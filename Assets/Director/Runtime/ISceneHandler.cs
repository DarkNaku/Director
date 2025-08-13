using System.Threading.Tasks;

namespace DarkNaku.Director {
    public interface ISceneHandler {
        public void OnBeforeTransitionIn() {
        }
        
        public Task OnEnter() {
            return Task.CompletedTask;
        }
        
        public void OnAfterTransitionIn() {
        }

        public void OnBeforeTransitionOut() {
        }
        
        public Task OnExit() {
            return Task.CompletedTask;
        }
        
        public void OnAfterTransitionOut() {
        }
    }
    
    public interface ISceneHandler<in T> : ISceneHandler {
        public Task OnEnter(T param) {
            return Task.CompletedTask;
        }
    }
}