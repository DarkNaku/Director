namespace DarkNaku.Director
{
    public interface ISceneHandler
    {
        public float Progress { get; }

        public void OnEnter()
        {
        }

        public void OnExit()
        {
        }
    }
}