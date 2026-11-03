# 대시보드: Play 직후부터 화면이 버벅인다 (깔리지 않은 폰트와 가변 폰트가 글자마다 경고를 낸다)

> 작성일: 2026-11-03 · 브랜치: `feature/pw-grasp-hold-tremble` · Unity 6000.6.0f1
> 범위: 에디터에서 Play를 누르면 데모 패널(IMGUI)이 버벅이고, "적용"을 누른 뒤에는 더 심하게 느껴지는 문제
> 표시: **확정**(측정이나 코드로 확인) · **추정**(코드나 정황으로 본 것) · **미검증**(아직 확인하지 않음)

## 0. 한 줄 요약

대시보드 글꼴 목록의 맨 앞 이름(Pretendard, JetBrains Mono)이 이 PC에 깔려 있지 않다. Unity는 깔려 있지 않아도 글꼴 객체를 만들어 주지만, IMGUI가 글자를 재거나 그릴 때마다 그 글꼴을 폰트 에셋으로 바꾸려다 실패하고, 실패는 캐시되지 않아서 **글자마다 다시 시도하며 경고와 스택 트레이스를 남긴다.** 한 번 플레이에 경고가 4만 8천 번, 로그가 248만 줄(272MB)이었다. 설치되어 있고 폰트 에셋으로 실제 바뀌는 글꼴만 고르게 고쳤다. **헤드리스에서 글꼴 선택은 확인했지만, 버벅임이 사라졌는지는 에디터에서 직접 확인하지 않았다 (미검증).**

## 1. 증상 (확정)

- 사용자 보고: "play 해서 적용 버튼을 눌렀는데 왜 이렇게 버벅여".
- 에디터 로그(`Logs/Editor.log`, 첫 줄 근처 `BatchMode: 0`이면 사람이 쓴 세션)에 같은 경고가 계속 찍힌다.

```
Unable to load font face for [Pretendard]. Make sure "Include Font Data" is enabled in the Font Import Settings.
Unable to load font face for [JetBrains Mono]. Make sure "Include Font Data" is enabled in the Font Import Settings.
```

## 2. 원인을 찾은 방법 (확정)

로그에서 반복되는 줄을 숫자만 `N`으로 묶어 센다. 스택 트레이스 바로 앞 줄이 메시지이다.

```bash
export LC_ALL=C
grep -B1 "^UnityEngine.Debug:ExtractStackTraceNoAlloc" Logs/Editor.log \
  | grep -v "ExtractStackTraceNoAlloc\|^--" \
  | sed -E 's/[0-9]+(\.[0-9]+)?/N/g' | cut -c1-170 | sort | uniq -c | sort -rn | head
```

| 항목 | 값 |
|---|---|
| `Logs/Editor.log` | 2,481,710줄, 약 272MB (직전 세션 `Editor-prev.log`는 약 206MB) |
| `[Pretendard]` 경고 | 22,916번 (직전 세션 16,927번) |
| `[JetBrains Mono]` 경고 | 25,701번 (직전 세션 18,887번) |
| 그 밖의 반복 메시지 | 없음. 나머지는 한 줄씩 나온 평범한 로그 |
| 첫 경고 | 씬을 연 줄(615) 바로 뒤인 666줄. 즉 **적용을 누르기 전, 씬을 연 직후부터** 났다 |
| 적용 전 / 후 | 첫 키팅 로그(1,159,916줄) 기준 앞쪽 22,889번, 뒤쪽 25,728번 |

경고마다 스택이 거의 같다. 패널이 머리줄 제목의 글자 폭을 재는 곳에서 시작한다.

```
FontAsset.CreateFontAsset  →  Debug.LogWarning
 ← FontAssetFactory.ConvertFontToFontAsset ← TextSettings.GetCachedFontAsset
 ← IMGUITextHandle.GetTextSettingsFontAssetAndFontSize ← GUIStyle.GetPreferredSize
 ← GUIStyle.CalcSize ← DashboardStyles.WidthOf (DashboardStyles.cs:31)
 ← DashboardCard.DrawHeader (DashboardCard.cs:37) ← DemoPanel.OnGUI (DemoPanel.cs:74)
```

