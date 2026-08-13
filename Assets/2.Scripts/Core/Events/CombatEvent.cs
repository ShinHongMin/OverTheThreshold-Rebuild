/// <summary>
/// 전투 중 일어난 일 하나. Core가 만들고 Presentation이 재생한다.
///
/// 원칙: "무엇이 일어났는가"만 담고 "어떻게 보이는가"는 담지 않는다.
/// 좌표, 프리팹, 재생 시간, 색상 같은 필드가 들어오면 계층이 무너진다.
/// 연출 정보가 필요하면 Presentation이 SkillData를 찾아가서 읽는다.
/// </summary>
public abstract class CombatEvent
{
}
