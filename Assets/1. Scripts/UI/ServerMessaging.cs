using System;
using System.Linq;
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
        if (!IsServer) return;

        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds
                    .Where(id => id != clientId)
                    .ToArray()
            }
        };

        PlayerConnectedClientRpc(clientId, rpcParams);
    }


    private void PrintMessageDisconned(ulong clientId)
    {
        if (!IsServer) return;
        PlayerDisconnectedClientRpc(clientId);
    }
    
    [ClientRpc]
    private void PlayerConnectedClientRpc(
        ulong clientId,
        ClientRpcParams rpcParams = default)
    {
        GameObject player = GetPlayerFromId(clientId);
        if (player == null) return;

        TextMeshProUGUI message = Instantiate(prefabMessage, parentUI);
        message.SetText(
            player.GetComponent<PlayerCustom>().PlayerName.Value + " a rejoint la partie"
        );
        Destroy(message, 5f);
    }


    [ClientRpc]
    private void PlayerDisconnectedClientRpc(ulong clientId)
    {
        TextMeshProUGUI message = Instantiate(prefabMessage, parentUI);
        message.SetText(GetPlayerFromId(clientId).GetComponent<PlayerCustom>().PlayerName.Value + " a quitter la partie");
        Destroy(message, 5f);
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
    
    public void PrintMessageOnKOPlayer(ulong clientID)
    {
        PlayerKOClientRpc(clientID);
    }
    
    [ClientRpc]
    private void PlayerKOClientRpc(ulong clientId)
    {
        TextMeshProUGUI message = Instantiate(prefabMessage, parentUI);
        message.SetText(GetPlayerFromId(clientId).GetComponent<PlayerCustom>().PlayerName.Value + " est tombé KO");
        Destroy(message, 5f);
    }
}
