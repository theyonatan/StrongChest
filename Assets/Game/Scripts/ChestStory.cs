using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class ChestStory : NetworkBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform spawnPoint;
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

    private void OnClientConnected(NetworkConnection connection)
    {
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
}
