using FishNet.Connection;
using FishNet.Object;
using OverallTimers;
using UnityEngine;

/// <summary>
/// This does not work with the original offline gun extensions.
/// requires its own on hit detections.
/// </summary>
public class RaycastGunMultiplayer : NetworkBehaviour, IPlayerBehavior
{
    [Header("Gun Stats")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float maxCooldown = 0.5f;

    private CountdownTimer cooldownTimer;
    private Transform cam;
    private Player _player;
    private InputDirector _director;
    
    [Header("Raycast Gun Settings")]
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private LayerMask hitLayers;
    private AnimationsManager _animationsManager;

    [Header("Multiplayer Settings")]
    private CountdownTimer respawnTimer;
    private float respawnTime = 5f;
    
    public void SetupExtensionClient()
    {
        // Multiplayer Guard
        _player = GetComponent<Player>();
        if (!_player.HasAuthority)
            return;
        
        // Get Assignables
        _animationsManager = GetComponent<AnimationsManager>();
        cam = _player.GetCamera().transform;
        _director = GetComponent<InputDirector>();
        
        // Subscribe to input events
        _director.OnFirePressed += OnFirePressed;

        // Reset cooldown
        cooldownTimer = new CountdownTimer(maxCooldown);
    }

    public void UpdatePlayer()
    {
        // Multiplayer Guard
        if (!_player.HasAuthority)
            return;
        
        respawnTimer?.Tick(Time.deltaTime);
        cooldownTimer?.Tick(Time.deltaTime);
    }

    private void PerformShoot()
    {
        if (cam == null)
        {
            Debug.LogWarning("RaycastGun: Missing camera reference.");
            return;
        }

        // Player animations
        // if (_animationsManager != null)
        // {
        //     _animationsManager.Play("Shoot");
        // }

        // send rpc to server
        OnPlayerShootRpc(cam.position, cam.forward);
    }
    
    private void OnFirePressed()
    {
        // Multiplayer Guard
        if (!_player.HasAuthority)
            return;
        
        // If there's still cooldown, don't shoot
        if (!cooldownTimer.IsFinished) return;
        cooldownTimer.Reset();
        cooldownTimer.Start();

        PerformShoot();
    }

    [ServerRpc(RequireOwnership = true)]
    private void OnPlayerShootRpc(Vector3 camPosition, Vector3 camForward)
    {
        if (!IsServerStarted)
            return;
        
        // log for self
        Debug.Log($"Doing some calculations. from {camPosition} to {camForward}");
        
        // rpc
        Vector3 origin = camPosition + camForward * 0.65f; // to not hit self
        Ray ray = new Ray(origin, camForward);

        if (Physics.Raycast(ray, out var hit, maxRange))
        {
            // We hit a player
            if (hit.collider.TryGetComponent(out Player playerHit))
                KillPlayer(playerHit);
        }
    }

    private void KillPlayer(Player playerHit)
    {
        // reply shoot Result to all clients
        ResultOfShootRpc(playerHit.PlayerId);
        
        // Despawn player
        var story = FindFirstObjectByType<ChestStory>();
        story.HandlePlayerKilled(playerHit.PlayerId);
    }

    [ObserversRpc]
    private void ResultOfShootRpc(int hitId)
    {
        Debug.Log($"Someone shot {hitId}!");
    }
}
