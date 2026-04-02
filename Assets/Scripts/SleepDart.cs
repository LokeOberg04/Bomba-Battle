using Unity.Netcode;
using UnityEngine;

public class SleepDart : NetworkBehaviour
{
    public ulong shooterId;
    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();

        //hit wall
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        ulong playerId = player.GetComponent<NetworkObject>().OwnerClientId;

        // hit enemy
        if (playerId != shooterId)
        {
            sleepTargetClientRpc(playerId);
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    public void sleepTargetClientRpc(ulong playerId)
    {
        foreach (Player player in GameManager.Instance.players)
        {
            if (player.GetComponent<NetworkObject>().OwnerClientId == playerId)
            {
                player.PushAction(new Sleeping(player));
            }
        }
    }

    public class Sleeping : ActionStack.ActionBehavior
    {
        float sleepDuration = 5;

        private Player player;

        private float playerInitialHealth;

        public Sleeping(Player inPlayer)
        {
            player = inPlayer;
        }

        public override bool IsDone()
        {
            return sleepDuration <= 0 || player.health.Value != playerInitialHealth;
        }

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            player.moveDirection = new Vector3(0, 0, 0);
            playerInitialHealth = player.health.Value;
            player.sleepingText.enabled = true;
        }

        public override void OnEnd()
        {
            base.OnEnd();
            player.sleepingText.enabled = false;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            sleepDuration -= Time.deltaTime;
        }
    }

}
