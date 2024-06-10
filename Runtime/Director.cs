using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace DarkNaku.Director
{
    public class Director : MonoBehaviour
    {
        public static Director Instance
        {
            get
            {
                if (_isQuitting) return null;

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        var instances = FindObjectsByType<Director>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                        if (instances.Length > 0)
                        {
                            _instance = instances[0];

                            for (int i = 1; i < instances.Length; i++)
                            {
                                Debug.LogWarningFormat("[Director] Instance Duplicated - {0}", instances[i].name);
                                Destroy(instances[i]);
                            }
                        }
                        else
                        {
                            _instance = new GameObject($"[Director]").AddComponent<Director>();
                        }
                    }

                    return _instance;
                }
            }
        }

        public static float MinLoadingTime
        {
            get => Instance._minLoadingTime;
            set => Instance._minLoadingTime = value;
        }

        private static readonly object _lock = new();
        private static Director _instance;
        private static bool _isQuitting;
        private bool _isLoading;
        private float _minLoadingTime;

        public static void Change(string nextSceneName)
        {
            Instance.StartCoroutine(Instance.CoChange<SceneHandler>(nextSceneName));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnSubSystemRegistration()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            var currentScene = SceneManager.GetActiveScene();
            var currentSceneHandler = Instance.FindComponent<SceneHandler>(currentScene);
            var currentSceneTransition = Instance.FindComponent<ISceneTransition>(currentScene);
            
            currentSceneHandler?.OnEnter();

            if (currentSceneTransition != null)
            {
                Instance.StartCoroutine(currentSceneTransition.CoTransitionIn(null));
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Debug.LogWarningFormat("[Director] Duplicated - {0}", name);
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            _isQuitting = true;
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private IEnumerator CoChange<T>(string nextSceneName) where T : SceneHandler
        {
            if (_isLoading) yield break;

            _isLoading = true;

            var currentScene = SceneManager.GetActiveScene();
            var currentEventSystem = GetEventSystemInScene(currentScene);
            var currentSceneHandler = FindComponent<SceneHandler>(currentScene);
            var currentSceneTransition = FindComponent<ISceneTransition>(currentScene);
            var currentLoadingProgress = FindComponent<ILoadingProgress>(currentScene);
            var currentSceneName = currentScene.name;

            var ao = SceneManager.LoadSceneAsync(nextSceneName);

            ao.allowSceneActivation = false;

            if (currentEventSystem != null)
            {
                currentEventSystem.enabled = false;
            }

            var loadingStart = Time.time;
            var progress = 0f;

            while (progress < 1f)
            {
                progress = ao.progress / 0.9f;
                
                if (_minLoadingTime > 0f)
                {
                    progress = Mathf.Min((Time.time - loadingStart) / _minLoadingTime, progress);
                }
                
                currentLoadingProgress?.OnProgress(progress);
                
                yield return null;
            }

            currentLoadingProgress?.OnProgress(1f);

            if (currentSceneTransition != null)
            {
                yield return currentSceneTransition.CoTransitionOut(nextSceneName);
            }

            currentSceneHandler?.OnExit();

            ao.allowSceneActivation = true;

            yield return new WaitUntil(() => ao.isDone);

            var nextScene = SceneManager.GetSceneByName(nextSceneName);
            var nextEventSystem = GetEventSystemInScene(nextScene);
            var nextSceneHandler = FindComponent<T>(nextScene);
            var nextSceneTransition = FindComponent<ISceneTransition>(nextScene);

            if (nextEventSystem != null)
            {
                nextEventSystem.enabled = false;
            }

            nextSceneHandler?.OnEnter();

            if (nextSceneTransition != null)
            {
                yield return nextSceneTransition.CoTransitionIn(currentSceneName);
            }

            if (nextEventSystem != null)
            {
                nextEventSystem.enabled = true;
            }

            _isLoading = false;
        }

/*
        private IEnumerator CoChange<T>(string nextSceneName, string loadingName, Action<T> onLoadScene) where T : SceneHandler
        {
            if (_isLoading) yield break;

            _isLoading = true;

            var currentScene = SceneManager.GetActiveScene();
            var currentEventSystem = GetEventSystemInScene(currentScene);
            var currentSceneHandler = FindComponent<SceneHandler>(currentScene);
            var currentSceneTransition = FindComponent<ISceneTransition>(currentScene);
            var currentSceneName = currentScene.name;
            
            if (currentEventSystem != null)
            {
                currentEventSystem.enabled = false;
            }
            
            if (currentSceneTransition != null)
            {
                yield return currentSceneTransition.CoTransitionOut(nextSceneName);
            }

            var loading = _loadingTable[loadingName];
            var loadingTransition = loading as ISceneTransition;
            var loadingProgress = loading as ILoadingProgress;
            
            if (loadingTransition != null)
            {
                yield return loadingTransition.CoTransitionIn(nextSceneName);
            }
            
            var ao = SceneManager.LoadSceneAsync(nextSceneName);

            ao.allowSceneActivation = false;

            var loadingStart = Time.time;
            var progress = 0f;

            while (progress < 1f)
            {
                progress = ao.progress / 0.9f;
                
                if (_minLoadingTime > 0f)
                {
                    progress = Mathf.Min((Time.time - loadingStart) / (_minLoadingTime * 0.5f), progress);
                }
                
                loadingProgress?.OnProgress(progress * 0.5f);
                
                yield return null;
            }

            currentSceneHandler?.OnExit();

            ao.allowSceneActivation = true;
            
            yield return new WaitUntil(() => ao.isDone);
            
            var nextScene = SceneManager.GetSceneByName(nextSceneName);
            var nextEventSystem = GetEventSystemInScene(nextScene);
            var nextSceneHandler = FindComponent<T>(nextScene);
            var nextSceneTransition = FindComponent<ISceneTransition>(nextScene);
            
            if (nextEventSystem != null)
            {
                nextEventSystem.enabled = false;
            }
            
            onLoadScene?.Invoke(nextSceneHandler);
            
            nextSceneHandler?.OnEnter();
            
            loadingStart = Time.time;
            progress = 0f;

            while (progress < 1f)
            {
                progress = Mathf.Min(1f, nextSceneHandler?.Progress ?? 1f);
                
                if (_minLoadingTime > 0f)
                {
                    progress = Mathf.Min((Time.time - loadingStart) / (_minLoadingTime * 0.5f), progress);
                }
                
                loadingProgress?.OnProgress((progress * 0.5f) + 0.5f);
                
                yield return null;
            }
            
            if (loadingTransition != null)
            {
                yield return loadingTransition.CoTransitionOut(currentSceneName);
            }

            if (nextSceneTransition != null)
            {
                yield return nextSceneTransition.CoTransitionIn(currentSceneName);
            }
            
            if (nextEventSystem != null)
            {
                nextEventSystem.enabled = true;
            }

            _isLoading = false;
        }
*/
        
        private EventSystem GetEventSystemInScene(Scene scene)
        {
            EventSystem[] ess = FindObjectsOfType<EventSystem>();

            for (int i = 0; i < ess.Length; i++)
            {
                if (ess[i].gameObject.scene == scene) return ess[i];
            }

            return null;
        }

        private T FindComponent<T>(Scene scene) where T : class
        {
            GameObject[] goes = scene.GetRootGameObjects();

            for (int i = 0; i < goes.Length; i++)
            {
                var handler = goes[i].GetComponent<T>();
                
                if (handler != null) return handler;
            }

            return null;
        }
    }
}