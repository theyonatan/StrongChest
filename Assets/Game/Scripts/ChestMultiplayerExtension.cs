using FishNet.Connection;
using FishNet.Object;

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
}
