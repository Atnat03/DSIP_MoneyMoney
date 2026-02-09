using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    #region Properties
    public bool IsLookedAt => _isLookedAtByPlayer;


    #endregion

    [Header("Parameters")]
    [SerializeField] protected KeyCode _interactKey = KeyCode.E;
    [SerializeField] protected float _reachDistance = 3f;
    [Tooltip("If enabled, this can be interacted with even if it is behind an object")]
    [SerializeField] protected bool _enableSeethrough;
    [SerializeField] public string NameInteraction;

    [Header("Events")]
    [SerializeField] protected UnityEvent _onInteract; // Invoked when the object is interacted with
    [SerializeField] protected UnityEvent _onLookedAt; // Invoked once when the player starts looking at the object
    [SerializeField] protected UnityEvent _onNotLookedAt; // Invoked once when the player stops looking at the object

    protected Collider _collider;
    protected bool _wasLookedAtByPlayer; // Useful for dirty detection
    protected bool _isLookedAtByPlayer; // Specifies if the object is looked at by the player. Should be updated in CheckIfLookedAt()

    protected virtual void Start()
    {
        _collider = GetComponent<Collider>();
    }
    protected virtual void Update()
    {
        //CheckIfLookedAt();

        //TryInteract(Reference.GetObject<PlayerInterface>().transform);
    }
    
    protected virtual bool CheckIfLookedAt(bool enableCallbacks=true)
    {
        Camera cam = Camera.main;
        Vector3 viewPos = cam.transform.position;
        Vector3 viewDir = cam.transform.forward;

        _isLookedAtByPlayer = false;

        if (!_enableSeethrough)
        {
            if (Physics.Raycast(viewPos, viewDir, out RaycastHit hit))
            {
                if (hit.collider == _collider)
                {
                    _isLookedAtByPlayer = true;
                }
            }
        }
        else
        {
            var hits = Physics.RaycastAll(viewPos, viewDir);
            foreach (var hit in hits)
            {
                if (hit.collider == _collider)
                {
                    _isLookedAtByPlayer = true;
                    break;
                }
            }
        }
        if (enableCallbacks)
        {
            if (_isLookedAtByPlayer && !_wasLookedAtByPlayer)
                _onLookedAt.Invoke();
            if (!_isLookedAtByPlayer && _wasLookedAtByPlayer)
                _onNotLookedAt.Invoke();
        }

        _wasLookedAtByPlayer = _isLookedAtByPlayer;
        return _isLookedAtByPlayer;
    }
    /// <summary>
    /// Specifies wether or not the player is inside the radius defined by _reachDistance
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public virtual bool IsReachable(Transform other)
        => Vector3.Distance(transform.position, other.position) <= _reachDistance;
    /// <summary>
    /// Using TryInteract, the object will try to be interacted with. Return true if it succeeds, false otherwise.
    /// </summary>
    /// <param name="playerTransform"></param>
    /// <returns></returns>
    protected virtual bool TryInteract(Transform playerTransform)
    {
        if (!CanInteract(playerTransform)) return false;

        if (!Input.GetKeyDown(_interactKey))
            return false;

        Interact(playerTransform);
        return true;
    }
    /// <summary>
    /// Specifies wether or not this object can be interacted with
    /// </summary>
    /// <param name="playerTransform"></param>
    /// <returns></returns>
    public virtual bool CanInteract(Transform playerTransform)
    {
        //if (!CheckIfLookedAt()) return false;
        if (!IsReachable(playerTransform)) return false;
        return true;
    }
    /// <summary>
    /// Override this methods to specify how the object should behave when interacted with
    /// </summary>
    /// <param name="playerTransform"></param>
    /// <param name="enableCallbacks"></param>
    public virtual void Interact(Transform playerTransform, bool enableCallbacks = true)
    {
        if (enableCallbacks)
            _onInteract.Invoke();
    }
}