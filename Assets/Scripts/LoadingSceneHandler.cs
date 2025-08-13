using System.Threading.Tasks;
using DarkNaku.Director;
using UnityEngine;
using UnityEngine.UI;

public class LoadingSceneHandler : MonoBehaviour, ISceneHandler, ILoadingProgress, ISceneTransition {
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _curtain;
    
    public Task OnEnter() {
        Debug.Log("[LoadingScene] OnEnter");
        return Task.CompletedTask;
    }
    
    public Task OnExit() {
        Debug.Log("[LoadingScene] OnExit");
        return Task.CompletedTask;
    }
    public void OnProgress(float progress) {
        _slider.value = _slider.maxValue * progress;
    }
    
    public void PrepareTransitionIn(string fromSceneName, string toSceneName) {
        _curtain.color = Color.black;
    }
    
    public async Task TransitionIn(string fromSceneName, string toSceneName) {
        await Fade(Color.black, new Color(0f, 0f, 0f, 0f), 0.5f);
    }
    
    public void PrepareTransitionOut(string fromSceneName, string toSceneName) {
        _curtain.color = Color.clear;
    }

    public async Task TransitionOut(string fromSceneName, string toSceneName) {
        await Fade(new Color(0f, 0f, 0f, 0f), Color.black, 0.5f);
    }

    private async Task Fade(Color start, Color end, float duration) {
        var elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            
            var t = elapsed / duration;

            _curtain.color = Color.Lerp(start, end, t);

            await Task.Yield();
        }

        _curtain.color = end;
    }
}
