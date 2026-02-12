using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Random = UnityEngine.Random;

public class Sangles : NetworkBehaviour, IInteractible
{
    [Header("Sangle Settings")]
    public Transform stayPos;

    [SerializeField] private string interactionName;
    public string InteractionName { get => interactionName; set => interactionName = value; }

    public Outline[] Outline { get { return outline; } set { } }
    public Outline[] outline;

    private NetworkVariable<ulong> storedObjectId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> dropTimer = new NetworkVariable<float>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Timer before drop")]
    [SerializeField] private float max_TimeBeforeDrop = 10f;
    [SerializeField] private float mini_TimeBeforeDrop = 5f;

    private NetworkObject interactionObject;

    #region NETWORK SPAWN

    public override void OnNetworkSpawn()
    {
        storedObjectId.OnValueChanged += OnStoredObjectChanged;
        OnStoredObjectChanged(0, storedObjectId.Value);
    }

    public override void OnNetworkDespawn()
    {
        storedObjectId.OnValueChanged -= OnStoredObjectChanged;
    }

    private void OnStoredObjectChanged(ulong oldVal, ulong newVal)
    {
        if (newVal == 0)
        {
            if (interactionObject != null)
            {
                SetObjectPhysics(interactionObject, false);
                interactionObject = null;
            }
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(newVal, out var obj))
        {
            return;
        }

        interactionObject = obj;

        if (IsServer)
        {
            SetObjectPhysics(obj, true);
        }
    }

    #endregion

    #region INTERACTION

    private void OnEnable() => Interact.OnInteract += HitInteract;
    private void OnDisable() => Interact.OnInteract -= HitInteract;

    private void HitInteract(GameObject obj, GameObject player)
    {
        if (obj.GetInstanceID() != gameObject.GetInstanceID()) return;

        NetworkObject playerNet = player.GetComponent<NetworkObject>();
        if (playerNet == null) return;

        ulong heldId = 0;
        GrabPoint gp = player.GetComponent<GrabPoint>();
        if (gp != null)
        {
            GameObject held = gp.GetCurrentObjectInHand();
            if (held != null)
            {
                NetworkObject n = held.GetComponent<NetworkObject>();
                if (n != null) heldId = n.NetworkObjectId;
            }
        }

        TryInteractServerRpc(playerNet.NetworkObjectId, heldId);
    }

    #endregion

    #region SERVER RPC - INTERACT

    [ServerRpc(RequireOwnership = false)]
    private void TryInteractServerRpc(ulong playerNetId, ulong heldObjectNetId, ServerRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetId, out _))
            return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (storedObjectId.Value == 0)
        {
            if (heldObjectNetId == 0) return;

            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(heldObjectNetId, out var heldNetObj))
                return;

            var grabbable = heldNetObj.GetComponent<GrabbableObject>();
            if (grabbable == null || grabbable.type != GrabType.Sac)
                return;

            if (heldNetObj.OwnerClientId != senderClientId && heldNetObj.OwnerClientId != 0)
                return;

            ForceClientHandReleaseClientRpc(senderClientId, heldObjectNetId);

            heldNetObj.transform.position = stayPos.position;
            heldNetObj.transform.rotation = stayPos.rotation;

            SetObjectPhysics(heldNetObj, true);
            heldNetObj.ChangeOwnership(0);

            SyncPositionClientRpc(heldObjectNetId, stayPos.position, stayPos.rotation);

            storedObjectId.Value = heldObjectNetId;
            interactionObject = heldNetObj;

            dropTimer.Value = Random.Range(mini_TimeBeforeDrop, max_TimeBeforeDrop);
        }
        else
        {
            TryRelease(senderClientId);
        }
    }

    #endregion

    #region CLIENT RPCs

    [ClientRpc]
    private void SyncPositionClientRpc(ulong objectId, Vector3 pos, Quaternion rot)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var obj))
            return;
        
        obj.gameObject.layer = LayerMask.NameToLayer("IgnoreRaycast");

        obj.transform.position = pos;
        obj.transform.rotation = rot;
    }

    [ClientRpc]
    private void ForceClientHandReleaseClientRpc(ulong targetClientId, ulong objectNetId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        GrabPoint grabPoint = localPlayer?.GetComponentInChildren<GrabPoint>();

        if (grabPoint != null)
        {
            GameObject current = grabPoint.GetCurrentObjectInHand();
            if (current != null && current.GetComponent<NetworkObject>()?.NetworkObjectId == objectNetId)
            {
                grabPoint.ForceLocalRelease();
            }
        }
    }

    #endregion

    #region RELEASE & DROP

    private void TryRelease(ulong playerId)
    {
        if (storedObjectId.Value == 0) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(storedObjectId.Value, out var netObj))
        {
            storedObjectId.Value = 0;
            interactionObject = null;
            return;
        }

        if (netObj.TryGetComponent<GrabbableObject>(out var grabbable))
        {
            grabbable.IsGrabbed.Value = false;
        }

        SetObjectPhysics(netObj, false);
        netObj.transform.position += Vector3.up * 0.2f;

        DropClientRpc(storedObjectId.Value);

        storedObjectId.Value = 0;
        interactionObject = null;
        dropTimer.Value = 0;
    }

    private void ForceDrop()
    {
        if (storedObjectId.Value == 0) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(storedObjectId.Value, out var obj))
        {
            storedObjectId.Value = 0;
            return;
        }

        if (obj.TryGetComponent<GrabbableObject>(out var grabbable))
        {
            grabbable.IsGrabbed.Value = false;
        }

        SetObjectPhysics(obj, false);
        obj.transform.position += Vector3.up * 0.2f;

        DropClientRpc(storedObjectId.Value);

        storedObjectId.Value = 0;
        interactionObject = null;
        dropTimer.Value = 0;
    }

    [ClientRpc]
    private void DropClientRpc(ulong id)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out var obj))
            return;

        SetObjectPhysics(obj, false);
    }

    #endregion

    #region UTILS

    private void SetObjectPhysics(NetworkObject obj, bool stocked)
    {
        if (obj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = stocked;
            rb.useGravity = !stocked;
            if (!stocked)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (obj.TryGetComponent<Collider>(out var col))
        {
            col.enabled = !stocked;
        }


        g = obj.gameObject;
        UpdateLayerClientRpc(stocked);
    }

    private GameObject g;

    [ClientRpc]
    private void UpdateLayerClientRpc(bool stocked)
    {
        g.layer = LayerMask.NameToLayer(stocked ? "IgnoreRaycast" : "Interactable");
    }

    private void Update()
    {
        if (!IsServer) return;
        if (storedObjectId.Value == 0) return;

        dropTimer.Value -= Time.deltaTime;

        if (dropTimer.Value <= 0)
        {
            ForceDrop();
        }
    }

    private void Log(string msg) => Debug.Log($"[SANGLES][{name}] {msg}");

    #endregion

    public bool IsStock()
    {
        return storedObjectId.Value != 0;
    }
}