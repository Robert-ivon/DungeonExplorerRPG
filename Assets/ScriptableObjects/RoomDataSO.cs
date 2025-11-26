using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Room")]
public class RoomDataSO : ScriptableObject
{
    public string roomName;

    [TextArea(3,6)]
    public string description;

    public Sprite roomImage;

    [Header("Connections")]
    public RoomDataSO north;
    public RoomDataSO south;
    public RoomDataSO east;
    public RoomDataSO west;

    [Header("Optional Special Actions")]
    public string[] specialActions;   
}
