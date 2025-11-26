using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    [Header("Stats")]
    public CharacterStatsSO playerStats;
    public CharacterStatsSO[] enemyStatsArray;

    [Header("Skills")]
    public SkillSO[] availableSkills;

    [Header("Background")]
    public Image BackgroundSprite;

    [Header("UI - Enemy")]
    public Image enemySprite;
    public TextMeshProUGUI enemyHPText;

    [Header("UI - Player")]
    public Image playerSprite;
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI playerMPText;

    [Header("UI - Commands")]
    public Button attackButton;
    public Button skillButton;
    public Button runButton;
    public GameObject skillMenuPanel;
    public Button skillButtonPrefab;
    public Button closeSkillMenuButton;

    [Header("UI - Battle Log")]
    public TextMeshProUGUI battleLogText;
    public ScrollRect battleLogScrollRect;

    [Header("Skill Menu")]
    public Transform skillButtonsContainer;

    [Header("Animation / Turn timing")]
    public float attackMoveDuration = 0.35f;
    public float attackMoveDistance = 50f;
    public float damageFlashDuration = 0.12f;
    public int damageFlashes = 2;
    public float turnPause = 0.25f;

    [Header("Turn indicator")]
    public TextMeshProUGUI turnIndicatorText;

    [Header("Target Selection")]
    public GameObject targetSelectionPanel;
    public Transform targetButtonsContainer;
    public Button targetButtonPrefab;

    private int selectedTargetIndex = -1;
    private SkillSO selectedSkill = null;

    private int playerHP;
    private int playerMP;

    private class EnemyInstance
    {
        public CharacterStatsSO stats;
        public int currentHP;
        public int currentMP;
        public string displayName;
        public Sprite portrait;
        public bool IsAlive => currentHP > 0;
    }

    private List<EnemyInstance> enemies = new List<EnemyInstance>();
    private string logContent = "";

    // --------------------------------------------------------------
    // START
    // --------------------------------------------------------------
    void Start()
    {
        // Pull data from BattleData (if available)
        if (BattleData.playerStats != null)
            playerStats = BattleData.playerStats;

        if (BattleData.pendingEnemies != null)
            enemyStatsArray = BattleData.pendingEnemies;

        // Safety: ensure playerStats is assigned
        if (playerStats == null)
        {
            Debug.LogError("CombatManager: playerStats is null! Assign a CharacterStatsSO or set BattleData.playerStats before loading.");
            // Prevent further null issues by creating a minimal placeholder (avoid crashes)
            playerStats = ScriptableObject.CreateInstance<CharacterStatsSO>();
            playerStats.displayName = "Player";
            playerStats.maxHP = 1;
            playerStats.maxMP = 0;
            playerStats.attack = 1;
            playerStats.defense = 0;
            playerStats.speed = 1;
        }

        playerHP = Mathf.Max(1, playerStats.maxHP);
        playerMP = Mathf.Max(0, playerStats.maxMP);

        if (playerStats != null && playerStats.portrait != null && playerSprite != null)
            playerSprite.sprite = playerStats.portrait;

        BuildEnemyInstances();
        UpdateTargetUI();
        UpdateHUD();

        // Wire main buttons with null checks
        if (attackButton != null) attackButton.onClick.AddListener(() => TakeTurnBasicAttack());
        if (skillButton != null) skillButton.onClick.AddListener(OpenSkillMenu);
        if (runButton != null) runButton.onClick.AddListener(RunAttempt);
        if (closeSkillMenuButton != null) closeSkillMenuButton.onClick.AddListener(CloseSkillMenu);

        BuildSkillMenu();

        if (skillMenuPanel != null)
            skillMenuPanel.SetActive(false);

        if (turnIndicatorText != null)
        {
            turnIndicatorText.gameObject.SetActive(true);
            turnIndicatorText.text = "";
        }
    }

    // --------------------------------------------------------------
    // TARGET SELECTION
    // --------------------------------------------------------------
    void OpenTargetSelection(SkillSO skill)
    {
        if (targetSelectionPanel == null || targetButtonsContainer == null || targetButtonPrefab == null)
        {
            Debug.LogWarning("Target selection UI not fully assigned.");
            return;
        }

        selectedSkill = skill;

        foreach (Transform child in targetButtonsContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < enemies.Count; i++)
        {
            if (!enemies[i].IsAlive) continue;

            int idx = i;

            Button btn = Instantiate(targetButtonPrefab, targetButtonsContainer);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = enemies[i].displayName;

            btn.onClick.AddListener(() =>
            {
                selectedTargetIndex = idx;
                targetSelectionPanel.SetActive(false);
                StartCoroutine(TakeTurnRoutine(selectedSkill, selectedTargetIndex));
            });
        }

        targetSelectionPanel.SetActive(true);
    }

    // --------------------------------------------------------------
    // SPAWN & NAME ENEMIES
    // --------------------------------------------------------------
    private void BuildEnemyInstances()
    {
        enemies.Clear();

        if (enemyStatsArray == null || enemyStatsArray.Length == 0)
        {
            AddToLog("No enemies provided.");
            return;
        }

        Dictionary<string, int> nameCount = new Dictionary<string, int>();
        Dictionary<string, int> suffixIndex = new Dictionary<string, int>();

        foreach (var s in enemyStatsArray)
        {
            string nm = string.IsNullOrEmpty(s.displayName) ? "Enemy" : s.displayName;
            if (!nameCount.ContainsKey(nm))
                nameCount[nm] = 0;
            nameCount[nm]++;
        }

        foreach (var s in enemyStatsArray)
        {
            EnemyInstance inst = new EnemyInstance();
            inst.stats = s;
            inst.currentHP = Mathf.Max(1, s.maxHP);
            inst.currentMP = Mathf.Max(0, s.maxMP);
            inst.portrait = s.portrait;

            string baseName = string.IsNullOrEmpty(s.displayName) ? "Enemy" : s.displayName;
            if (!suffixIndex.ContainsKey(baseName))
                suffixIndex[baseName] = 0;

            int idx = suffixIndex[baseName];
            suffixIndex[baseName]++;

            inst.displayName = (nameCount[baseName] > 1)
                ? $"{baseName} ({(char)('A' + idx)})"
                : baseName;

            enemies.Add(inst);
            AddToLog($"Spawned enemy: {inst.displayName} - HP: {inst.currentHP}");
        }
    }

    // --------------------------------------------------------------
    // HUD UPDATES
    // --------------------------------------------------------------
    void UpdateHUD()
    {
        if (playerHPText != null)
            playerHPText.text = $"HP: {playerHP}/{playerStats.maxHP}";
        if (playerMPText != null)
            playerMPText.text = $"MP: {playerMP}/{playerStats.maxMP}";
    }

    void UpdateTargetUI()
    {
        EnemyInstance target = GetFirstAliveEnemy();
        if (target == null)
        {
            if (enemyHPText != null) enemyHPText.text = $"HP: 0/0";
            return;
        }

        if (enemyHPText != null) enemyHPText.text = $"HP: {target.currentHP}/{target.stats.maxHP}";

        if (target.portrait != null && enemySprite != null)
            enemySprite.sprite = target.portrait;
    }

    // --------------------------------------------------------------
    // BATTLE LOG
    // --------------------------------------------------------------
    void AddToLog(string message)
    {
        logContent += message + "\n";
        if (battleLogText != null) battleLogText.text = logContent;
        StartCoroutine(ScrollToBottomNextFrame());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        if (battleLogScrollRect == null || battleLogScrollRect.content == null) yield break;
        LayoutRebuilder.ForceRebuildLayoutImmediate(battleLogScrollRect.content);
        battleLogScrollRect.verticalNormalizedPosition = 0f;
        battleLogScrollRect.velocity = Vector2.zero;
    }

    // --------------------------------------------------------------
    // TARGET HELPERS
    // --------------------------------------------------------------
    EnemyInstance GetFirstAliveEnemy()
    {
        foreach (var e in enemies)
            if (e.IsAlive)
                return e;

        return null;
    }

    int GetFirstAliveEnemyIndex()
    {
        for (int i = 0; i < enemies.Count; i++)
            if (enemies[i].IsAlive)
                return i;

        return -1;
    }

    private int CalculateDamageInt(int atk, int def, float power)
    {
        float raw = (atk - def) * power;
        return Mathf.RoundToInt(Mathf.Max(1f, raw));
    }

    // --------------------------------------------------------------
    // BASIC ATTACK (SAFE)
    // --------------------------------------------------------------
    void TakeTurnBasicAttack()
    {
        int idx = GetFirstAliveEnemyIndex();

        if (idx < 0)
        {
            AddToLog("No enemies to attack.");
            return;
        }

        SkillSO temp = ScriptableObject.CreateInstance<SkillSO>();
        temp.skillName = "Attack";
        temp.type = SkillType.Physical;
        temp.power = 1f;

        StartCoroutine(TakeTurnRoutine(temp, idx));
    }

    // --------------------------------------------------------------
    // SKILL MENU
    // --------------------------------------------------------------
    void OpenSkillMenu() { if (skillMenuPanel != null) skillMenuPanel.SetActive(true); }
    void CloseSkillMenu() { if (skillMenuPanel != null) skillMenuPanel.SetActive(false); }

    void BuildSkillMenu()
    {
        if (skillButtonsContainer == null || skillButtonPrefab == null) return;

        foreach (Transform child in skillButtonsContainer)
            Destroy(child.gameObject);

        if (availableSkills == null || availableSkills.Length == 0) return;

        foreach (SkillSO skill in availableSkills)
        {
            Button btn = Instantiate(skillButtonPrefab, skillButtonsContainer);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = skill.skillName;

            btn.onClick.AddListener(() =>
            {
                CloseSkillMenu();
                StartCoroutine(TakeTurnRoutine(skill, GetFirstAliveEnemyIndex()));
            });
        }
    }

    // --------------------------------------------------------------
    // MAIN TURN ROUTINE (SAFE TARGETING)
    // --------------------------------------------------------------
    IEnumerator TakeTurnRoutine(SkillSO skill, int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= enemies.Count || !enemies[targetIndex].IsAlive)
        {
            targetIndex = GetFirstAliveEnemyIndex();
            if (targetIndex < 0)
            {
                EndBattle(true);
                yield break;
            }
        }

        SetCommands(false);

        EnemyInstance target = enemies[targetIndex];

        int playerSpeed = playerStats.speed + Mathf.RoundToInt(skill.speedModifier);
        int enemySpeed = target.stats.speed;

        bool playerGoesFirst = playerSpeed >= enemySpeed;

        if (playerGoesFirst)
        {
            if (turnIndicatorText != null) turnIndicatorText.text = "Player Turn";
            PlayerAction(skill, targetIndex);
            yield return new WaitForSeconds(0.5f);

            if (playerHP > 0 && target.IsAlive)
            {
                if (turnIndicatorText != null) turnIndicatorText.text = "Enemy Turn";
                EnemyTurn();
                yield return new WaitForSeconds(0.5f);
            }
        }
        else
        {
            if (turnIndicatorText != null) turnIndicatorText.text = "Enemy Turn";
            EnemyTurn();
            yield return new WaitForSeconds(0.5f);

            if (playerHP > 0 && target.IsAlive)
            {
                if (turnIndicatorText != null) turnIndicatorText.text = "Player Turn";
                PlayerAction(skill, targetIndex);
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (turnIndicatorText != null) turnIndicatorText.text = "";
        SetCommands(true);
    }

    // --------------------------------------------------------------
    // PLAYER ACTION
    // --------------------------------------------------------------
    bool PlayerAction(SkillSO skill, int targetIndex)
    {
        if (playerMP < skill.mpCost)
        {
            AddToLog("Not enough MP!");
            return false;
        }

        playerMP -= skill.mpCost;

        if (targetIndex < 0 || targetIndex >= enemies.Count)
        {
            AddToLog("No valid target.");
            return false;
        }

        EnemyInstance target = enemies[targetIndex];

        int dmg = 0;

        if (skill.type == SkillType.Physical)
            dmg = CalculateDamageInt(playerStats.attack, target.stats.defense, skill.power);

        else if (skill.type == SkillType.Magical)
            dmg = CalculateDamageInt(playerStats.mAttack, target.stats.mDefense, skill.power);

        else if (skill.type == SkillType.Support)
        {
            int heal = skill.effectAmount;
            playerHP = Mathf.Min(playerStats.maxHP, playerHP + heal);
            AddToLog($"Player healed {heal} HP!");
            UpdateHUD();
            return true;
        }

        target.currentHP = Mathf.Max(0, target.currentHP - dmg);
        AddToLog($"Player used {skill.skillName} on {target.displayName} for {dmg} damage!");

        if (target.currentHP <= 0)
            HandleEnemyDeath(targetIndex);
        else
            UpdateTargetUI();

        UpdateHUD();
        return true;
    }

    // --------------------------------------------------------------
    // DEATH HANDLING
    // --------------------------------------------------------------
    void HandleEnemyDeath(int index)
    {
        if (index < 0 || index >= enemies.Count) return;

        AddToLog($"{enemies[index].displayName} defeated!");

        selectedTargetIndex = GetFirstAliveEnemyIndex();

        if (!AnyEnemyAlive())
            EndBattle(true);

        UpdateTargetUI();
    }

    bool AnyEnemyAlive()
    {
        foreach (var e in enemies)
            if (e.IsAlive) return true;

        return false;
    }

    // --------------------------------------------------------------
    // ENEMY TURN
    // --------------------------------------------------------------
    void EnemyTurn()
    {
        int i = GetFirstAliveEnemyIndex();
        if (i < 0) return;

        EnemyInstance e = enemies[i];

        SkillSO skill = null;

        if (e.stats.availableSkills != null && e.stats.availableSkills.Length > 0)
        {
            skill = e.stats.availableSkills[Random.Range(0, e.stats.availableSkills.Length)];
            if (e.currentMP < skill.mpCost)
                skill = null;
        }

        int dmg = 0;

        if (skill == null)
        {
            dmg = CalculateDamageInt(e.stats.attack, playerStats.defense, 1f);
            AddToLog($"{e.displayName} attacks for {dmg}!");
        }
        else
        {
            dmg = CalculateDamageInt(e.stats.attack, playerStats.defense, skill.power);
            AddToLog($"{e.displayName} used {skill.skillName} for {dmg}!");
            e.currentMP -= skill.mpCost;
        }

        playerHP = Mathf.Max(0, playerHP - dmg);

        if (playerHP <= 0)
        {
            EndBattle(false);
            return;
        }

        UpdateHUD();
    }

    // --------------------------------------------------------------
    // RUN
    // --------------------------------------------------------------
    void RunAttempt()
    {
        EnemyInstance e = GetFirstAliveEnemy();

        if (e == null)
        {
            AddToLog("No enemies to escape from.");
            StartCoroutine(ReturnToRoom());
            return;
        }

        if (playerStats.speed > e.stats.speed)
        {
            AddToLog("Escaped!");
            EndBattle(false);
        }
        else
        {
            int roll = Random.Range(0, 100);
            if (roll < 50)
            {
                AddToLog("Failed to escape!");
                EnemyTurn();
            }
            else
            {
                AddToLog("Escaped!");
                EndBattle(false);
            }
        }
    }

    // --------------------------------------------------------------
    // END BATTLE
    // --------------------------------------------------------------
    void EndBattle(bool playerWon)
    {
        AddToLog(playerWon ? "VICTORY!" : "DEFEAT...");

        // Disable UI input
        if (attackButton != null) attackButton.interactable = false;
        if (skillButton != null) skillButton.interactable = false;
        if (runButton != null) runButton.interactable = false;
        if (skillMenuPanel != null) skillMenuPanel.SetActive(false);

        // Start return coroutine
        StartCoroutine(ReturnToRoom());
    }

    IEnumerator ReturnToRoom()
    {
        // clear pending enemies so memory doesn't leak
        BattleData.pendingEnemies = null;

        yield return new WaitForSeconds(1.5f);

        // Load RoomScene (safe)
        string sceneToReturn = RoomMemory.lastRoomScene;

if (string.IsNullOrEmpty(sceneToReturn))
    sceneToReturn = "RoomScene"; // fallback

SceneManager.LoadScene(sceneToReturn);

    }

    void SetCommands(bool v)
    {
        if (attackButton != null) attackButton.interactable = v;
        if (skillButton != null) skillButton.interactable = v;
        if (runButton != null) runButton.interactable = v;
    }
}
