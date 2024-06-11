using System;
using System.Collections;
using System.Collections.Generic;
using DarkNaku.Director;
using UnityEngine;
using UnityEngine.UI;

public class MainHandler : SceneHandler, ILoadingProgress
{
    [SerializeField] private Slider _slider;
    
    public override void OnEnter()
    {
        Debug.Log("[Main] OnEnter");
    }
    
    public override void OnExit()
    {
        Debug.Log("[Main] OnExit");
    }
    
    public void OnProgress(float progress)
    {
        _slider.value = progress;
    }

    public void OnClickButtonWithLoading()
    {
        Director.MinLoadingTime = 2f;
        Director.Change("SceneA", "Loading");
    }
    
    public void OnClickButtonWithoutLoading()
    {
        Director.MinLoadingTime = 2f;
        Director.Change("SceneA");
    }
    
    private void Awake()
    {
        Director.RegisterLoadingFromResource("Loading", "Loading");
    }
}