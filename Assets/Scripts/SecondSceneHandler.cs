using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DarkNaku.Director;
using UnityEngine;
using UnityEngine.UI;

public class SecondSceneHandler : MonoBehaviour, ISceneHandler, ILoadingProgress, ISceneTransition {
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _curtain;
    
    public void OnEnter() {
        Debug.Log("[SecondScene] OnEnter");
    }
    
    public void OnExit() {
        Debug.Log("[SecondScene] OnExit");
    }
    
    public void OnClickWithLoading() {
        Director.Change("FirstScene")
            .WithLoading("LoadingScene")
            .SetMinLoadingTime(2f);
    }
    
    public void OnClickWithoutLoading() {
        Director.Change("FirstScene")
            .SetMinLoadingTime(2f);
    }
    
    public void OnProgress(float progress) {
        _slider.value = _slider.maxValue * progress;
    }
    
    public async Task TransitionIn(string fromSceneName, string toSceneName) {
        await Fade(Color.black, new Color(0f, 0f, 0f, 0f), 0.5f);
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