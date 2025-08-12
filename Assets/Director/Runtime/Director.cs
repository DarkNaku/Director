using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace DarkNaku.Director {
    public class Director : MonoBehaviour {
        private sealed class Param {
            public Type ParamType => _paramType;
            public object Value => _value;
            
            private Type _paramType;
            private object _value;
            
            public Param(Type paramType, object value) {
                _paramType = paramType;
                _value = value;
            }
        }
        
        public static Director Instance {
            get {
                if (_isDestroyed) {
                    Debug.LogError("[Director] Already destroyed.");
                    return null;
                }
                
                if (_instance == null) {
                    lock (_lock) {
                        if (_instance == null) {
                            _instance = new GameObject($"[Director]").AddComponent<Director>();
                        }
                    }
                }
                
                return _instance;
            }
        }

        private static readonly object _lock = new();
        private static Director _instance;
        private static bool _isDestroyed;
        private static bool _isFirstEnterCalled;
        
        private bool _initialized;
        private bool _isSceneChanging;
        private string _loadingSceneName;
        private float _minLoadingTime;
        private Param _param;
        
        public static Director Change(string sceneName) {
            _ = Instance._Change(sceneName);
            return Instance;
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnSubSystemRegistration() {
            _instance = null;
            _isDestroyed = false;
            _isFirstEnterCalled = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad() {
            if (_isFirstEnterCalled) return;
            
            var currentScene = SceneManager.GetActiveScene();
            var currentSceneHandler = FindComponent<ISceneHandler>(currentScene);
            
            currentSceneHandler?.OnEnter();

            _ = TransitionIn(currentScene); 
            
            _isFirstEnterCalled = true;
            
            async Task TransitionIn(Scene scene) {
                var sceneTransition = FindComponent<ISceneTransition>(scene);

                if (sceneTransition == null) return;
                
                var eventSystem = GetEventSystemInScene(scene);
                
                if (eventSystem != null) {
                    eventSystem.enabled = false;
                }
                
                await sceneTransition.TransitionIn(null, currentScene.name);
                    
                if (eventSystem != null) {
                    eventSystem.enabled = true;
                }
            }
        }
        
        private static T FindComponent<T>(Scene scene) where T : class {
            GameObject[] goes = scene.GetRootGameObjects();

            for (int i = 0; i < goes.Length; i++) {
                var handler = goes[i].GetComponent<T>();
                
                if (handler != null) return handler;
            }

            return null;
        }
        
        private static EventSystem GetEventSystemInScene(Scene scene) {
            EventSystem[] ess = FindObjectsOfType<EventSystem>();

            for (int i = 0; i < ess.Length; i++) {
                if (ess[i].gameObject.scene == scene) return ess[i];
            }

            return null;
        }
        
        public Director WithLoading(string loadingSceneName) {
            _loadingSceneName = loadingSceneName;
            return this;
        }
        
        public Director SetMinLoadingTime(float minLoadingTime) {
            _minLoadingTime = minLoadingTime;
            return this;
        }

        public Director WithParam<T>(T param) {
            _param = new Param(typeof(T), param);
            return this;
        }
        
        private void Awake() {
            if (_instance != null && _instance != this) {
                Debug.LogWarningFormat("[Director] Duplicated - {0}", name);
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }

        private void OnDestroy() {
            if (_instance != this) return;
            
            _instance = null;
            _isDestroyed = true;
        }

        private void OnApplicationQuit() {
            if (_instance != this) return;
            
            var currentScene = SceneManager.GetActiveScene();
            var currentSceneHandler = FindComponent<ISceneHandler>(currentScene);
            
            currentSceneHandler?.OnExit();
        }
        
        private void Initialize() {
            if (_initialized) return;
            
            _initialized = true;
        }

        private async Task _Change(string nextSceneName) {
            if (_isSceneChanging) return;

            _isSceneChanging = true;
            _loadingSceneName = null;
            _minLoadingTime = 0f;
            _param = null;

            var currentScene = SceneManager.GetActiveScene();
            var currentEventSystem = GetEventSystemInScene(currentScene);
            var currentHandler = FindComponent<ISceneHandler>(currentScene);
            var currentTransition = FindComponent<ISceneTransition>(currentScene);
            var currentProgress = FindComponent<ILoadingProgress>(currentScene);
            var currentSceneName = currentScene.name;

            if (currentEventSystem != null) {
                currentEventSystem.enabled = false;
            }
            
            await Task.Yield();

            if (_loadingSceneName == null) {
                await LoadAndActivateSceneAsync(currentSceneName, nextSceneName, currentProgress, currentTransition, currentHandler);
            } else {
                var ao = SceneManager.LoadSceneAsync(_loadingSceneName);

                ao.allowSceneActivation = false;

                while (ao.progress < 0.9f) await Task.Yield();

                if (currentTransition != null) {
                    await currentTransition.TransitionOut(currentSceneName, nextSceneName);
                }
                
                currentHandler?.OnExit();
                
                ao.allowSceneActivation = true;

                while (!ao.isDone) await Task.Yield();
                
                var loadingScene = SceneManager.GetSceneByName(_loadingSceneName);
                var loadingHandler = FindComponent<ISceneHandler>(loadingScene);
                var loadingTransition = FindComponent<ISceneTransition>(loadingScene);
                var loadingProgress = FindComponent<ILoadingProgress>(loadingScene);
                
                loadingHandler?.OnEnter();

                if (loadingTransition != null) {
                    await loadingTransition.TransitionIn(currentSceneName, nextSceneName);
                }
                
                await LoadAndActivateSceneAsync(currentSceneName, nextSceneName, loadingProgress, loadingTransition, loadingHandler);
            }

            _isSceneChanging = false;
        }

        private async Task LoadAndActivateSceneAsync(string prevSceneName, string nextSceneName, 
            ILoadingProgress prevProgress, ISceneTransition prevTransition, ISceneHandler prevHandler) {
            var ao = SceneManager.LoadSceneAsync(nextSceneName);

            ao.allowSceneActivation = false;

            var startTime = Time.realtimeSinceStartup;
            var minEndTime = startTime + _minLoadingTime;

            while (ao.progress < 0.9f || Time.realtimeSinceStartup < minEndTime) {
                var loadProgress = ao.progress / 0.9f;
                var timeProgress = _minLoadingTime > 0f ? Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / _minLoadingTime) : 1f;
                var progress = Mathf.Min(loadProgress, timeProgress);
                prevProgress?.OnProgress(progress);
                await Task.Yield();
            }
                
            prevProgress?.OnProgress(1f);

            if (prevTransition != null) {
                await prevTransition.TransitionOut(prevSceneName, nextSceneName);
            }

            prevHandler?.OnExit();
                
            ao.allowSceneActivation = true;

            while (!ao.isDone) await Task.Yield();
                
            var nextScene = SceneManager.GetSceneByName(nextSceneName);
            var nextEventSystem = GetEventSystemInScene(nextScene);
            var nextHandler = FindComponent<ISceneHandler>(nextScene);
            var nextTransition = FindComponent<ISceneTransition>(nextScene);
                
            if (nextEventSystem != null) {
                nextEventSystem.enabled = false;
            }

            if (_param == null) {
                nextHandler?.OnEnter();
            } else {
                var @interface = nextHandler.GetType()
                    .GetInterfaces()
                    .FirstOrDefault(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(ISceneHandler<>) &&
                        i.GetGenericArguments()[0].IsAssignableFrom(_param.ParamType));

                if (@interface == null) {
                    nextHandler?.OnEnter();
                } else {
                    var method = @interface.GetMethod("OnEnter");
                    var value = _param.Value;

                    if (value == null && _param.ParamType.IsValueType) {
                        value = Activator.CreateInstance(_param.ParamType);
                    }

                    try {
                        method?.Invoke(nextHandler, new[] { value });
                    } catch (Exception e) {
                        Debug.LogError($"[Director] LoadAndActiveSceneAsync : Param fail to sending. - {e}");
                    }
                }
                
                _param = null; // 더 이상 재시도하지 않음
            }
                
            if (nextTransition != null) await nextTransition.TransitionIn(prevSceneName, nextSceneName);

            if (nextEventSystem != null) {
                nextEventSystem.enabled = true;
            }
        }
    }
}