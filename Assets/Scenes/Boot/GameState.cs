using System;
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    [Header("Valores Iniciales")]
    public int startGold = 0;
    public int startExp = 0;
    public int startHealth = 100;
    public int startMP = 30;
    public int startMaxHealth = 100;
    public int startMaxMP = 30;
    public List<string> initialFlags = new List<string>();

    // Runtime state
    public int Gold { get; private set; }
    public int Exp { get; private set; }
    public int Health { get; private set; }
    public int MP { get; private set; }
    public int MaxHealth { get; private set; }
    public int MaxMP { get; private set; }
    private HashSet<string> flags = new HashSet<string>();

    // Evento para actualizar HUD y otros
    public event Action OnStateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Gold = startGold;
        Exp = startExp;
        MaxHealth = startMaxHealth;
        MaxMP = startMaxMP;
        Health = Mathf.Clamp(startHealth, 0, MaxHealth);
        MP = Mathf.Clamp(startMP, 0, MaxMP);
        flags = new HashSet<string>(initialFlags);
    }

    // Métodos para modificar el estado del juego
    public void AddGold(int amount)
    {
        Gold += amount;
        OnStateChanged?.Invoke();
    }

    public void AddExp(int amount)
    {
        Exp += amount;
        OnStateChanged?.Invoke();
    }

    public void ChangeHealth(int delta)
    {
        Health = Mathf.Clamp(Health + delta, 0, MaxHealth);
        OnStateChanged?.Invoke();
    }

    public void ChangeMP(int delta)
    {
        MP = Mathf.Clamp(MP + delta, 0, MaxMP);
        OnStateChanged?.Invoke();
    }

    public void ChangeMaxHealth(int delta)
    {
        MaxHealth = Mathf.Max(1, MaxHealth + delta);
        Health = Mathf.Clamp(Health, 0, MaxHealth);
        OnStateChanged?.Invoke();
    }

    public void ChangeMaxMP(int delta)
    {
        MaxMP = Mathf.Max(1, MaxMP + delta);
        MP = Mathf.Clamp(MP, 0, MaxMP);
        OnStateChanged?.Invoke();
    }

    public void AddFlag(string flag)
    {
        if (!string.IsNullOrEmpty(flag))
        {
            flags.Add(flag);
            OnStateChanged?.Invoke();
        }
    }

    public void RemoveFlag(string flag)
    {
        if (!string.IsNullOrEmpty(flag) && flags.Contains(flag))
        {
            flags.Remove(flag);
            OnStateChanged?.Invoke();
        }
    }

    public bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
    }
}