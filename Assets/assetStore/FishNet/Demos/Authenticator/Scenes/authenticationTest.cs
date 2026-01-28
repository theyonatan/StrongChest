using FishNet;
using FishNet.Managing;
using UnityEngine;

public class authenticationTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("client ready " + InstanceFinder.IsClientStarted);
        Debug.Log("authenticated " + InstanceFinder.ClientManager.Connection.IsAuthenticated);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
