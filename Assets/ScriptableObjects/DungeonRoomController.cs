using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DungeonRoomController : MonoBehaviour
{
    [Header("UI")]
    public Image roomImageUI;
    public TextMeshProUGUI descriptionText;
    public Transform actionsContainer;
    public Button actionButtonPrefab;

    [Header("Fade")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 0.5f;

    [Header("Current Room")]
    public RoomDataSO currentRoom;

    void Start()
    {
        LoadRoom(currentRoom);
    }

    public void LoadRoom(RoomDataSO room)
    {
        currentRoom = room;

        roomImageUI.sprite = currentRoom.roomImage;
        descriptionText.text = currentRoom.description;

        BuildActions();
    }

    void BuildActions()
    {
        foreach (Transform t in actionsContainer)
            Destroy(t.gameObject);

        // Movement buttons
        if (currentRoom.north != null) AddAction("Ir al Norte", () => MoveTo(currentRoom.north));
        if (currentRoom.south != null) AddAction("Ir al Sur", () => MoveTo(currentRoom.south));
        if (currentRoom.east != null) AddAction("Ir al Este", () => MoveTo(currentRoom.east));
        if (currentRoom.west != null) AddAction("Ir al Oeste", () => MoveTo(currentRoom.west));

        // Special actions
        if (currentRoom.specialActions != null)
        {
            foreach (string action in currentRoom.specialActions)
            {
                AddAction(action, () => Debug.Log("Action: " + action));
            }
        }
    }

    void AddAction(string label, System.Action callback)
    {
        Button btn = Instantiate(actionButtonPrefab, actionsContainer);
        btn.GetComponentInChildren<TextMeshProUGUI>().text = label;
        btn.onClick.AddListener(() => callback());
    }

    void MoveTo(RoomDataSO newRoom)
    {
        StartCoroutine(Transition(newRoom));
    }

    IEnumerator Transition(RoomDataSO newRoom)
    {
        yield return StartCoroutine(Fade(1));
        LoadRoom(newRoom);
        yield return StartCoroutine(Fade(0));
    }

    IEnumerator Fade(float target)
    {
        float start = fadePanel.alpha;
        float timer = 0;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(start, target, timer / fadeDuration);
            yield return null;
        }
    }
}