## 3. 왜 글꼴이 실패하나 (확정)

`DashboardFonts`는 `Font.CreateDynamicFontFromOSFont(이름 목록, 16)`으로 컴퓨터에 깔린 글꼴을 찾는다. 이름 목록을 앞에서부터 고르는 것으로 생각했지만, 만들어진 글꼴의 이름(`Font.name`)은 **목록의 맨 앞 이름**이고, IMGUI가 폰트 에셋으로 바꿀 때는 그 이름의 글꼴만 찾는다. 뒤쪽 이름으로 넘어가 주지 않는다.

임시 프로브(에디터 스크립트, 확인 뒤 지움)로 후보를 하나씩 `Font.GetOSInstalledFontNames()`와 `FontAsset.CreateFontAsset(font)`에 넣어 봤다. 배치 모드에서 돌렸다.

| 글꼴 | 설치됨 | 폰트 에셋으로 바뀜 |
|---|---|---|
| Pretendard | 아니오 | **실패** (경고) |
| Noto Sans KR | 예 (`NotoSansKR-VF.ttf`) | **실패** (경고) |
| Malgun Gothic | 예 | 성공 |
| Segoe UI | 예 | 성공 |
| Arial | 예 | 성공 |
| JetBrains Mono | 아니오 | **실패** (경고) |
| D2Coding | 아니오 | **실패** (경고) |
| Consolas | 예 | 성공 |
| Courier New | 예 | 성공 |
| 지금 코드의 목록 전체 (Sans) | | `Font.name='Pretendard'`, **실패** |
| 지금 코드의 목록 전체 (Mono) | | `Font.name='JetBrains Mono'`, **실패** |

- 깔려 있지 않은 글꼴이 맨 앞이면 이 PC에서는 목록 전체가 실패한다. 뒤에 Malgun Gothic이나 Consolas가 있어도 소용없다.
- **"깔려 있다"만으로는 부족하다.** Noto Sans KR은 깔려 있지만 바뀌지 않는다. 가변 글꼴(`-VF`) 파일이라서 폰트 에셋으로 바뀌지 않는 것으로 보이지만 이유는 따로 시험하지 않았다 (추정).
- 어느 글꼴이 깔려 있는지는 컴퓨터마다 다르다. 이 PC에서 안 되는 목록이 다른 PC에서는 되고, 반대도 있다.

## 4. 왜 Play 직후부터, 적용 뒤에 더 심했나

- 글자 폭을 재거나 그릴 때마다 실패하는 변환을 새로 시도하고, 경고에는 스택 트레이스 수집이 붙는다. 패널이 그려지는 동안 계속 반복된다 (확정, 로그. 프레임당 몇 번인지는 세지 않았다).
- 로그 패널은 `UnityLogCapture`로 경고까지 모두 받아 버퍼에 담는다 (확정, `UnityLogCapture.OnLogReceived`). 새 줄이 올 때마다 화면에 그리는 최근 60줄의 높이를 다시 잰다(`LogRowList.Refresh`). 그래서 경고가 쏟아지면 글자 폭 재기 비용에 더해 로그 패널이 매 프레임 다시 만들어졌을 것이다 (추정, 비용은 재지 않았다).
- 적용을 누르면 키팅이 시작되어 물리와 로그가 늘어, 이미 느리던 화면이 더 느리게 느껴졌을 것이다 (추정). 로그만 보면 경고 발생 속도는 적용 전후가 비슷하다 (확정, 위 표).

## 5. 한 고침

