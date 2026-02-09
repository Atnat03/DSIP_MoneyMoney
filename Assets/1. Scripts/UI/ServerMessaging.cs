using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ServerMessaging : NetworkBehaviour
{
    public Transform parentUI;
    public TextMeshProUGUI prefabMessage;

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += PrintMessageConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += PrintMessageDisconned;
    }

    private void PrintMessageConnected(ulong clientId)
    {
        PlayerConnectedClientRpc(clientId);
    }

    private void PrintMessageDisconned(ulong clientId)
    {
        PlayerDisconnectedClientRpc(clientId);
    }
    
    [ClientRpc]
    private void PlayerConnectedClientRpc(ulong clientId)
    {
        Debug.Log($"[INFO] Joueur {clientId} a rejoint la partie");
        
        TextMeshProUGUI message = Instantiate(prefabMessage, parentUI);
        message.SetText(GetPlayerFromId(clientId).GetComponent<PlayerCustom>().PlayerName.Value + " a rejoint la partie");
        Destroy(message.gameObject, 5f);
    }

    [ClientRpc]
    private void PlayerDisconnectedClientRpc(ulong clientId)
    {
        Debug.Log($"[INFO] Joueur {clientId} a quitté la partie");
        
        TextMeshProUGUI message = Instantiate(prefabMessage, parentUI);
        message.SetText(GetPlayerFromId(clientId).GetComponent<PlayerCustom>().PlayerName.Value + " a quitter la partie");
        Destroy(message.gameObject, 5f);
    }

    public GameObject GetPlayerFromId(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            NetworkObject playerObject = client.PlayerObject;

            if (playerObject != null)
            {
                GameObject playerGO = playerObject.gameObject;
                return playerGO;
            }
        }

        return null;
    }
}
