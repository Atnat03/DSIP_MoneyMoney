using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

#if UNITY_EDITOR
using UnityEditor.Multiplayer;
#endif

public class AutoJoinedLobby : MonoBehaviour
{
    public static AutoJoinedLobby Instance;

    private Lobby lobby;
    public int maxPlayers = 4;
    private bool isBusy = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
            return;

        await InitializeServices();

        await Task.Delay(100);

        Debug.Log("⏱ Checking for existing lobby…");
        await TryJoinOrCreateLobby();
    }


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

    private async Task TryJoinOrCreateLobby()
    {
        var options = new QueryLobbiesOptions
        {
            Count = 1,
            Filters = new List<QueryFilter>
            {
                new QueryFilter(
                    QueryFilter.FieldOptions.AvailableSlots,
                    "0",
                    QueryFilter.OpOptions.GT
                )
            }
        };

        QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

        if (response.Results.Count > 0)
        {
            lobby = response.Results[0];
            await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);

            string relayCode = lobby.Data["RelayCode"].Value;
            Debug.Log("Lobby trouvé → Join Relay: " + relayCode);

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
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, relayCode) }
                }
            }
        );

        NetworkManager.Singleton.StartHost();
        Debug.Log("🟢 Host démarré | RelayCode: " + relayCode);
    }

    // ========================= CLIENT =========================

    private async Task JoinRelay(string relayCode)
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

        NetworkManager.Singleton.StartClient();
        Debug.Log("🔵 Client StartClient()");
    }

    private async void OnDestroy()
    {
        if (lobby != null && lobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
        }
    }
}

