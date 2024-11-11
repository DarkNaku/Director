using System;
using System.Collections;
using System.Collections.Generic;
using DarkNaku.Director;
using UnityEngine;
using UnityEngine.UI;

public class MainHandler : MonoBehaviour, ISceneHandler, ILoadingProgress
{
    [SerializeField] private Slider _slider;
    
    public void OnEnter()
    {
        Debug.Log("[Main] OnEnter");
    }
    
    public void OnExit()
    {
        Debug.Log("[Main] OnExit");
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