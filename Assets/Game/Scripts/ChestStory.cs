using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

public class ChestStory : MonoBehaviour
{
    ///<summary>data about each player via ClientId</summary>
    private Dictionary<int, OnlinePlayerData> _players = new();

    ///<summary>Network Communication Handler for the game</summary>
    private ChestGameManager _gameManager;

    #region NetworkEvents

    [SerializeField] private ChestGameManager chestGameManagerPrefab;

    void Start()
    {
        _players = new Dictionary<int, OnlinePlayerData>();

        InstanceFinder.SceneManager.OnClientLoadedStartScenes += SceneManager_OnOnClientLoadedStartScenes;
        InstanceFinder.ServerManager.OnRemoteConnectionState += ServerManager_OnOnRemoteConnectionState;
    }

    private void ServerManager_OnOnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        // player left this game scene
        if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            _players.Remove(conn.ClientId);
        }
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
            Debug.Log("connection with server loading, not running setup as server.");
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
        SetupNewPlayer(conn);
    }

    #endregion

    #region GameSetup

    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private void SetupNewPlayer(NetworkConnection conn)
    {
        // verify new
        if (_players.ContainsKey(conn.ClientId))
        {
            Debug.LogWarning($"Client {conn.ClientId} already setup. skipping.");
            return;
        }
        
        // Add new to players list
        OnlinePlayerData newPlayerData = new OnlinePlayerData()
        {
            ClientConnection = conn,
            Score = 0,
        };
        _players.Add(conn.ClientId, newPlayerData);

        // Spawn Player
        var newPlayer = SpawnPlayerToGame(conn);
        var newPlayerReference = newPlayer.GetComponent<Player>();
        var newPlayerUsername = Wind.Instance.GetUsernameForId(conn.ClientId);
        newPlayerReference.PlayerId = conn.ClientId;
        
        // Update Username of all players
        _players[conn.ClientId].Username = newPlayerUsername;
        foreach (var playerHandler in _players.Values)
            playerHandler.ChestMultiplayerExtension.UpdateUsernameOnPlayer(newPlayerUsername);

        // Leaderboard
        var leaderboardDict = _players.ToDictionary(
            k => k.Value.Username,
            v => v.Value.Score);

        _gameManager.RebuildLeaderboardRpc(leaderboardDict); // Update leaderboard for all players
    }

    /// <summary>
    /// Call to spawn a player to the game
    /// </summary>
    /// <param name="connection">Connection to own the player</param>
    /// <returns></returns>
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

        // save player data
        _players[connection.ClientId].PlayerReference = spawnedPlayer;
        _players[connection.ClientId].ChestMultiplayerExtension = newPlayer;
        
        // tell the client to set itself up locally
        newPlayer.SetupFPSPlayerRpc(connection);
        
        // also set up it's username for everyone
        var newPlayerUsername = Wind.Instance.GetUsernameForId(connection.ClientId);
        newPlayer.UpdateUsernameOnPlayer(newPlayerUsername);
        
        return newPlayer;
    }

    #endregion

    #region PlayerSpawning

    /// <summary>
    /// Used to spawn a new player in the scene.
    /// Should not be called without handling further game context.
    /// </summary>
    /// <returns>Player runner</returns>
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

    public void HandlePlayerKilled(int shooterPlayerId, int killedPlayerId)
    {
        // verify killed player
        if (!_players.TryGetValue(killedPlayerId, out var killedPlayer))
            return;

        // update leaderboard
        _players[shooterPlayerId].Score += 1;
        var playerShooterScore = _players[shooterPlayerId].Score;
        var playerShooterUsername = Wind.Instance.GetUsernameForId(shooterPlayerId);
        var playerKilledUsername = Wind.Instance.GetUsernameForId(killedPlayerId);

        Debug.Log(_gameManager.IsSpawned);
        _gameManager.UpdateLeaderboardScoreRpc(
            playerShooterUsername,
            playerKilledUsername,
            playerShooterScore);
        Debug.Log(_gameManager.isActiveAndEnabled);

        // Neutralize killed player
        var handler = killedPlayer.ChestMultiplayerExtension;
        handler.NeutralizePlayerRpc(killedPlayer.ClientConnection);
    }

    public void ConfirmNeutralized(int playerId)
    {
        var handler = _players[playerId].ChestMultiplayerExtension;
        var owner = _players[playerId].ClientConnection;

        InstanceFinder.ServerManager.Despawn(handler);
        StartCoroutine(RespawnClient(owner));
    }

    private IEnumerator RespawnClient(NetworkConnection conn)
    {
        // respawn cooldown
        yield return new WaitForSeconds(respawnTime);
        
        // spawn new player
        SpawnPlayerToGame(conn);
    }

#endregion
}
