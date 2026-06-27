# CLAUDE.md

이 파일은 Claude Code (claude.ai/code)가 이 저장소의 코드를 다룰 때 참고하는 안내 문서입니다.

## 프로젝트 개요

**Director**는 씬 전환 시 시각 효과, 로딩 화면, 라이프사이클 이벤트를 관리하는 Unity 패키지 라이브러리(`com.darknaku.director` v0.6.6)입니다. Unity Package Manager의 Git URL을 통해 배포됩니다.

## Unity 환경

- **Unity 버전**: 6000.3.9f1 (Unity 6)
- **최소 지원 Unity**: 2018.3
- **렌더 파이프라인**: Universal Render Pipeline (URP)
- **패키지 이름**: `com.darknaku.director`
- **네임스페이스**: `DarkNaku.Director`

## 빌드 및 실행

Unity 프로젝트이므로 CLI 빌드/테스트 명령어는 없습니다. Unity Editor에서 열어 빌드 및 실행합니다. CI/CD 파이프라인은 구성되어 있지 않습니다. 테스트 프레임워크(`com.unity.test-framework` v1.6.0)는 포함되어 있으나 작성된 테스트는 아직 없습니다.

## 아키텍처

### 핵심 라이브러리 (`Assets/Director/Runtime/`)

4개의 파일로 구성됩니다:

- **Director.cs** — 싱글톤 오케스트레이터. `async Task`와 `Task.Yield()`를 사용하여 프레임 동기화 기반으로 씬 전환 흐름을 관리합니다. 타입 정보를 캡처한 클로저(`_enterDispatcher`)를 통해 박싱·리플렉션 없이 씬 핸들러의 제네릭 `OnEnterScene<T>(param)`을 호출합니다. 플루언트 빌더 패턴 구현: `Director.Change("Scene").WithLoading("Loading").SetMinLoadingTime(2f).WithParam(123)`.
- **ISceneHandler.cs** — 씬 라이프사이클 콜백을 정의하는 인터페이스. 타입 파라미터를 받을 수 있는 제네릭 변형 `ISceneHandler<T>`도 포함합니다.
- **ISceneTransition.cs** — 시각적 전환 효과(페이드 인/아웃)를 위한 인터페이스.
- **ILoadingProgress.cs** — 로딩 진행 상황 업데이트를 받기 위한 인터페이스 (`OnProgress(float)`).

### 씬 전환 흐름

```
Director.Change(next) → EventSystem 비활성화
  → 현재 씬: OnBeforeTransitionOut → TransitionOut → OnAfterTransitionOut → OnExitScene → 언로드
  → 로딩 (선택): 로드 → OnEnterScene → TransitionIn → 다음 씬 비동기 로드 (진행률 전달)
  → 다음 씬: OnEnterScene() 또는 OnEnterScene<T>(param) → OnBeforeTransitionIn → TransitionIn → OnAfterTransitionIn
  → EventSystem 재활성화
```

### 데모/테스트 스크립트 (`Assets/Scripts/`)

- **FirstSceneHandler.cs** — `ISceneHandler`, `ILoadingProgress`, `ISceneTransition` 구현
- **SecondSceneHandler.cs** — `ISceneHandler<int>`, `ILoadingProgress`, `ISceneTransition` 구현 (타입 파라미터 전달 예시)
- **LoadingSceneHandler.cs** — 로딩 화면 구현

### 샘플 스크립트 (`Assets/Director/Samples~/SampleDirector/Scripts/`)

참조 구현: `ASceneHandler.cs`, `Fader.cs`, `Loading.cs`, `MainHandler.cs`.

## 주요 패턴 및 컨벤션

- **싱글톤**: 이중 잠금(double-checked locking)을 사용한 스레드 안전 구현 (`_lock` 객체, `DontDestroyOnLoad`)
- **비동기**: 모든 전환 메서드는 `Task`를 반환, `async void` 사용하지 않음
- **네이밍**: private 필드는 `_camelCase`, public 프로퍼티/메서드는 `PascalCase`, 인터페이스는 `I` 접두사
- **씬 핸들러 탐색**: Director가 로드된 씬의 루트 GameObject에서 `GetComponents`를 통해 `ISceneHandler` 구현체를 찾음
- **커밋 메시지**: 한국어로 작성
