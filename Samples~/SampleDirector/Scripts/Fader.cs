using System.Collections;
using System.Collections.Generic;
using DarkNaku.Director;
using UnityEngine;
using UnityEngine.UI;

public class Fader : MonoBehaviour, ISceneTransition
{
    [SerializeField] private Image _imageFader;
    
    public IEnumerator CoTransitionIn(string prevSceneName)
    {
        yield return CoFade(Color.black, new Color(0f, 0f, 0f, 0f), 0.5f);
    }

    public IEnumerator CoTransitionOut(string nextSceneName)
    {
        yield return CoFade(new Color(0f, 0f, 0f, 0f), Color.black, 0.5f);
    }

    private IEnumerator CoFade(Color start, Color end, float duration)
    {
        _imageFader.color = start;
        
        var elapsed = 0f;
        
        while (elapsed <= duration)
        {
            _imageFader.color = Color.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}