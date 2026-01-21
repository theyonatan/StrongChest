using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class ChestMultiplayerExtension : NetworkBehaviour
{
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
        GetComponent<RaycastGunMultiplayer>().SetupExtensionClient();
    }
    
    // On Death
    [TargetRpc]
    public void NeutrilizePlayerRpc(NetworkConnection conn)
    {
        var player = GetComponent<Player>();
        player.GetComponent<InputDirector>().DisableInput();
        player.DisablePlayerBehaviors();
        
        // respond
        RespondRespawnServerRpc();
    }
    
    [ServerRpc(RequireOwnership = true)]
    private void RespondRespawnServerRpc()
    {
        var story = FindFirstObjectByType<ChestStory>();
        story.RespondRespawn(GetComponent<Player>().PlayerId);
    }
}
