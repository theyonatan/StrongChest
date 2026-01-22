using System.Collections.Generic;
using UnityEngine;

public class Leaderboard : MonoBehaviour
{
    private readonly Dictionary<int, LeaderboardCard> _cards = new ();
    
    #region Singleton
    
    public static Leaderboard Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    /// <summary>
    /// For existing players, add this new player to the leaderboard
    /// </summary>
    /// <param name="playerId">new player id</param>
    public void AddPlayerToLeaderboard(int playerId)
    {
        var cardObject = Resources.Load<LeaderboardCard>("LeaderboardCard");
        if (!cardObject)
        {
            Debug.LogError($"{nameof(Leaderboard)}: Card Object not found.");
            return;
        }
        
        var spawnedCard = Instantiate(cardObject, transform);
        spawnedCard.SetPlayerNameText($"Player {playerId}");
        spawnedCard.SetPlayerScoreText("0");
        
        _cards.Add(playerId, spawnedCard);
    }

    /// <summary>
    /// for the new player, give the full leaderboard including himself.
    /// </summary>
    /// <param name="updatedBoard">all connected players, including myself (new player)</param>
    public void UpdateLeaderboard(Dictionary<int, int> updatedBoard)
    {
        // add existing
        foreach (var player in updatedBoard)
        {
            AddPlayerToLeaderboard(player.Key);
            UpdateCount(player.Key, player.Value);
        }
    }
    
    public void UpdateCount(int playerId, int count)
    {
        if (!_cards.TryGetValue(playerId, out var card))
        {
            Debug.LogError($"{nameof(Leaderboard)}: Player {playerId} not found.");
            return;
        }
        
        card.SetPlayerScoreText(count.ToString());
    }
}
