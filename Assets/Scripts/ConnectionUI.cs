using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using System.Text;
using TMPro;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private TMP_InputField _ipInput;

    public static string PlayerNickname { get; private set; } = "Player";
    public static bool IsConnected { get; private set; } = false;

    private void Start()
    {
        if (_networkManager == null)
        {
            _networkManager = FindObjectOfType<NetworkManager>();
            if (_networkManager == null)
            {
                Debug.LogError("[ConnectionUI] NetworkManager не найден ни в инспекторе, ни на сцене!");
                return;
            }
            Debug.LogWarning("[ConnectionUI] NetworkManager найден автоматически. Рекомендуется назначить в инспекторе.");
        }

        _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;

        Debug.Log("[ConnectionUI] Инициализирован. Ожидание действий...");
    }

    private void OnDestroy()
    {
        if (_networkManager != null)
        {
            _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
            _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        }
    }

    public void StartAsClient()
    {
        SaveNickname();
        string serverIp = _ipInput != null ? _ipInput.text.Trim() : "127.0.0.1";
        if (string.IsNullOrEmpty(serverIp)) serverIp = "127.0.0.1";

        ConfigureClientTransport(serverIp, 7770);
        _networkManager.ClientManager.StartConnection();
        Debug.Log($"[ConnectionUI] Клиент пытается подключиться к {serverIp}:7770. Ник: {PlayerNickname}");
    }

    public void StartAsHost()
    {
        SaveNickname();

        var tugboat = _networkManager.GetComponent<Tugboat>();
        if (tugboat == null)
        {
            Debug.LogError("[ConnectionUI] Tugboat не найден!");
            return;
        }

        tugboat.SetServerBindAddress("0.0.0.0", IPAddressType.IPv4);
        tugboat.SetPort(7770);

        _networkManager.ServerManager.StartConnection();

        tugboat.SetClientAddress("127.0.0.1");
        _networkManager.ClientManager.StartConnection();

        Debug.Log($"[ConnectionUI] Хост запущен. Ник: {PlayerNickname}");
    }

    private void ConfigureClientTransport(string ip, ushort port)
    {
        var tugboat = _networkManager.GetComponent<Tugboat>();
        if (tugboat == null)
        {
            Debug.LogError("[ConnectionUI] Tugboat не найден!");
            return;
        }

        tugboat.SetClientAddress(ip);
        tugboat.SetPort(port);
    }

    private void SaveNickname()
    {
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            IsConnected = true;
            Debug.Log($"[ConnectionUI] Подключён! ClientID: {_networkManager.ClientManager.Connection.ClientId}");
            if (_menuPanel != null) _menuPanel.SetActive(false);
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            IsConnected = false;
            Debug.Log("[ConnectionUI] Клиент остановлен.");
            if (_menuPanel != null) _menuPanel.SetActive(true);
        }
        else if (args.ConnectionState == LocalConnectionState.Stopping)
        {
            Debug.Log("[ConnectionUI] Клиент отключается...");
        }
    }

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            IsConnected = false;
            Debug.Log("[ConnectionUI] Сервер остановлен");
            if (_menuPanel != null) _menuPanel.SetActive(true);
        }
    }
}
