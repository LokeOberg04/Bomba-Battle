using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public NetworkVariable<bool> isPaused = new NetworkVariable<bool>();

    private HashSet<Player> m_players = new HashSet<Player>();

    public HashSet<Player> players => m_players;

    public static GameManager Instance { get; private set; }

    private void OnEnable()
    {
        isPaused.OnValueChanged += onPausedChanged;
    }

    private void OnDisable()
    {
        isPaused.OnValueChanged -= onPausedChanged;
    }

    public void onPausedChanged(bool oldValue, bool newValue)
    {
        if(newValue)
        {
            PauseMenu.Open();
            isPaused.Value = true;
            Debug.Log("pausing");
            Time.timeScale = 0.0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            isPaused.Value = false;
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


    }

    public void addPlayer(Player player)
    {
        m_players.Add(player);
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
