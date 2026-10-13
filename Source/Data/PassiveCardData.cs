using UnityEngine;

[CreateAssetMenu(fileName = "NewPassiveCard", menuName = "Card/Passive Card Data")]
public class PassiveCardData : ScriptableObject
{
    [Header("카드 기본 정보")]
    public string cardId;
    public string cardName;
    public Sprite cardImage;
    [TextArea] public string Description;

    [Header("효과 설정")]
    [Tooltip("카드의 실제 적용 종류. 기존 Special 대신 구체적인 효과를 선택한다.")]
    public PassiveEffectType effectType;
    [Tooltip("퍼센트 효과는 10을 입력하면 10%이다.")]
    public float effectValue;

    [Header("희귀도")]
    public CardRartiy rarity;
    public Sprite rarityBG;
}
