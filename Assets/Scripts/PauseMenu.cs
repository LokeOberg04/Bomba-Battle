using Bezier;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PauseMenu : ActionStack.ActionBehavior
{
    static GameObject prefab;

    public override bool IsDone()
    {
        return !GameManager.Instance.isPaused.Value;
    }

    public override void OnBegin(bool bFirstTime)
    {
        base.OnBegin(bFirstTime);
    }

    public override void OnEnd()
    {
        base.OnEnd();
        Destroy(gameObject);
    }

    public void OnResume()
    {
        GameManager.Instance.pauseServerRpc();
    }

    public void OnQuit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (!GameManager.Instance.isPaused.Value)
            {
                return;
            }
            Debug.Log("pause menu p");
            OnResume();
        }
    }


    public static void Open()
    {
        if(prefab == null)
        {
            prefab = Resources.Load<GameObject>("Prefabs/PauseMenu");
        }

        GameObject pauseMenuGO = Instantiate(prefab);
        PauseMenu pauseMenu = pauseMenuGO.GetComponent<PauseMenu>();
        foreach(Player player in GameManager.Instance.players)
        {
            if(player.IsOwner)
            {
                player.PushAction(pauseMenu);
            }
        }
    }
}
