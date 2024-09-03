using System.Collections;
using System.Collections.Generic;
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
    
    public void OnExit()
    {
        Debug.Log("[SceneA] OnExit");
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