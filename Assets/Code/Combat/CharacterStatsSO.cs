using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character Stats")]
public class CharacterStatsSO : ScriptableObject
{
    public string displayName;

    public int maxHP;
    public int maxMP;

    public int attack;
    public int defense;
    public int mAttack;
    public int mDefense;
    public int speed;

    public Sprite portrait;

    public SkillSO[] availableSkills;
}
