using FishNet.Broadcast;
using FishNet.Serializing;

[System.Serializable]
public struct UsernameRequest : IBroadcast
{
    public string Username;
}

[System.Serializable]
public struct UsernameResponse : IBroadcast
{
    public bool Success;
    public string Message;
}
