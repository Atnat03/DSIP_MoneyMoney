using System;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class Sangles : NetworkBehaviour, IInteractible
{
    public Transform stayPos;
    
    public string InteractionName
    {
        get { return interactionName; }
        set { interactionName = value; }
    }
    
    private NetworkVariable<ulong> storedObjectId = new NetworkVariable<ulong>(
        0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );
    
    private NetworkObject interactionObject = null;
    
    [Header("Timer before drop")]
    [SerializeField] float max_TimeBeforeDrop = 10f;
    [SerializeField] float mini_TimeBeforeDrop = 5f;
    private NetworkVariable<float> dropTimer = new NetworkVariable<float>(
        0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public string interactionName;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        storedObjectId.OnValueChanged += OnStoredObjectChanged;
        
        if (storedObjectId.Value != 0)
        {
            UpdateStoredObjectReference();
        }
    }

    public override void OnNetworkDespawn()
    {
        storedObjectId.OnValueChanged -= OnStoredObjectChanged;
        base.OnNetworkDespawn();
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
        }
        else if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(storedObjectId.Value, out var netObj))
        {
            interactionObject = netObj;
            
            if (interactionObject != null)
            {
                interactionObject.transform.position = stayPos.position;
            }
        }
    }

    public bool IsStock()
    {
        return storedObjectId.Value != 0;
    }
    
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
        if (obj.GetInstanceID() != gameObject.GetInstanceID()) return;
        if (!player.GetComponent<NetworkObject>().IsOwner) return;

        Debug.Log("Hit interact");

        if (storedObjectId.Value == 0)
        {
            StockObject(player);
        }
        else
        {
            ReleaseObjectServerRpc(player.GetComponent<NetworkObject>().OwnerClientId, true);
        }
    }

    private void StockObject(GameObject player)
    {
        GrabPoint grabPoint = player.GetComponent<GrabPoint>();
        if (grabPoint == null || !grabPoint.IsSacInHand()) return;

        GameObject heldObject = grabPoint.GetCurrentObjectInHand();
        if (heldObject == null) return;

        NetworkObject netObj = heldObject.GetComponent<NetworkObject>();
        if (netObj == null) return;

        StockObjectServerRpc(netObj.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StockObjectServerRpc(ulong objectId, ServerRpcParams rpc = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
            return;

        ulong playerId = rpc.Receive.SenderClientId;
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client))
        {
            var grabPoint = client.PlayerObject.GetComponent<GrabPoint>();
            if (grabPoint != null)
            {
                grabPoint.Throw();
            }
        }

        storedObjectId.Value = objectId;
        interactionObject = netObj;

        if (netObj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (netObj.TryGetComponent<Collider>(out var col))
        {
            col.enabled = false;
        }

        dropTimer.Value = Random.Range(mini_TimeBeforeDrop, max_TimeBeforeDrop);

        UpdateObjectPositionClientRpc(objectId, stayPos.position);
    }

    [ClientRpc]
    private void UpdateObjectPositionClientRpc(ulong objectId, Vector3 position)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
            return;

        netObj.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        netObj.transform.position = position;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReleaseObjectServerRpc(ulong playerId, bool isGrabbing, ServerRpcParams rpc = default)
    {
        if (storedObjectId.Value == 0) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(storedObjectId.Value, out var netObj))
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
        {
            col.enabled = true;
        }

        ReleaseObjectClientRpc(storedObjectId.Value, playerId, isGrabbing);
        
        storedObjectId.Value = 0;
        interactionObject = null;
        dropTimer.Value = 0;
    }

    [ClientRpc]
    private void ReleaseObjectClientRpc(ulong objectId, ulong playerId, bool isGrabbing)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
            return;

        netObj.gameObject.layer = LayerMask.NameToLayer("Interactable");

        if (isGrabbing && NetworkManager.Singleton.LocalClientId == playerId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client))
            {
                var grabPoint = client.PlayerObject.GetComponent<GrabPoint>();
                if (grabPoint != null)
                {
                    grabPoint.TryGrab(netObj);
                }
            }
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        if (storedObjectId.Value == 0) return;
        
        if (dropTimer.Value > 0)
        {
            dropTimer.Value -= Time.deltaTime;
        }
        else
        {
            DropServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DropServerRpc()
    {
        if (storedObjectId.Value == 0) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(storedObjectId.Value, out var netObj))
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
        {
            col.enabled = true;
        }

        DropClientRpc(storedObjectId.Value);
        
        storedObjectId.Value = 0;
        interactionObject = null;
        dropTimer.Value = 0;
    }

    [ClientRpc]
    private void DropClientRpc(ulong objectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
            return;

        netObj.gameObject.layer = LayerMask.NameToLayer("Interactable");
    }
    
    public Outline[] Outline     
    {
        get { return outline; ; }
        set { }
    }

    public Outline[] outline;
}