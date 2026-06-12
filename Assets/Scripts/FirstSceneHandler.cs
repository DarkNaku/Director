using System.Threading.Tasks;
using DarkNaku.Director;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirstSceneHandler : MonoBehaviour, ISceneHandler, ILoadingProgress, ISceneTransition {
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _curtain;
    
    public void OnEnter() {
        Debug.Log("[FirstScene] OnEnter");
    }

    public Task ProcessOnEnter() {
        Debug.Log("[FirstScene] ProcessOnEnter");
        return Task.CompletedTask;
    }

    public void OnExit() {
        Debug.Log("[FirstScene] OnExit");
    }

    public Task ProcessOnExit() {
        Debug.Log("[FirstScene] ProcessOnExit");
        return Task.CompletedTask;
    }
    
    public void OnClickWithLoading() {
        Director.Change("SecondScene")
            .WithLoading("LoadingScene")
            .SetMinLoadingTime(2f);
    }
    
    public void OnClickWithoutLoading() {
        Director.Change("SecondScene")
            .SetMinLoadingTime(2f)
            .WithParam(100);
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