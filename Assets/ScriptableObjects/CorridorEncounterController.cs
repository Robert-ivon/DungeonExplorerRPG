using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class CorridorEncounterController : MonoBehaviour
{
    [Header("UI")]
    public Image corridorImage;
    public TextMeshProUGUI descriptionText;

    [Header("Corridor Image Movement")]
    public RectTransform corridorImageTransform;
    public float bobHeight = 12f;
    public float bobSpeed = 5f;

    [Header("Corridor Data")]
    public CorridorSize corridorSize;
    public float stepDuration = 1.2f;
    public RandomEncounterManager encounterManager;

    private bool encountersAllowed = true;

    private int stepsToWalk;
    private int maxEncounters;
    private int encountersTriggered = 0;

    public enum CorridorSize
    {
        Short,
        Medium,
        Long
    }

    void Start()
    {
        // Disable random encounters if originating room forbids it
        if (CorridorDataCache.returnToRoom != null)
            encountersAllowed = !CorridorDataCache.returnToRoom.disableCorridorsAndEncounters;

        // Resume after battle
        if (CorridorMemory.returningFromBattle)
        {
            stepsToWalk = CorridorMemory.remainingSteps;
            maxEncounters = CorridorMemory.maxEncounters;
            encountersTriggered = CorridorMemory.encountersTriggered;

            CorridorMemory.returningFromBattle = false;
            StartCoroutine(WalkRoutine());
            return;
        }

        corridorSize = CorridorDataCache.chosenSize;
        InitializeCorridor();
        StartCoroutine(WalkRoutine());
    }

    // --------------------------------------------------------------
    // INITIALIZE
    // --------------------------------------------------------------
    void InitializeCorridor()
    {
        switch (corridorSize)
        {
            case CorridorSize.Short:
                stepsToWalk = Random.Range(3, 5);
                maxEncounters = Random.Range(0, encountersAllowed ? 2 : 1);
                descriptionText.text = "You advance through the corridor...";
                break;

            case CorridorSize.Medium:
                stepsToWalk = Random.Range(5, 8);
                maxEncounters = Random.Range(0, encountersAllowed ? 3 : 1);
                descriptionText.text = "Your steps echo through the corridor...";
                break;

            case CorridorSize.Long:
                stepsToWalk = Random.Range(8, 12);
                maxEncounters = encountersAllowed ? Random.Range(1, 4) : 0;
                descriptionText.text = "You walk across a long corridor...";
                break;
        }
    }

    // --------------------------------------------------------------
    // WALK ROUTINE
    // --------------------------------------------------------------
    IEnumerator WalkRoutine()
    {
        for (int i = 0; i < stepsToWalk; i++)
        {
            StartCoroutine(StepBobbing());
            yield return new WaitForSeconds(stepDuration);

            // Skip encounter logic if forbidden
            if (!encountersAllowed)
                continue;

            if (encountersTriggered < maxEncounters)
            {
                float chance = 0.3f;
                if (Random.value < chance)
                {
                    encountersTriggered++;

                    // Save progress for return
                    CorridorMemory.returningFromBattle = true;
                    CorridorMemory.remainingSteps = stepsToWalk - (i + 1);
                    CorridorMemory.maxEncounters = maxEncounters;
                    CorridorMemory.encountersTriggered = encountersTriggered;

                    TriggerEncounter();
                    yield break;
                }
            }
        }

        LoadRoomScene();
    }

    // --------------------------------------------------------------
    // FADE BACK TO ROOMSCENE
    // --------------------------------------------------------------
    void LoadRoomScene()
    {
        var f = FindObjectOfType<ScreenFader>();
        if (f != null)
            StartCoroutine(f.FadeOutAndLoad("RoomScene"));
        else
        {
            Debug.LogWarning("ScreenFader not found — loading scene instantly.");
            SceneManager.LoadScene("RoomScene");
        }
    }

    // --------------------------------------------------------------
    // TRIGGER ENCOUNTER
    // --------------------------------------------------------------
    void TriggerEncounter()
    {
        BattleData.playerStats = PlayerData.Instance.playerStats;
        var group = encounterManager.GetRandomGroup();
        BattleData.pendingEnemies = EnemyStatAdjuster.GenerateAdjustedEnemies(group);

        var f = FindObjectOfType<ScreenFader>();
        if (f != null)
            StartCoroutine(f.FadeOutAndLoad(encounterManager.battleSceneName));
        else
            SceneManager.LoadScene(encounterManager.battleSceneName);
    }

    // --------------------------------------------------------------
    // BOBBING ANIMATION
    // --------------------------------------------------------------
    IEnumerator StepBobbing()
    {
        Vector3 orig = corridorImageTransform.anchoredPosition;
        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime * bobSpeed;
            corridorImageTransform.anchoredPosition =
                orig + Vector3.down * (Mathf.Sin(t * Mathf.PI) * bobHeight);
            yield return null;
        }

        corridorImageTransform.anchoredPosition = orig;
    }
}

