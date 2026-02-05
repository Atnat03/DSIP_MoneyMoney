
using Shooting;
using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour
{
    #region Properties
    public float Health => _health;
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




    // Private fields
    Target _target; // Target component attached to this same gameobject
    Collider _collider;
    Rigidbody _rb;

    float _previousHealth;
    float _health;
    bool _enableCallbacks;
    bool _invulnerable;

    #endregion


    #region Methods
    private void Start()
    {
        _bulletDamage = TrySetupTarget();
        _impactDamage = TrySetupImpact();
    }

    private void Update()
    {
        CheckDirty_Health();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_impactDamage)
            return;
        if (!other.TryGetComponent(out ImpactProvider impactor))
            return;

        Debug.Log("Impact detected");

        Vector3 inVelocity = Vector3.zero; // Velocity of the other object
        if (other.TryGetComponent(out Rigidbody rb))
            inVelocity = rb.linearVelocity;

        Vector3 outVelocity = _rb.linearVelocity; // Velocity of this object
        
        Vector3 diff = inVelocity - outVelocity; // Force of the impact

        Vector3 normal = (other.transform.position - transform.position).normalized; // Direction from this object towards the other. Inaccurate but simple (assumes both objects are spheres).

        Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;

        float proj = Vector3.Dot(diff, tangent); // The only part of the impact that we care for is the one that is perpendicular to the surface

        Vector3 perpendicular = diff - (tangent * proj);

        float force = perpendicular.magnitude; // This is actually how hard we are being hit

        force = Mathf.Clamp(force, _impactDamageRange.x, _impactDamageRange.y);

        float damage = Mathf.Lerp(_impactDamageRange.z, _impactDamageRange.w, Mathf.InverseLerp(_impactDamageRange.x, _impactDamageRange.y, force));

        TakeDamage(damage);
    }

    private bool TrySetupImpact()
    {
        if (!_impactDamage)
            return false;
        if (!TryGetComponent(out _collider))
            return false;
        if (!TryGetComponent(out _rb))
            return false;

        return true;
    }

    private bool TrySetupTarget()
    {
        if (!_bulletDamage)
            return false;

        if (!TryGetComponent(out _target))
            return false;

        _target.OnShot += TakeBulletDamage;
        return true;
    }
    public void Heal()
    {
        Heal(_maxHealth);
    }
    public void Heal(float hp)
    {
        _health += hp;
        EventBus.Invoke("LocalPlayerHeal", new DataPacket(hp));
    }
    public bool TryTakeDamage(float damage)
    {
        if (!CanTakeDamage()) return false;

        TakeDamage(damage);
        return true;
    }

    public bool TryTakeDamage(BulletInfo bulletInfo)
    {
        if (!CanTakeDamage()) return false;

        TakeBulletDamage(bulletInfo);
        return true;
    }
    public bool CanTakeDamage() => !_invulnerable;

    private void TakeBulletDamage(BulletInfo bulletInfo) => TakeDamage(bulletInfo.Damage);

    private void TakeDamage(float damage)
    {
        _health -= damage;
        EventBus.Invoke("LocalPlayerDamage", new DataPacket(damage));
    }

    private bool CheckDirty_Health()
    {
        if (_health != _previousHealth)
        {
            if (_enableCallbacks)
            {
                if (_health > _previousHealth)
                    _onRegeneration.Invoke();
                else if (_health < _previousHealth)
                    _onDamageTaken.Invoke();
                if (_health <= 0f)
                    _onDeath.Invoke();
                EventBus.Invoke("Health_DirtyFlag", new DataPacket(_health));
            }

            _previousHealth = _health;
            return true;
        }
        return false;
    }
    #endregion
}