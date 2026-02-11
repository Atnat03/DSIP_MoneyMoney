using System;
using System.Collections;
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
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += PrintMessageConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += PrintMessageDisconnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= PrintMessageConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= PrintMessageDisconnected;
        }
    }

    private void PrintMessageConnected(ulong clientId)
    {
        if (!IsServer) return;

        StartCoroutine(WaitForPlayerSpawn(clientId));
    }

    private IEnumerator WaitForPlayerSpawn(ulong clientId)
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            GameObject player = GetPlayerFromId(clientId);
            
            if (player != null)
            {
                PlayerCustom playerCustom = player.GetComponent<PlayerCustom>();
                
                if (playerCustom != null && !string.IsNullOrEmpty(playerCustom.PlayerName.Value.ToString()))
                {
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
                    yield break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning($"Timeout: Le joueur {clientId} n'a pas spawné à temps");
    }

    private void PrintMessageDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        
        GameObject player = GetPlayerFromId(clientId);
        if (player != null)
        {
            string playerName = player.GetComponent<PlayerCustom>()?.PlayerName.Value.ToString() ?? "Joueur inconnu";
            PlayerDisconnectedClientRpc(playerName);
        }
    }
    
    [ClientRpc]
    private void PlayerConnectedClientRpc(ulong clientId, ClientRpcParams rpcParams = default)
    {
        GameObject player = GetPlayerFromId(clientId);
        if (player == null)
        {
            Debug.LogWarning($"Client RPC: Joueur {clientId} non trouvé");
            return;
        }

        PlayerCustom playerCustom = player.GetComponent<PlayerCustom>();
        if (playerCustom == null)
        {
            Debug.LogWarning($"Client RPC: PlayerCustom manquant sur {clientId}");
            return;
        }

        string playerName = playerCustom.PlayerName.Value.ToString();
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "Joueur inconnu";
        }

        TextMeshProUGUI message = Instantiate(prefabMessage, parentUI);
        message.SetText($"{playerName} a rejoint la partie");
        Destroy(message.gameObject, 5f);
    }

    [ClientRpc]
    private void PlayerDisconnectedClientRpc(string playerName)
    {
        TextMeshProUGUI message = Instantiate(prefabMessage, parentUI);
        message.SetText($"{playerName} a quitté la partie");
        Destroy(message.gameObject, 5f);
    }

    public GameObject GetPlayerFromId(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            return client.PlayerObject?.gameObject;
        }
        return null;
    }
    
    public void PrintMessageOnKOPlayer(ulong clientID)
    {
        if (IsServer)
        {
            PlayerKOClientRpc(clientID);
        }
    }
    
    [ClientRpc]
    private void PlayerKOClientRpc(ulong clientId)
    {
        GameObject player = GetPlayerFromId(clientId);
        if (player == null) return;

        string playerName = player.GetComponent<PlayerCustom>()?.PlayerName.Value.ToString() ?? "Joueur inconnu";
        
        TextMeshProUGUI message = Instantiate(prefabMessage, parentUI);
        message.SetText($"{playerName} est tombé KO");
        Destroy(message.gameObject, 5f);
    }
}