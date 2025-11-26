using UnityEngine;

public class RandomEncounterTrigger : MonoBehaviour
{
    public RandomEncounterManager encounterManager;

    public float encounterChance = 0.05f;  // 5% por paso o evento
    private bool canTrigger = true;

    public void TryEncounter()
    {
        if (!canTrigger || encounterManager == null)
            return;

        if (Random.value < encounterChance)
        {
            // Solo llama el encounter del manager
            // NO cargues la escena aquí
            encounterManager.TriggerEncounter();
        }
    }

    // Llamar este método cuando el jugador se mueva
    public void OnStep()
    {
        TryEncounter();
    }
}
