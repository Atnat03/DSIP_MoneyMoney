using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    public GameObject playerPrefab;
    public Camera startCam;
    
    public Transform defaultSpawnPoint;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        
        if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null)
            return;

        GameObject player = Instantiate(playerPrefab, defaultSpawnPoint.position, Quaternion.identity);
        NetworkObject networkObject = player.GetComponent<NetworkObject>();
        
        networkObject.SpawnAsPlayerObject(clientId, true);
    }
}