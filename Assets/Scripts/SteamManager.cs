using System;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class SteamManager : MonoBehaviour
{

    public uint appId;

    public static SteamManager Instance { get; private set; }

    public Lobby? currentLobby { get; private set; } = null;

    private FacepunchTransport transport = null;

    private void Awake()
    {

        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }


        //DontDestroyOnLoad(this);
        //try
        //{
        //    //Steamworks.SteamClient.Init(appId, true);
        //    Debug.Log("Steam works YAPPI!!!");
        //}
        //catch(System.Exception e) 
        //{
        //    Debug.Log(e.Message);
        //}

    }
    private void Start()
    {
        transport = GetComponent<FacepunchTransport>();

        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreated;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }



    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= OnLobbyGameCreated;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;

        if(NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    public async void StartHost()
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("started host");
        }

        // max members 2
        currentLobby = await SteamMatchmaking.CreateLobbyAsync(2);
    }

    public void StartClient(SteamId id)
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

        transport.targetSteamId = id;

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Client Started", this);
        }
    }

    private void OnApplicationQuit() => Disconnect();

    public void Disconnect()
    {
        currentLobby?.Leave();

        if(NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.Shutdown();
    }

    #region Network Callbacks

    private void OnServerStarted()
    {
        Debug.Log("server has started", this);
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        Debug.Log($"Client connected, clientId={clientId}");
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        Debug.Log($"Client disconnected, clientId={clientId}");

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;

    }

    #endregion

    #region Steam Callbacks

    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        StartClient(id);
    }

    private void OnLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId id)
    {

    }

    private void OnLobbyInvite(Friend friend, Lobby lobby)
    {
        Debug.Log($"You got an invite from {friend.Name}", this);
    }

    private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {

    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {

    }

    private void OnLobbyEntered(Steamworks.Data.Lobby lobby)
    {
        if(NetworkManager.Singleton.IsHost)
        {
            return;
        }

        StartClient(lobby.Id);
    }

    private void OnLobbyCreated(Result result, Steamworks.Data.Lobby lobby)
    {
        if(result != Result.OK)
        {
            Debug.LogError($"Lobby couldnt be created, {result}", this);
            return;
        }

        lobby.SetFriendsOnly();
        lobby.SetData("cool name", "cool lobby");
        lobby.SetJoinable(true);

        Debug.Log("Lobby created", this);
    }

    #endregion

}
