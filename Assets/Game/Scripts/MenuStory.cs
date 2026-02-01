using System.Net;
using System.Net.Sockets;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using UnityEngine;

public class MenuStory : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_InputField ipField;
    [SerializeField] private TMPro.TMP_InputField usernameField;
    [SerializeField] private TMPro.TextMeshProUGUI usernameErrorText;
    
    [SerializeField] private TMPro.TextMeshProUGUI displayIpText;

    private NetworkManager _networkManager;
    
    private string _chosenUsername;
    private bool _isConnecting;

    private void Awake()
    {
        _networkManager = InstanceFinder.NetworkManager;
        usernameErrorText.text = "";
        displayIpText.text = "";
        
        _networkManager.GetComponent<UsernameAuthenticator>()
            .StoryOnAuthenticationResult.AddListener(OnUsernameResult);
    }

    public void StartHost()
    {
        // server initialized
        _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnOnServerConnectionState;
        
        // Start Host Connection
        _networkManager.ServerManager.StartConnection();
        
        // Show Ip
        displayIpText.text = $"IP: {GetLocalIPv4()}";
        
        // Start Client Connection
        StartClient();
    }

    private void ServerManager_OnOnServerConnectionState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState != LocalConnectionState.Started)
            return;
        
        // Load game scene for all clients
        SceneLoadData sceneLoadData = new SceneLoadData("game") {
            ReplaceScenes = ReplaceOption.All
        };
        _networkManager.SceneManager.LoadGlobalScenes(sceneLoadData);
    }

    public void StartClient()
    {
        // Guard connection attempt
        if (_isConnecting) return;
        _isConnecting = true;
        
        // collect ip and username data
        string ip = ipField.text;
        if (string.IsNullOrEmpty(ip))
        {
            Debug.Log("ip can't be empty!");
            return;
        }
        _chosenUsername = usernameField.text.Trim();
        usernameErrorText.text = "";

        // verify username
        if (string.IsNullOrEmpty(_chosenUsername))
        {
            usernameErrorText.text = "Username is empty.";
            _isConnecting = false;
            return;
        }
        
        // request username from server
        _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        _networkManager.ClientManager.StartConnection(ip);
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // auto approve host
            if (InstanceFinder.IsHostStarted)
            {
                Debug.Log("oh, host! auto approved.");
                return;
            }
            
            // send username request to server
            UsernameRequest req = new() { Username = _chosenUsername };
            InstanceFinder.ClientManager.Broadcast(req);
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            Debug.Log("Disconnected (probably rejected)");
        }
    }

    private void OnUsernameResult(bool available, string message)
    {
        _isConnecting = false;

        if (!available)
        {
            usernameErrorText.text = message;
            
            // disconnect if username is bad
            if (_networkManager != null)
                _networkManager.ClientManager.StopConnection();
        }
        
        if (available)
        {
            // Confirm username locally
            Debug.Log("username received. connection success!");
            usernameErrorText.text = "Connection Successful";
        }
    }

    private static string GetLocalIPv4()
    {
        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "Unknown";
    }
}
