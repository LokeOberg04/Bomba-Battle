using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HeroSelect : ActionStack.ActionBehavior
{
    public GameObject select;

    public TextMeshProUGUI waitingText;

    public Player player;

    private GameObject pickedModel;

    private GameObject bomberHeroModel;
    private GameObject LQHeroModel;
    private GameObject gunslingerHeroModel;

    private Hero pickedHero;

    private bool countdownDone = false;

    public override bool IsDone()
    {
        return countdownDone;
    }

    public override void OnBegin(bool bFirstTime)
    {
        bomberHeroModel = Resources.Load<GameObject>("Prefabs/BomberModel");
        base.OnBegin(bFirstTime);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        GameManager.Instance.gameStarted.OnValueChanged += startGame;
    }

    public override void OnEnd()
    {
        base.OnEnd();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        player.PushAction(pickedHero);
        GameManager.Instance.gameStarted.OnValueChanged -= startGame;
        Destroy(gameObject);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
    }

    public void startGame(bool oldValue, bool newValue)
    {
        StartCoroutine(startCountdown());
    }

    IEnumerator startCountdown()
    {
        int time = 3;
        while (time > 0)
        {
            waitingText.text = time.ToString();
            yield return new WaitForSeconds(1);
            time--;
        }
        countdownDone = true;
        GameManager.Instance.startGame();
    }

    public void pickBomber()
    {
        pickedModel = bomberHeroModel;
        pickedHero = new Bomber(player);
        player.spawnModelRpc(player.gameObject, player,1);
        select.SetActive(false);
        waitingText.enabled = true;
        player.readyUpServerRpc();
    }

    public void pickLQ()
    {
        pickedModel = LQHeroModel;
        pickedHero = new LQ(player);
        player.spawnModelRpc(player.gameObject, player, 2);
        select.SetActive(false);
        waitingText.enabled = true;
        player.readyUpServerRpc();
    }

    public void pickGunslinger()
    {
        pickedModel = gunslingerHeroModel;
        pickedHero = new Gunslinger(player);
        player.spawnModelRpc(player.gameObject, player, 3);
        select.SetActive(false);
        waitingText.enabled = true;
        player.readyUpServerRpc();
    }

}
