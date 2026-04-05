using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroSelect : ActionStack.ActionBehavior
{
    bool heroPicked = false;

    public GameObject select;

    public TextMeshProUGUI waitingText;

    public Player player;

    private Hero pickedHero;

    private bool countdownDone = false;

    public override bool IsDone()
    {
        return countdownDone;
    }

    public override void OnBegin(bool bFirstTime)
    {
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
        pickedHero = new Bomber(player);
        heroPicked = true;
        select.SetActive(false);
        waitingText.enabled = true;
        player.readyUpServerRpc();
    }

    public void pickLQ()
    {
        pickedHero = new LQ(player);
        heroPicked = true;
        select.SetActive(false);
        waitingText.enabled = true;
        player.readyUpServerRpc();
    }

    public void pickGunslinger()
    {
        pickedHero = new Gunslinger(player);
        heroPicked = true;
        select.SetActive(false);
        waitingText.enabled = true;
        player.readyUpServerRpc();
    }

}
