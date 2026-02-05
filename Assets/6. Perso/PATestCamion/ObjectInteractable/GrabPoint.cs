using UnityEngine;
using UnityEngine.Events;

public class GrabPoint : MonoBehaviour
{

    #region Properties
    public bool IsFree => _heldItem == null;
    public bool IsNotFree => _heldItem != null;
    public GameObject HeldItem => _heldItem;


    #endregion

    #region Fields
    [SerializeField] private KeyCode _throw = KeyCode.Mouse1;
    [SerializeField] private float _throwStrength = 1f;

    [SerializeField] private UnityEvent _onGrab;
    [SerializeField] private UnityEvent _onThrow;

    private GameObject _heldItem;
    private bool _enableCallbacks = true;

    #endregion

    #region Methods

    private void Update()
    {
        TryThrow();
    }

    public void Grab(GameObject other)
    { 
        other.transform.SetParent(transform);
        other.transform.localPosition = transform.localPosition;
        other.transform.localRotation = transform.localRotation;

        _heldItem = other;

        if (other.TryGetComponent(out Rigidbody rb))
            rb.isKinematic = true;
        if (other.TryGetComponent(out SphereCollider sc))
            sc.isTrigger = true;

        if (_enableCallbacks)
            _onGrab.Invoke();
    }

    public bool TryThrow()
    {
        if (!CanThrow()) return false;

        if (!Input.GetKey(_throw)) return false;

        Throw();
        return true;
    }
    private void Throw()
    {
        Vector3 direction = Camera.main.transform.forward;
        float force = _throwStrength;

        _heldItem.transform.SetParent(null);

        if (_heldItem.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = false;
            rb.AddForce(direction * force, ForceMode.VelocityChange);
        }
        if (_heldItem.TryGetComponent(out SphereCollider sc))
            sc.isTrigger = false;

        _heldItem = null;

        if (_enableCallbacks)
            _onGrab.Invoke();
    }

    public bool CanThrow()
    {
        return IsNotFree;
    }
    #endregion
}