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
    private int localPlayerId = -1;
    private bool neutralized;
    
    public void OnEnablePlayer()
    {
        // Multiplayer Guard
        _player = GetComponent<Player>();
        if (!_player.HasAuthority)
            return;
        
        // Get Assignables
        _animationsManager = GetComponent<AnimationsManager>();
        cam = _player.GetCamera().transform;
        _director = GetComponent<InputDirector>();
        localPlayerId = _player.PlayerId;
        
        // Subscribe to input events
        _director.OnFirePressed += OnFirePressed;

        // Reset cooldown
        cooldownTimer = new CountdownTimer(maxCooldown);
    }

    public void OnPlayerNeutralized()
    {
        Debug.Log("Player Gun Neutralized!");
        
        if (!_director)
            return;
        if (neutralized)
            return;
        neutralized = true;
        
        _director.OnFirePressed -= OnFirePressed;
    }

    public void OnDestroy()
    {
        OnPlayerNeutralized();
    }

    public void UpdatePlayer()
    {
        // Multiplayer Guard
        if (!_player.HasAuthority)
            return;
        
        respawnTimer?.Tick(Time.deltaTime);
        cooldownTimer?.Tick(Time.deltaTime);
    }
    
    private void OnFirePressed()
    {
        // Multiplayer Guard
        if (!_player.HasAuthority)
            return;
        
        if (!IsClientInitialized || !IsSpawned)
            return;
        
        // If there's still cooldown, don't shoot
        if (!cooldownTimer.IsFinished) return;
        cooldownTimer.Reset();
        cooldownTimer.Start();

        PerformShoot();
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

        // request shoot from server
        OnPlayerShootRpc(localPlayerId, cam.position, cam.forward);
    }

    [ServerRpc(RequireOwnership = true)]
    private void OnPlayerShootRpc(int localId, Vector3 camPosition, Vector3 camForward)
    {
        // calculate on server shoot result
        if (!IsServerStarted)
            return;
        
        Debug.Log($"Shooting requested. from {camPosition} to {camForward}");
        
        // using raycast gun
        Vector3 origin = camPosition + camForward * 0.65f; // to not hit self
        Ray ray = new Ray(origin, camForward);

        if (Physics.Raycast(ray, out var hit, maxRange))
        {
            // We hit a player
            if (hit.collider.TryGetComponent(out Player playerHit))
                KillPlayer(localId, playerHit);
        }
    }

    private void KillPlayer(int shootingPlayerId, Player playerHit)
    {
        // Despawn player
        var story = FindFirstObjectByType<ChestStory>();
        story.HandlePlayerKilled(shootingPlayerId, playerHit.PlayerId);
    }
}
