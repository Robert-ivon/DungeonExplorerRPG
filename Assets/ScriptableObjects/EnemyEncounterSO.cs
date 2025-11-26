using UnityEngine;

[CreateAssetMenu(menuName = "Game/Encounter/Enemy Encounter")]
public class EnemyEncounterSO : ScriptableObject
{
    public CharacterStatsSO enemyStats;
    [Range(1,100)]
    public int encounterWeight = 20; // probabilidad relativa (no porcentaje exacto)

    public int EnemyBaseLevel = 1;
}
