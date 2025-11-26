using UnityEngine;

public static class EnemyStatAdjuster
{
    public static CharacterStatsSO[] GenerateAdjustedEnemies(EncounterGroupSO group)
    {
        CharacterStatsSO[] result = new CharacterStatsSO[group.groupSize];

        for (int i = 0; i < group.groupSize; i++)
        {
            // Ahora usamos baseStats en lugar de enemyStats
            CharacterStatsSO baseEnemy = group.enemies[i].baseStats;

            // Crear copia del ScriptableObject original
            CharacterStatsSO copy = ScriptableObject.Instantiate(baseEnemy);

            // Aplicar multiplicador dependiendo del tamaño del grupo
            copy.maxHP     = Mathf.RoundToInt(copy.maxHP     * group.statsMultiplier);
            copy.attack    = Mathf.RoundToInt(copy.attack    * group.statsMultiplier);
            copy.mAttack   = Mathf.RoundToInt(copy.mAttack   * group.statsMultiplier);
            copy.defense   = Mathf.RoundToInt(copy.defense   * group.statsMultiplier);
            copy.mDefense  = Mathf.RoundToInt(copy.mDefense  * group.statsMultiplier);

            result[i] = copy;
        }

        return result;
    }
}

