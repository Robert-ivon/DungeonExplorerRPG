using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance;

    public CharacterStatsSO playerStats;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
