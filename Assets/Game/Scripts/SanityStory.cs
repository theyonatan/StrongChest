using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using UnityEngine;

public class SanityStory : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Start always gets called");
        Debug.Log("authenticated " + InstanceFinder.ClientManager.Connection.IsAuthenticated);
        
        InstanceFinder.SceneManager.OnClientLoadedStartScenes += SceneManagerOnOnClientLoadedStartScenes;
        // var netManager = InstanceFinder.NetworkManager;
        // Debug.Log("isclient " + netManager.IsServerStarted);
        // Debug.Log("isserver " + netManager.IsServerStarted);
        // Debug.Log("ishost " + netManager.IsHostStarted);
        // Debug.Log("is authenticated " + netManager.ClientManager.Connection.IsAuthenticated);
    }

    private void SceneManagerOnOnClientLoadedStartScenes(NetworkConnection conn, bool isServer)
    {
        Debug.Log("am I server? " + isServer);
        Debug.Log("now authenticated? " + InstanceFinder.ClientManager.Connection.IsAuthenticated);
        Debug.Log("Does this know if I am a host? " + InstanceFinder.ClientManager.Connection.IsHost);
    }
}
