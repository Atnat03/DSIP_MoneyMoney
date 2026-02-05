
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
    [SerializeField] private bool _impactDamage = true;
    [Tooltip(
        "x : The force at which we start taking damage ; " +
        "y : The maximal force that deals damages ; " +
        "z : How much damage we take with a minimal force ; " +
        "w : How much damage we take with a maximal force"
        )]
    [SerializeField] private Vector4 _impactDamageRange = new Vector4(0f, 100f, 0f, 100f);
    [Space]
    [Tooltip("Specifies if bullets should deal damage")]
    [SerializeField] private bool _bulletDamage = true;

    [Header("Events")]
    [SerializeField] private UnityEvent _onDamageTaken;
    [SerializeField] private UnityEvent _onRegeneration;
    [SerializeField] private UnityEvent _onDeath;




    // Private fields
    Target _target; // Target component attached to this same gameobject
    Rigidbody _rb;

    float _previousHealth;
    float _health;
    bool _enableCallbacks = true;
    bool _invulnerable;

    LineDebugger lineDebug = new();

    #endregion


    #region Methods
    private void Start()
    {
        _bulletDamage = TrySetupTarget();
        _impactDamage = TrySetupImpact();
        _health = _maxHealth;
    }

    private void Update()
    {
        CheckDirty_Health();
    }

    private void OnDrawGizmos()
    {

        lineDebug.Draw();
    }

    private void OnCollisionEnter(Collision col)
    {
        Collider other = col.collider;
        if (!_impactDamage)
            return;
        if (!other.TryGetComponent(out ImpactProvider impactor))
            return;

        Vector3 inVelocity = Vector3.zero; // Velocity of the other object
        if (other.TryGetComponent(out Rigidbody rb))
            inVelocity = rb.linearVelocity;

        Vector3 outVelocity = _rb.linearVelocity; // Velocity of this object
        
        Vector3 diff = inVelocity - outVelocity; // Force of the impact

        Vector3 normal = (other.transform.position - transform.position).normalized; // Direction from this object towards the other. Inaccurate but simple (assumes both objects are spheres).

        Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;

        float proj = Vector3.Dot(diff, normal); // The only part of the impact that we care for is the one that is perpendicular to the surface

        Vector3 perpendicularForce = Vector3.Dot(diff, normal) * normal;

        float force = perpendicularForce.magnitude; // This is actually how hard we are being hit

        lineDebug.Add(col.GetContact(0).point, col.GetContact(0).point + normal * 0.5f, Color.blue);
        lineDebug.Add(col.GetContact(0).point, col.GetContact(0).point + tangent * 0.5f, Color.red);
        lineDebug.Add(col.GetContact(0).point, col.GetContact(0).point + Vector3.Cross(normal, tangent) * 0.5f, Color.green);
        lineDebug.Add(col.GetContact(0).point, col.GetContact(0).point + perpendicularForce * 0.8f, Color.magenta);

        // Don't take damages if the impact is to weak.
        if (force < _impactDamageRange.x)
            return;
        // Don't take damages if the other object is going out of us instead of in
        if (Vector3.Dot(perpendicularForce, normal) > 0)
            return;

        force = Mathf.Clamp(force, _impactDamageRange.x, _impactDamageRange.y);

        float damage = Mathf.Lerp(_impactDamageRange.z, _impactDamageRange.w, Mathf.InverseLerp(_impactDamageRange.x, _impactDamageRange.y, force));

        TakeDamage(damage);
    }

    private bool TrySetupImpact()
    {
        if (!_impactDamage)
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
        if (_enableCallbacks)
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
        if (_enableCallbacks)
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