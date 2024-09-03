using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DarkNaku.Director
{
    public interface ISceneTransition
    {
        Task TransitionIn(string prevSceneName);
        Task TransitionOut(string nextSceneName);
    }
}