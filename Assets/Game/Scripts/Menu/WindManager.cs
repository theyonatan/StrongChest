using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// What goes between scenes in a multiplayer environment
/// </summary>
public class WindManager : NetworkBehaviour
{
    private readonly HashSet<string> _taken = new();
    public static WindManager Instance { get; private set; }

    #region Singleton

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        
        if (Instance == this) Instance = null;
    }

    #endregion
    
    // --------------------------
    // Username check
    // --------------------------
    public UnityAction<bool, string> OnUsernameResult; // client gets result
    
    [ServerRpc(RequireOwnership = false)]
    public void CheckUsernameServerRpc(string requestedName, NetworkConnection conn = null)
    {
        if (conn == null || !conn.IsValid)
            return;
        
        requestedName = (requestedName ?? "").Trim();

        Debug.Log("looking for username");
        if (string.IsNullOrEmpty(requestedName))
        {
            UsernameResultTargetRpc(conn, false, "Username is empty");
            return;
        }

        if (_taken.Contains(requestedName))
        {
            UsernameResultTargetRpc(conn, false, "Username already taken in requested server");
            return;
        }
        
        Debug.Log("ok approved");
        _taken.Add(requestedName);
        UsernameResultTargetRpc(conn, true, "");
    }

    [TargetRpc]
    private void UsernameResultTargetRpc(NetworkConnection conn, bool available, string message)
    {
        // Menu story will listen to this
        OnUsernameResult?.Invoke(available, message);
    }
}
