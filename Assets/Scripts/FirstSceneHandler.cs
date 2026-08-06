using System;
using DarkNaku.Director;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirstSceneHandler : MonoBehaviour, ISceneHandler, ILoadingProgress, ISceneTransition {
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _curtain;
    
    public void OnEnterScene() {
        Debug.Log("[FirstScene] OnEnterScene");
    }

    public Awaitable ProcessOnEnterScene() {
        Debug.Log("[FirstScene] ProcessOnEnterScene");
        return AwaitableUtility.Completed;
    }

    public void OnExitScene() {
        Debug.Log("[FirstScene] OnExitScene");
    }

    public Awaitable ProcessOnExitScene() {
        Debug.Log("[FirstScene] ProcessOnExitScene");
        return AwaitableUtility.Completed;
    }
    
    public void OnClickWithLoading() {
        // 여러 SDK 초기화(InitializeSdksAsync)를 로딩 진행률의 70% 구간으로 처리하고,
        // 나머지 30%를 씬 로드가 채웁니다.
        Director.Change("SecondScene")
            .WithLoading("LoadingScene")
            .WithLoadingTask(0.7f, InitializeSdksAsync)
            .SetMinLoadingTime(2f);
    }

    public void OnClickWithoutLoading() {
        Director.Change("SecondScene")
            .SetMinLoadingTime(2f)
            .WithParam(100);
    }

    // 광고 등 여러 SDK의 비동기 초기화 파사드 예시. 자기 구간(0~1)의 진행률을 IProgress로 통지하면
    // Director가 가중치(0.7)를 반영해 전체 로딩 진행률로 변환합니다.
    private async Awaitable InitializeSdksAsync(IProgress<float> progress) {
        Debug.Log("[FirstScene] SDK 초기화 시작");

        await FakeInitializeAsync("Ads");
        progress.Report(0.4f);

        await FakeInitializeAsync("Auth");
        progress.Report(0.7f);

        await FakeInitializeAsync("RemoteConfig");
        progress.Report(1f);

        Debug.Log("[FirstScene] SDK 초기화 완료");
    }

    // 실제 SDK 초기화를 흉내 내는 지연 (데모용).
    private async Awaitable FakeInitializeAsync(string sdkName) {
        Debug.Log($"[FirstScene] {sdkName} 초기화 중...");
        await Awaitable.WaitForSecondsAsync(0.6f);
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