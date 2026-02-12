using System;
using System.Collections;
using Shooting;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HealthComponent : NetworkBehaviour
{
    #region Properties
    public float Health => _health.Value;
    public float MaxHealth => _maxHealth;
    public bool Invulnerable { get => _invulnerable; set => _invulnerable = value; }
    #endregion

    #region Fields
    [Header("Parameters")]
    [SerializeField] private float _maxHealth;

    [Space]
    [Tooltip("Specifies if objects colliding at high velocity should deal damage")]
    [SerializeField] private bool _impactDamage;

    [Tooltip(
        "x : The force at which we start taking damage ; " +
        "y : The maximal force that deals damages ; " +
        "z : How much damage we take with a minimal force ; " +
        "w : How much damage we take with a maximal force"
        )]
    [SerializeField] private Vector4 _impactDamageRange;

    [Space]
    [Tooltip("Specifies if bullets should deal damage")]
    [SerializeField] private bool _bulletDamage;

    [Header("Events")]
    [SerializeField] private UnityEvent _onDamageTaken;
    [SerializeField] private UnityEvent _onRegeneration;
    [SerializeField] private UnityEvent _onDeath;
    [SerializeField] private DamageEffectController effetDamage;

    public Image healthBar;

    // Private fields
    Target _target; // Target component attached to this same gameobject
    Collider _collider;
    Rigidbody _rb;

    private NetworkVariable<float> _health = new NetworkVariable<float>();
    bool _invulnerable;
    #endregion

    #region Methods

    private void Start()
    {
        _bulletDamage = TrySetupTarget();
        _impactDamage = TrySetupImpact();
        healthBar = VariableManager.instance.healthBar;

        if (IsServer)
            _health.Value = _maxHealth;

        _health.OnValueChanged += OnHealthChanged;
    }

    private void OnHealthChanged(float previous, float current)
    {
        // Update UI only for local player
        if (IsOwner && healthBar != null)
            healthBar.fillAmount = current / _maxHealth;

        // Invoke events
        if (current > previous) _onRegeneration.Invoke();
        else if (current < previous) _onDamageTaken.Invoke();

        if (current <= 0f && IsServer)
        {
            GetComponent<KnockOut>()?.KOServerRpc();
        }
    }

    public void Heal(float amount)
    {
        if (!IsServer) return;

        _health.Value = Mathf.Min(_health.Value + amount, _maxHealth);
        EventBus.Invoke("LocalPlayerHeal", new DataPacket(amount));
    }

    public void Heal() => Heal(_maxHealth);

    public bool TryTakeDamage(float damage)
    {
        if (!CanTakeDamage()) return false;

        TakeDamageServerRpc(damage);
        return true;
    }

    public bool TryTakeDamage(BulletInfo bulletInfo)
    {
        if (!CanTakeDamage()) return false;
        
        if(hitDamageCoroutine != null)
            StopCoroutine(hitDamageCoroutine);
        hitDamageCoroutine = StartCoroutine(Effecting());

        TakeDamageServerRpc(bulletInfo.Damage);
        return true;
    }

    public bool CanTakeDamage() => !_invulnerable;

    private bool TrySetupImpact()
    {
        if (!_impactDamage) return false;
        if (!TryGetComponent(out _collider)) return false;
        if (!TryGetComponent(out _rb)) return false;

        return true;
    }

    private bool TrySetupTarget()
    {
        if (!_bulletDamage) return false;
        if (!TryGetComponent(out _target)) return false;

        _target.OnShot += TakeBulletDamage;
        return true;
    }

    private void TakeBulletDamage(BulletInfo bulletInfo) => TryTakeDamage(bulletInfo.Damage);

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage)
    {
        _health.Value = Mathf.Max(_health.Value - damage, 0f);
        EventBus.Invoke("LocalPlayerDamage", new DataPacket(damage));
    }
    
    Coroutine hitDamageCoroutine;
    
    IEnumerator Effecting()
    {
        effetDamage.SetColor(Color.red);
        effetDamage.SetDizziness(0);
        effetDamage.SetIntensity(0.5f);
        
        yield return new WaitForSeconds(0.5f);

        effetDamage.SetIntensity(0);

        hitDamageCoroutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_impactDamage) return;
        if (!other.TryGetComponent(out ImpactProvider impactor)) return;

        Vector3 inVelocity = Vector3.zero;
        if (other.TryGetComponent(out Rigidbody rb)) inVelocity = rb.linearVelocity;

        Vector3 diff = inVelocity - _rb.linearVelocity;
        Vector3 normal = (other.transform.position - transform.position).normalized;
        Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;
        Vector3 perpendicular = diff - (tangent * Vector3.Dot(diff, tangent));
        float force = Mathf.Clamp(perpendicular.magnitude, _impactDamageRange.x, _impactDamageRange.y);
        float damage = Mathf.Lerp(_impactDamageRange.z, _impactDamageRange.w, Mathf.InverseLerp(_impactDamageRange.x, _impactDamageRange.y, force));

        TryTakeDamage(damage);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Damage"))
        {
            TryTakeDamage(10);
        }
    }

    #endregion
}
