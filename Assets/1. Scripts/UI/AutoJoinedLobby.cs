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

    public TMP_InputField inputName;
    public string LocalPlayerName { get; private set; }
    public Color LocalPlayerColor { get; set; }
    public Color[] colorsList;

    public GameObject StartCanva;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        LocalPlayerColor = colorsList[0];

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Join()
    {
        if(inputName.text != "")
            LocalPlayerName = inputName.text;
        else
            LocalPlayerName = "Player " +  Random.Range(0, 99);
        
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
        await TryJoinOrCreateLobby();
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

    // ========================= LOBBY =========================

    private async Task TryJoinOrCreateLobby()
    {
        QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(
            new QueryLobbiesOptions { Count = 1 }
        );
        
        if (response.Results.Count > 0)
        {
            lobby = response.Results[0];
    
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

            Debug.Log($"🔵 Client rejoint Lobby | RelayCode: {relayCode} | Name: {LocalPlayerName}");

            await JoinRelay(relayCode);
        }
        else
        {
            await CreateLobbyAndHost();
        }

    }

    // ========================= HOST =========================

    private async Task CreateLobbyAndHost()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
        string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
        
        lobby = await LobbyService.Instance.CreateLobbyAsync(
            "AutoLobby",
            maxPlayers,
            new CreateLobbyOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        {
                            "Name",
                            new PlayerDataObject(
                                PlayerDataObject.VisibilityOptions.Public,
                                LocalPlayerName
                            )
                        }
                    }
                },
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "RelayCode",
                        new DataObject(DataObject.VisibilityOptions.Public, relayCode)
                    }
                }
            }
        );

        NetworkManager.Singleton.StartHost();
        Debug.Log("🟢 Host démarré | RelayCode: " + relayCode + " | Name: " + LocalPlayerName);
        StartCanva.SetActive(false);
    }

    // ========================= CLIENT =========================

    private async Task JoinRelay(string relayCode)
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

        NetworkManager.Singleton.StartClient();
        Debug.Log("🔵 Client démarré | Name: " + LocalPlayerName);
        StartCanva.SetActive(false);
    }
    
    private async void OnDestroy()
    {
        if (lobby != null && lobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
        }
    }
}
