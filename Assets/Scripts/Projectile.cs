//3 практика

/*
using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 18f;
    [SerializeField] private int _damage = 20;
    [SerializeField] private float _ignoreOwnerTime = 0.1f;

    private float _spawnTime;
    private bool _initialized;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _spawnTime = Time.time;
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Projectile] Trigger with {other.name}");

        if (!base.IsServerStarted) return;
        if (!_initialized) return;

        if (Time.time < _spawnTime + _ignoreOwnerTime)
        {
            NetworkObject ownerNetObj = other.GetComponentInParent<NetworkObject>();
            if (ownerNetObj != null && ownerNetObj.OwnerId == OwnerId)
                return;
        }

        PlayerNetwork target = other.GetComponentInParent<PlayerNetwork>();
        if (target == null)
        {
            Debug.Log("[Projectile] No PlayerNetwork on target");
            return;
        }

        Debug.Log($"[Projectile] HIT target OwnerId={target.OwnerId}, myOwner={OwnerId}");

        if (target.OwnerId == OwnerId) return;

        int newHp = Mathf.Max(0, target.HP.Value - _damage);
        target.HP.Value = newHp;
        Debug.Log($"[Projectile] DAMAGE: {_damage}, new HP={target.HP.Value}");

        base.Despawn();
    }
}
*/


using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 18f;
    [SerializeField] private int _damage = 20;
    [SerializeField] private float _ignoreOwnerTime = 0.1f;

    private float _spawnTime;
    private bool _initialized;
    private PlayerNetwork _shooter;

    public void SetShooter(PlayerNetwork shooter) => _shooter = shooter;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _spawnTime = Time.time;
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServerStarted || !_initialized) return;

        if (Time.time < _spawnTime + _ignoreOwnerTime)
        {
            var ownerNetObj = other.GetComponentInParent<NetworkObject>();
            if (ownerNetObj != null && ownerNetObj.OwnerId == OwnerId) return;
        }

        var target = other.GetComponentInParent<PlayerNetwork>();
        if (target == null || target.OwnerId == OwnerId) return;

        int newHp = Mathf.Max(0, target.HP.Value - _damage);
        target.HP.Value = newHp;

        if (newHp <= 0 && _shooter != null)
            _shooter.AddScore();

        base.Despawn();
    }
}