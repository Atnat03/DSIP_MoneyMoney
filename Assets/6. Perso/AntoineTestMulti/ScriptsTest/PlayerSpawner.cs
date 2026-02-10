using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    public GameObject player1Prefab;
    public Camera startCam;
    
    public Transform defaultSpawnPoint;


    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

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

        GameObject playerInstance = Instantiate(player1Prefab);
        playerInstance.transform.position = defaultSpawnPoint.position;
        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    
        if (startCam != null)
            startCam.gameObject.SetActive(false);
    }
}