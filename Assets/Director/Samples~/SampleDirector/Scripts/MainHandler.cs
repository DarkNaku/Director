using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DarkNaku.Director;
using UnityEngine;
using UnityEngine.UI;

public class MainHandler : MonoBehaviour, ISceneHandler, ILoadingProgress
{
    [SerializeField] private Slider _slider;

    public void OnEnterScene()
    {
        Debug.Log("[Main] OnEnterScene");
    }

    public Task ProcessOnEnterScene()
    {
        Debug.Log("[Main] ProcessOnEnterScene");
        return Task.CompletedTask;
    }

    public void OnExitScene()
    {
        Debug.Log("[Main] OnExitScene");
    }

    public Task ProcessOnExitScene()
    {
        Debug.Log("[Main] ProcessOnExitScene");
        return Task.CompletedTask;
    }
    
    public void OnProgress(float progress)
    {
        _slider.value = progress;
    }

    public void OnClickButtonWithLoading()
    {
        Director.Change("SceneA").WithLoading("Loading").SetMinLoadingTime(2f);
    }
    
    public void OnClickButtonWithoutLoading()
    {
        Director.Change("SceneA");
    }
    
    private void Awake()
    {
        Director.RegisterLoadingFromResource("Loading", "Loading");
    }
}