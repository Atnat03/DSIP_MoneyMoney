using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    public GameObject player1Prefab;
    public GameObject player2Prefab;
    public Camera startCam;
    
    public Transform defaultSpawnPoint;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        GameObject prefabToSpawn = (clientId == 0) ? player1Prefab : player2Prefab;
        GameObject playerInstance = Instantiate(prefabToSpawn);
    
        if (clientId != 0)
        {
            // IMPORTANT : Positionner le joueur AVANT de le spawner
            playerInstance.transform.position = TruckController.instance.spawnPlayer.position;
            playerInstance.transform.rotation = TruckController.instance.transform.rotation;
            
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            
            NotifyPlayerInTruckClientRpc(playerInstance.GetComponent<NetworkObject>().NetworkObjectId);
        }
        else
        {
            playerInstance.transform.position = defaultSpawnPoint.position;
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        }
    
        if (startCam != null)
            startCam.gameObject.SetActive(false);
    }

    [ClientRpc]
    private void NotifyPlayerInTruckClientRpc(ulong playerNetworkId)
    {
        print("NotifyPlayerInTruckClientRpc");
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkId, out NetworkObject playerNetObj))
        {
            var fpsController = playerNetObj.GetComponent<FPSControllerMulti>();
            if (fpsController != null && fpsController.IsOwner)
            {
                print("Calling GetInTruck on player");
                fpsController.GetInTruck();
            }
        }
    }
}