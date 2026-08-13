using UnityEngine;

[CreateAssetMenu(fileName = "NewPassiveCard", menuName = "Card/Passive Card Data")]
public class PassiveCardData : ScriptableObject
{
    [Header("카드 기본 정보")]
    public string cardId;
    public string cardName;
    public Sprite cardImage;
    [TextArea]public string Description;

    [Header("효과 설정")]
    //public PassiveEffectType effectType;
    public float effectValue;

    [Header("희귀성(회색-normal, 파란색-rare, 금색-epic)")]
    //희귀도
    public CardRartiy rarity;
    public Sprite rarityBG;
}
