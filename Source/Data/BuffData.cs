using UnityEngine;

[CreateAssetMenu(fileName = "New Buff Data", menuName = "Buff/Buff Data")]
public class BuffData : ScriptableObject
{
    [Header("기본정보")]
    public string buffName;
    public BuffType type;
    public Sprite icon;
    public bool isDebuff;

    [Header("수치")]
    public float value;

    [TextArea]
    public string description;
}
