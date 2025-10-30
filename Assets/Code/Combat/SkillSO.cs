using UnityEngine;

public enum SkillType { Physical, Magical, Support }

[CreateAssetMenu(menuName = "Game/Skill")]
public class SkillSO : ScriptableObject
{
    public string skillName;
    public Sprite icon;      // optional for UI
    public string description;
    public SkillType type;
    public int mpCost = 0;
    public float power = 1f;  // multiplier, e.g. 1 = normal, 2 = double damage
    public float speedModifier = 0; // can affect turn order
    public int effectAmount = 0; // e.g. healing amount or flat damage
    public float accuracy = 1f; // 1 = 100% hit chance
    public float criticalChance = 0.1f; // e.g. 0.1 = 10% chance
    public float criticalMultiplier = 1.5f; // e.g. 1.5 = 50% more damage on crits
    public bool targetEnemies = true; // true for offensive skills
    public bool targetAllies = false; // true for healing/support skills
    public bool targetAll = false;    // true to target all characters in the chosen group
    public bool targetSelf = false;   // true if the skill can target the user
    public bool targetSingle = true;  // true if the skill can target a single character
    public bool targetRandom = false; // true if the skill targets random characters    
    public bool canTargetDead = false; // true if the skill can target dead characters (for revival)
}
    