| 파일 | 내용 |
|---|---|
| `Scripts/Dashboard/FontPicker.cs` (새 파일) | 이름 후보를 앞에서부터 시도해 처음 만들어진 것을 돌려준다. 만드는 방법은 받아서 쓰므로 Unity 없이 시험한다 |
| `Scripts/Dashboard/DashboardFonts.cs` | 설치된 글꼴(`Font.GetOSInstalledFontNames()`)만 시도하고, 폰트 에셋으로 실제 바뀌는 첫 글꼴을 고른다. 바뀌지 않는 후보는 버리고 다음으로 넘어간다. 하나도 없으면 null(Unity 기본 글꼴). 고른 결과는 한 번만 계산해 둔다(없을 때도 다시 시도하지 않는다) |
| `Tests/EditMode/Dashboard/FontPickerTests.cs` (새 파일) | 첫 성공을 고름, 실패는 순서대로 건너뜀, 첫 성공 뒤에는 더 시도하지 않음, 전부 실패하면 null, 빈 목록, 인자 검사 |
| `Tests/EditMode/Dashboard/DashboardFontsTests.cs` (새 파일) | 고른 Sans와 Mono가 폰트 에셋으로 바뀌고 경고가 없는지, 한 번만 고르는지. 글꼴이 하나도 없는 환경은 건너뜀 |

- 바뀌지 않는 후보를 시험해 볼 때 경고가 한 번 남는데, 다음 후보로 넘어가는 정상 과정이라 그 순간만 `Debug.unityLogger.logEnabled`를 끄고 곧바로 되돌린다.
- 글꼴 이름 목록은 그대로 두었다. 목록 앞쪽에 있는 글꼴이 이 PC에 깔려 있으면 자동으로 우선한다.
- 이 PC에서 고르는 글꼴은 후보별 변환 결과로 보아 본문이 Malgun Gothic, 숫자·영문이 Consolas일 것이다 (추정, 시험에서 선택된 이름을 찍지는 않았다). 디자인 시안의 Pretendard와 모양이 다르다.

## 6. 확인한 것과 한계

헤드리스 (배치 모드, 에디터 화면 아님):

- 대시보드 EditMode 시험 46개 모두 통과, 그 실행 로그에 `Unable to load font face`는 0건 (확정).
- 이 수정 전후로 `error CS`는 외부 에셋 `StraightConveyor.cs`의 기존 플레이어 빌드 오류뿐이다.

한계:

- **에디터에서 버벅임이 사라졌는지는 확인하지 않았다 (미검증).** 배치 모드(`-nographics`)에서는 `OnGUI`가 돌지 않아 IMGUI 그리기 비용을 잴 수 없다.
- 프레임당 걸린 시간(ms)은 재지 않았다. 경고 횟수와 로그 크기로 원인을 본 것이다.
- 글꼴이 바뀌어 글자 폭이 달라지면 패널 안의 줄바꿈과 배치가 조금 달라질 수 있다. 화면으로는 보지 못했다.
- 가변 글꼴이 바뀌지 않는 이유는 따로 확인하지 않았다 (추정).

## 7. 다음에 할 일

1. 에디터에서 Play와 "적용"을 눌러 버벅임이 줄었는지 본다. 느리면 먼저 `Logs/Editor.log` 크기가 빠르게 커지는지, 같은 경고가 다시 찍히는지 본다 (위 2장의 명령).
2. Pretendard를 쓰려면 **가변 글꼴이 아닌 정적 TTF/OTF**로 설치한다. 설치하면 자동으로 우선 선택된다. 가변 글꼴은 건너뛴다.
3. 글꼴 파일을 프로젝트에 넣어 어느 PC에서나 같은 글꼴을 쓰게 할지 정한다. 이때는 글꼴 가져오기 설정에서 "Include Font Data"를 켜야 하고, 글꼴 사용 허가를 확인해야 한다.
4. 버벅임이 남아 있으면 그때는 로그 패널이 새 줄마다 최근 60줄의 높이를 다시 재는 비용(`LogRowList.Refresh`)과 도구 카메라가 매 프레임 한 번 더 그리는 비용(`ToolCameraFeed.LateUpdate`)을 따로 잰다. 둘 다 이번에는 재지 않았다.
