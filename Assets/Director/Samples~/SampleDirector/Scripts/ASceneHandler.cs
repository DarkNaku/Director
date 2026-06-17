using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    public Task ProcessOnEnterScene()
    {
        Debug.Log("[SceneA] ProcessOnEnterScene");
        return Task.CompletedTask;
    }

    public void OnExitScene()
    {
        Debug.Log("[SceneA] OnExitScene");
    }

    public Task ProcessOnExitScene()
    {
        Debug.Log("[SceneA] ProcessOnExitScene");
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