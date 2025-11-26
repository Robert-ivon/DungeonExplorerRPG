using UnityEngine;

namespace RoomSystem
{
    [CreateAssetMenu(menuName = "Game/Room")]
    public class RoomSO : ScriptableObject
    {
        [TextArea(2, 4)]
        public string description;

        public Sprite roomImage;

        public RoomChoice[] choices;

        public bool disableCorridorsAndEncounters = false;

    }

    [System.Serializable]
    public class RoomChoice
    {
        public string choiceText;

        public ChoiceResultType resultType;

        public RoomSO nextRoom;
        public CorridorEncounterController.CorridorSize corridorSize;
        public string eventID;


    }

    public enum ChoiceResultType
    {
        NextRoom,
        Corridor,
        Event

    }
}
