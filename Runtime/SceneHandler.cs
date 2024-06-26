using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DarkNaku.Director
{
    public abstract class SceneHandler : MonoBehaviour
    {
        public float Progress { get; protected set; } = 1f;
        
        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }
    }
}