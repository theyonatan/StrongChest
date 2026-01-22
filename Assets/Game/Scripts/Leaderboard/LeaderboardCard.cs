using System;
using UnityEngine;
using TMPro;

public class LeaderboardCard : MonoBehaviour
{
    private TextMeshProUGUI _playerNameText;
    private TextMeshProUGUI _playerScoreText;

    private void Awake()
    {
        _playerNameText = GetComponentInChildren<LeaderboardPlayerName>().GetComponent<TextMeshProUGUI>();
        _playerScoreText = GetComponentInChildren<LeaderboardPlayerScore>().GetComponent<TextMeshProUGUI>();
    }

    public void SetPlayerNameText(string playerName)
    {
        _playerNameText.text = playerName;
    }
    
    public void SetPlayerScoreText(string playerScore)
    {
        _playerScoreText.text = playerScore;
    }
}
