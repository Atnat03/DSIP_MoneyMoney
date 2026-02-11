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
    public TMP_InputField inputLobbyCode;
    
    public string LocalPlayerName { get; private set; }
    
    public string CodeLobby { get; private set; }
    
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
        // Définir le nom du joueur
        if (!string.IsNullOrEmpty(inputName.text))
            LocalPlayerName = inputName.text;
        else
            LocalPlayerName = "Player " + Random.Range(0, 99);
        
        ConnectingTXT.SetActive(true);
        ElementsToConnect.SetActive(false);
        
        EnterGame();
    }

    public void ChangeColor(int colorIndex)
    {
        LocalPlayerColor = colorsList[colorIndex];
    }

    private async void EnterGame()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("NetworkManager already started!");
            return;
        }
    
        await InitializeServices();
        
        string lobbyCode = inputLobbyCode.text.Trim().ToUpper();
        
        if (string.IsNullOrEmpty(lobbyCode))
        {
            await CreateLobbyAndHost();
        }
        else
        {
            await JoinLobbyByCode(lobbyCode);
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
            
            string lobbyCode = GenerateLobbyCode();
            
            lobby = await LobbyService.Instance.CreateLobbyAsync(
                lobbyCode,
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
                        { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, relayCode) },
                        { "LobbyCode", new DataObject(DataObject.VisibilityOptions.Public, lobbyCode) } // ✅ Stocker le code
                    }
                }
            );

            NetworkManager.Singleton.StartHost();
            Debug.Log($"🟢 Host démarré | Lobby Code: {lobbyCode} | RelayCode: {relayCode} | Name: {LocalPlayerName}");
            
            ShowLobbyCode(lobbyCode);
            
            StartCanva.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur création lobby: {e.Message}");
            ResetConnection();
        }
    }

    private async Task JoinLobbyByCode(string lobbyCode)
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
                            QueryFilter.FieldOptions.Name,
                            lobbyCode,
                            QueryFilter.OpOptions.EQ
                        )
                    }
                }
            );

            if (response.Results.Count == 0)
            {
                Debug.LogError($"❌ Aucun lobby trouvé avec le code: {lobbyCode}");
                ShowError($"Lobby '{lobbyCode}' introuvable");
                ResetConnection();
                return;
            }

            lobby = response.Results[0];

            // Rejoindre le lobby
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

            lobby = await LobbyService.Instance.GetLobbyAsync(lobby.Id);
            string relayCode = lobby.Data["RelayCode"].Value;

            Debug.Log($"🔵 Client rejoint Lobby: {lobbyCode} | RelayCode: {relayCode} | Name: {LocalPlayerName}");

            await JoinRelay(relayCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Erreur rejoindre lobby: {e.Message}");
            ShowError("Impossible de rejoindre le lobby");
            ResetConnection();
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
            Debug.Log($"🔵 Client démarré | Name: {LocalPlayerName}");
            StartCanva.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur connexion relay: {e.Message}");
            ResetConnection();
        }
    }

    // ========================= HELPERS =========================

    private string GenerateLobbyCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] code = new char[6];
        
        for (int i = 0; i < 6; i++)
        {
            code[i] = chars[Random.Range(0, chars.Length)];
        }
        
        return new string(code);
    }

    private void ShowLobbyCode(string code)
    {
        Debug.Log($"📋 CODE LOBBY: {code}");
        CodeLobby = code;
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