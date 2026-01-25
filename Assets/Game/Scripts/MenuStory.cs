using System.Collections;
using System.Net;
using System.Net.Sockets;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.LowLevelPhysics;

public class MenuStory : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_InputField ipField;
    [SerializeField] private TMPro.TMP_InputField usernameField;
    [SerializeField] private TMPro.TextMeshProUGUI usernameErrorText;
    
    [SerializeField] private TMPro.TextMeshProUGUI displayIpText;

    [SerializeField] private WindManager windManagerPrefab;
    
    private NetworkManager _networkManager;
    
    private string _chosenUsername;
    private bool _isConnecting;

    private void Awake()
    {
        _networkManager = InstanceFinder.NetworkManager;
        usernameErrorText.text = "";
        displayIpText.text = "";
    }

    public void StartHost()
    {
        // Start Host
        _networkManager.ServerManager.StartConnection();
        
        // Show Ip
        displayIpText.text = $"IP: {GetLocalIPv4()}";
        
        StartClient();
        // On Server Start
        // _networkManager.ServerManager.OnServerConnectionState += args =>
        // {
        //     if (args.ConnectionState == LocalConnectionState.Started)
        //         OnServerStarted();
        // };
    }

    private void OnServerStarted()
    {
        // Spawn wind manager on the server, so it persist across scenes
        if (_networkManager != null && _networkManager.IsServerStarted)
        {
            if (WindManager.Instance != null)
                return;
            
            var wind = Instantiate(windManagerPrefab);
            _networkManager.ServerManager.Spawn(wind);
        }
        
        // self username
        var username = usernameField.text;
        // StartCoroutine(WaitConnectedThenRequestUsername(username));
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
        Debug.Log($"bro {message}");
        // Unsubscribe from event
        if (WindManager.Instance)
            WindManager.Instance.OnUsernameResult -= OnUsernameResult;
        
        _isConnecting = false;

        // unavailable
        if (!available)
        {
            Debug.Log("Bad");
            usernameErrorText.text = message;
            
            // disconnect if username is bad
            if (_networkManager != null)
                _networkManager.ClientManager.StopConnection();
            
        }
        
        // available
        if (available)
        {
            Debug.Log("Good");
            // Confirm username locally
            PlayerWindManager.ChosenUsername = usernameField.text.Trim();
            usernameErrorText.text = "Connection Successful";
            
            // Load scene for new client
            SceneLoadData sceneLoadData = new SceneLoadData("game") {
                ReplaceScenes = ReplaceOption.All
            };
            
            _networkManager.SceneManager.LoadGlobalScenes(sceneLoadData);
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
