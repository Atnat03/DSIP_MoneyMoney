using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    public GameObject player1Prefab;
    public Camera startCam;
    
    public Transform defaultSpawnPoint;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        if (IsServer)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                OnClientConnected(client.ClientId);
            }
        }
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