using System;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class Sangles : NetworkBehaviour, IInteractible
{
    public Transform stayPos;

    public string InteractionName
    {
        get => interactionName;
        set => interactionName = value;
    }

    [SerializeField] private string interactionName;

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

    public Outline[] outline;
    public Outline[] Outline
    {
        get => outline;
        set { }
    }

    #region Network Spawn

    public override void OnNetworkSpawn()
    {
        storedObjectId.OnValueChanged += OnStoredObjectChanged;

        if (storedObjectId.Value != 0)
            UpdateStoredObjectReference();
    }

    public override void OnNetworkDespawn()
    {
        storedObjectId.OnValueChanged -= OnStoredObjectChanged;
    }

    private void OnStoredObjectChanged(ulong oldValue, ulong newValue)
    {
        UpdateStoredObjectReference();
    }

    private void UpdateStoredObjectReference()
    {
        if (storedObjectId.Value == 0)
        {
            interactionObject = null;
            return;
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(storedObjectId.Value, out var netObj))
        {
            interactionObject = netObj;
            
            netObj.transform.position = stayPos.position;
            
            if (netObj.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            if (netObj.TryGetComponent<Collider>(out var col))
                col.enabled = false;
                
            netObj.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
    }

    #endregion

    #region Interaction

    private void OnEnable()
    {
        Interact.OnInteract += HitInteract;
    }

    private void OnDisable()
    {
        Interact.OnInteract -= HitInteract;
    }

    private void HitInteract(GameObject obj, GameObject player)
    {
        if (obj != gameObject) return;

        NetworkObject playerNetObj = player.GetComponent<NetworkObject>();
        if (playerNetObj == null) return;

        TryInteractServerRpc(playerNetObj.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryInteractServerRpc(ulong playerObjectId, ServerRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(playerObjectId, out var playerNetObj))
            return;

        ulong senderId = rpcParams.Receive.SenderClientId;

        if (storedObjectId.Value == 0)
            TryStock(playerNetObj, senderId);
        else
            TryRelease(senderId);
    }

    #endregion

    #region Stock

    private void TryStock(NetworkObject playerNetObj, ulong senderId)
    {
        var grabPoint = playerNetObj.GetComponent<GrabPoint>();
        if (grabPoint == null) return;

        GameObject heldObject = grabPoint.GetCurrentObjectInHand();
        if (heldObject == null) return;

        var grabbable = heldObject.GetComponent<GrabbableObject>();
        if (grabbable == null || grabbable.type != GrabType.Sac) return;

        NetworkObject objectNetObj = heldObject.GetComponent<NetworkObject>();
        if (objectNetObj == null) return;

        // ✅ Relâcher l'interface du joueur
        ForceReleaseFromHandServerRpc(objectNetObj.NetworkObjectId, senderId);
        
        // ⏳ Stocker après relâchement
        StartCoroutine(StockAfterRelease(objectNetObj));
    }

    private System.Collections.IEnumerator StockAfterRelease(NetworkObject objectNetObj)
    {
        yield return null;
        
        storedObjectId.Value = objectNetObj.NetworkObjectId;
        interactionObject = objectNetObj;
        
        objectNetObj.transform.position = stayPos.position;

        Rigidbody rb = objectNetObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (objectNetObj.TryGetComponent<Collider>(out var col))
            col.enabled = false;

        objectNetObj.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        dropTimer.Value = Random.Range(mini_TimeBeforeDrop, max_TimeBeforeDrop);

        UpdateObjectPositionClientRpc(objectNetObj.NetworkObjectId, stayPos.position);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ForceReleaseFromHandServerRpc(ulong itemId, ulong playerId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemId, out var item))
            return;

        // ✅ Juste changer le flag IsGrabbed
        if (item.TryGetComponent<GrabbableObject>(out var g))
            g.IsGrabbed.Value = false;

        // ✅ Notifier le client de relâcher l'UI
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

        grabPoint.ForceLocalRelease();
    }

    [ClientRpc]
    private void UpdateObjectPositionClientRpc(ulong objectId, Vector3 position)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(objectId, out var netObj))
            return;

        netObj.transform.position = position;
        netObj.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        
        if (netObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        if (netObj.TryGetComponent<Collider>(out var col))
            col.enabled = false;
    }

    #endregion

    #region Release

    private void TryRelease(ulong playerId)
    {
        if (storedObjectId.Value == 0) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(storedObjectId.Value, out var netObj))
        {
            storedObjectId.Value = 0;
            return;
        }

        if (netObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (netObj.TryGetComponent<Collider>(out var col))
            col.enabled = true;

        netObj.gameObject.layer = LayerMask.NameToLayer("Interactable");

        ReleaseClientRpc(storedObjectId.Value, playerId);

        storedObjectId.Value = 0;
        interactionObject = null;
        dropTimer.Value = 0;
    }

    [ClientRpc]
    private void ReleaseClientRpc(ulong objectId, ulong playerId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(objectId, out var netObj))
            return;

        netObj.gameObject.layer = LayerMask.NameToLayer("Interactable");
        
        if (netObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        
        if (netObj.TryGetComponent<Collider>(out var col))
            col.enabled = true;

        if (NetworkManager.Singleton.LocalClientId == playerId)
        {
            RequestGrabServerRpc(objectId, playerId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestGrabServerRpc(ulong objectId, ulong playerId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(objectId, out var netObj))
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client))
            return;

        var grabPoint = client.PlayerObject.GetComponent<GrabPoint>();
        
        if (grabPoint != null)
        {
            grabPoint.TryGrab(netObj);
        }
    }

    #endregion

    #region Drop Timer

    public bool IsStock()
    {
        return storedObjectId.Value != 0;
    }
    
    private void Update()
    {
        if (storedObjectId.Value != 0 && interactionObject != null)
        {
            interactionObject.transform.position = stayPos.position;
        }

        if (!IsServer) return;
        if (storedObjectId.Value == 0) return;

        if (dropTimer.Value > 0)
            dropTimer.Value -= Time.deltaTime;
        else
            ForceDrop();
    }

    private void ForceDrop()
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(storedObjectId.Value, out var netObj))
        {
            storedObjectId.Value = 0;
            return;
        }

        if (netObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (netObj.TryGetComponent<Collider>(out var col))
            col.enabled = true;

        netObj.gameObject.layer = LayerMask.NameToLayer("Interactable");

        DropClientRpc(storedObjectId.Value);

        storedObjectId.Value = 0;
        interactionObject = null;
        dropTimer.Value = 0;
    }

    [ClientRpc]
    private void DropClientRpc(ulong objectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(objectId, out var netObj))
            return;

        netObj.gameObject.layer = LayerMask.NameToLayer("Interactable");
        
        if (netObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        
        if (netObj.TryGetComponent<Collider>(out var col))
            col.enabled = true;
    }

    #endregion
}