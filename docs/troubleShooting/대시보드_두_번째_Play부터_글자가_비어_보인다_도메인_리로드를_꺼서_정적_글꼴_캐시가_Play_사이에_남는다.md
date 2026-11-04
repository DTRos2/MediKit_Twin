# 대시보드: Play를 두 번째 하면 글자가 비어 보인다 (Domain Reload를 꺼서 정적 글꼴 캐시가 Play 사이에 남는다)

> 작성일: 2026-11-04 · 브랜치: `feature/pw-dashboard-ugui-panels` · Unity 6000.6.0f1
> 범위: 에디터에서 Play → 종료 → 다시 Play 하면 대시보드의 글자가 그려지지 않는 증상. `DashboardFonts`(글꼴을 한 번 골라 정적 변수에 캐시하는 클래스)
> 표시: **확정**(측정이나 코드로 확인) · **추정**(코드나 정황으로 본 것) · **미검증**(아직 확인하지 않음)
> **상태:** 원인은 **추정**이고, 추정에 맞춰 고쳤다. EditMode 시험 3개가 통과했다(헤드리스). **에디터에서 Play → 종료 → Play로 글자가 나오는지는 확인하지 못했다.**

## 0. 한 줄 요약

이 프로젝트는 에디터 설정 `Enter Play Mode Options`(`m_EnterPlayModeOptions: 3`)로 Play 진입 때 도메인 리로드와 씬 리로드를 끈다. 그러면 `static` 변수가 Play가 끝나도 지워지지 않는다. `DashboardFonts`는 고른 글꼴을 정적 변수에 캐시하는데, 이전 Play에서 만든 글꼴 객체가 파괴된 채 캐시에 남으면 두 번째 Play부터 모든 글자가 비어 그려질 수 있다. 캐시를 Play를 시작할 때마다 비우고, 파괴된 글꼴은 다시 고르게 했다.

## 1. 증상

- 에디터에서 Play를 두 번째 했을 때부터 대시보드 글자가 비어 보인다 (사용자가 에디터에서 본 증상, 작업 인계 기록). 첫 Play에서는 정상이다.
- 헤드리스 시험은 매번 새 프로세스라 이 증상을 못 만난다.

## 2. 원인 (추정)

- `ProjectSettings/EditorSettings.asset`에 `m_EnterPlayModeOptionsEnabled: 1`, `m_EnterPlayModeOptions: 3`(도메인 리로드 끔 + 씬 리로드 끔)이다 (확정: 파일 확인). Play 시작이 빨라지는 대신 정적 변수와 정적 이벤트가 Play 사이에 남는다.
- `DashboardFonts.Sans`, `Mono`는 처음 쓸 때 `Pick()`으로 글꼴을 고르고 `_isSansResolved` 같은 정적 플래그로 "고름"을 기억한다 (확정: 코드). Play 중에 만든 글꼴은 Play가 끝날 때 `Destroy`로 정리될 수 있다. 그러면 정적 변수에는 파괴된 `Font` 참조가 남고 플래그는 "고름"이라, 다음 Play에서 다시 고르지 않고 파괴된 글꼴을 내준다 (추정).
- 같은 구조의 다른 클래스(`CellEvents`, `SimClock`)는 이미 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`로 Play 시작 때 상태를 비운다 (확정: 코드). `DashboardFonts`만 없었다.
- 글자가 비는 직접 원인이 파괴된 글꼴 참조인지는 **에디터에서 재현해 확인하지 못했다** (미검증).

## 3. 고친 것

`Scripts/Dashboard/DashboardFonts.cs`

| 변경 | 이유 |
|---|---|
| `ResetCache()` 추가: 캐시한 글꼴을 버리고 플래그를 초기화 | 다음에 쓸 때 다시 고르게 한다 |
| `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` `ResetWhenPlayStarts()`가 `ResetCache()`를 부름 | Play를 시작할 때마다 처음 실행처럼 새로 고른다 (`CellEvents`, `SimClock`과 같은 방식) |
| `Sans`, `Mono`가 `_isSansResolved`일 때도 `IsDestroyed(_sans)`면 다시 고른다 | 시작 때 비우지 못하는 경로(Play 중 파괴)도 막는다 |
| `Discard()`가 null이면 건너뜀 | 비어 있을 때 `ResetCache()`를 불러도 안전하게 |

- `IsDestroyed`는 `object.ReferenceEquals(x, null)`로 "처음부터 null"(쓸 수 있는 글꼴이 없음)과 "파괴됨"(`== null`이 참인 유니티 가짜 null)을 가른다. 처음부터 null이면 매번 다시 고르는 일을 하지 않는다.
- 시험 `Tests/EditMode/Dashboard/DashboardFontsTests.cs`에 3개 추가: `ResetCache_DiscardsTheCachedFont_AndTheNextUsePicksAFreshOne`, `AFontThatWasDestroyed_IsNotHandedOutAgain`, `ResetCache_CanBeCalledAnyTime_EvenBeforeAnythingWasResolved`.

## 4. 확인

| 항목 | 결과 |
|---|---|
| `DashboardFontsTests` 새 3개 | 통과 — EditMode 헤드리스 |
| 에디터에서 Play → 종료 → Play 해서 두 번째 Play에도 글자가 나온다 | **미검증** |

## 5. 아직 하지 않은 것

- **`Label/LabelFont`(뚜껑 라벨 글꼴 캐시)는 같은 구조인데 아직 안 고쳤다.** 뚜껑 라벨 글자가 두 번째 Play부터 비면 같은 원인을 의심한다.
- 이 고침으로 증상이 사라지지 않으면 원인은 다른 곳이다. 다음으로 볼 것: 에디터 `Logs/Editor.log`에서 글꼴 경고 줄, 정적 변수가 Play 사이에 남는 다른 클래스(`static` 캐시와 `static event`를 검색), 증상이 보이는 순간 일시정지해서 `Text` 컴포넌트의 `font` 필드가 `None`(missing)인지 확인.

## 6. 재발 방지·확인 방법

- **Domain Reload를 끈 프로젝트에서 `static` 캐시를 새로 만들 때는 Play 시작 때 비우는 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 메서드를 같이 만든다.** 유니티 객체(`Font`, `Texture`, `Material`)를 캐시한다면 파괴됐는지도 본다.
- 새 정적 캐시가 있는지 찾으려면 `private static` 필드 중 `UnityEngine.Object`를 담은 것을 검색한다.
- "두 번째 Play부터 이상하다"는 증상은 헤드리스 시험으로 못 잡는다. 에디터에서 Play → 종료 → 다시 Play로 확인한다.
