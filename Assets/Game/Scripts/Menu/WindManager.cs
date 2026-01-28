using System.Collections.Generic;
using FishNet.Object;

/// <summary>
/// What goes between scenes in a multiplayer environment
/// </summary>
public class WindManager : NetworkBehaviour
{
    private readonly HashSet<string> _usernames = new();
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
    
    public void AddUsername(string username) => _usernames.Add(username);
    
    public void RemoveUsername(string username) => _usernames.Remove(username);
}
