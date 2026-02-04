using Unity.Netcode;
using UnityEngine;

public class TruckInteraction : NetworkBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Positions")]
    [SerializeField] private Transform driverPosition;
    [SerializeField] private Transform passengerSpawnPosition;
    
    // Network variable pour suivre qui est le conducteur
    private NetworkVariable<ulong> driverClientId = new NetworkVariable<ulong>(
        ulong.MaxValue, // ulong.MaxValue signifie "pas de conducteur"
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private TruckController truckController;
    
    private void Awake()
    {
        truckController = GetComponent<TruckController>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // S'assurer que driverPosition et passengerSpawnPosition sont définis
        if (driverPosition == null)
            driverPosition = truckController.driverPos;
        
        if (passengerSpawnPosition == null)
            passengerSpawnPosition = truckController.spawnPassager;
    }
    
    /// <summary>
    /// Appelé par le joueur pour tenter d'entrer dans le camion
    /// </summary>
    public void TryEnterTruck(FPSControllerMulti player)
    {
        if (!IsServer)
        {
            // Le client demande au serveur de le faire entrer
            RequestEnterTruckServerRpc(player.NetworkObjectId);
            return;
        }
        
        // Logique côté serveur
        EnterTruckServerLogic(player);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RequestEnterTruckServerRpc(ulong playerNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        // Trouver le joueur par son NetworkObjectId
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
        if (player.isInTruck) return; // Déjà dans le camion
        
        ulong playerId = player.OwnerClientId;
        bool isDriver = false;
        Vector3 targetPosition;
        
        // Vérifier s'il y a déjà un conducteur
        if (driverClientId.Value == ulong.MaxValue)
        {
            // Pas de conducteur, ce joueur devient le conducteur
            driverClientId.Value = playerId;
            targetPosition = driverPosition.position;
            isDriver = true;
            Debug.Log($"Player {playerId} devient le conducteur");
        }
        else
        {
            // Il y a déjà un conducteur, ce joueur devient passager
            targetPosition = passengerSpawnPosition.position;
            isDriver = false;
            Debug.Log($"Player {playerId} devient passager");
        }
        
        // Notifier le client spécifique
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
                // Appliquer les changements localement pour ce joueur
                player.EnterTruck(isDriver, spawnPosition);
            }
        }
    }
    
    /// <summary>
    /// Appelé par le joueur pour sortir du camion
    /// </summary>
    public void TryExitTruck(FPSControllerMulti player)
    {
        if (!IsServer)
        {
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
    
    private void ExitTruckServerLogic(FPSControllerMulti player)
    {
        if (!player.isInTruck) return;
        
        ulong playerId = player.OwnerClientId;
        
        // Si c'était le conducteur, retirer l'assignation
        if (driverClientId.Value == playerId)
        {
            driverClientId.Value = ulong.MaxValue;
            Debug.Log($"Player {playerId} n'est plus conducteur");
        }
        
        // Position de sortie (à côté du camion)
        Vector3 exitPosition = transform.position + transform.right * 3f;
        
        // Notifier le client
        NotifyPlayerExitedClientRpc(player.NetworkObjectId, exitPosition);
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
}