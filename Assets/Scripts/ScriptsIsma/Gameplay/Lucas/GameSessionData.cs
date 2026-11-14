using UnityEngine;

public class GameSessionData
{
    public static GameObject PlayerLeftPrefab;
    public static GameObject PlayerRightPrefab;

    public static void ClearSelection()
    {
        PlayerLeftPrefab = null;
        PlayerRightPrefab = null;
    }
}