using System.Collections;
using System.Collections.Generic;
using DarkNaku.Director;
using UnityEngine;
using UnityEngine.UI;

public class ASceneHandler : SceneHandler, ILoadingProgress
{
    [SerializeField] private Slider _slider;
    
    public override void OnEnter()
    {
        Debug.Log("[SceneA] OnEnter");
    }
    
    public override void OnExit()
    {
        Debug.Log("[SceneA] OnExit");
    }
    
    public void OnProgress(float progress)
    {
        _slider.value = progress;
    }
    
    public void OnClickButtonWithLoading()
    {
        Director.MinLoadingTime = 1f;
        Director.Change("Main", "Loading");
    }
    
    public void OnClickButtonWithoutLoading()
    {
        Director.MinLoadingTime = 1f;
        Director.Change("Main");
    }
}