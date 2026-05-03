using Unity.Netcode;
using UnityEngine;

public class Bomberbomb : NetworkBehaviour
{

    public ulong shooterId;
    public float directDamage = 20;
    public float knockback = 5;
    public float damage = 25;
    public float range = 5;
    public float lifeTime = 5.0f;
    float explosionTime;
    public LayerMask whatIsMapGeometry;
    public NetworkBehaviourReference shooter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        explosionTime = Time.time + lifeTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time > explosionTime)
        {
            explodeServerRpc();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Player enemy = collision.gameObject.GetComponent<Player>();
        if(shooter.TryGet(out Player shooterScript))
        {
            if (enemy == shooterScript)
            {
                Debug.Log("hit urself");
                return;
            }
        }
        // hit wall
        if (enemy == null)
        {
            explodeServerRpc();
            return;
        }
        // hit enemy
        ulong enemyOwnerClientId = enemy.GetComponent<NetworkObject>().OwnerClientId;
        if (enemyOwnerClientId != shooterId)
        {
            if (enemy.deflecting.Value)
            {
                //Direct hit deflecting enemy
                Debug.Log("hit deflecting");
                enemy.spawnBulletServerRpc(enemy.gameObject, enemy, enemyOwnerClientId);
                destroyRpc();
                return;
            }
            //direct hit enemy
            Debug.Log("Direct hit");
            if (enemy.health.Value > directDamage)
            {
                enemy.takeDamageRpc(directDamage);
            }
            explodeServerRpc();
            return;
        }
    }

    [Rpc(SendTo.Owner)]
    public void destroyRpc()
    {
        Destroy(gameObject);
    }

    [ServerRpc]
    private void explodeServerRpc()
    {
        foreach(Player player in GameManager.Instance.players)
        {
            float distance = Vector3.Distance(player.transform.position, gameObject.transform.position);

            bool Los = !Physics.Raycast(transform.position, player.transform.position - transform.position, distance, whatIsMapGeometry);

            if (distance < range && Los && !player.spawnProtection.Value)
            {
                //In range of explosion and has Los

                float playerDamage = damage * (1 - distance / range);

                if (player.GetComponent<NetworkObject>().OwnerClientId == shooterId)
                {
                    playerDamage *= 0.1f;
                }
                player.takeDamageRpc(playerDamage);
                Rigidbody playerRb = player.GetComponent<Rigidbody>();
                Vector3 direction = player.transform.position - transform.position;
                Vector3 force = direction.normalized * knockback;
                //playerRb.AddForce(force, ForceMode.Impulse);
                player.takeKnockbackClientRpc(force);
                if (shooter.TryGet(out Player shooterScript))
                {
                    shooterScript.hitEnemyRpc();
                }
            }
        }
        ParticleManager.Instance.spawnExplosionClientRpc(transform.position);
        Destroy(gameObject);
    }

}
