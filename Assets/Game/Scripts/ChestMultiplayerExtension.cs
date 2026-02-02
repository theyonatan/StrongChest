using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class ChestMultiplayerExtension : NetworkBehaviour
{
    #region Setup

    [TargetRpc]
    public void SetupFPSPlayerRpc(NetworkConnection conn)
    {
        // give authority
        var player = GetComponent<Player>();
        player.PlayerId = OwnerId;
        player.SetAuthority(true);
        player.EnablePlayerBehaviors();
        
        // Load State
        player.Awake();
        player.OnEnable();
        player.Start();
        
        player.SwapPlayerState<cc_fpState, FP_CameraState>();
        
        // load extensions too
        GetComponent<RaycastGunMultiplayer>().OnEnablePlayer();
        
        FindFirstObjectByType<RespawnScreen>().HideScreen();
    }

    [SerializeField] private TextMeshPro usernameText;
    
    /// <summary>
    /// for all clients, updates the username on this player.
    /// </summary>
    [ObserversRpc]
    public void UpdateUsernameOnPlayer(string username)
    {
        // update for everyone but self
        if (GetComponent<Player>().HasAuthority)
            return;
        
        usernameText.text = username;
    }

    #endregion

    #region GameCycle

    // On Death
    [TargetRpc]
    public void NeutralizePlayerRpc(NetworkConnection conn)
    {
        var player = GetComponent<Player>();
        player.GetComponent<InputDirector>().DisableInput();
        player.DisablePlayerBehaviors();
        FindFirstObjectByType<RespawnScreen>().ShowScreen();
        GetComponent<RaycastGunMultiplayer>().OnPlayerNeutralized();
        
        // respond
        ConfirmNeutralizedRpc();
    }
    
    [ServerRpc(RequireOwnership = true)]
    private void ConfirmNeutralizedRpc()
    {
        if (!IsServerStarted)
            return;
        
        var story = FindFirstObjectByType<ChestStory>();
        story.ConfirmNeutralized(GetComponent<Player>().PlayerId);
    }

    #endregion
}
