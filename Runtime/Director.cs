using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.Animations;
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

        private static readonly object _lock = new();
        private static Director _instance;
        private static bool _isQuitting;
        private bool _isLoading;
        private float _minLoadingTime;
        private ISceneLoading _sceneLoading;
        private Dictionary<string, ISceneLoading> _loadingTable;
        
        public static void RegisterLoadingBuiltIn(string name, string path)
        {
            Instance._RegisterLoadingBuiltIn(name, path);
        }
        
        public static void RegisterLoading(string name, ISceneLoading loading)
        {
            Instance._RegisterLoading(name, loading);
        }

        public static Director Change(string sceneName)
        {
            _ = Instance._Change(sceneName);
            
            return Instance;
        }
        
        public Director WithLoading(string loadingName)
        {
            if (_loadingTable.TryGetValue(loadingName, out _sceneLoading) == false)
            {
                _sceneLoading = _RegisterLoadingBuiltIn(loadingName);

                if (_sceneLoading == null)
                {
                    Debug.LogErrorFormat("[Director] Can't found loading - {0}", loadingName);
                }
            }
            
            return Instance;
        }
        
        public Director SetMinLoadingTime(float minLoadingTime)
        {
            _minLoadingTime = minLoadingTime;
            
            return Instance;
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
            var currentSceneHandler = Instance.FindComponent<ISceneHandler>(currentScene);
            
            currentSceneHandler?.OnEnter();

            _ = TransitionIn(currentScene);

            return;

            async Task TransitionIn(Scene scene)
            {
                var sceneTransition = Instance.FindComponent<ISceneTransition>(scene);

                if (sceneTransition == null) return;
                
                var eventSystem = Instance.GetEventSystemInScene(scene);
                
                if (eventSystem != null)
                {
                    eventSystem.enabled = false;
                }
                
                await sceneTransition.TransitionIn(null);
                    
                if (eventSystem != null)
                {
                    eventSystem.enabled = true;
                }
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
        
        private ISceneLoading _RegisterLoadingBuiltIn(string name, string path = null)
        {
            if (string.IsNullOrEmpty(path)) path = name;
            
            if (_loadingTable.ContainsKey(name)) return null;

            var resource = Resources.Load<GameObject>(path);
            var loadingInterface = resource?.GetComponent<ISceneLoading>();
                
            if (resource == null || loadingInterface == null)
            {
                Debug.LogWarningFormat("[Director] RegisterLoadingBuiltIn : Can't found loading from resource - {0}", name);
                return null;
            }
            
            var go = Instantiate(resource);
                
            go.transform.SetParent(transform);

            if (go.TryGetComponent<ISceneLoading>(out var sceneLoading))
            {
                _RegisterLoading(name, sceneLoading);

                return sceneLoading;
            }

            return null;
        }
        
        private void _RegisterLoading(string name, ISceneLoading sceneLoading)
        {
            if (sceneLoading == null) return;

            if (_loadingTable.ContainsKey(name) || _loadingTable.ContainsValue(sceneLoading))
            {
                Debug.LogWarningFormat("[Director] RegisterLoading : Duplicated - {0}", name);
                return;
            }
            
            sceneLoading.Initialize();
            sceneLoading.Hide();

            _loadingTable.Add(name, sceneLoading);
        }

        private async Task _Change(string sceneName)
        {
            if (_isLoading) return;

            _isLoading = true;
            _sceneLoading = null;
            _minLoadingTime = 0f;

            var currentScene = SceneManager.GetActiveScene();
            var currentEventSystem = GetEventSystemInScene(currentScene);
            var currentHandler = FindComponent<ISceneHandler>(currentScene);
            var currentTransition = FindComponent<ISceneTransition>(currentScene);
            var sceneProgress = FindComponent<ILoadingProgress>(currentScene);
            var currentSceneName = currentScene.name;

            if (currentEventSystem != null)
            {
                currentEventSystem.enabled = false;
            }
            
            await Task.Yield();
            
            var ao = SceneManager.LoadSceneAsync(sceneName);

            ao.allowSceneActivation = false;
            
            if (_sceneLoading == null)
            {
                if (sceneProgress != null)
                {
                    await Progress(sceneProgress, 0f, 1f, () => ao.progress / 0.9f);
                }
            }

            if (currentTransition != null)
            {
                await currentTransition.TransitionOut(sceneName);
            }
            
            if (_sceneLoading != null)
            {
                _sceneLoading?.Show();

                if (_sceneLoading is ISceneTransition loadingTransition)
                {
                    await loadingTransition.TransitionIn(sceneName);
                }
            }
            
            if (_sceneLoading != null)
            {
                if (_sceneLoading is ILoadingProgress loadingProgress)
                {
                    await Progress(loadingProgress, 0f, 0.5f, () => ao.progress / 0.9f);
                }
            }

            currentHandler?.OnExit();

            ao.allowSceneActivation = true;

            while (!ao.isDone) await Task.Yield();
            
            var nextScene = SceneManager.GetSceneByName(sceneName);
            var nextEventSystem = GetEventSystemInScene(nextScene);
            var nextHandler = FindComponent<ISceneHandler>(nextScene);
            var nextTransition = FindComponent<ISceneTransition>(nextScene);

            if (nextEventSystem != null)
            {
                nextEventSystem.enabled = false;
            }
            
            nextHandler?.OnEnter();
            
            if (_sceneLoading != null)
            {
                if (_sceneLoading is ILoadingProgress loadingProgress)
                {
                    await Progress(loadingProgress, 0.5f, 0.5f, () => nextHandler?.Progress ?? 1f);
                }

                if (_sceneLoading is ISceneTransition loadingTransition)
                {
                    await loadingTransition.TransitionOut(sceneName);
                }
            
                _sceneLoading.Hide();
            }

            if (nextTransition != null)
            {
                await nextTransition.TransitionIn(currentSceneName);
            }
            
            if (nextEventSystem != null)
            {
                nextEventSystem.enabled = true;
            }

            _isLoading = false;
        }

        private async Task Progress(ILoadingProgress loadingProgress, float start, float length, Func<float> getProgress)
        {
            var loadingStart = Time.realtimeSinceStartup;
            var progress = 0f;
            
            start = Mathf.Clamp01(start);
            length = Mathf.Min(1f - start, length);
            
            var minLoadingTime = _minLoadingTime * length;
            
            while (progress < 1f)
            {
                progress = getProgress();
                
                if (minLoadingTime > 0f)
                {
                    var elapsed = Time.realtimeSinceStartup - loadingStart;
                    
                    progress = Mathf.Min(elapsed / minLoadingTime, progress);
                }
                
                loadingProgress?.OnProgress(start + (progress * length));
                
                await Task.Yield();
            }
        }
        
        public EventSystem GetEventSystemInScene(Scene scene)
        {
            EventSystem[] ess = FindObjectsOfType<EventSystem>();

            for (int i = 0; i < ess.Length; i++)
            {
                if (ess[i].gameObject.scene == scene) return ess[i];
            }

            return null;
        }

        public T FindComponent<T>(Scene scene) where T : class
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