using FishNet.Connection;
using FishNet.Object;
using OverallTimers;
using UnityEngine;

public class ChestStory : NetworkBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float respawnTime = 5f;
    private MultiplayerManager _mm;

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

    public void OnClientConnected(NetworkConnection connection)
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
        
        // tell the client to set itself up locally
        spawnedPlayer.GetComponent<ChestMultiplayerExtension>().SetupFPSPlayerRpc(connection);
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
    
    [Server]
    public void HandlePlayerKilled(int playerId)
    {
        var player = Player.GetPlayer(playerId);
        if (!player)
            return;

        // disable player input on the client
        var handler = player.GetComponent<ChestMultiplayerExtension>();
        handler.NeutrilizePlayerRpc(handler.Owner);
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
        OnClientConnected(conn);
    }
}
