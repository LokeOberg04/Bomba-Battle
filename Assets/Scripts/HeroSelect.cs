using UnityEngine;
using UnityEngine.UI;

public class HeroSelect : ActionStack.ActionBehavior
{
    bool heroPicked = false;

    public Player player;

    private Hero pickedHero;

    public override bool IsDone()
    {
        return heroPicked;
    }

    public override void OnBegin(bool bFirstTime)
    {
        base.OnBegin(bFirstTime);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public override void OnEnd()
    {
        base.OnEnd();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        player.PushAction(pickedHero);
        Destroy(gameObject);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
    }

    public void pickBomber()
    {
        //player.PushAction(new Bomber(player));
        pickedHero = new Bomber(player);
        heroPicked = true;
    }

    public void pickLQ()
    {

    }

    public void pickHero3()
    {

    }

}
