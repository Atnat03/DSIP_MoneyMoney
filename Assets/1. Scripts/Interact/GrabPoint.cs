using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class GrabPoint : NetworkBehaviour
{
    public enum HandState { Free, Grab }
    private HandState handState = HandState.Free;

    [Header("Grab Settings")]
    [SerializeField] private KeyCode _throw = KeyCode.Mouse1;
    [SerializeField] private float _throwStrength = 10f;
    [SerializeField] private float holdDistance = 2f;

    [Header("Events")]
    [SerializeField] private UnityEvent _onGrab;
    [SerializeField] private UnityEvent _onThrow;

    private NetworkObject _heldItem;
    private Transform _camera;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _camera = GetComponent<FPSControllerMulti>()?.MyCamera()?.transform;
            if (_camera == null) Debug.LogError("GrabPoint: Camera not found!");
        }
    }

    private void OnEnable()
    {
        Interact.OnInteract += OnInteractGrab;
    }

    private void OnDisable()
    {
        Interact.OnInteract -= OnInteractGrab;
    }

    private void OnInteractGrab(GameObject obj, GameObject player)
    {
        if (obj.GetComponent<ListenEventDoor>() != null) return;
        if (!obj.CompareTag("Grabbable")) return;

        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            TryGrab(netObj);
        }
        else
        {
            Debug.LogWarning($"{obj.name} has no NetworkObject!");
        }
    }

    private void Update()
    {
        if (_heldItem != null && handState == HandState.Grab && _camera != null)
        {
            Vector3 targetPos = _camera.position + _camera.forward * holdDistance;
            Quaternion targetRot = _camera.rotation;

            // Envoi au serveur (unreliable pour perf, appelé souvent)
            UpdateHeldPositionServerRpc(_heldItem.NetworkObjectId, targetPos, targetRot);
        }

        TryThrow();
    }

    #region Grab Logic
    public void TryGrab(NetworkObject itemNetObj)
    {
        if (!CanGrab() || itemNetObj == null) return;

        // TOUJOURS RPC pour uniformité (fonctionne pour host aussi)
        GrabServerRpc(itemNetObj.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void GrabServerRpc(ulong itemId, ServerRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemId, out var itemNetObj)) return;

        ulong ownerClientId = rpcParams.Receive.SenderClientId;
        GrabItem(itemNetObj, ownerClientId);
    }

    private void GrabItem(NetworkObject itemNetObj, ulong ownerClientId)
    {
        if (itemNetObj == null) return;

        // Transfert ownership (utile même en server auth)
        if (itemNetObj.OwnerClientId != ownerClientId)
        {
            itemNetObj.ChangeOwnership(ownerClientId);
            Debug.Log($"Ownership of {itemNetObj.name} given to client {ownerClientId}");
        }

        // Set kinematic sur serveur
        if (itemNetObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (itemNetObj.TryGetComponent<Collider>(out var col)) col.isTrigger = true;

        // Confirmation au owner SEULEMENT (set _heldItem local)
        ConfirmGrabClientRpc(itemNetObj.NetworkObjectId, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { ownerClientId } }
        });

        // Informe les AUTRES (set kinematic local)
        ulong[] otherClients = NetworkManager.Singleton.ConnectedClientsIds
            .Where(id => id != ownerClientId).ToArray();
        GrabClientRpc(itemNetObj.NetworkObjectId, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = otherClients }
        });
    }

    [ClientRpc]
    private void ConfirmGrabClientRpc(ulong itemId, ClientRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemId, out var itemNetObj)) return;

        _heldItem = itemNetObj;
        handState = HandState.Grab;

        // Set kinematic local (sécurité)
        if (itemNetObj.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        if (itemNetObj.TryGetComponent<Collider>(out var col)) col.isTrigger = true;

        _onGrab?.Invoke();
        Debug.Log($"Local client now holds {_heldItem.name}");
    }

    [ClientRpc]
    private void GrabClientRpc(ulong itemId, ClientRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemId, out var itemNetObj)) return;

        // Set kinematic local pour non-owners
        if (itemNetObj.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        if (itemNetObj.TryGetComponent<Collider>(out var col)) col.isTrigger = true;

        Debug.Log($"Client sees {itemNetObj.name} grabbed by someone else");
    }
    #endregion

    #region Throw Logic
    private void TryThrow()
    {
        if (!CanThrow() || !Input.GetKeyDown(_throw)) return;

        // TOUJOURS RPC (uniforme)
        if (_heldItem == null) return;
        ThrowServerRpc(_heldItem.NetworkObjectId, _camera.forward);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ThrowServerRpc(ulong itemId, Vector3 throwDirection, ServerRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemId, out var itemNetObj)) return;

        // Anti-triche : vérifie que sender == owner
        if (itemNetObj.OwnerClientId != rpcParams.Receive.SenderClientId) return;

        ThrowItem(itemNetObj, throwDirection);

        // Confirmation au thrower SEULEMENT
        ulong throwerClientId = rpcParams.Receive.SenderClientId;
        ConfirmReleaseClientRpc(itemNetObj.NetworkObjectId, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { throwerClientId } }
        });
    }

    private void ThrowItem(NetworkObject item, Vector3 throwDirection)
    {
        if (item.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(throwDirection * _throwStrength, ForceMode.Impulse);
        }

        if (item.TryGetComponent<Collider>(out var col)) col.isTrigger = false;
    }

    [ClientRpc]
    private void ConfirmReleaseClientRpc(ulong itemId, ClientRpcParams rpcParams = default)
    {
        _heldItem = null;
        handState = HandState.Free;
        _onThrow?.Invoke();
        Debug.Log("Item thrown/released");
    }
    #endregion

    #region Position Sync (Server Authoritative)
    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
    private void UpdateHeldPositionServerRpc(ulong itemId, Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemId, out var itemNetObj)) return;

        // Anti-triche
        if (itemNetObj.OwnerClientId != rpcParams.Receive.SenderClientId) return;

        // Applique sur serveur (NT propagera)
        if (itemNetObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.position = position;
            rb.rotation = rotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            itemNetObj.transform.SetPositionAndRotation(position, rotation);
        }
    }
    #endregion

    public bool CanGrab() => handState == HandState.Free;
    public bool CanThrow() => handState == HandState.Grab;
}