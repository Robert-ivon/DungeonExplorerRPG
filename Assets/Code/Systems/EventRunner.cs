using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;                // para usar TextMeshPro
using UnityEngine.UI;      // para usar botones

public class EventRunner : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI narrativeText;     // texto principal
    public Transform choicesParent;           // contenedor de botones
    public GameObject choiceButtonPrefab;     // prefab de botón de opción

    [Header("HUD")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;

    [Header("Event Data")]
    public EventNodeSO startNode;             // nodo inicial del evento
    private EventNodeSO currentNode;          // nodo actual del evento

    void Start()
    {
        if (GameState.Instance != null)
            GameState.Instance.OnStateChanged += UpdateHUD;

        UpdateHUD();
        StartNode(startNode);
    }

    void OnDestroy()
    {
        if (GameState.Instance != null)
            GameState.Instance.OnStateChanged -= UpdateHUD;
    }

    public void StartNode(EventNodeSO node)
    {
        if (node == null) return;
        currentNode = node;
        ShowNode();
    }

    void ShowNode()
    {
        narrativeText.text = currentNode.body;

        // limpiar botones antiguos
        foreach (Transform child in choicesParent) Destroy(child.gameObject);

        // crear botones por cada choice que cumpla requirements
        foreach (var choice in currentNode.choices)
        {
            // comprobar requisitos de flags
            bool allowed = true;
            if (choice.requiredFlags != null && choice.requiredFlags.Length > 0)
            {
                foreach (var req in choice.requiredFlags)
                {
                    if (!GameState.Instance.HasFlag(req)) { allowed = false; break; }
                }
            }
            if (!allowed) continue;

            var localChoice = choice; // evitar captura del mismo item en el lambda
            var btnObj = Instantiate(choiceButtonPrefab, choicesParent);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = localChoice.text;

            btnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnChoice(localChoice);
            });
        }
    }

    void OnChoice(Choice choice)
    {
        // aplicar efectos
        if (choice.effects != null)
        {
            foreach (var e in choice.effects)
            {
                ApplyEffect(e);
            }
        }

        // actualizar HUD
        UpdateHUD();

        // avanzar
        if (choice.nextNode != null) StartNode(choice.nextNode);
        else narrativeText.text = "End of path.";
    }

    void ApplyEffect(Effect e)
    {
        if (GameState.Instance == null) return;
        switch (e.effectType)
        {
            case Effect.EffectType.AddGold:
                GameState.Instance.AddGold(e.intValue);
                break;
            case Effect.EffectType.AddExp:
                GameState.Instance.AddExp(e.intValue);
                break;
            case Effect.EffectType.AddFlag:
                GameState.Instance.AddFlag(e.stringValue);
                break;
            case Effect.EffectType.RemoveFlag:
                GameState.Instance.RemoveFlag(e.stringValue);
                break;
            case Effect.EffectType.DamagePlayer:
                GameState.Instance.ChangeHealth(-e.intValue);
                break;
            case Effect.EffectType.HealPlayer:
                GameState.Instance.ChangeHealth(e.intValue);
                break;
            case Effect.EffectType.HealMP:
                GameState.Instance.ChangeMP(e.intValue);
                break;
            case Effect.EffectType.DamageMP:
                GameState.Instance.ChangeMP(-e.intValue);
                break;
        }
    }

    void UpdateHUD()
    {
        if (goldText != null) goldText.text = "Gold: " + (GameState.Instance?.Gold ?? 0);
        if (hpText != null) hpText.text = $"HP: {(GameState.Instance?.Health ?? 0)}/{(GameState.Instance?.MaxHealth ?? 0)}";
        if (mpText != null) mpText.text = $"MP: {(GameState.Instance?.MP ?? 0)}/{(GameState.Instance?.MaxMP ?? 0)}";
    }
}



