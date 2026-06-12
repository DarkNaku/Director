using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DarkNaku.Director;
using UnityEngine;
using UnityEngine.UI;

public class ASceneHandler : MonoBehaviour, ISceneHandler, ILoadingProgress
{
    [SerializeField] private Slider _slider;

    public void OnEnter()
    {
        Debug.Log("[SceneA] OnEnter");
    }

    public Task ProcessOnEnter()
    {
        Debug.Log("[SceneA] ProcessOnEnter");
        return Task.CompletedTask;
    }

    public void OnExit()
    {
        Debug.Log("[SceneA] OnExit");
    }

    public Task ProcessOnExit()
    {
        Debug.Log("[SceneA] ProcessOnExit");
        return Task.CompletedTask;
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