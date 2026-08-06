using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace DarkNaku.Director {
    /// <summary>
    /// 씬 전환 시 시각 효과, 로딩 화면, 라이프사이클 이벤트를 관리하는 싱글톤 오케스트레이터.
    /// <para>
    /// 사용 예시:
    /// <code>Director.Change("NextScene").WithLoading("Loading").SetMinLoadingTime(2f).WithParam(123);</code>
    /// </para>
    /// </summary>
    public class Director : MonoBehaviour {
        #region Inner Types

        /// <summary>
        /// 씬에서 탐색한 핸들러, 트랜지션, 프로그레스, EventSystem을 묶어 전달하는 구조체.
        /// </summary>
        private readonly struct SceneContext {
            public readonly string Name;
            public readonly ISceneHandler Handler;
            public readonly ISceneTransition Transition;
            public readonly ILoadingProgress Progress;
            public readonly EventSystem EventSystem;

            public SceneContext(Scene scene) {
                Name = scene.name;
                Handler = FindInScene<ISceneHandler>(scene);
                Transition = FindInScene<ISceneTransition>(scene);
                Progress = FindInScene<ILoadingProgress>(scene);
                EventSystem = FindEventSystem(scene);
            }
        }

        /// <summary>
        /// 씬 로드 전에 실행할 비동기 작업과, 전체 로딩 진행률에서 차지하는 가중치를 묶은 항목.
        /// </summary>
        private readonly struct LoadingTask {
            public readonly float Weight;
            public readonly Func<IProgress<float>, Awaitable> Run;

            public LoadingTask(float weight, Func<IProgress<float>, Awaitable> run) {
                Weight = weight;
                Run = run;
            }
        }

        /// <summary>
        /// 개별 세그먼트(작업 또는 씬 로드)의 로컬 진행률(0~1)을 전체 진행률로 변환하여
        /// <see cref="ILoadingProgress"/>에 통지하는 리포터. 통지 값은 단조 증가로 클램프됩니다.
        /// </summary>
        private sealed class SegmentReporter : IProgress<float> {
            private readonly ILoadingProgress _progress;
            private readonly float _baseGlobal;
            private readonly float _weight;
            private float _last;

            public float Last => _last;

            public SegmentReporter(ILoadingProgress progress, float baseGlobal, float weight, float last) {
                _progress = progress;
                _baseGlobal = baseGlobal;
                _weight = weight;
                _last = last;
            }

            public void Report(float local) {
                var global = _baseGlobal + _weight * Mathf.Clamp01(local);

                if (global <= _last) return;

                _last = global;
                _progress?.OnProgress(global);
            }
        }

        #endregion

        #region Singleton

        /// <summary>
        /// Director 싱글톤 인스턴스. 최초 접근 시 자동 생성되며, DontDestroyOnLoad로 유지됩니다.
        /// </summary>
        public static Director Instance {
            get {
                if (_isDestroyed) {
                    Debug.LogError("[Director] Already destroyed.");
                    return null;
                }

                if (_instance == null) {
                    lock (_lock) {
                        if (_instance == null) {
                            _instance = new GameObject("[Director]").AddComponent<Director>();
                        }
                    }
                }

                return _instance;
            }
        }

        private static readonly object _lock = new();
        private static Director _instance;
        private static bool _isDestroyed;
        private static bool _firstEnterDone;

        #endregion

        #region Fields

        private bool _initialized;
        private bool _changing;
        private string _loadingScene;
        private float _minLoadingTime;
        private Action<ISceneHandler> _enterDispatcher;
        private List<LoadingTask> _loadingTasks;

        #endregion

        #region Public API

        /// <summary>
        /// 지정한 씬으로 전환을 시작합니다. 플루언트 빌더 패턴으로 옵션을 체이닝할 수 있습니다.
        /// </summary>
        /// <param name="sceneName">전환할 대상 씬 이름.</param>
        /// <returns>옵션 체이닝을 위한 Director 인스턴스.</returns>
        public static Director Change(string sceneName) {
            _ = Instance.ChangeAsync(sceneName);
            return Instance;
        }

        /// <summary>
        /// 씬 전환 시 로딩 씬을 경유하도록 설정합니다.
        /// </summary>
        /// <param name="sceneName">로딩 화면으로 사용할 씬 이름.</param>
        /// <returns>옵션 체이닝을 위한 Director 인스턴스.</returns>
        public Director WithLoading(string sceneName) {
            _loadingScene = sceneName;
            return this;
        }

        /// <summary>
        /// 로딩 화면의 최소 표시 시간을 설정합니다.
        /// </summary>
        /// <param name="seconds">최소 로딩 시간(초).</param>
        /// <returns>옵션 체이닝을 위한 Director 인스턴스.</returns>
        public Director SetMinLoadingTime(float seconds) {
            _minLoadingTime = seconds;
            return this;
        }

        /// <summary>
        /// 씬 로드 전에 실행할 비동기 작업을 전체 로딩 진행률의 일부로 등록합니다.
        /// 여러 번 호출하면 등록한 순서대로 순차 실행되며, 각 작업이 차지하는 가중치만큼 진행률을 채웁니다.
        /// 씬 로드는 나머지 가중치(<c>1 - 작업 가중치 합</c>)를 차지합니다.
        /// <para>
        /// 작업은 전달받은 <see cref="IProgress{Single}"/>에 자기 구간의 로컬 진행률(0~1)을 통지하면 되고,
        /// Director가 가중치를 반영해 전체 진행률로 변환합니다. 통지하지 않아도 작업 완료 시 해당 구간이 채워집니다.
        /// </para>
        /// <example>
        /// <code>
        /// Director.Change("Game").WithLoading("Loading").WithLoadingTask(0.7f, InitializeSdksAsync);
        ///
        /// async Awaitable InitializeSdksAsync(IProgress&lt;float&gt; progress) {
        ///     await AdSdk.InitAsync();          progress.Report(0.5f);
        ///     await RemoteConfig.FetchAsync();  progress.Report(1f);
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="weight">전체 로딩 진행률에서 이 작업이 차지하는 비율(0~1).</param>
        /// <param name="task">진행률 리포터를 받아 실행할 비동기 작업.</param>
        /// <returns>옵션 체이닝을 위한 Director 인스턴스.</returns>
        public Director WithLoadingTask(float weight, Func<IProgress<float>, Awaitable> task) {
            if (task == null) return this;

            _loadingTasks ??= new List<LoadingTask>();
            _loadingTasks.Add(new LoadingTask(Mathf.Clamp01(weight), task));
            return this;
        }

        /// <summary>
        /// 다음 씬에 전달할 타입 파라미터를 설정합니다.
        /// 대상 씬의 핸들러가 <see cref="ISceneHandler{T}"/>를 구현해야 수신할 수 있습니다.
        /// 값은 컴파일 타임 타입 정보를 유지하는 클로저에 캡처되어 박싱·리플렉션 없이 전달됩니다.
        /// </summary>
        /// <typeparam name="T">파라미터 타입.</typeparam>
        /// <param name="value">전달할 파라미터 값.</param>
        /// <returns>옵션 체이닝을 위한 Director 인스턴스.</returns>
        public Director WithParam<T>(T value) {
            _enterDispatcher = handler => {
                if (handler is ISceneHandler<T> typed) {
                    typed.OnEnterScene(value);
                } else {
                    handler?.OnEnterScene();
                }
            };
            return this;
        }

        #endregion

        #region MonoBehaviour Lifecycle

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

            // 앱 종료 시점이므로 동기 OnExitScene만 호출하고 ProcessOnExitScene 비동기 대기는 생략합니다.
            FindInScene<ISceneHandler>(SceneManager.GetActiveScene())?.OnExitScene();
        }

        private void Initialize() {
            if (_initialized) return;

            _initialized = true;
        }

        #endregion

        #region Runtime Initialization

        /// <summary>
        /// 도메인 리로드 시 정적 필드를 초기화합니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnSubSystemRegistration() {
            _instance = null;
            _isDestroyed = false;
            _firstEnterDone = false;
        }

        /// <summary>
        /// 첫 씬 로드 완료 후 씬 핸들러의 진입 라이프사이클을 실행합니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad() {
            if (_firstEnterDone) return;

            _ = EnterFirstScene();
        }

        /// <summary>
        /// 최초 로드된 씬의 진입 라이프사이클(OnEnterScene → TransitionIn)을 실행합니다.
        /// </summary>
        private static async Awaitable EnterFirstScene() {
            var scene = new SceneContext(SceneManager.GetActiveScene());

            EnableEventSystem(scene.EventSystem, false);
            scene.Handler?.OnEnterScene();
            if (scene.Handler != null) await scene.Handler.ProcessOnEnterScene();
            await TransitionInAsync(scene.Handler, scene.Transition, null, scene.Name);
            EnableEventSystem(scene.EventSystem, true);

            _firstEnterDone = true;
        }

        #endregion

        #region Scene Change Orchestration

        /// <summary>
        /// 씬 전환 흐름을 시작합니다. 로딩 씬이 설정되어 있으면 로딩 씬을 먼저 진입한 뒤 대상 씬을 로드합니다.
        /// </summary>
        /// <param name="nextScene">전환할 대상 씬 이름.</param>
        private async Awaitable ChangeAsync(string nextScene) {
            if (_changing) return;

            _changing = true;
            _loadingScene = null;
            _minLoadingTime = 0f;
            _enterDispatcher = null;
            _loadingTasks = null;

            var currentContext = new SceneContext(SceneManager.GetActiveScene());

            EnableEventSystem(currentContext.EventSystem, false);
            await Awaitable.NextFrameAsync();

            if (_loadingScene != null) {
                currentContext = await ChangeToLoadingSceneAsync(nextScene, currentContext);
            }

            await ChangeToNextSceneAsync(nextScene, currentContext);

            _changing = false;
        }

        /// <summary>
        /// 현재 씬에서 퇴장한 뒤 로딩 씬에 진입합니다.
        /// </summary>
        /// <param name="nextScene">최종 전환 대상 씬 이름.</param>
        /// <param name="current">현재 씬 컨텍스트.</param>
        /// <returns>진입 완료된 로딩 씬의 컨텍스트.</returns>
        private async Awaitable<SceneContext> ChangeToLoadingSceneAsync(string nextScene, SceneContext current) {
            var op = SceneManager.LoadSceneAsync(_loadingScene);
            op.allowSceneActivation = false;

            await WaitForPreloadAsync(op);
            await TransitionOutAsync(current.Handler, current.Transition, current.Name, nextScene);
            if (current.Handler != null) await current.Handler.ProcessOnExitScene();
            current.Handler?.OnExitScene();
            await ActivateSceneAsync(op);

            var loadingContext = new SceneContext(SceneManager.GetSceneByName(_loadingScene));
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(_loadingScene));

            loadingContext.Handler?.OnEnterScene();
            if (loadingContext.Handler != null) await loadingContext.Handler.ProcessOnEnterScene();
            await TransitionInAsync(loadingContext.Handler, loadingContext.Transition, current.Name, nextScene);

            return loadingContext;
        }

        /// <summary>
        /// 대상 씬을 비동기 로드 후 활성화하고, 라이프사이클 이벤트를 실행합니다.
        /// 등록된 로딩 작업(Phase A) → 씬 로드(Phase B) → 이전 씬 퇴장 → 대상 씬 진입 순서로 진행됩니다.
        /// </summary>
        /// <param name="nextScene">전환할 대상 씬 이름.</param>
        /// <param name="prev">이전 씬(또는 로딩 씬) 컨텍스트.</param>
        private async Awaitable ChangeToNextSceneAsync(string nextScene, SceneContext prev) {
            var startTime = Time.realtimeSinceStartup;
            var (sceneWeight, scale) = ResolveLoadingWeights();

            // Phase A: 등록된 비동기 작업을 순차 실행하며 가중치만큼 진행률을 채웁니다.
            var completed = await RunLoadingTasksAsync(prev.Progress, scale);

            // Phase B: 대상 씬을 로드하며 나머지 가중치 구간을 채웁니다.
            var op = SceneManager.LoadSceneAsync(nextScene);
            op.allowSceneActivation = false;

            await ReportSceneProgressAsync(op, prev.Progress, completed, sceneWeight, startTime);
            await TransitionOutAsync(prev.Handler, prev.Transition, prev.Name, nextScene);
            if (prev.Handler != null) await prev.Handler.ProcessOnExitScene();
            prev.Handler?.OnExitScene();
            await ActivateSceneAsync(op);

            var next = new SceneContext(SceneManager.GetSceneByName(nextScene));
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(nextScene));

            EnableEventSystem(next.EventSystem, false);
            PrepareTransitionIn(next.Handler, next.Transition, prev.Name, nextScene);
            await EnterSceneAsync(next.Handler);
            await FinishTransitionInAsync(next.Handler, next.Transition, prev.Name, nextScene);
            EnableEventSystem(next.EventSystem, true);
        }

        #endregion

        #region Component Lookup Helpers

        /// <summary>
        /// 씬의 루트 GameObject에서 지정한 타입의 컴포넌트를 탐색합니다.
        /// </summary>
        /// <typeparam name="T">탐색할 컴포넌트 타입.</typeparam>
        /// <param name="scene">탐색 대상 씬.</param>
        /// <returns>찾은 컴포넌트 또는 null.</returns>
        private static T FindInScene<T>(Scene scene) where T : class {
            var roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++) {
                var component = roots[i].GetComponent<T>();

                if (component != null) return component;
            }

            return null;
        }

        /// <summary>
        /// 지정한 씬에 속한 EventSystem을 탐색합니다.
        /// </summary>
        /// <param name="scene">탐색 대상 씬.</param>
        /// <returns>찾은 EventSystem 또는 null.</returns>
        private static EventSystem FindEventSystem(Scene scene) {
            var all = FindObjectsOfType<EventSystem>();

            for (int i = 0; i < all.Length; i++) {
                if (all[i].gameObject.scene == scene) return all[i];
            }

            return null;
        }

        #endregion

        #region EventSystem Helpers

        /// <summary>
        /// EventSystem의 활성 상태를 null-safe하게 설정합니다.
        /// </summary>
        /// <param name="eventSystem">대상 EventSystem. null이면 무시됩니다.</param>
        /// <param name="enabled">활성화 여부.</param>
        private static void EnableEventSystem(EventSystem eventSystem, bool enabled) {
            if (eventSystem != null) eventSystem.enabled = enabled;
        }

        #endregion

        #region Transition Helpers

        /// <summary>
        /// 트랜지션 아웃 전체 라이프사이클을 실행합니다.
        /// OnBeforeTransitionOut → PrepareTransitionOut → TransitionOut → OnAfterTransitionOut.
        /// </summary>
        private static async Awaitable TransitionOutAsync(
                ISceneHandler handler, ISceneTransition transition, string from, string to) {
            if (transition == null) return;

            handler?.OnBeforeTransitionOut();
            transition.PrepareTransitionOut(from, to);
            await transition.TransitionOut(from, to);
            handler?.OnAfterTransitionOut();
        }

        /// <summary>
        /// 트랜지션 인 전체 라이프사이클을 실행합니다.
        /// OnBeforeTransitionIn → PrepareTransitionIn → TransitionIn → OnAfterTransitionIn.
        /// </summary>
        private static async Awaitable TransitionInAsync(
                ISceneHandler handler, ISceneTransition transition, string from, string to) {
            if (transition == null) return;

            handler?.OnBeforeTransitionIn();
            transition.PrepareTransitionIn(from, to);
            await transition.TransitionIn(from, to);
            handler?.OnAfterTransitionIn();
        }

        /// <summary>
        /// 트랜지션 인의 준비 단계만 실행합니다. OnEnterScene 호출 전에 사용됩니다.
        /// OnBeforeTransitionIn → PrepareTransitionIn.
        /// </summary>
        private static void PrepareTransitionIn(
                ISceneHandler handler, ISceneTransition transition, string from, string to) {
            if (transition == null) return;

            handler?.OnBeforeTransitionIn();
            transition.PrepareTransitionIn(from, to);
        }

        /// <summary>
        /// 트랜지션 인의 실행 단계만 완료합니다. OnEnterScene 호출 후에 사용됩니다.
        /// TransitionIn → OnAfterTransitionIn.
        /// </summary>
        private static async Awaitable FinishTransitionInAsync(
                ISceneHandler handler, ISceneTransition transition, string from, string to) {
            if (transition == null) return;

            await transition.TransitionIn(from, to);
            handler?.OnAfterTransitionIn();
        }

        #endregion

        #region Async Scene Loading Helpers

        /// <summary>
        /// 씬 비동기 로드가 활성화 대기 상태(progress 0.9)에 도달할 때까지 대기합니다.
        /// </summary>
        /// <param name="op">씬 로드 AsyncOperation.</param>
        private static async Awaitable WaitForPreloadAsync(AsyncOperation op) {
            while (op.progress < 0.9f) await Awaitable.NextFrameAsync();
        }

        /// <summary>
        /// 씬 활성화를 허용하고 완전히 로드될 때까지 대기합니다.
        /// </summary>
        /// <param name="op">씬 로드 AsyncOperation.</param>
        private static async Awaitable ActivateSceneAsync(AsyncOperation op) {
            op.allowSceneActivation = true;
            while (!op.isDone) await Awaitable.NextFrameAsync();
        }

        /// <summary>
        /// 씬 로드 진행률(나머지 가중치 구간)과 최소 로딩 시간을 결합하여 <see cref="ILoadingProgress"/>에 보고합니다.
        /// 통지 값은 <paramref name="baseGlobal"/> 이상으로 단조 증가하며, 로드 완료와 최소 시간 경과가
        /// 모두 충족되면 완료(1.0)를 보고합니다.
        /// </summary>
        /// <param name="op">씬 로드 AsyncOperation.</param>
        /// <param name="progress">진행률 수신자. null이면 대기만 수행합니다.</param>
        /// <param name="baseGlobal">Phase A(작업)까지 누적된 전체 진행률.</param>
        /// <param name="sceneWeight">씬 로드가 차지하는 가중치.</param>
        /// <param name="startTime">최소 로딩 시간 기준 시작 시각(작업 시작 시점).</param>
        private async Awaitable ReportSceneProgressAsync(
                AsyncOperation op, ILoadingProgress progress, float baseGlobal, float sceneWeight, float startTime) {
            var minEndTime = startTime + _minLoadingTime;
            var last = baseGlobal;

            while (op.progress < 0.9f || Time.realtimeSinceStartup < minEndTime) {
                var loadRatio = op.progress / 0.9f;
                var sceneGlobal = baseGlobal + sceneWeight * loadRatio;
                var timeRatio = _minLoadingTime > 0f
                    ? Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / _minLoadingTime)
                    : 1f;

                var value = Mathf.Min(sceneGlobal, timeRatio);

                if (value > last) {
                    last = value;
                    progress?.OnProgress(value);
                }

                await Awaitable.NextFrameAsync();
            }

            progress?.OnProgress(1f);
        }

        #endregion

        #region Loading Task Helpers

        /// <summary>
        /// 등록된 로딩 작업의 가중치 합을 바탕으로 씬 로드 가중치와 작업 가중치 배율을 계산합니다.
        /// 작업 가중치 합이 1을 넘으면 경고 후 정규화하여 전체 합이 1이 되도록 하고, 씬 로드는 0을 차지합니다.
        /// </summary>
        /// <returns>씬 로드 가중치와 작업 가중치에 곱할 배율.</returns>
        private (float sceneWeight, float scale) ResolveLoadingWeights() {
            if (_loadingTasks == null || _loadingTasks.Count == 0) return (1f, 1f);

            var taskTotal = 0f;

            for (int i = 0; i < _loadingTasks.Count; i++) taskTotal += _loadingTasks[i].Weight;

            if (taskTotal <= 1f) return (1f - taskTotal, 1f);

            Debug.LogWarningFormat(
                "[Director] 로딩 작업 가중치 합이 {0:0.###}로 1을 초과하여 정규화합니다. 씬 로드는 진행률을 차지하지 않습니다.",
                taskTotal);

            return (0f, 1f / taskTotal);
        }

        /// <summary>
        /// 등록된 로딩 작업(Phase A)을 순서대로 실행하며 진행률을 통지하고, 누적된 전체 진행률을 반환합니다.
        /// 각 작업에는 자기 구간(0~1)을 전체 진행률로 변환하는 <see cref="SegmentReporter"/>가 전달되며,
        /// 작업이 통지하지 않아도 완료 시 해당 구간을 가득 채웁니다.
        /// </summary>
        /// <param name="progress">진행률 수신자. null이면 통지 없이 실행만 합니다.</param>
        /// <param name="scale">작업 가중치에 곱할 배율(정규화용).</param>
        /// <returns>모든 작업 완료 후 누적된 전체 진행률.</returns>
        private async Awaitable<float> RunLoadingTasksAsync(ILoadingProgress progress, float scale) {
            if (_loadingTasks == null) return 0f;

            var completed = 0f;

            for (int i = 0; i < _loadingTasks.Count; i++) {
                var weight = _loadingTasks[i].Weight * scale;
                var reporter = new SegmentReporter(progress, completed, weight, completed);

                await _loadingTasks[i].Run(reporter);

                completed += weight;
                progress?.OnProgress(completed);
            }

            return completed;
        }

        #endregion

        #region Scene Enter Helpers

        /// <summary>
        /// 씬 핸들러의 진입 콜백을 동기 호출한 뒤 비동기 처리(<see cref="ISceneHandler.ProcessOnEnterScene"/>)
        /// 완료를 대기합니다. <see cref="_enterDispatcher"/>가 설정되어 있으면 클로저로 캡처된 타입
        /// 파라미터와 함께 <see cref="ISceneHandler{T}.OnEnterScene(T)"/>를 호출하고, 매칭되는 인터페이스가
        /// 없으면 파라미터 없는 <see cref="ISceneHandler.OnEnterScene"/>를 호출합니다.
        /// </summary>
        /// <param name="handler">대상 씬 핸들러.</param>
        private async Awaitable EnterSceneAsync(ISceneHandler handler) {
            if (_enterDispatcher == null) {
                handler?.OnEnterScene();
            } else {
                var dispatcher = _enterDispatcher;
                _enterDispatcher = null;
                dispatcher(handler);
            }

            if (handler != null) await handler.ProcessOnEnterScene();
        }

        #endregion
    }
}
