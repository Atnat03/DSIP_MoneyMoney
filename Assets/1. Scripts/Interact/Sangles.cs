using System;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class Sangles : NetworkBehaviour, IInteractible
{
    public Transform stayPos;

    [SerializeField] private string interactionName;

    public string InteractionName
    {
        get => interactionName;
        set => interactionName = value;
    }

    public Outline[] Outline     
    {
        get { return outline; }
        set { }
    }

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
        Log($"OnNetworkSpawn | Server={IsServer} | Client={IsClient} | Owner={OwnerClientId}");

        storedObjectId.OnValueChanged += OnStoredObjectChanged;

        if (storedObjectId.Value != 0)
            UpdateStoredObjectReference();
    }

    public override void OnNetworkDespawn()
    {
        storedObjectId.OnValueChanged -= OnStoredObjectChanged;
    }

    private void OnStoredObjectChanged(ulong oldVal, ulong newVal)
    {
        Log($"StoredObject changed {oldVal} → {newVal}");
        UpdateStoredObjectReference();
    }

    #endregion

    #region INTERACTION

    private void OnEnable() => Interact.OnInteract += HitInteract;
    private void OnDisable() => Interact.OnInteract -= HitInteract;

    private void HitInteract(GameObject obj, GameObject player)
    {
        if (obj != gameObject) return;

        NetworkObject playerNet = player.GetComponent<NetworkObject>();
        if (playerNet == null)
        {
            LogError("Player has no NetworkObject!");
            return;
        }

        Log($"Interact by Player {playerNet.OwnerClientId}");

        TryInteractServerRpc(playerNet.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryInteractServerRpc(ulong playerNetId, ServerRpcParams rpcParams = default)
    {
        Log($"TryInteractServerRpc | Sender={rpcParams.Receive.SenderClientId}");

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(playerNetId, out var playerNet))
        {
            LogError("Player not found in SpawnedObjects!");
            return;
        }

        if (storedObjectId.Value == 0)
            TryStock(playerNet, rpcParams.Receive.SenderClientId);
        else
            TryRelease(rpcParams.Receive.SenderClientId);
    }

    #endregion

    #region STOCK

    private void TryStock(NetworkObject playerNetObj, ulong senderId)
    {
        Log("=== TryStock START ===");

        var grabPoint = playerNetObj.GetComponent<GrabPoint>();
        Log($"GrabPoint = {grabPoint}");

        if (grabPoint == null)
        {
            LogError("GrabPoint NULL côté serveur");
            return;
        }

        GameObject heldObject = grabPoint.GetCurrentObjectInHand();
        Log($"HeldObject = {heldObject}");

        if (heldObject == null)
        {
            LogWarning("Le joueur ne tient rien côté serveur !");
            return;
        }

        var grabbable = heldObject.GetComponent<GrabbableObject>();
        Log($"Grabbable = {grabbable}");

        if (grabbable == null)
        {
            LogError("Pas de GrabbableObject côté serveur !");
            return;
        }

        Log($"Type = {grabbable.type}");

        if (grabbable.type != GrabType.Sac)
        {
            LogWarning("Ce n’est PAS un sac côté serveur !");
            return;
        }

        var objectNet = heldObject.GetComponent<NetworkObject>();
        Log($"NetworkObject = {objectNet} | IsSpawned={objectNet?.IsSpawned}");
    }


    private System.Collections.IEnumerator StockAfterRelease(NetworkObject objectNet)
    {
        yield return null;

        storedObjectId.Value = objectNet.NetworkObjectId;
        interactionObject = objectNet;

        objectNet.transform.position = stayPos.position;

        SetObjectPhysics(objectNet, true);

        dropTimer.Value = Random.Range(mini_TimeBeforeDrop, max_TimeBeforeDrop);

        Log($"Object stocked | Timer={dropTimer.Value}");

        UpdateObjectPositionClientRpc(objectNet.NetworkObjectId, stayPos.position);
    }

    #endregion

    #region RELEASE

    private void TryRelease(ulong playerId)
    {
        Log("=== TryRelease ===");

        if (storedObjectId.Value == 0)
        {
            LogWarning("Nothing stored");
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(storedObjectId.Value, out var netObj))
        {
            LogError("Stored object missing!");
            storedObjectId.Value = 0;
            return;
        }

        SetObjectPhysics(netObj, false);

        ReleaseClientRpc(storedObjectId.Value, playerId);

        storedObjectId.Value = 0;
        interactionObject = null;
        dropTimer.Value = 0;
    }

    #endregion

    #region FORCE RELEASE

    [ServerRpc(RequireOwnership = false)]
    private void ForceReleaseFromHandServerRpc(ulong itemId, ulong playerId)
    {
        Log($"ForceReleaseFromHandServerRpc | Item={itemId} Player={playerId}");

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(itemId, out var item))
        {
            LogError("Item not found for force release");
            return;
        }

        if (item.TryGetComponent<GrabbableObject>(out var g))
            g.IsGrabbed.Value = false;

        ForceClientReleaseClientRpc(playerId);
    }

    [ClientRpc]
    private void ForceClientReleaseClientRpc(ulong playerId)
    {
        if (NetworkManager.Singleton.LocalClientId != playerId)
            return;

        var player = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (player == null) return;

        var grabPoint = player.GetComponent<GrabPoint>();
        if (grabPoint == null) return;

        Log("Client forcing local release");
        grabPoint.ForceLocalRelease();
    }

    #endregion

    #region CLIENT RPCs

    [ClientRpc]
    private void UpdateObjectPositionClientRpc(ulong id, Vector3 pos)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(id, out var obj)) return;

        obj.transform.position = pos;
        SetObjectPhysics(obj, true);
    }

    [ClientRpc]
    private void ReleaseClientRpc(ulong id, ulong playerId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(id, out var obj)) return;

        SetObjectPhysics(obj, false);

        if (NetworkManager.Singleton.LocalClientId == playerId)
            RequestGrabServerRpc(id, playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestGrabServerRpc(ulong id, ulong playerId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(id, out var obj)) return;

        if (!NetworkManager.Singleton.ConnectedClients
            .TryGetValue(playerId, out var client)) return;

        var grabPoint = client.PlayerObject.GetComponent<GrabPoint>();
        grabPoint?.TryGrab(obj);
    }

    #endregion

    #region DROP TIMER

    private void Update()
    {
        if (!IsServer) return;
        if (storedObjectId.Value == 0) return;

        if (dropTimer.Value > 0)
            dropTimer.Value -= Time.deltaTime;
        else
            ForceDrop();
    }

    private void ForceDrop()
    {
        Log("ForceDrop triggered");

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(storedObjectId.Value, out var obj))
        {
            storedObjectId.Value = 0;
            return;
        }

        SetObjectPhysics(obj, false);
        DropClientRpc(storedObjectId.Value);

        storedObjectId.Value = 0;
        interactionObject = null;
        dropTimer.Value = 0;
    }

    [ClientRpc]
    private void DropClientRpc(ulong id)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(id, out var obj)) return;

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
        }

        if (obj.TryGetComponent<Collider>(out var col))
            col.enabled = !stocked;

        obj.gameObject.layer = LayerMask.NameToLayer(
            stocked ? "Ignore Raycast" : "Interactable"
        );
    }

    private void UpdateStoredObjectReference()
    {
        if (storedObjectId.Value == 0)
        {
            interactionObject = null;
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(storedObjectId.Value, out var obj))
        {
            LogError("Stored object not found on update reference");
            return;
        }

        interactionObject = obj;
        obj.transform.position = stayPos.position;
        SetObjectPhysics(obj, true);
    }

    private void Log(string msg)
    {
        Debug.Log($"[SANGLES][{name}][Client:{NetworkManager.Singleton.LocalClientId}] {msg}");
    }

    private void LogWarning(string msg)
    {
        Debug.LogWarning($"[SANGLES][{name}] {msg}");
    }

    private void LogError(string msg)
    {
        Debug.LogError($"[SANGLES][{name}] {msg}");
    }
    
    public bool IsStock()
    {
        bool stocked = storedObjectId.Value != 0;
        Debug.Log($"[SANGLES] IsStock() → {stocked} | StoredId={storedObjectId.Value}");
        return stocked;
    }


    #endregion
}
