using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using OverallTimers;
using UnityEngine;

public class DeprecatedChestStory : NetworkBehaviour
{
    // ---- Network Connection ----
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float respawnTime = 5f;
    private MultiplayerManager _mm;
    
    private bool _playerInitialized;
    
    // ---- Combat ----
    private readonly Dictionary<string, int> _playerScores = new ();

    private void Start()
    {
        // var netManager = InstanceFinder.NetworkManager;
        // Debug.Log("isclient " + netManager.IsServerStarted);
        // Debug.Log("isserver " + netManager.IsServerStarted);
        // Debug.Log("ishost " + netManager.IsHostStarted);
        // Debug.Log("is authenticated " + netManager.ClientManager.Connection.IsAuthenticated);
        
        InstanceFinder.SceneManager.OnClientLoadedStartScenes += SceneManagerOnOnClientLoadedStartScenes;
        
        
        Debug.Log("cheststory start");
        // _playerInitialized = false;
        //
        // // subscribe to client connection events
        _mm = MultiplayerManager.Instance;
        // _mm.OnClientLoaded += OnClientLoaded;
        // _mm.OnClientDisconnected += OnClientDisconnected;
        
        // setup for host
        // StartCoroutine(SceneLoaded(NetworkManager.ClientManager.Connection));
    }
    
    private void SceneManagerOnOnClientLoadedStartScenes(NetworkConnection conn, bool isServer)
    {
        Debug.Log("am I server? " + isServer);
        Debug.Log("now authenticated? " + InstanceFinder.ClientManager.Connection.IsAuthenticated);
        Debug.Log("Does this know if I am a host? " + InstanceFinder.ClientManager.Connection.IsHost);
    }

    private IEnumerator SceneLoaded(NetworkConnection conn)
    {
        Debug.Log("isclient " + NetworkManager.IsServerStarted);
        Debug.Log("isserver " + NetworkManager.IsServerStarted);
        Debug.Log("ishost " + NetworkManager.IsHostStarted);
        Debug.Log("is authenticated " + conn.IsAuthenticated);
        
        while (!conn.IsAuthenticated)
        {
            Debug.Log("bro what:");
            Debug.Log(InstanceFinder.ClientManager.Connection.IsAuthenticated);
            yield return null;
        }
        while (!conn.LoadedStartScenes())
        {
            yield return null;
        }
        
        if (!NetworkManager.IsHostStarted)
        {
            Debug.Log("good");
            yield break;
        }

        OnHostConnected(NetworkManager.ClientManager.Connection);
    }

    private void OnDestroy()
    {
        // unsubscribe client connection events
        _mm.OnClientLoaded -= OnClientLoaded;
        _mm.OnClientDisconnected -= OnClientDisconnected;
    }

    /// <summary>
    /// this gets called for anyone that joins but host.
    /// </summary>
    /// <param name="connection"></param>
    private void OnClientLoaded(NetworkConnection connection)
    {
        Debug.Log("I am Host and I loaded: " + connection.IsHost);
        
        Debug.Log("loading client");
        if (_playerInitialized)
            return;
        _playerInitialized = true;
        Debug.Log("client loaded");
        
        // Setup new player
        var newPlayer = SpawnPlayer(connection);
        Debug.Log("Great!");
        // Leaderboard
        // if (Wind.Instance.TryGetUsernameForId(connection.ClientId, out var newPlayerUsername))
        // {
        //     _playerScores.Add(newPlayerUsername, 0);                       // update on server
        //     AddPlayerToLeaderboardRpc(newPlayerUsername);                  // update on existing clients
        //     newPlayer.FetchExistingLeaderboard(connection, _playerScores); // update on connected client
        // }
        // else
        //     Debug.LogError("username not found for id: " + connection.ClientId);
    }

    private void OnHostConnected(NetworkConnection connection)
    {
        Debug.Log("loading host");
        if (_playerInitialized)
            return;
        _playerInitialized = true;
        Debug.Log("host loaded");
        
        // Setup new player
        SpawnPlayer(connection);
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
    // Player combat events
    // --------------------------
    [Server]
    public void HandlePlayerKilled(int shootingPlayerId, int killedPlayerId)
    {
        var player = Player.GetPlayer(killedPlayerId);
        if (!player)
            return;
        
        // if (!Wind.Instance.TryGetUsernameForId(shootingPlayerId, out var shootingPlayerUsername)
        //     || !Wind.Instance.TryGetUsernameForId(shootingPlayerId, out var shotPlayerUsername))
        //     return;
        
        // _playerScores[shootingPlayerUsername] += 1;
        //
        // UpdateLeaderboardScoreRpc(
        //     shootingPlayerUsername,
        //     shotPlayerUsername,
        //     _playerScores[shootingPlayerUsername]);

        // disable player input on the client
        var handler = player.GetComponent<ChestMultiplayerExtension>();
        handler.NeutralizePlayerRpc(handler.Owner);
    }

    [ObserversRpc]
    private void UpdateLeaderboardScoreRpc(string shooting, string shot, int shootingKillCount)
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
    
    private IEnumerator TickTimer(CountdownTimer timer)
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
    private void AddPlayerToLeaderboardRpc(string playerUsername)
    {
        // Leaderboard.Instance.AddPlayerToLeaderboard(playerUsername);
    }
}
