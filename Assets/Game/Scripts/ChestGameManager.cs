using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class ChestGameManager : NetworkBehaviour
{
    [ObserversRpc]
    public void RebuildLeaderboardRpc(Dictionary<string, int> leaderboard)
    {
        Debug.Log($"Leaderboard Rebuild Requested for {leaderboard.Count} Players.");
        Leaderboard.Instance.RebuildLeaderboard(leaderboard);
    }
}
