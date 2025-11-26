using UnityEngine;

[CreateAssetMenu(menuName = "Game/Encounters/Encounter Table")]
public class EncounterTableSO : ScriptableObject
{
    [System.Serializable]
    public class EncounterEntry
    {
        public EncounterGroupSO group;
        [Range(1,100)] public int weight = 10;   // Higher = more likely
    }

    public EncounterEntry[] encounters;
}
