using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Authenticating;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.Events;

public class UsernameAuthenticator : Authenticator
{
    public override event Action<NetworkConnection, bool> OnAuthenticationResult;
    public UnityEvent<bool, string> StoryOnAuthenticationResult = new ();
    
    private readonly HashSet<string> _takenUsernames = new (StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public override void InitializeOnce(NetworkManager networkManager)
    {
        base.InitializeOnce(networkManager);
        
        // Server: listen for username requests
        networkManager.ServerManager.RegisterBroadcast<UsernameRequest>(OnReceiveUsernameRequest, false);
        
        // client: listen for result
        networkManager.ClientManager.RegisterBroadcast<UsernameResponse>(OnReceiveResponse);
    }
    
    /// <summary>
    /// Server: receives username request from client
    /// </summary>
    private void OnReceiveUsernameRequest(NetworkConnection conn, UsernameRequest request, Channel channel)
    {
        // verify username
        string username = request.Username?.Trim();
        bool success = !string.IsNullOrWhiteSpace(username) && !_takenUsernames.Contains(username);
        if (success)
        {
            _takenUsernames.Add(username);
            conn.CustomData = username;
        }
        
        string message = success ? "Connection Success"  : "Username taken or invalid!";
        
        // response with the result
        UsernameResponse response = new () { Success = success, Message = message };
        InstanceFinder.ServerManager.Broadcast(conn, response, false);
        
        // approve / reject connection
        OnAuthenticationResult?.Invoke(conn, success);
    }
    
    // client: receives result
    private void OnReceiveResponse(UsernameResponse response, Channel channel)
    {
        if (response.Success)
            Debug.Log(response.Message);
        else
            Debug.LogWarning($"Login failed: {response.Message}");
        
        StoryOnAuthenticationResult?.Invoke(response.Success, response.Message);
    }
    
    // cleanup for disconnect
    private void OnEnable()
    {
        InstanceFinder.ServerManager.OnRemoteConnectionState += ServerManager_OnOnRemoteConnectionState;
    }

    private void OnDisable()
    {
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnRemoteConnectionState -= ServerManager_OnOnRemoteConnectionState;
    }

    private void ServerManager_OnOnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Stopped && conn.CustomData is string username)
            _takenUsernames.Remove(username);
    }
}
