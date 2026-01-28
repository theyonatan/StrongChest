using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Moves player data between scenes
/// </summary>
public class Wind : MonoBehaviour
{
    #region Singleton

    public static Wind Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #endregion

    public UnityEvent<string> OnClientApproved = new ();
    public UnityEvent<int> OnClientLeave = new ();

    private readonly Dictionary<int, string> _playerUsernames = new();

    public void AddPlayer(int id, string username)
    {
        if (!_playerUsernames.TryAdd(id, username))
            Debug.LogError("id already exists in Wind's usernames!");
        
        OnClientApproved?.Invoke(username);
    }

    public void RemovePlayer(int id)
    {
        if (!_playerUsernames.Remove(id))
            Debug.LogError("id does not exist in Wind's usernames!");
        
        OnClientLeave?.Invoke(id);
    }

    public string GetUsernameForId(int playerId)
    {
        if (_playerUsernames.TryGetValue(playerId, out var username))
            return username;
        
        throw new KeyNotFoundException($"Wind: can't find username for player id: {playerId}");
    }
}
