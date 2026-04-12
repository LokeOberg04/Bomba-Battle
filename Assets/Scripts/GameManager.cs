using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public NetworkVariable<bool> isPaused = new NetworkVariable<bool>();

    private HashSet<Player> m_players = new HashSet<Player>();

    public NetworkList<int> score;

    public NetworkVariable<bool> gameStarted = new NetworkVariable<bool>();

    public HashSet<Player> players => m_players;

    [SerializeField]
    private Transform[] m_spawnPoints;

    public Transform[] spawnPoints => m_spawnPoints;

    public static GameManager Instance { get; private set; }

    private void OnEnable()
    {
        isPaused.OnValueChanged += onPausedChanged;
    }

    private void OnDisable()
    {
        isPaused.OnValueChanged -= onPausedChanged;
    }

    public void checkStartConditions(bool oldValue, bool newValue)
    {
        int reads = 0;
        foreach (var player in m_players)
        {
            reads += player.ready.Value ? 1 : 0;
        }
        if (reads > 1)
        {
            if(IsServer)
            {
                gameStarted.Value = true;
            }
        }
    }

    public void startGame()
    {
        foreach (Player player in m_players)
        {
            player.spawnPlayer();
        }
    }

    public void updateScore(ulong playerId)
    {
        if (GetComponent<NetworkObject>().OwnerClientId == playerId)
        {
            score[0]++;
        }
        else
        {
            score[1]++;
        }
    }

    public void onPausedChanged(bool oldValue, bool newValue)
    {
        if(newValue)
        {
            PauseMenu.Open();
            if(IsServer)
            {
                isPaused.Value = true;
            }
            Debug.Log("pausing");
            Time.timeScale = 0.0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            if (IsServer)
            {
                isPaused.Value = false;
            }
            Debug.Log("unpausing");
            Time.timeScale = 1.0f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void pauseServerRpc()
    {
        isPaused.Value = !isPaused.Value;
    }

    public Transform getSpawnPoint()
    {
        Vector3 killer = Vector3.zero;
        foreach(Player player in players)
        {
            if(player.health.Value > 0)
            {
                killer = player.transform.position;
            }
        }

        return spawnPoints.OrderBy(p => Vector3.Distance(p.position, killer)).ToList().Last();

    }

    public void respawnPlayers()
    {
        foreach (Player player in players)
        {
            player.healClientRpc();
            if (player.health.Value < 1)
            {
                player.respawnClientRpc();
            }
        }
    }

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

        score = new NetworkList<int>();
        score.Add(0);
        score.Add(0);

    }

    public void addPlayer(Player player)
    {
        m_players.Add(player);
        player.ready.OnValueChanged += checkStartConditions;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
