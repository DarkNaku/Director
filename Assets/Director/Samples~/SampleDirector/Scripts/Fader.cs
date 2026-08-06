using System.Collections;
using System.Collections.Generic;
using DarkNaku.Director;
using UnityEngine;
using UnityEngine.UI;

public class Fader : MonoBehaviour, ISceneTransition
{
    [SerializeField] private Image _imageFader;
    
    public async Awaitable TransitionIn(string prevSceneName)
    {
        await Fade(Color.black, new Color(0f, 0f, 0f, 0f), 0.5f);
    }

    public async Awaitable TransitionOut(string nextSceneName)
    {
        await Fade(new Color(0f, 0f, 0f, 0f), Color.black, 0.5f);
    }

    private async Awaitable Fade(Color start, Color end, float duration)
    {
        _imageFader.color = start;
        
        var elapsed = 0f;
        
        while (elapsed <= duration)
        {
            _imageFader.color = Color.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            await Awaitable.NextFrameAsync();
        }
        
        _imageFader.color = end;
    }
}