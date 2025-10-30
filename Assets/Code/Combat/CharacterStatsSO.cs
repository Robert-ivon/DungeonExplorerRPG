using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character Stats")]
public class CharacterStatsSO : ScriptableObject
{
    public string id;
    public string displayName;
   
    public int maxHP = 100;
    public int maxMP = 20;
    public int attack = 10;
    public int defense = 5;
    public int speed = 10;
    public int mAttack = 8;   
    public int mDefense = 5;  
    
    public Sprite portrait; // optional for UI

    [Header("Skills")]
    public SkillSO[] availableSkills;
}
