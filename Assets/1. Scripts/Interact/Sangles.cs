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
            interactionObject.transform.position = stayPos.position;
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

        // Le client demande au serveur
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

        // Vérifie que c'est bien un sac
        var grabbable = heldObject.GetComponent<GrabbableObject>();
        if (grabbable == null || grabbable.type != GrabType.Sac) return;

        NetworkObject objectNetObj = heldObject.GetComponent<NetworkObject>();
        if (objectNetObj == null) return;

        // 🔹 Lâcher l’objet côté serveur
        Rigidbody rb = objectNetObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (objectNetObj.TryGetComponent<Collider>(out var col))
            col.enabled = true;

        if (grabbable != null)
            grabbable.IsGrabbed.Value = false;

        // 🔹 Stocker sur la sangle
        storedObjectId.Value = objectNetObj.NetworkObjectId;
        interactionObject = objectNetObj;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (col != null)
            col.enabled = false;

        dropTimer.Value = Random.Range(mini_TimeBeforeDrop, max_TimeBeforeDrop);

        UpdateObjectPositionClientRpc(objectNetObj.NetworkObjectId, stayPos.position);
    }



    [ClientRpc]
    private void UpdateObjectPositionClientRpc(ulong objectId, Vector3 position)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(objectId, out var netObj))
            return;

        netObj.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        netObj.transform.position = position;
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

        // Redonner l’objet au bon joueur uniquement en local
        if (NetworkManager.Singleton.LocalClientId == playerId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client))
            {
                var grabPoint = client.PlayerObject.GetComponent<GrabPoint>();
                if (grabPoint != null)
                    grabPoint.TryGrab(netObj);
            }
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
    }

    #endregion
}
