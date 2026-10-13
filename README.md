# OVER THE THRESHOLD

3인 파티로 노드 맵을 탐험하고, 전투·이벤트·휴식을 거쳐 보스에게 도전하는 **턴제 RPG**입니다.

- **개발:** 신홍민 · Unity / C# 개인 프로젝트
- **환경:** Unity 6000.0.40f1 · Windows 64비트
- **[실행 파일 다운로드](https://drive.google.com/drive/folders/1cWrH0cpvPovhfEoSleIvj-MhSVZYW1Z9?usp=drive_link)**

## 주요 구현

| 기능 | 구현 내용 | 대표 코드 |
| --- | --- | --- |
| 턴제 전투 | 행동 예약과 계산·연출 시점 분리, SP·OP·ER 자원 관리 | [TurnLoop](Source/Core/Turns/TurnLoop.cs) · [CombatEventPlayer](Source/Presentation/Combat/CombatEventPlayer.cs) |
| 오브젝트 풀 | 이펙트·발사체·UI 항목 등의 대여·반환과 상태 초기화 | [PrefabPoolManager](Source/Presentation/Pooling/PrefabPoolManager.cs) |
| UI 모듈화 | 패널 표시·조회, 팝업 입력 차단과 일시정지 관리 | [UiManager](Source/Presentation/UI/Common/UiManager.cs) · [CombatPopupManager](Source/Presentation/UI/Combat/CombatPopupManager.cs) |
| 저장·이어하기 | JSON으로 파티와 노드 진행 저장, 선택한 노드 복원 | [RunManager](Source/App/Run/RunManager.cs) · [SaveManager](Source/App/Run/SaveManager.cs) |
| 데이터 관리 | ScriptableObject 기반 스킬·유닛 정의와 적 편성 조회 | [SkillTableBuilder](Source/App/Combat/SkillTableBuilder.cs) · [EnemyTableAsset](Source/Data/EnemyTableAsset.cs) |

## 코드 구성

`Source`의 **Core**는 게임 규칙, **App**은 씬과 진행 흐름, **Data**는 편집용 데이터, **Presentation**은 UI와 연출을 담당합니다. 테스트 코드는 **Tests**에 있습니다.

## 실행 안내

공유 폴더의 Windows 실행본을 내려받아 압축을 모두 해제한 뒤 실행하세요. 실행 파일과 데이터 폴더를 함께 유지해야 합니다.

이 저장소는 **코드 검토용**으로, 씬·프리팹·이미지·음원 등의 에셋을 제외했습니다. 저장소만으로 전체 게임을 실행하거나 빌드할 수는 없습니다.
