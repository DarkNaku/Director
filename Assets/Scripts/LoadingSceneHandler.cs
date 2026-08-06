using DarkNaku.Director;
using UnityEngine;
using UnityEngine.UI;

public class LoadingSceneHandler : MonoBehaviour, ISceneHandler, ILoadingProgress, ISceneTransition {
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _curtain;
    
    public void OnEnterScene() {
        Debug.Log("[LoadingScene] OnEnterScene");
    }

    public Awaitable ProcessOnEnterScene() {
        Debug.Log("[LoadingScene] ProcessOnEnterScene");
        return AwaitableUtility.Completed;
    }

    public void OnExitScene() {
        Debug.Log("[LoadingScene] OnExitScene");
    }

    public Awaitable ProcessOnExitScene() {
        Debug.Log("[LoadingScene] ProcessOnExitScene");
        return AwaitableUtility.Completed;
    }
    public void OnProgress(float progress) {
        _slider.value = _slider.maxValue * progress;
    }
    
    public void PrepareTransitionIn(string fromSceneName, string toSceneName) {
        _curtain.color = Color.black;
    }
    
    public async Awaitable TransitionIn(string fromSceneName, string toSceneName) {
        await Fade(Color.black, new Color(0f, 0f, 0f, 0f), 0.5f);
    }
    
    public void PrepareTransitionOut(string fromSceneName, string toSceneName) {
        _curtain.color = Color.clear;
    }

    public async Awaitable TransitionOut(string fromSceneName, string toSceneName) {
        await Fade(new Color(0f, 0f, 0f, 0f), Color.black, 0.5f);
    }

    private async Awaitable Fade(Color start, Color end, float duration) {
        var elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            
            var t = elapsed / duration;

            _curtain.color = Color.Lerp(start, end, t);

            await Awaitable.NextFrameAsync();
        }

        _curtain.color = end;
    }
}
