using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using OverallTimers;
using UnityEngine;

public class ChestStory : NetworkBehaviour
{
    // ---- Network Connection ----
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float respawnTime = 5f;
    private MultiplayerManager _mm;
    
    // Connection
    private readonly HashSet<string> _takenUsernames = new();
    
    // ---- Combat ----
    private readonly Dictionary<int, int> _playerScores = new ();

    private void Start()
    {
        // subscribe to client connection events
        _mm = MultiplayerManager.Instance;
        _mm.OnClientConnected += OnClientConnected;
        _mm.OnClientDisconnected += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        // unsubscribe client connection events
        _mm.OnClientConnected -= OnClientConnected;
        _mm.OnClientDisconnected -= OnClientDisconnected;
    }

    private void OnClientConnected(NetworkConnection connection)
    {
        // Setup new player
        var newPlayer = SpawnPlayer(connection);
        
        // Leaderboard
        _playerScores.Add(connection.ClientId, 0);                  // update on server
        UpdateLeaderboardRpc(connection.ClientId);                  // update on existing clients
        newPlayer.FetchExistingLeaderboard(connection, _playerScores); // update on connected client
    }

    private void OnClientDisconnected(NetworkConnection connection)
    {
        // despawn the leaving client object for everyone when he disconnects
        var players = FindObjectsByType<Player>(FindObjectsSortMode.InstanceID);
        
        foreach (var player in players)
        {
            if (player.PlayerId != connection.ClientId)
                continue;
            
            _mm.DespawnPlayer(player);
            return;
        }
    }
    
    private ChestMultiplayerExtension SpawnPlayer(NetworkConnection connection)
    {
        // choose random spawn point to start at
        var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        // spawn new player for everyone when joins (like middle of a match with no timer)
        var spawnedPlayer = _mm.SpawnPlayer(
            connection,
            playerPrefab,
            spawnPoint.position,
            Quaternion.identity);
        
        spawnedPlayer.PlayerId = connection.ClientId;

        var newPlayer = spawnedPlayer.GetComponent<ChestMultiplayerExtension>();
        
        // tell the client to set itself up locally
        newPlayer.SetupFPSPlayerRpc(connection);

        return newPlayer;
    }
    
    // --------------------------
    // Connection
    // --------------------------
    [ServerRpc]
    public void RequestUsername(string requestedName, NetworkConnection conn = null)
    {
        string finalName = MakeUnique(requestedName);
        _takenUsernames.Add(finalName);

        SetUsernameRpc(conn, finalName);
    }
    
    private string MakeUnique(string baseName)
    {
        if (!_takenUsernames.Contains(baseName))
            return baseName;

        int i = 1;
        while (_takenUsernames.Contains($"{baseName} {i}"))
            i++;

        return $"{baseName} {i}";
    }
    
    [TargetRpc]
    private void SetUsernameRpc(NetworkConnection conn, string finalName)
    {
        // Player.Local.Username = finalName;
        Debug.Log("my username: " + finalName);
    }
    
    // --------------------------
    // Player combat events
    // --------------------------
    [Server]
    public void HandlePlayerKilled(int shootingPlayerId, int killedPlayerId)
    {
        var player = Player.GetPlayer(killedPlayerId);
        if (!player)
            return;

        _playerScores[shootingPlayerId] += 1;
        
        UpdateLeaderboardRpc(
            shootingPlayerId,
            killedPlayerId,
            _playerScores[shootingPlayerId]);

        // disable player input on the client
        var handler = player.GetComponent<ChestMultiplayerExtension>();
        handler.NeutrilizePlayerRpc(handler.Owner);
    }

    [ObserversRpc]
    private void UpdateLeaderboardRpc(int shooting, int shot, int shootingKillCount)
    {
        Debug.Log($"{shooting} shot {shot}.");
        Leaderboard.Instance.UpdateCount(shooting, shootingKillCount);
    }

    public void RespondRespawn(int playerId)
    {
        if (!IsServerStarted)
            return;
        
        var player = Player.GetPlayer(playerId);
        if (!player)
            return;

        var handler = player.GetComponent<ChestMultiplayerExtension>();
        var owner = handler.Owner;

        handler.Despawn();
        StartRespawn(owner);
    }
    
    private void StartRespawn(NetworkConnection conn)
    {
        var timer = new CountdownTimer(respawnTime);
        timer.OnTimerStop += () => RespawnPlayer(conn);
        timer.Start();

        // tick from story
        StartCoroutine(TickTimer(timer));
    }
    
    private System.Collections.IEnumerator TickTimer(CountdownTimer timer)
    {
        while (!timer.IsFinished)
        {
            timer.Tick(Time.deltaTime);
            yield return null;
        }
    }

    private void RespawnPlayer(NetworkConnection conn)
    {
        Debug.Log($"Respawning player {conn.ClientId}");
        SpawnPlayer(conn);
    }
    
    // --------------------------
    // Leaderboard
    // --------------------------
    
    // for existing players
    [ObserversRpc]
    private void UpdateLeaderboardRpc(int playerId)
    {
        Debug.Log($"man {playerId}");
        Leaderboard.Instance.AddPlayerToLeaderboard(playerId);
    }
}
