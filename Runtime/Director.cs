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
        [SerializeField] private List<GameObject> _loadings;
        
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
        private Dictionary<string, ISceneLoading> _loadingTable;
        
        public static void RegisterLoadingFromResource(string name, string path)
        {
            Instance._RegisterLoadingFromResource(name, path);
        }
        
        public static void RegisterLoading(string name, ISceneLoading loading)
        {
            Instance._RegisterLoading(name, loading);
        }

        public static void Change(string nextSceneName)
        {
            Instance.StartCoroutine(Instance.CoChange(nextSceneName));
        }
        
        public static void Change(string nextSceneName, string loadingName)
        {
            if (Instance._loadingTable.ContainsKey(loadingName))
            {
                Instance.StartCoroutine(Instance.CoChange(nextSceneName, loadingName));
            }
            else
            {
                Instance.StartCoroutine(Instance.CoChange(nextSceneName));
            }
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
            
            Initialize();
        }

        private void OnDestroy()
        {
            _isQuitting = true;
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }
        
        private void Initialize()
        {
            _loadingTable = new Dictionary<string, ISceneLoading>();

            if (_loadings == null || _loadings.Count == 0) return;
            
            for (int i = 0; i < _loadings.Count; i++)
            {
                if (_loadings[i] == null) continue;
                
                var loading = _loadings[i].GetComponent<ISceneLoading>();
                
                if (loading != null)
                {
                    loading.Initialize();

                    _loadingTable.Add(_loadings[i].name, loading);
                }
            }
        }
        
        private void _RegisterLoadingFromResource(string name, string path)
        {
            if (_loadingTable.ContainsKey(name)) return;

            var resource = Resources.Load<GameObject>(path);
            var loadingInterface = resource?.GetComponent<ISceneLoading>();
                
            if (resource == null || loadingInterface == null)
            {
                Debug.LogWarningFormat("[Director] RegisterLoadingFromResource : Not Found Resource - {0}", name);
                return;
            }
            
            var go = Instantiate(resource);
                
            go.transform.SetParent(transform);
            
            var loading = go.GetComponent<ISceneLoading>();
            
            _RegisterLoading(name, loading);
        }
        
        private void _RegisterLoading(string name, ISceneLoading loading)
        {
            if (loading == null) return;

            if (_loadingTable.ContainsKey(name) || _loadingTable.ContainsValue(loading))
            {
                Debug.LogWarningFormat("[Director] RegisterLoading : Duplicated Loading - {0}", name);
                return;
            }
            
            loading.Initialize();
            loading.Hide();

            _loadingTable.Add(name, loading);
        }

        private IEnumerator CoChange(string nextSceneName)
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

            yield return CoProgressLoading(currentLoadingProgress, 0f, 1f, () => ao.progress / 0.9f);

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
            var nextSceneHandler = FindComponent<SceneHandler>(nextScene);
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

        private IEnumerator CoChange(string nextSceneName, string loadingName)
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
            
            loading.Show();
            
            if (loadingTransition != null)
            {
                yield return loadingTransition.CoTransitionIn(nextSceneName);
            }
            
            var ao = SceneManager.LoadSceneAsync(nextSceneName);

            ao.allowSceneActivation = false;

            yield return CoProgressLoading(loadingProgress, 0f, 0.5f, () => ao.progress / 0.9f);

            currentSceneHandler?.OnExit();

            ao.allowSceneActivation = true;
            
            yield return new WaitUntil(() => ao.isDone);
            
            var nextScene = SceneManager.GetSceneByName(nextSceneName);
            var nextEventSystem = GetEventSystemInScene(nextScene);
            var nextSceneHandler = FindComponent<SceneHandler>(nextScene);
            var nextSceneTransition = FindComponent<ISceneTransition>(nextScene);
            
            if (nextEventSystem != null)
            {
                nextEventSystem.enabled = false;
            }
            
            nextSceneHandler?.OnEnter();
            
            yield return CoProgressLoading(loadingProgress, 0.5f, 0.5f,
                () => Mathf.Min(1f, nextSceneHandler?.Progress ?? 1f));
            
            if (loadingTransition != null)
            {
                yield return loadingTransition.CoTransitionOut(currentSceneName);
            }
            
            loading.Hide();

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

        private IEnumerator CoProgressLoading(ILoadingProgress loadingProgress, float start, float progressRate, Func<float> getProgress)
        {
            var loadingStart = Time.time;
            var progress = 0f;

            while (progress < 1f)
            {
                progress = getProgress();
                
                if (_minLoadingTime > 0f)
                {
                    progress = Mathf.Min((Time.time - loadingStart) / (_minLoadingTime * progressRate), progress);
                }
                
                loadingProgress?.OnProgress(start + (progress * progressRate));
                
                yield return null;
            }
        }
        
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