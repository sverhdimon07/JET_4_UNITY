using TMPro;
using UnityEngine;
using System.Text;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private TMP_Text _playerCountText;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private GameObject _waitingPanel;
    [SerializeField] private GameObject _resultsPanel;
    [SerializeField] private TMP_Text _resultsText;

    private GameManager _gameManager;

    private void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
        if (_gameManager == null) return;

        _gameManager.CurrentState.OnChange += OnStateChanged;
        _gameManager.ConnectedPlayers.OnChange += OnPlayerCountChanged;
        _gameManager.MatchTimer.OnChange += OnTimerChanged;

        OnStateChanged(_gameManager.CurrentState.Value, _gameManager.CurrentState.Value, false);
        OnPlayerCountChanged(0, _gameManager.ConnectedPlayers.Value, false);
        OnTimerChanged(0, _gameManager.MatchTimer.Value, false);
    }

    private void OnDestroy()
    {
        if (_gameManager != null)
        {
            _gameManager.CurrentState.OnChange -= OnStateChanged;
            _gameManager.ConnectedPlayers.OnChange -= OnPlayerCountChanged;
            _gameManager.MatchTimer.OnChange -= OnTimerChanged;
        }
    }

    private void OnStateChanged(GameManager.GameState oldVal, GameManager.GameState newVal, bool asServer)
    {
        switch (newVal)
        {
            case GameManager.GameState.WaitingForPlayers:
                _waitingPanel.SetActive(true);
                _resultsPanel.SetActive(false);
                if (_statusText) _statusText.text = "Waiting for players...";
                break;
            case GameManager.GameState.InProgress:
                _waitingPanel.SetActive(false);
                _resultsPanel.SetActive(false);
                break;
            case GameManager.GameState.ShowingResults:
                _resultsPanel.SetActive(true);
                StringBuilder sb = new StringBuilder("Match Over!\n");
                foreach (PlayerNetwork pn in FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None))
                {
                    sb.AppendLine($"{pn.Nickname.Value}: {pn.Score.Value} kills");
                }
                _resultsText.text = sb.ToString();
                break;
        }
    }

    private void OnPlayerCountChanged(int oldVal, int newVal, bool asServer)
    {
        if (_playerCountText)
            _playerCountText.text = $"Players: {newVal}/{_gameManager.RequiredPlayers}";
    }

    private void OnTimerChanged(float oldVal, float newVal, bool asServer)
    {
        if (_timerText)
            _timerText.text = $"Time: {Mathf.CeilToInt(newVal)}s";
    }
}
