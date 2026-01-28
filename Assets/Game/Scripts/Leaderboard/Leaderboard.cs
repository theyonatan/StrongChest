using System.Collections.Generic;
using UnityEngine;

public class Leaderboard : MonoBehaviour
{
    // leaderboard: username, score
    private readonly Dictionary<string, LeaderboardCard> _cards = new ();
    
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
    /// add new player to the leaderboard
    /// </summary>
    /// <param name="playerUsername">new player username</param>
    private void AddPlayerToLeaderboard(string playerUsername)
    {
        var cardObject = Resources.Load<LeaderboardCard>("LeaderboardCard");
        if (!cardObject)
        {
            Debug.LogError($"{nameof(Leaderboard)}: Card Object not found.");
            return;
        }
        
        var spawnedCard = Instantiate(cardObject, transform);
        spawnedCard.SetPlayerNameText(playerUsername);
        spawnedCard.SetPlayerScoreText("0");
        
        _cards.Add(playerUsername, spawnedCard);
    }

    /// <summary>
    /// for the new player, give the full leaderboard including himself.
    /// </summary>
    /// <param name="updatedBoard">all connected players, including myself (new player)</param>
    public void RebuildLeaderboard(Dictionary<string, int> updatedBoard)
    {
        // Clean Leaderboard
        foreach (var card in _cards)
            Destroy(card.Value.gameObject);
        _cards.Clear();
        
        // Rebuild Leaderboard
        foreach (var player in updatedBoard)
        {
            AddPlayerToLeaderboard(player.Key);
            UpdateCount(player.Key, player.Value);
        }
    }
    
    public void UpdateCount(string playerUsername, int count)
    {
        if (!_cards.TryGetValue(playerUsername, out var card))
        {
            Debug.LogError($"{nameof(Leaderboard)}: Player {playerUsername} not found.");
            return;
        }
        
        card.SetPlayerScoreText(count.ToString());
    }
}
