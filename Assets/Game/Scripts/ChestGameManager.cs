using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class ChestGameManager : NetworkBehaviour
{
    #region LeaderboardSetup
    
    private Dictionary<string, int> _cachedLeaderboard = new();
    
    [ObserversRpc]
    public void RebuildLeaderboardRpc(Dictionary<string, int> leaderboard)
    {
        Debug.Log($"Leaderboard Rebuild Requested for {leaderboard.Count} Players.");
        Leaderboard.Instance.RebuildLeaderboard(leaderboard);

        // Copy to avoid the reference sent over the RPC
        _cachedLeaderboard = new Dictionary<string, int>(leaderboard);
    }

    /// <summary>
    /// Sometimes the network object will not spawn in time,
    /// so we request leaderboard rebuild once we know the object is ready.
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        RequestLeaderboardRebuild();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestLeaderboardRebuild()
    {
        if (!IsServerStarted)
            return;
        
        Debug.Log($"Leaderboard Rebuild Requested. using cached with {_cachedLeaderboard.Count} Players.");
        RebuildLeaderboardRpc(_cachedLeaderboard);
    }

    #endregion

    #region LeaderboardUpdate

    [ObserversRpc]
    public void UpdateLeaderboardScoreRpc(string shooting, string shot, int shootingKillCount)
    {
        Debug.Log($"{shooting} shot {shot}.");
        Leaderboard.Instance.UpdateCount(shooting, shootingKillCount);
    }

    #endregion
}
