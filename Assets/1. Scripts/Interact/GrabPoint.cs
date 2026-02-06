using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class GrabPoint : MonoBehaviour
{

    #region Properties
    
    public enum HandState {Free, Grab }
    private HandState handState;
    private HandState freeHandState = HandState.Free;
    private HandState grabHandState = HandState.Grab;

    #endregion

    #region Fields
    [SerializeField] private KeyCode _throw = KeyCode.Mouse1;
    [SerializeField] private float _throwStrength = 1000f;

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

    public void TryGrab(GameObject other)
    {
        if (!CanGrab()) return;
        Grab(other);
        Debug.Log("Hit interact");
    }
    private void Grab(GameObject other)
    { 
        other.transform.SetParent(transform);
        other.transform.localPosition = new Vector3(0, 0, 0);
        other.transform.localRotation = new Quaternion(0, 0, 0, 0);

        _heldItem = other;

        if (other.TryGetComponent(out Rigidbody rb))
            rb.isKinematic = true;
        if (other.TryGetComponent(out SphereCollider sc))
            sc.isTrigger = true;

        if (_enableCallbacks)
            _onGrab.Invoke();
        handState = HandState.Grab;
    }

    public bool TryThrow()
    {
        if (!CanThrow()) return false;

        if (!Input.GetKeyDown(_throw)) return false;

        Throw();
        return true;
    }

    /// <summary>
    /// Scans the children objects to find a grabbable object.
    /// If one is found, it is considered as owned by this grabpoint.
    /// Otherwise, the grabpoint considers itself empty
    /// </summary>
    private void DetectHeldObject()
    {
        var comp = GetComponentInChildren<GrabbableObject>();
        if (comp != null)
            _heldItem = comp.gameObject;
        else
            _heldItem = null;
    }

    private void Throw()
    {
        if(GetComponent<FPSControllerMulti>().MyCamera() == null)
            return;
        
        Vector3 direction = GetComponent<FPSControllerMulti>().MyCamera().transform.forward;
        float force = _throwStrength;

        _heldItem.transform.SetParent(null);
        _heldItem.transform.position += direction * 0.2f;

        if (_heldItem.TryGetComponent(out SphereCollider sc))
            sc.isTrigger = false;
        if (_heldItem.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(direction * force, ForceMode.Impulse);
        }


        _heldItem = null;

        if (_enableCallbacks)
            _onGrab.Invoke();
        handState = HandState.Free;
    }

    public bool CanThrow()
    {
        if(handState == grabHandState)
        {
            return true;
        }
        return false;
    }


    public bool CanGrab()
    {
        if(handState == freeHandState)
        {
            return true;
        }
        return false;
    }
    #endregion
}