namespace DarkNaku.Director {
    public interface ISceneHandler {
        public void OnEnter() {
        }

        public void OnExit() {
        }
    }
    
    public interface ISceneHandler<in T> : ISceneHandler {
        public void OnEnter(T param) {
        }
    }
}