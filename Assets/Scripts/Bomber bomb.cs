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
        Player player = collision.gameObject.GetComponent<Player>();
        // hit wall
        if (player == null)
        {
            explodeServerRpc();
            return;
        }
        // hit enemy
        if (player.GetComponent<NetworkObject>().OwnerClientId != shooterId)
        {
            player.takeDamage(directDamage);
            explodeServerRpc();
            return;
        }
    }

    [ServerRpc]
    private void explodeServerRpc()
    {
        foreach(Player player in GameManager.Instance.players)
        {
            float distance = Vector3.Distance(player.transform.position, gameObject.transform.position);
            if (distance < range)
            {
                //In range of explosion
                float playerDamage = damage * (1 - distance / range);

                if (player.GetComponent<NetworkObject>().OwnerClientId == shooterId)
                {
                    playerDamage *= 0.1f;
                }
                player.takeDamage(playerDamage);
                Rigidbody playerRb = player.GetComponent<Rigidbody>();
                Vector3 direction = player.transform.position - transform.position;
                Vector3 force = direction.normalized * knockback;
                //playerRb.AddForce(force, ForceMode.Impulse);
                player.takeKnockbackClientRpc(force);
            }
        }
        Destroy(gameObject);
    }

}
