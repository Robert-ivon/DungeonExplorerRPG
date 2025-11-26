using UnityEngine;

[CreateAssetMenu(menuName = "Game/Encounter Group")]
public class EncounterGroupSO : ScriptableObject
{
    public int groupSize = 1;
    public float statsMultiplier = 1f;

    public EnemyEntry[] enemies;
}

[System.Serializable]
public class EnemyEntry
{
    public CharacterStatsSO baseStats;
}
