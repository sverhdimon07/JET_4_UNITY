using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System.Collections;
using System.Linq;
using FishNet.Connection;

[RequireComponent(typeof(NetworkObject))]
public class PlayerNetwork : NetworkBehaviour
{
    public event System.Action<string> OnNicknameChanged;
    public event System.Action<int> OnHpChanged;

    public readonly SyncVar<string> Nickname = new SyncVar<string>("Player");
    public readonly SyncVar<int> HP = new SyncVar<int>(100);
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);
    public readonly SyncVar<int> Score = new SyncVar<int>(0);

    [SerializeField] private GameObject _playerModel;

    private Transform[] _spawnPoints;
    private Coroutine _respawnCoroutine;

    private void Awake()
    {
        Nickname.OnChange += OnNicknameValueChanged;
        HP.OnChange += OnHpValueChanged;
        IsAlive.OnChange += OnIsAliveValueChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        FindSpawnPoints();

        Vector3 spawnPos = GetRandomSpawnPosition();
        transform.position = spawnPos;
        transform.rotation = Quaternion.identity;
        Debug.Log($"[Spawn] Initial Server Position set to {spawnPos}");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
    }

    private void FindSpawnPoints()
    {
        GameObject[] spawns = GameObject.FindGameObjectsWithTag("SpawnPoint");

        if (spawns.Length == 0)
        {
            Debug.LogError("[Spawn] ОШИБКА: Объекты с тегом 'SpawnPoint' не найдены на сцене!");
            _spawnPoints = null;
        }
        else
        {
            _spawnPoints = spawns.Select(go => go.transform).ToArray();
            Debug.Log($"[Spawn] Успешно найдено точек спавна: {_spawnPoints.Length}");
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            return _spawnPoints[Random.Range(0, _spawnPoints.Length)].position;
        }

        return new Vector3(0f, 5f, 0f);
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        if (_respawnCoroutine != null)
        {
            StopCoroutine(_respawnCoroutine);
            _respawnCoroutine = null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitNicknameServerRpc(string nickname)
    {
        string safe = string.IsNullOrWhiteSpace(nickname) ? $"Player_{OwnerId}" : nickname.Trim();
        Nickname.Value = safe;
    }

    private void OnNicknameValueChanged(string prev, string next, bool asServer)
    {
        OnNicknameChanged?.Invoke(next);
    }

    private void OnHpValueChanged(int prev, int next, bool asServer)
    {
        OnHpChanged?.Invoke(next);

        if (next <= 0 && IsAlive.Value)
        {
            if (asServer || base.IsServerStarted)
                Die();
        }
    }

    private void OnIsAliveValueChanged(bool prev, bool next, bool asServer)
    {
        if (_playerModel) _playerModel.SetActive(next);

        var cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = next;

        var movement = GetComponent<PlayerMovement>();
        if (movement) movement.enabled = next;

        var shooting = GetComponent<PlayerShooting>();
        if (shooting) shooting.enabled = next;
    }

    private void Die()
    {
        IsAlive.Value = false;
        if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);
        _respawnCoroutine = StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        Debug.Log("[Respawn] Waiting 3s...");
        yield return new WaitForSeconds(3f);

        Vector3 spawnPos = GetRandomSpawnPosition();

        TeleportClientRpc(Owner, spawnPos);
        yield return null;

        HP.Value = 100;
        IsAlive.Value = true;

        Debug.Log($"[Respawn] Server initiated respawn to {spawnPos}");
        _respawnCoroutine = null;
    }


    [Server]
    public void ForceRespawnServer()
    {
        if (_respawnCoroutine != null)
        {
            StopCoroutine(_respawnCoroutine);
            _respawnCoroutine = null;
        }

        Score.Value = 0;
        HP.Value = 100;
        IsAlive.Value = true;

        Vector3 spawnPos = GetRandomSpawnPosition();
        TeleportClientRpc(Owner, spawnPos);
    }

    public void AddScore()
    {
        if (IsServerStarted)
            Score.Value++;
        else
            AddScoreServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddScoreServerRpc()
    {
        Score.Value++;
    }

    [TargetRpc]
    private void TeleportClientRpc(NetworkConnection target, Vector3 newPosition)
    {
        var cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        transform.position = newPosition;
        transform.rotation = Quaternion.identity;

        if (cc) cc.enabled = true;

        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.ResetVerticalVelocity();

        var shooting = GetComponent<PlayerShooting>();
        if (shooting != null) shooting.ResetAmmo();
    }
}
