using UnityEngine;

public class RoomEncounterTrigger : MonoBehaviour
{
    [Range(0f, 1f)]
    public float roomEncounterChance = 0.20f; // 20% por cuarto

    public RandomEncounterManager encounterManager;

    public void OnPlayerEnterRoom()
    {
        if (Random.value < roomEncounterChance)
        {
            encounterManager.TriggerEncounter(); 
        }
    }
}
