using System;
using Unity.Netcode;
using UnityEngine;

public class TruckInteraction : NetworkBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] public float interactionRange = 3f;
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Positions")]
    [SerializeField] private Transform driverPosition;
    [SerializeField] private Transform passengerSpawnPosition;
    [SerializeField] private Transform exitPosition;
    
    public NetworkVariable<ulong> driverClientId = new NetworkVariable<ulong>(
        ulong.MaxValue, 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> hasDriver = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private TruckController truckController;

    public GameObject door;
    
    private void Awake()
    {
        truckController = GetComponent<TruckController>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (driverPosition == null)
            driverPosition = truckController.driverPos;
        
        if (passengerSpawnPosition == null)
            passengerSpawnPosition = truckController.spawnPassager;
    }
    
    public void TryEnterTruck(FPSControllerMulti player)
    {
        if (!IsServer)
        {
            RequestEnterTruckServerRpc(player.NetworkObjectId);
            return;
        }
        
        EnterTruckServerLogic(player);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RequestEnterTruckServerRpc(ulong playerNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject playerNetObj))
        {
            FPSControllerMulti player = playerNetObj.GetComponent<FPSControllerMulti>();
            if (player != null)
            {
                EnterTruckServerLogic(player);
            }
        }
    }
    
    private void EnterTruckServerLogic(FPSControllerMulti player)
    {
        if (player.isInTruck) return; 
        
        ulong playerId = player.OwnerClientId;
        bool isDriver = false;
        Vector3 targetPosition = passengerSpawnPosition.position;
        
        if (driverClientId.Value == ulong.MaxValue)
        {
            driverClientId.Value = playerId;
            hasDriver.Value = true;
            targetPosition = driverPosition.position;
            isDriver = true;
            Debug.Log($"Player {playerId} devient le conducteur");
        }
        
        NotifyPlayerEnteredClientRpc(player.NetworkObjectId, isDriver, targetPosition);
    }
    
    [ClientRpc]
    private void NotifyPlayerEnteredClientRpc(ulong playerNetworkObjectId, bool isDriver, Vector3 spawnPosition)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject playerNetObj))
        {
            FPSControllerMulti player = playerNetObj.GetComponent<FPSControllerMulti>();
            if (player != null && player.IsOwner)
            {
                player.EnterTruck(isDriver, spawnPosition);
            }
        }
    }
    
    public void TryExitTruck(FPSControllerMulti player)
    {
        if (!IsServer)
        {
            print("Serveur request exit truck");
            RequestExitTruckServerRpc(player.NetworkObjectId);
            return;
        }
        
        ExitTruckServerLogic(player);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RequestExitTruckServerRpc(ulong playerNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject playerNetObj))
        {
            FPSControllerMulti player = playerNetObj.GetComponent<FPSControllerMulti>();
            if (player != null)
            {
                ExitTruckServerLogic(player);
            }
        }
    }
    
    public void ExitTruckServerLogic(FPSControllerMulti player)
    {
        ulong playerId = player.OwnerClientId;
        
        if (driverClientId.Value == playerId)
        {
            driverClientId.Value = ulong.MaxValue;
            hasDriver.Value = false;
            Debug.Log($"Player {playerId} n'est plus conducteur");
        }
        
        NotifyPlayerExitedClientRpc(player.NetworkObjectId, exitPosition.position);
    }
    
    [ClientRpc]
    private void NotifyPlayerExitedClientRpc(ulong playerNetworkObjectId, Vector3 exitPosition)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject playerNetObj))
        {
            FPSControllerMulti player = playerNetObj.GetComponent<FPSControllerMulti>();
            if (player != null && player.IsOwner)
            {
                player.ExitTruck(exitPosition);
            }
        }
    }
    
    public bool IsDriver(ulong clientId)
    {
        return driverClientId.Value == clientId;
    }

    public bool HasDriver()
    {
        return driverClientId.Value != ulong.MaxValue;
    }

    private void Update()
    {
        door.SetActive(!hasDriver.Value);
    }
}