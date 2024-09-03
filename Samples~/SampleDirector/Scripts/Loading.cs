using System.Collections;
using System.Collections.Generic;
using DarkNaku.Director;
using UnityEngine;
using UnityEngine.UI;

public class Loading : Fader, ISceneLoading, ILoadingProgress 
{
    [SerializeField] private Slider _slider;
    
    public void Initialize()
    {
        gameObject.SetActive(false);
        Debug.Log("[Loading] Initialized.");
    }

    public void Show()
    {
        _slider.value = 0f;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    public void OnProgress(float progress)
    {
        _slider.value = progress;
    }
}