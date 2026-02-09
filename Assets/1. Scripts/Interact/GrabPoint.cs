using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GrabPoint : NetworkBehaviour
{
    public enum HandState { Free, Grab }
    private HandState handState = HandState.Free;

    [Header("Grab Settings")]
    [SerializeField] private KeyCode _throw = KeyCode.Mouse1;
    [SerializeField] private float holdDistance = 2f;

    [Header("Throw Charge")]
    [SerializeField] private float _maxChargeTime = 1.5f;
    [SerializeField] private float _minThrowStrength = 2f;
    [SerializeField] private float _maxThrowStrength = 10f;
    [SerializeField] private Image throwJauge;

    [Header("Events")]
    [SerializeField] private UnityEvent _onGrab;
    [SerializeField] private UnityEvent _onThrow;

    private float _chargeTimer;
    private bool _isCharging;

    private NetworkObject _heldItem;
    private Transform _camera;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        _camera = GetComponent<FPSControllerMulti>()?.MyCamera()?.transform;
        if (_camera == null)
            Debug.LogError("GrabPoint: Camera not found!");
    }

    private void OnEnable() => Interact.OnInteract += OnInteractGrab;
    private void OnDisable() => Interact.OnInteract -= OnInteractGrab;

    private void OnInteractGrab(GameObject obj, GameObject player)
    {
        if (!IsOwner) return;
        
        if (obj.GetComponent<IGrabbable>() == null)
            return;
        
        if(player.GetComponent<FPSControllerMulti>().isDriver)
            return;

        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        if (netObj != null)
            TryGrab(netObj);
    }

    private void Update()
    {
        if (!IsOwner) return;
        
        if (_heldItem != null && handState == HandState.Grab && _camera != null)
        {
            Vector3 pos = _camera.position + _camera.forward * holdDistance;
            Quaternion rot = _camera.rotation;

            UpdateHeldPositionServerRpc(_heldItem.NetworkObjectId, pos, rot);
        }

        HandleThrowInput();
    }

    #region GRAB

    public void TryGrab(NetworkObject item)
    {
        if (handState != HandState.Free) return;
        GrabServerRpc(item.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void GrabServerRpc(ulong itemId, ServerRpcParams rpc = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemId, out var item))
            return;

        if (item.TryGetComponent<GrabbableObject>(out var g) && g.IsGrabbed.Value)
            return;

        if (item.gameObject.CompareTag("Material") || item.GetComponent<ListenEventDoor>())
            return;

        item.ChangeOwnership(rpc.Receive.SenderClientId);

        if (item.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (item.TryGetComponent<Collider>(out var col))
            col.isTrigger = true;

        if (item.TryGetComponent<GrabbableObject>(out var grab))
            grab.IsGrabbed.Value = true;

        ConfirmGrabClientRpc(itemId, rpc.Receive.SenderClientId);
    }

    [ClientRpc]
    private void ConfirmGrabClientRpc(ulong itemId, ulong ownerId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemId, out var item))
            return;

        if (NetworkManager.Singleton.LocalClientId != ownerId) return;

        GetComponent<FPSControllerMulti>().hasSomethingInHand = true;

        _heldItem = item;
        handState = HandState.Grab;

        _onGrab?.Invoke();
    }

    #endregion

    #region THROW

    private void HandleThrowInput()
    {
        throwJauge.transform.parent.gameObject.SetActive(_isCharging);

        if (handState != HandState.Grab || _heldItem == null) return;

        if (Input.GetKeyDown(_throw))
        {
            _isCharging = true;
            _chargeTimer = 0f;
        }

        if (Input.GetKey(_throw))
        {
            _chargeTimer += Time.deltaTime;
            _chargeTimer = Mathf.Clamp(_chargeTimer, 0, _maxChargeTime);

            throwJauge.fillAmount = _chargeTimer / _maxChargeTime;
        }

        if (Input.GetKeyUp(_throw))
        {
            _isCharging = false;

            float ratio = _chargeTimer / _maxChargeTime;
            float force = Mathf.Lerp(_minThrowStrength, _maxThrowStrength, ratio);

            ThrowServerRpc(_heldItem.NetworkObjectId, _camera.forward, force);

            _chargeTimer = 0;
            throwJauge.fillAmount = 0;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ThrowServerRpc(ulong itemId, Vector3 direction, float force, ServerRpcParams rpc = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemId, out var item))
            return;

        if (item.OwnerClientId != rpc.Receive.SenderClientId)
            return;
        
        if (item.gameObject.CompareTag("Material"))
            return;

        if (item.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.AddForce(direction.normalized * force, ForceMode.Impulse);
        }

        if (item.TryGetComponent<Collider>(out var col))
            col.isTrigger = false;

        if (item.TryGetComponent<GrabbableObject>(out var g))
            g.IsGrabbed.Value = false;

        ReleaseClientRpc(rpc.Receive.SenderClientId);
    }

    [ClientRpc]
    private void ReleaseClientRpc(ulong ownerId)
    {
        if (NetworkManager.Singleton.LocalClientId != ownerId) return;

        GetComponent<FPSControllerMulti>().hasSomethingInHand = false;

        _heldItem = null;
        handState = HandState.Free;
        _onThrow?.Invoke();
    }

    #endregion

    #region HOLD POSITION

    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
    private void UpdateHeldPositionServerRpc(ulong itemId, Vector3 pos, Quaternion rot, ServerRpcParams rpc = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemId, out var item))
            return;

        if (item.OwnerClientId != rpc.Receive.SenderClientId)
            return;

        if (item.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.position = pos;
            rb.rotation = rot;
        }
    }

    #endregion
}
