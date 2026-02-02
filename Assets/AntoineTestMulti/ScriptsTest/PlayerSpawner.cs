using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    public GameObject player1Prefab;
    public GameObject player2Prefab;
    public Camera startCam;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDisable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        GameObject prefabToSpawn;

        if (clientId == 0)
        {
            prefabToSpawn = player1Prefab;
        }
        else
        {
            prefabToSpawn = player2Prefab;
        }

        GameObject playerInstance = Instantiate(prefabToSpawn);
        
        
        if (clientId == 1)
        {
            playerInstance.transform.position = TruckController.instance.spawnPlayer.position;
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            playerInstance.transform.SetParent(TruckController.instance.transform, true);
        }
        else
        {
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        }
        
        startCam.gameObject.SetActive(false);
    }

}
