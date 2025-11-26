using UnityEngine;

public static class CorridorDataCache
{
    public static RoomSystem.RoomSO returnToRoom = null;

    public static CorridorEncounterController.CorridorSize chosenSize;

    public static bool returningFromBattle = false;
    public static int remainingSteps = 0;
    public static int maxEncounters = 0;
    public static int encountersTriggered = 0;
}
