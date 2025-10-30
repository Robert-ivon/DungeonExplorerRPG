using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Event Node")]
public class EventNodeSO : ScriptableObject
{
    [TextArea] public string body;
    public Choice[] choices;
}

[System.Serializable]
public class Choice
{
    public string text;
    public EventNodeSO nextNode;
    public string[] requiredFlags;
    public Effect[] effects;
}

[System.Serializable]
public class Effect
{
    public enum EffectType
    {
        AddGold,
        AddExp,
        AddFlag,
        RemoveFlag,
        DamagePlayer,
        HealPlayer,
        HealMP,
        DamageMP,
        ChangeMaxHealth,
        ChangeMaxMP
    }
    public EffectType effectType;
    public int intValue;
    public string stringValue;
}
