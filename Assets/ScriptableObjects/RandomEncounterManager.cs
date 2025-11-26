using UnityEngine;
using UnityEngine.SceneManagement;

public class RandomEncounterManager : MonoBehaviour
{
    public EncounterTableSO encounterTable;
    public string battleSceneName = "BattleScene";

    [Header("Random Encounter Settings")]
    public float encounterCheckInterval = 1.5f;
    public float encounterChance = 0.15f; // 15% per interval

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= encounterCheckInterval)
        {
            timer = 0f;

            if (Random.value < encounterChance)
            {
                TriggerEncounter();
            }
        }
    }

    public void TriggerEncounter()
    {
        if (encounterTable == null || encounterTable.encounters.Length == 0)
        {
            Debug.LogError("Encounter Table is empty!");
            return;
        }

        // Weighted selection
        EncounterGroupSO selectedGroup = ChooseWeightedEncounter();

        if (selectedGroup == null)
        {
            Debug.LogError("No encounter group selected!");
            return;
        }

        // Adjust multiple enemies (scaling)
        CharacterStatsSO[] finalEnemies = EnemyStatAdjuster.GenerateAdjustedEnemies(selectedGroup);

        // Pass data to BattleData
        if (PlayerData.Instance == null)
        {
            Debug.LogError("PlayerData.Instance is NULL! Ensure PlayerData exists in your scene.");
            return;
        }

        BattleData.playerStats = PlayerData.Instance.playerStats;
        BattleData.pendingEnemies = finalEnemies;

        // Load battle scene
        SceneManager.LoadScene(battleSceneName);
    }

    EncounterGroupSO ChooseWeightedEncounter()
    {
        int totalWeight = 0;

        foreach (var entry in encounterTable.encounters)
            totalWeight += entry.weight;

        int roll = Random.Range(0, totalWeight);
        int running = 0;

        foreach (var entry in encounterTable.encounters)
        {
            running += entry.weight;
            if (roll < running)
                return entry.group;
        }

        return encounterTable.encounters[0].group; // fallback
    }

    public EncounterGroupSO GetRandomGroup()
    {
        return ChooseWeightedEncounter();
    }
}
