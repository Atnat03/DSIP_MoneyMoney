using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Random = UnityEngine.Random;

public class AutoJoinedLobby : MonoBehaviour
{
    public static AutoJoinedLobby Instance;

    private Lobby lobby;
    public int maxPlayers = 4;

    [Header("UI Fields")]
    public TMP_InputField inputName;
    public TMP_InputField inputRelayCode;
    
    public string LocalPlayerName { get; private set; }
    public int LocalPlayerSkin { get; private set; }
    public string RelayCode { get; private set; } 
    
    public Color LocalPlayerColor { get; set; }
    public Color[] colorsList;

    public GameObject StartCanva;
    public GameObject ConnectingTXT;
    public GameObject ElementsToConnect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        LocalPlayerColor = colorsList[0];
        
        ConnectingTXT.SetActive(false);

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Join()
    {
        if (!string.IsNullOrEmpty(inputName.text))
            LocalPlayerName = inputName.text;
        else
            LocalPlayerName = "Player " + Random.Range(0, 99);
        
        ConnectingTXT.SetActive(true);
        ElementsToConnect.SetActive(false);
        
        _ = EnterGame();
    }

    public void ChangeColor(int colorIndex)
    {
        LocalPlayerColor = colorsList[colorIndex];
    }

    public void ChangeSkin(int skinIndex)
    {
        LocalPlayerSkin = skinIndex;
    }

    private async Task EnterGame()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("NetworkManager already started!");
            return;
        }
    
        await InitializeServices();
        
        string relayCode = inputRelayCode.text.Trim();
        
        if (string.IsNullOrEmpty(relayCode))
        {
            await CreateLobbyAndHost();
        }
        else
        {
            await JoinByRelayCode(relayCode);
        }
    }

    // ========================= INIT =========================

    private async Task InitializeServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in: " + AuthenticationService.Instance.PlayerId);
        }
    }

    // ========================= HOST =========================

    private async Task CreateLobbyAndHost()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
            utp.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
            
            // ✅ OPTIONNEL : Créer un lobby pour tracker les joueurs (peut être retiré si non nécessaire)
            lobby = await LobbyService.Instance.CreateLobbyAsync(
                $"Game_{relayCode.Substring(0, 4)}", // Nom basé sur le code Relay
                maxPlayers,
                new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Player = new Player
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            { "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, LocalPlayerName) }
                        }
                    },
                    Data = new Dictionary<string, DataObject>
                    {
                        { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, relayCode) }
                    }
                }
            );

            NetworkManager.Singleton.StartHost();
            Debug.Log($"🟢 Host démarré | RelayCode: {relayCode} | Name: {LocalPlayerName}");
            
            ShowRelayCode(relayCode);
            
            StartCanva.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur création lobby: {e.Message}");
            ResetConnection();
        }
    }

    // ✅ NOUVEAU : Rejoindre directement avec le code Relay
    private async Task JoinByRelayCode(string relayCode)
    {
        try
        {
            Debug.Log($"🔵 Tentative de connexion avec RelayCode: {relayCode}");

            // ✅ OPTION 1 : Rejoindre DIRECTEMENT avec le code Relay (pas besoin de lobby)
            await JoinRelay(relayCode);

            // ✅ OPTION 2 (optionnel) : Chercher et rejoindre le lobby correspondant pour le tracking
            await TryJoinLobbyByRelayCode(relayCode);
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur rejoindre avec RelayCode: {e.Message}");
            ShowError($"Code Relay invalide: {relayCode}");
            ResetConnection();
        }
    }

    // ✅ OPTIONNEL : Chercher le lobby par code Relay pour le tracking
    private async Task TryJoinLobbyByRelayCode(string relayCode)
    {
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(
                new QueryLobbiesOptions
                {
                    Count = 100,
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(
                            QueryFilter.FieldOptions.S1, // Utilise un slot custom data
                            relayCode,
                            QueryFilter.OpOptions.EQ
                        )
                    }
                }
            );

            // Chercher manuellement le lobby avec le bon RelayCode
            foreach (var foundLobby in response.Results)
            {
                if (foundLobby.Data.TryGetValue("RelayCode", out var data) && data.Value == relayCode)
                {
                    lobby = foundLobby;
                    
                    await LobbyService.Instance.JoinLobbyByIdAsync(
                        lobby.Id,
                        new JoinLobbyByIdOptions
                        {
                            Player = new Player
                            {
                                Data = new Dictionary<string, PlayerDataObject>
                                {
                                    { "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, LocalPlayerName) }
                                }
                            }
                        }
                    );
                    
                    Debug.Log($"✅ Lobby rejoint: {lobby.Name}");
                    return;
                }
            }

            Debug.LogWarning("Aucun lobby trouvé avec ce RelayCode (normal si c'est juste du Relay pur)");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Impossible de rejoindre le lobby: {e.Message} (connexion Relay maintenue)");
        }
    }

    // ========================= CLIENT =========================

    private async Task JoinRelay(string relayCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
            utp.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

            NetworkManager.Singleton.StartClient();
            Debug.Log($"🔵 Client démarré avec RelayCode: {relayCode} | Name: {LocalPlayerName}");
            StartCanva.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur connexion relay: {e.Message}");
            throw; // Renvoyer l'erreur pour la gestion en amont
        }
    }

    // ========================= HELPERS =========================

    private void ShowRelayCode(string code)
    {
        Debug.Log($"📋 CODE RELAY: {code}");
        RelayCode = code;
        // TODO: Afficher dans un TextMeshPro
        // Exemple: relayCodeText.text = $"Code: {code}";
    }

    private void ShowError(string message)
    {
        Debug.LogError(message);
        // TODO: Afficher dans votre UI
    }

    private void ResetConnection()
    {
        ConnectingTXT.SetActive(false);
        ElementsToConnect.SetActive(true);
    }

    // ========================= CLEANUP =========================

    private async void OnDestroy()
    {
        if (lobby != null && lobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Erreur suppression lobby: {e.Message}");
            }
        }
    }
}