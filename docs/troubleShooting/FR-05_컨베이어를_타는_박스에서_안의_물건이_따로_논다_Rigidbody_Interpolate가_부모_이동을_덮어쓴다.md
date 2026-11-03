# FR-05 컨베이어: 박스가 가는데 안의 물건이 제자리에 남는다

> 작성일: 2026-11-03 · 브랜치: `feature/pw-conveyor-dashboard-polish`
> 범위: 키팅이 끝난 박스를 컨베이어에 올리면 박스만 가고 박스 안에 놓은 물건은 따로 논다. 사용자 보고: "물건 적재한 것과 kitbox가 따로 이동해, kitbox 안의 물건은 같이 이동되어야지".
> 표시: **확정**(측정으로 확인) · **추정**(코드나 정황으로 본 것) · **미검증**(아직 확인하지 않음)
> 시험은 모두 헤드리스(프레임 50fps 고정)다. **에디터에서 직접 확인한 것이 아니다.**

## 0. 한 줄 요약

놓인 물건을 얼릴 때 `isKinematic`만 켰는데, 물건 몸체의 보간(`interpolation`)이 `Interpolate`로 남아 있어서 물건이 매 프레임 자기 물리 자세로 transform을 덮어썼다.
그래서 부모(박스 받침)를 스크립트로 옮겨도 자식 물건이 따라오지 않았다. 얼릴 때 보간과 충돌 감지도 함께 꺼서 고쳤다(`Graspable.Freeze()`). **확정.**

## 1. 증상 (확정)

- 박스 받침(`KitBoxCarrier`)은 컨베이어(`Parcel`)가 옮기고, 물건은 박스의 자식이다. 부모가 움직이면 자식도 같이 가야 한다.
- 시험(`KitBoxRideTests`)으로 박스가 지그를 떠난 뒤 물건의 받침 기준 자리를 프레임마다 쟀다. **수정 전**: 컨베이어에 오른 뒤 진행 로그(2초 간격)에서 최대 벗어남이 0.0cm → 218.8cm → 483.0cm로 커졌다. 박스만 앞으로 가고 물건은 남았다는 뜻이다(시험이 끝까지 가기 전에 멈춰 수정 전의 최종 요약 줄은 없다).

## 2. 원인 (확정)

1. `Graspable.EnablePhysics`가 물건을 집을 때 `interpolation = Interpolate`, `collisionDetectionMode = ContinuousDynamic`으로 바꾼다. 쥐고 가는 동안 움직임을 부드럽게 보이게 하려는 것이다.
2. 놓은 뒤 `PlacedItemLanding.Land`는 속도를 0으로 만들고 `isKinematic = true`만 켰다. 보간 설정은 그대로 남았다.
3. `Interpolate`인 Rigidbody는 키네마틱이어도 렌더 프레임마다 **자기 물리 자세**로 transform을 되돌려 쓴다. 부모가 스크립트로 `transform.position`을 바꿔도 자식의 물리 자세는 옛 자리이므로 매 프레임 옛 자리로 덮인다.
4. 컨베이어는 받침을 transform 이동으로 옮긴다(`Parcel`, `PackingStop`의 미끄러뜨림). 그러니 박스는 가고 물건은 남는다.

## 3. 고친 것

- `Graspable.Freeze()`를 만들었다: 속도 0, `isKinematic = true`, `interpolation = None`, `collisionDetectionMode = Discrete`. (움직이지 않는 물건이라 연속 충돌 검출이 필요 없어 집을 때의 `ContinuousDynamic`을 되돌린다. 이 변경이 원인 해결에 필요했는지는 따로 가르지 않았다. 확정된 원인은 보간이다.)
- `PlacedItemLanding.Land`의 끝에서 흩어져 있던 "속도 0 + isKinematic"을 `item.Freeze()` 한 줄로 바꿨다.
- `EnablePhysics`는 다시 집을 때 `Interpolate`, `ContinuousDynamic`, 비키네마틱으로 되돌린다. `Body`는 처음 쓸 때 가져오도록 했다.
- 단위 시험 `GraspableFreezeTests` 4개: 얼리면 키네마틱·보간 None, 충돌 감지 Discrete, 속도 0, `EnablePhysics`가 설정을 복원.

## 4. 시험 결과 (헤드리스, 각 1번)

`KitBoxRideTests.ItemsInTheBox_RideWithTheBox_ToTheEnd_WithoutTheLidCover` (팔 B 뚜껑 덮기를 끄고 컨베이어와 물건만 본다):

| 물건 | 최대 벗어남 | 고정 | 충돌 감지 | 물리로 움직인 적 |
|---|---|---|---|---|
| Item_gauze_1 | 0.0cm | True | Discrete | False |
| Item_bandage_1 | 0.0cm | True | Discrete | False |
| Item_antiseptic_1 | 0.0cm | True | Discrete | False |
| Item_gloves_1 | 0.0cm | True | Discrete | False |

통과 기준은 끝에 닿을 것, 물리로 움직인 물건이 없을 것, 최대 벗어남 5mm 미만이다.

## 5. 한계와 다음에 할 일

- **에디터에서 직접 확인하지 않았다.** 헤드리스 결과다.
- 팔 B가 물건이 든 박스에 뚜껑을 덮는 경로는 이 문서의 범위가 아니다. 그 경로에서는 `AtPacking` 직후 PhysX 솔버가 죽는 별개의 문제가 있어 따로 조사 중이다(원인을 찾으면 별도 문서로 쓴다). 이 문서의 수정은 그 충돌과 무관하게 필요했다(충돌은 수정 전에도 났다).

## 6. 함정으로 남길 것

- "키네마틱으로 얼렸다"만으로는 부모를 따라오게 되지 않는다. **`interpolation`이 `None`인지** 함께 본다. 물건이 부모와 따로 놀면 Rigidbody 보간부터 의심한다.
- 부모를 스크립트로 옮기는 구조(컨베이어 `Parcel`, 미끄러뜨림)에서는 자식 Rigidbody를 "얼린 상태"로 일관되게 만드는 한 곳(`Freeze`)을 두는 편이 안전하다.
- 이 종류의 문제는 박스만 보는 시험(`ConveyorHandoffTests`, 빈 박스)으로는 잡히지 않는다. 물건이 든 박스를 끝까지 태우는 시험(`KitBoxRideTests`)이 필요했다.
