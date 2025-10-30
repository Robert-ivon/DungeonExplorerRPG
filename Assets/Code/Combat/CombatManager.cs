using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    [Header("Stats")]
    public CharacterStatsSO playerStats;
    [Tooltip("Assign one or more enemy CharacterStatsSO here to spawn multiple enemies")]
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
    public float attackMoveDuration = 0.35f;        // how long attack move takes
    public float attackMoveDistance = 50f;         // distance used for AttackMove calls
    public float damageFlashDuration = 0.12f;      // flash time for TakeDamageFlash
    public int damageFlashes = 2;                  // number of flashes
    public float turnPause = 0.25f;                // pause between actions to make turns clear

    [Header("Turn indicator")]
    public TextMeshProUGUI turnIndicatorText;      // optional UI text to show "Player Turn"/"Enemy Turn"

    // runtime state
    private int playerHP;
    private int playerMP;

    private class EnemyInstance
    {
        public CharacterStatsSO stats;
        public int currentHP;
        public int currentMP;
        public string displayName; // with suffix if needed
        public Sprite portrait;
        public bool IsAlive => currentHP > 0;
    }

    private List<EnemyInstance> enemies = new List<EnemyInstance>();

    private string logContent = "";

    void Start()
    {
        // initialize player
        playerHP = playerStats.maxHP;
        playerMP = playerStats.maxMP;
        if (playerStats.portrait != null && playerSprite != null)
            playerSprite.sprite = playerStats.portrait;

        // initialize enemies list from enemyStatsArray
        BuildEnemyInstances();

        // set UI for current target (first alive)
        UpdateTargetUI();

        UpdateHUD();

        // wire buttons (null-checked) - start coroutines for turn sequencing
        if (attackButton != null) attackButton.onClick.AddListener(() => TakeTurnBasicAttack());
        if (skillButton != null) skillButton.onClick.AddListener(OpenSkillMenu);
        if (runButton != null) runButton.onClick.AddListener(RunAttempt);
        if (closeSkillMenuButton != null) closeSkillMenuButton.onClick.AddListener(CloseSkillMenu);

        BuildSkillMenu();

        // hide skill menu
        if (skillMenuPanel != null)
            skillMenuPanel.SetActive(false);

        // ensure turn indicator is always present; start empty
        if (turnIndicatorText != null)
        {
            turnIndicatorText.gameObject.SetActive(true);
            turnIndicatorText.text = "";
        }
    }

    // -----------------------------
    // Enemy spawn / naming
    // -----------------------------
    private void BuildEnemyInstances()
    {
        enemies.Clear();

        if (enemyStatsArray == null || enemyStatsArray.Length == 0)
        {
            AddToLog("No enemies assigned to spawn.");
            return;
        }

        // Count occurrences of each base name
        Dictionary<string, int> nameCounts = new Dictionary<string, int>();
        foreach (var s in enemyStatsArray)
        {
            string baseName = s.displayName ?? "Enemy";
            if (!nameCounts.ContainsKey(baseName)) nameCounts[baseName] = 0;
            nameCounts[baseName]++;
        }

        // Prepare suffix counters to assign letters A, B, C...
        Dictionary<string, int> suffixIndex = new Dictionary<string, int>();

        // Create instances and assign display names with suffix if needed
        foreach (var s in enemyStatsArray)
        {
            var inst = new EnemyInstance();
            inst.stats = s;
            inst.currentHP = s.maxHP;
            inst.currentMP = s.maxMP;
            inst.portrait = s.portrait;

            string baseName = s.displayName ?? "Enemy";
            int totalSame = nameCounts.ContainsKey(baseName) ? nameCounts[baseName] : 1;

            if (!suffixIndex.ContainsKey(baseName)) suffixIndex[baseName] = 0;
            int idx = suffixIndex[baseName];
            suffixIndex[baseName] = idx + 1;

            if (totalSame > 1)
            {
                // assign suffix as letter A, B, C...
                char letter = (char)('A' + idx);
                inst.displayName = $"{baseName} ({letter})";
            }
            else
            {
                inst.displayName = baseName;
            }

            enemies.Add(inst);

            // Log spawn
            AddToLog($"Spawned enemy: {inst.displayName} - HP: {inst.currentHP}");
        }
    }

    // -----------------------------
    // UI updates & HUD
    // -----------------------------
    void UpdateHUD()
    {
        // player HUD
        if (playerHPText != null) playerHPText.text = $"HP: {playerHP}/{playerStats.maxHP}";
        if (playerMPText != null) playerMPText.text = $"MP: {playerMP}/{playerStats.maxMP}";

        // target (first alive) is displayed separately by UpdateTargetUI()
    }

    void UpdateTargetUI()
    {
        EnemyInstance target = GetFirstAliveEnemy();
        if (target != null)
        {
            if (enemyHPText != null) enemyHPText.text = $"HP: {target.currentHP}/{target.stats.maxHP}";
            if (target.portrait != null && enemySprite != null)
                enemySprite.sprite = target.portrait;
        }
        else
        {
            // no alive enemies
            if (enemyHPText != null) enemyHPText.text = $"HP: 0/0";
            // keep sprite as last state or clear it
        }
    }

    // replace existing AddToLog with this:
    void AddToLog(string message)
    {
        logContent += message + "\n";

        if (battleLogText != null)
        {
            // ensure log text object is active and visible
            if (!battleLogText.gameObject.activeSelf) battleLogText.gameObject.SetActive(true);
            var c = battleLogText.color;
            if (c.a < 1f) battleLogText.color = new Color(c.r, c.g, c.b, 1f);

            battleLogText.text = logContent;
        }

        // start coroutine to scroll after UI updates
        StartCoroutine(ScrollToBottomNextFrame());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        // wait one frame so Unity updates layout
        yield return null;

        if (battleLogScrollRect == null)
            yield break;

        RectTransform content = battleLogScrollRect.content;
        RectTransform viewport = battleLogScrollRect.viewport;
        if (content == null || viewport == null)
            yield break;

        // Force layout rebuild so sizes are accurate
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        // sometimes ContentSizeFitter / LayoutGroups need an extra frame
        yield return null;

        // decide whether to snap to bottom or keep at top depending on content size and pivot
        bool contentFits = content.rect.height <= viewport.rect.height;

        // If content fits, keep at top (1f). If content larger than viewport, scroll to bottom (0f).
        float target = contentFits ? 1f : 0f;

        // Apply target and clear any velocity/inertia
        battleLogScrollRect.verticalNormalizedPosition = target;
        battleLogScrollRect.velocity = Vector2.zero;
        battleLogScrollRect.inertia = false;

        // Force update so the visible position is applied immediately
        Canvas.ForceUpdateCanvases();

        // restore inertia next frame to avoid visual jump
        yield return null;
        battleLogScrollRect.inertia = true;
    }

    void ClearLog()
    {
        logContent = "";
        if (battleLogText != null) battleLogText.text = "";
    }

    // -----------------------------
    // Damage helper and targeting
    // -----------------------------
    private EnemyInstance GetFirstAliveEnemy()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].IsAlive) return enemies[i];
        }
        return null;
    }

    private int GetFirstAliveEnemyIndex()
    {
        for (int i = 0; i < enemies.Count; i++)
            if (enemies[i].IsAlive) return i;
        return -1;
    }

    private void HandleEnemyDeathIfAny(int enemyIndex)
    {
        if (enemyIndex < 0 || enemyIndex >= enemies.Count) return;
        var inst = enemies[enemyIndex];
        if (inst.IsAlive) return;

        // Log defeat
        AddToLog($"{inst.displayName} defeated");

        // Optional: change sprite/hide etc. For now, update target UI to next alive
        UpdateTargetUI();

        // Check if all enemies dead
        bool anyAlive = false;
        foreach (var e in enemies)
        {
            if (e.IsAlive) { anyAlive = true; break; }
        }

        if (!anyAlive)
        {
            // player wins
            EndBattle(true);
        }
    }

    private int CalculateDamageInt(int atk, int def, float power)
    {
        float raw = (atk - def) * power;
        return Mathf.RoundToInt(Mathf.Max(1f, raw));
    }

    // --- Attack ---
    void TakeTurnBasicAttack()
    {
        SkillSO temp = ScriptableObject.CreateInstance<SkillSO>();
        temp.skillName = "Attack";
        temp.type = SkillType.Physical;
        temp.power = 1f;
        temp.mpCost = 0;
        temp.speedModifier = 0;

        StartCoroutine(TakeTurnRoutine(temp)); // use coroutine so animation timing is respected
    }

    // -----------------------------
    // Skill menu
    // -----------------------------
    void OpenSkillMenu()
    {
        if (skillMenuPanel != null)
            skillMenuPanel.SetActive(true);
    }

    void CloseSkillMenu()
    {
        if (skillMenuPanel != null)
            skillMenuPanel.SetActive(false);
    }

    void BuildSkillMenu()
    {
        if (skillMenuPanel == null || skillButtonsContainer == null || skillButtonPrefab == null) return;

        // clear only the container so Close button remains
        for (int i = skillButtonsContainer.childCount - 1; i >= 0; i--)
            Destroy(skillButtonsContainer.GetChild(i).gameObject);

        if (availableSkills == null || availableSkills.Length == 0) return;

        foreach (SkillSO skill in availableSkills)
        {
            Button btn = Instantiate(skillButtonPrefab, skillButtonsContainer, false);
            btn.gameObject.SetActive(true);

            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = skill.skillName;

            // ensure LayoutElement for visible size in a Vertical Layout Group
            var le = btn.GetComponent<LayoutElement>();
            if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 56;

            btn.onClick.RemoveAllListeners();
            SkillSO captured = skill;
            btn.onClick.AddListener(() =>
            {
                CloseSkillMenu();
                StartCoroutine(TakeTurnRoutine(captured)); // start coroutine so sequencing works
            });
        }
    }

    // -----------------------------
    // Core turn flow (targets first alive enemy)
    // -----------------------------
    void TakeTurn(SkillSO skill)
    {
        // compute player effective speed
        int playerEffectiveSpeed = playerStats.speed + Mathf.RoundToInt(skill.speedModifier);

        // for simplicity enemy speed compare to first alive enemy (if any)
        EnemyInstance target = GetFirstAliveEnemy();
        if (target == null)
        {
            AddToLog("No enemies to target.");
            EndBattle(true);
            return;
        }

        int enemyEffectiveSpeed = target.stats.speed;

        bool playerGoesFirst = playerEffectiveSpeed >= enemyEffectiveSpeed;

        if (playerGoesFirst)
        {
            if (PlayerAction(skill) && AnyEnemyAlive()) EnemyTurn();
        }
        else
        {
            EnemyTurn();
            if (playerHP > 0 && AnyEnemyAlive()) PlayerAction(skill);
        }
    }

    bool AnyEnemyAlive()
    {
        foreach (var e in enemies) if (e.IsAlive) return true;
        return false;
    }

    // -----------------------------
    // PlayerAction: hits first alive enemy
    // -----------------------------
    bool PlayerAction(SkillSO skill)
    {
        if (playerMP < skill.mpCost)
        {
            AddToLog("Not enough MP for " + skill.skillName + "!");
            return false;
        }

        playerMP -= skill.mpCost;

        if (Random.value > skill.accuracy)
        {
            AddToLog($"Player's {skill.skillName} missed!");
            UpdateHUD();
            return true;
        }

        int dmg = 0;
        EnemyInstance target = GetFirstAliveEnemy();
        int targetIndex = GetFirstAliveEnemyIndex();
        if (target == null)
        {
            AddToLog("No valid target.");
            return false;
        }

        if (skill.type == SkillType.Physical)
        {
            dmg = CalculateDamageInt(playerStats.attack, target.stats.defense, skill.power);
            if (Random.value < skill.criticalChance)
            {
                dmg = Mathf.RoundToInt(dmg * skill.criticalMultiplier);
                AddToLog("Critical hit!");
            }
            // animación de ataque del jugador
            if (playerSprite != null && playerSprite.rectTransform != null)
                StartCoroutine(BattleAnimations.AttackMove(
                    playerSprite.rectTransform,
                    playerSprite.rectTransform.anchoredPosition,
                    attackMoveDistance,
                    attackMoveDuration));

            target.currentHP = Mathf.Max(0, target.currentHP - dmg);
            AddToLog($"Player uses {skill.skillName} on {target.displayName} for {dmg} physical damage!");

            if (enemySprite != null)
                StartCoroutine(BattleAnimations.TakeDamageFlash(
                    enemySprite,
                    Color.red,
                    damageFlashDuration,
                    damageFlashes));
        }
        else if (skill.type == SkillType.Magical)
        {
            dmg = CalculateDamageInt(playerStats.mAttack, target.stats.mDefense, skill.power);
            if (Random.value < skill.criticalChance)
            {
                dmg = Mathf.RoundToInt(dmg * skill.criticalMultiplier);
                AddToLog("Magic critical hit!");
            }
            if (playerSprite != null && playerSprite.rectTransform != null)
                StartCoroutine(BattleAnimations.AttackMove(playerSprite.rectTransform, playerSprite.rectTransform.anchoredPosition));
            target.currentHP = Mathf.Max(0, target.currentHP - dmg);
            AddToLog($"Player casts {skill.skillName} on {target.displayName} for {dmg} magical damage!");
            if (enemySprite != null)
                StartCoroutine(BattleAnimations.TakeDamageFlash(enemySprite, Color.red));
        }
        else if (skill.type == SkillType.Support)
        {
            int heal = Mathf.RoundToInt(Mathf.Max(1f, skill.effectAmount));
            playerHP = Mathf.Min(playerStats.maxHP, playerHP + heal);
            AddToLog($"Player uses {skill.skillName} and heals {heal} HP!");
        }

        // handle potential death of targeted enemy
        if (target.currentHP <= 0)
        {
            HandleEnemyDeathIfAny(targetIndex);
        }
        else
        {
            // update target UI only if current target changed or damaged
            UpdateTargetUI();
        }

        UpdateHUD();
        return true;
    }

    // -----------------------------
    // Enemy AI: choose a skill and act against player
    // -----------------------------
    void EnemyTurn()
    {
        // pick first alive enemy to act
        int actingIndex = GetFirstAliveEnemyIndex();
        if (actingIndex < 0) return;

        var actingEnemy = enemies[actingIndex];

        if (actingEnemy.stats.availableSkills == null || actingEnemy.stats.availableSkills.Length == 0)
        {
            // fallback to physical attack
            int dmg = CalculateDamageInt(actingEnemy.stats.attack, playerStats.defense, 1f);
            playerHP = Mathf.Max(0, playerHP - dmg);
            AddToLog($"{actingEnemy.displayName} attacks for {dmg} damage!");
            if (playerHP <= 0) { EndBattle(false); return; }
            UpdateHUD();
            return;
        }

        // pick random skill from that enemy's list
        SkillSO skill = actingEnemy.stats.availableSkills[Random.Range(0, actingEnemy.stats.availableSkills.Length)];

        if (actingEnemy.currentMP < skill.mpCost)
        {
            AddToLog($"{actingEnemy.displayName} tried to use {skill.skillName} but lacks MP!");
            // fallback to physical attack
            int dmg = CalculateDamageInt(actingEnemy.stats.attack, playerStats.defense, 1f);
            playerHP = Mathf.Max(0, playerHP - dmg);
            AddToLog($"{actingEnemy.displayName} attacks for {dmg} damage!");
            if (playerHP <= 0) { EndBattle(false); return; }
            UpdateHUD();
            return;
        }

        // perform enemy action (target player)
        EnemyAction(actingEnemy, skill);
    }

    void EnemyAction(EnemyInstance actingEnemy, SkillSO skill)
    {
        actingEnemy.currentMP -= skill.mpCost;

        if (Random.value > skill.accuracy)
        {
            AddToLog($"{actingEnemy.displayName}'s {skill.skillName} missed!");
            return;
        }

        int dmg = 0;

        if (skill.type == SkillType.Physical)
        {
            dmg = CalculateDamageInt(actingEnemy.stats.attack, playerStats.defense, skill.power);
            if (Random.value < skill.criticalChance)
            {
                dmg = Mathf.RoundToInt(dmg * skill.criticalMultiplier);
                AddToLog("Enemy critical hit!");
            }
            if (enemySprite != null && enemySprite.rectTransform != null)
                StartCoroutine(BattleAnimations.AttackMove(
                    enemySprite.rectTransform,
                    enemySprite.rectTransform.anchoredPosition,
                    -attackMoveDistance,
                    attackMoveDuration));

            playerHP = Mathf.Max(0, playerHP - dmg);
            AddToLog($"{actingEnemy.displayName} uses {skill.skillName} for {dmg} physical damage!");
            if (playerSprite != null)
                StartCoroutine(BattleAnimations.TakeDamageFlash(
                    playerSprite,
                    Color.red,
                    damageFlashDuration,
                    damageFlashes));
        }
        else if (skill.type == SkillType.Magical)
        {
            dmg = CalculateDamageInt(actingEnemy.stats.mAttack, playerStats.mDefense, skill.power);
            if (Random.value < skill.criticalChance)
            {
                dmg = Mathf.RoundToInt(dmg * skill.criticalMultiplier);
                AddToLog("Enemy magic critical hit!");
            }
            if (enemySprite != null && enemySprite.rectTransform != null)
                StartCoroutine(BattleAnimations.AttackMove(enemySprite.rectTransform, enemySprite.rectTransform.anchoredPosition, -50f));
            playerHP = Mathf.Max(0, playerHP - dmg);
            AddToLog($"{actingEnemy.displayName} casts {skill.skillName} for {dmg} magical damage!");
            if (playerSprite != null)
                StartCoroutine(BattleAnimations.TakeDamageFlash(playerSprite, Color.red));  
        }
        else if (skill.type == SkillType.Support)
        {
            int heal = Mathf.RoundToInt(Mathf.Max(1f, skill.effectAmount));
            actingEnemy.currentHP = Mathf.Min(actingEnemy.stats.maxHP, actingEnemy.currentHP + heal);
            AddToLog($"{actingEnemy.displayName} heals for {heal} HP!");
        }

        if (playerHP <= 0)
        {
            EndBattle(false);
            return;
        }

        UpdateHUD();
    }

    // -----------------------------
    // Run attempt (unchanged)
    // -----------------------------
    void RunAttempt()
    {
        AddToLog("Player tries to escape...");

        EnemyInstance target = GetFirstAliveEnemy();
        if (target == null)
        {
            AddToLog("No enemies to escape from.");
            EndBattle(false);
            return;
        }

        if (playerStats.speed > target.stats.speed)
        {
            AddToLog("Escaped successfully!");
            EndBattle(false);
        }
        else
        {
            int roll = Random.Range(0, 100);
            if (roll < 50)
            {
                AddToLog("Escape failed!");
                EnemyTurn();
            }
            else
            {
                AddToLog("Escaped successfully!");
                EndBattle(false);
            }
        }
    }

    // -----------------------------
    // End battle
    // -----------------------------
    void EndBattle(bool playerWon)
    {
        if (playerWon) AddToLog("Victory!");
        else AddToLog("Battle ended.");

        if (attackButton != null) attackButton.interactable = false;
        if (skillButton != null) skillButton.interactable = false;
        if (runButton != null) runButton.interactable = false;
        if (skillMenuPanel != null) skillMenuPanel.SetActive(false);

        UpdateHUD();
    }

    // New: Sequenced turn coroutine to show turn indicator, run action, wait for animations, then enemy action
    private IEnumerator TakeTurnRoutine(SkillSO skill)
    {
        // disable input while turn sequence plays
        SetCommandButtonsInteractable(false);

        // compute who goes first (same logic as before)
        int playerEffectiveSpeed = playerStats.speed + Mathf.RoundToInt(skill.speedModifier);
        EnemyInstance target = GetFirstAliveEnemy();
        if (target == null)
        {
            AddToLog("No enemies to target.");
            EndBattle(true);
            yield break;
        }
        int enemyEffectiveSpeed = target.stats.speed;
        bool playerGoesFirst = playerEffectiveSpeed >= enemyEffectiveSpeed;

        if (playerGoesFirst)
        {
            // show player turn (do not add log separator; update indicator text only)
            if (turnIndicatorText != null) turnIndicatorText.text = "Player Turn";

            // perform player action (synchronous logic triggers animations)
            PlayerAction(skill);

            // wait for animations and flashes to finish
            yield return new WaitForSeconds(attackMoveDuration + damageFlashDuration * damageFlashes + turnPause);

            // if enemy still alive, enemy acts
            if (AnyEnemyAlive())
            {
                if (turnIndicatorText != null) turnIndicatorText.text = "Enemy Turn";

                EnemyTurn(); // enemy logic triggers its animations
                yield return new WaitForSeconds(attackMoveDuration + damageFlashDuration * damageFlashes + turnPause);
            }
        }
        else
        {
            // enemy goes first
            if (turnIndicatorText != null) turnIndicatorText.text = "Enemy Turn";

            EnemyTurn();
            yield return new WaitForSeconds(attackMoveDuration + damageFlashDuration * damageFlashes + turnPause);

            if (playerHP > 0 && AnyEnemyAlive())
            {
                if (turnIndicatorText != null) turnIndicatorText.text = "Player Turn";

                PlayerAction(skill);
                yield return new WaitForSeconds(attackMoveDuration + damageFlashDuration * damageFlashes + turnPause);
            }
        }

        // clear turn indicator text (do not deactivate GameObject)
        if (turnIndicatorText != null)
        {
            yield return new WaitForSeconds(0.25f);
            turnIndicatorText.text = "";
        }

        // re-enable input
        SetCommandButtonsInteractable(true);

        UpdateHUD();
        UpdateTargetUI();
    }

    private void SetCommandButtonsInteractable(bool value)
    {
        if (attackButton != null) attackButton.interactable = value;
        if (skillButton != null) skillButton.interactable = value;
        if (runButton != null) runButton.interactable = value;
    }
}
