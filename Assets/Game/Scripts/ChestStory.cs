using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class ChestStory : MonoBehaviour
{
    private Dictionary<int, OnlinePlayerData> _players = new ();
    private ChestGameManager _gameManager;
    
    #region NetworkEvents
    
    [SerializeField] private ChestGameManager chestGameManagerPrefab;
    
    void Start()
    {
        _players = new Dictionary<int, OnlinePlayerData>();
        
        InstanceFinder.SceneManager.OnClientLoadedStartScenes += SceneManager_OnOnClientLoadedStartScenes;
    }
    
    private void OnDestroy()
    {
        if (InstanceFinder.SceneManager)
            InstanceFinder.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnOnClientLoadedStartScenes;
    }

    
    /// <summary>
    /// This function sets up new clients that join.
    /// can be called on the client when they finished connecting
    /// here I only run code from the server (I know the new client finished connecting here)
    /// - so I can set the new client up and start sending rpc's to it.
    /// </summary>
    private void SceneManager_OnOnClientLoadedStartScenes(NetworkConnection conn, bool isServer)
    {
        Debug.Log($"New connection {conn.ClientId} joined - Scene load finished.");

        if (!isServer)
        {
            Debug.LogWarning("connection with server loading, not running setup as server.");
            return;
        }
        
        // verify Host
        bool isHost = InstanceFinder.ClientManager.Connection.IsHost
            && conn.IsLocalClient;
        Debug.Log($"Setting up scene for {(isHost ? "Host" : "Regular Player")}");

        // spawn scene network objects
        _gameManager = Instantiate(chestGameManagerPrefab);
        InstanceFinder.ServerManager.Spawn(_gameManager);
        
        // Setup Game
        SetupPlayerInScene(conn);
    }

    #endregion

    #region GameSetup

    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    
    private void SetupPlayerInScene(NetworkConnection conn)
    {
        if (_players.ContainsKey(conn.ClientId))
        {
            Debug.LogWarning($"Client {conn.ClientId} already setup. skipping.");
            return;
        }
            
        // Spawn Player
        var newPlayer = SpawnPlayerToGame(conn);
        var newPlayerReference = newPlayer.GetComponent<Player>();
        var newPlayerUsername = Wind.Instance.GetUsernameForId(conn.ClientId);
        
        // Add to local list
        OnlinePlayerData newPlayerData = new OnlinePlayerData()
        {
            ClientConnection = conn,
            PlayerReference = newPlayerReference,
            
            ChestMultiplayerExtension = newPlayer,
            Username = newPlayerUsername,
            Score = 0,
        };
        
        _players.Add(conn.ClientId, newPlayerData);
        
        // Leaderboard
        var leaderboardDict = _players.ToDictionary(
            k => k.Value.Username,
            v => v.Value.Score);
        
        _gameManager.RebuildLeaderboardRpc(leaderboardDict); // Update leaderboard for all players
    }
    
    private ChestMultiplayerExtension SpawnPlayerToGame(NetworkConnection connection)
    {
        // choose random spawn point to start at
        var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        // spawn new player online
        var spawnedPlayer = SpawnPlayerOnline(
            connection,
            spawnPoint.position,
            Quaternion.identity);
        
        spawnedPlayer.PlayerId = connection.ClientId;
        var newPlayer = spawnedPlayer.GetComponent<ChestMultiplayerExtension>();
        
        // tell the client to set itself up locally
        newPlayer.SetupFPSPlayerRpc(connection);

        return newPlayer;
    }

    #endregion

    #region PlayerSpawning

    private Player SpawnPlayerOnline(
        NetworkConnection spawningClientConnection,
        Vector3 position,
        Quaternion rotation)
    {
        if (!playerPrefab || spawningClientConnection == null) return null;
        Debug.Log($"Spawning Player for client {spawningClientConnection.ClientId}");

        NetworkObject spawnedPlayer = Instantiate(playerPrefab, position, rotation);
        InstanceFinder.ServerManager.Spawn(spawnedPlayer, spawningClientConnection);
        
        // Self Authority is given locally.

        return spawnedPlayer.GetComponent<Player>();
    }

    public void DespawnPlayer(Player player)
    {
        if (player == null) return;
        Debug.Log($"Despawning Player for client {player.PlayerId}");
        
        InstanceFinder.ServerManager.Despawn(player.gameObject);
    }

    #endregion

    #region GameCycle

    [SerializeField] private float respawnTime = 5f;
    

    #endregion
}
