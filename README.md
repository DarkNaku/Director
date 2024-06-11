# Director

### 소개
Scene 사이의 전환 할 때 연출과 로딩을 표시하거나 핸들러를 통해 이벤트를 받는 등의 작업을 편리하게 하기 위한 라이브러리 입니다.

![image](Samples~/Images/screenshot.gif)

### 설치방법
1. 패키지 관리자의 툴바에서 좌측 상단에 플러스 메뉴를 클릭합니다.
2. 추가 메뉴에서 Add package from git URL을 선택하면 텍스트 상자와 Add 버튼이 나타납니다.
3. https://github.com/DarkNaku/Director.git 입력하고 Add를 클릭합니다.

### 사용방법
```csharp
Director.Change("SceneA");

Director.Change("SceneB", "Loading");
```

### 기능
* 로딩 화면 표시 가능
* 로딩 진행 상황 표시 가능
* 장면 사이 전환 효과 표현 가능

### 추가설명
* 전환간 Additive Scene을 더하고 빼는 기능은 추가 예정입니다.