using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] public int RequiredPlayers = 2;
    [SerializeField] private float _matchDuration = 60f;

    public readonly SyncVar<GameState> CurrentState = new SyncVar<GameState>(GameState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);
    public readonly SyncVar<float> MatchTimer = new SyncVar<float>(60f);

    public enum GameState
    {
        WaitingForPlayers,
        InProgress,
        ShowingResults
    }

    private void Awake()
    {
        CurrentState.OnChange += OnStateChanged;
        ConnectedPlayers.OnChange += OnConnectedPlayersChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        ClientManager.OnClientConnectionState += OnClientConnectionState;
        MatchTimer.Value = _matchDuration;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (ServerManager != null)
            ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        if (ClientManager != null)
            ClientManager.OnClientConnectionState -= OnClientConnectionState;
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        ConnectedPlayers.Value = ServerManager.Clients.Count;
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started ||
            args.ConnectionState == LocalConnectionState.Stopped)
        {
            ConnectedPlayers.Value = ServerManager.Clients.Count;
        }
    }

    private void OnConnectedPlayersChanged(int oldVal, int newVal, bool asServer)
    {
        if (asServer)
        {
            Debug.Log($"[GameManager] Players changed: {oldVal} -> {newVal}");
            TryStartMatch();
        }
    }

    private void OnStateChanged(GameState oldVal, GameState newVal, bool asServer)
    {
        Debug.Log($"[GameManager] State: {oldVal} -> {newVal}");
    }

    private void TryStartMatch()
    {
        if (CurrentState.Value == GameState.WaitingForPlayers && ConnectedPlayers.Value >= RequiredPlayers)
        {
            StartMatch();
        }
    }

    private void StartMatch()
    {
        CurrentState.Value = GameState.InProgress;
        MatchTimer.Value = _matchDuration;
        Debug.Log("[GameManager] Match started");
    }

    private void Update()
    {
        if (!IsServerStarted || CurrentState.Value != GameState.InProgress) return;

        MatchTimer.Value -= Time.deltaTime;
        if (MatchTimer.Value <= 0f)
            EndMatch();
    }

    private void EndMatch()
    {
        CurrentState.Value = GameState.ShowingResults;
        Debug.Log("[GameManager] Match ended");
        Invoke(nameof(ResetToLobby), 5f);
    }

    private void ResetToLobby()
    {
        foreach (NetworkConnection conn in ServerManager.Clients.Values)
        {
            foreach (NetworkObject nob in conn.Objects)
            {
                if (nob.TryGetComponent<PlayerNetwork>(out var pn))
                {
                    pn.ForceRespawnServer();
                }
            }
        }
        MatchTimer.Value = _matchDuration;
        CurrentState.Value = GameState.WaitingForPlayers;
    }
}
