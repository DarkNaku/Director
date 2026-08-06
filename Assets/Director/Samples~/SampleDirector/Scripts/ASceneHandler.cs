using System.Collections;
using System.Collections.Generic;
using DarkNaku.Director;
using UnityEngine;
using UnityEngine.UI;

public class ASceneHandler : MonoBehaviour, ISceneHandler, ILoadingProgress
{
    [SerializeField] private Slider _slider;

    public void OnEnterScene()
    {
        Debug.Log("[SceneA] OnEnterScene");
    }

    public Awaitable ProcessOnEnterScene()
    {
        Debug.Log("[SceneA] ProcessOnEnterScene");
        return AwaitableUtility.Completed;
    }

    public void OnExitScene()
    {
        Debug.Log("[SceneA] OnExitScene");
    }

    public Awaitable ProcessOnExitScene()
    {
        Debug.Log("[SceneA] ProcessOnExitScene");
        return AwaitableUtility.Completed;
    }
    
    public void OnProgress(float progress)
    {
        _slider.value = progress;
    }
    
    public void OnClickButtonWithLoading()
    {
        Director.Change("Main").WithLoading("Loading").SetMinLoadingTime(5f);
    }
    
    public void OnClickButtonWithoutLoading()
    {
        Director.Change("Main");
    }
}