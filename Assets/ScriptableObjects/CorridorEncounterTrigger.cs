using UnityEngine;

public class CorridorEncounterTrigger : MonoBehaviour
{
    public RandomEncounterManager encounterManager;

    public float corridorLength = 10f; // metros
    public float encounterDensity = 0.15f; // qué tan "peligroso" es por metro

    private int maxEncounters = 3;
    private int encountersTriggered = 0;

    private bool playerInside = false;

    void Update()
    {
        if (!playerInside) return;
        if (encountersTriggered >= maxEncounters) return;

        float stepChance = encounterDensity * Time.deltaTime;

        if (Random.value < stepChance)
        {
            encountersTriggered++;
            encounterManager.TriggerEncounter();
        }
    }

    public void OnPlayerEnterCorridor()
    {
        playerInside = true;

        // Número teórico máximo basado en longitud
        int theoretical = Mathf.FloorToInt(corridorLength / 10f);
        maxEncounters = Mathf.Clamp(theoretical, 0, 3);

        encountersTriggered = 0;
    }

    public void OnPlayerExitCorridor()
    {
        playerInside = false;
    }
}
