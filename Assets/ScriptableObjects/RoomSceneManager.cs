using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using RoomSystem;

public class RoomSceneManager : MonoBehaviour
{
    [Header("UI References")]
    public Image roomImage;
    public TextMeshProUGUI descriptionText;
    public Transform choicesContainer;
    public Button choiceButtonPrefab;

    [Header("Room Setup")]
    public RoomSO startingRoom;
    private RoomSO currentRoom;

    private void Start()
    {
        // Returning from a corridor → restore last room
        if (CorridorDataCache.returnToRoom != null)
        {
            LoadRoom(CorridorDataCache.returnToRoom);
            CorridorDataCache.returnToRoom = null;
            return;
        }

        // Normal start
        LoadRoom(startingRoom);
    }

    // --------------------------------------------------------------
    // LOAD ROOM
    // --------------------------------------------------------------
    public void LoadRoom(RoomSO room)
    {
        if (room == null)
        {
            Debug.LogError("LoadRoom() received NULL room.");
            return;
        }

        // Set the new active room
        currentRoom = room;

        // Display room image
        if (roomImage != null)
        {
            if (currentRoom.roomImage != null)
            {
                roomImage.sprite = currentRoom.roomImage;
                roomImage.preserveAspect = true;
            }
            else
            {
                Debug.LogWarning("Room has no image assigned: " + currentRoom.name);
            }
        }

        // Display description text
        if (descriptionText != null)
            descriptionText.text = currentRoom.description;

        // Remove previous choices
        foreach (Transform t in choicesContainer)
            Destroy(t.gameObject);

        // Create buttons for new choices
        foreach (RoomChoice c in currentRoom.choices)
        {
            Button btn = Instantiate(choiceButtonPrefab, choicesContainer);
            btn.gameObject.SetActive(true);

            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = c.choiceText;

            btn.onClick.AddListener(() => HandleChoice(c));
        }
    }

    // --------------------------------------------------------------
    // HANDLE CHOICE
    // --------------------------------------------------------------
    private void HandleChoice(RoomChoice choice)
    {
        // If this room forbids corridors → convert corridor choices into next room choices
        if (currentRoom.disableCorridorsAndEncounters &&
            choice.resultType == ChoiceResultType.Corridor)
        {
            Debug.Log("Corridors disabled → forcing NextRoom.");

            if (choice.nextRoom != null)
            {
                LoadRoom(choice.nextRoom);
                return;
            }
            else
            {
                Debug.LogError("Corridor disabled but no fallback nextRoom assigned.");
                return;
            }
        }

        switch (choice.resultType)
        {
            case ChoiceResultType.NextRoom:
                if (choice.nextRoom != null)
                    LoadRoom(choice.nextRoom);
                else
                    Debug.LogError("NextRoom choice has NULL nextRoom.");
                break;

            case ChoiceResultType.Corridor:
                StartCorridor(choice);
                break;

            case ChoiceResultType.Event:
                TriggerEvent(choice.eventID);
                break;
        }
    }

    // --------------------------------------------------------------
    // START CORRIDOR
    // --------------------------------------------------------------
    private void StartCorridor(RoomChoice choice)
    {
        // Save where we must return after finishing the hallway
        CorridorDataCache.returnToRoom = currentRoom;

        // Save corridor size
        CorridorDataCache.chosenSize = choice.corridorSize;

        // Load corridor
        SceneManager.LoadScene("CorridorScene");
    }

    // --------------------------------------------------------------
    // EVENT HANDLER
    // --------------------------------------------------------------
    private void TriggerEvent(string eventID)
    {
        if (string.IsNullOrEmpty(eventID))
        {
            Debug.LogWarning("Event triggered but eventID is empty.");
            return;
        }

        // Placeholder — you can expand with custom logic
        Debug.Log("Event Triggered: " + eventID);
        descriptionText.text = "Something happened... (" + eventID + ")";
    }
}
