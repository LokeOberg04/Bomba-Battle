using Unity.Netcode;
using UnityEngine;

public class SleepDart : NetworkBehaviour
{
    public ulong shooterId;
    public float speed;
    public NetworkBehaviourReference shooter;
    private void OnCollisionEnter(Collision collision)
    {
        Player enemy = collision.gameObject.GetComponent<Player>();

        //hit wall
        if (enemy == null)
        {
            Destroy(gameObject);
            return;
        }

        ulong enemyId = enemy.GetComponent<NetworkObject>().OwnerClientId;

        if (enemy.deflecting.Value)
        {
            //hit deflecting enemy
            deflectRpc(enemy.gameObject, enemy, enemyId);
            return;
        }

        // hit enemy
        if (enemyId != shooterId)
        {
            if(shooter.TryGet(out Player player))
            {
                player.hitEnemyRpc();
            }
            sleepTargetClientRpc(enemyId);
            Destroy(gameObject);
        }
    }

    [Rpc(SendTo.Server)]
    public void deflectRpc(NetworkObjectReference inPlayer, NetworkBehaviourReference inPlayerScript, ulong enemyId)
    {
        if(inPlayer.TryGet(out NetworkObject player))
        {
            if(inPlayerScript.TryGet(out Player playerScript))
            {
                playerScript.gunslingerSleepServerRpc(player.gameObject, shooter, speed, enemyId);
                Destroy(gameObject);
            }
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
