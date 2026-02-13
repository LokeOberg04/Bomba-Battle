using Bezier;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BezierCurve))]
public class Bomba : MonoBehaviour
{
    public float knockback = 10;
    public float damage = 50;
    public float range = 10;
    public float explosionTime = 1;
    private float distance = 0;
    private BezierCurve bc;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bc = GetComponent<BezierCurve>();
    }

    // Update is called once per frame
    void Update()
    {
        distance += Time.deltaTime / explosionTime;

        transform.position = BezierCurve.GetPosition(bc.FirstPoint, bc.LastPoint, distance);
        Vector3 vForward = BezierCurve.GetForward(bc.FirstPoint, bc.LastPoint, distance);

        transform.rotation = Quaternion.LookRotation(vForward);



       Debug.Log(bc.LastPoint.m_fDistance);

        if (distance >= 1)
        {
            explodeServerRpc();
        }
    }

    [ServerRpc]
    private void explodeServerRpc()
    {
        foreach (Player player in GameManager.Instance.players)
        {
            float distance = Vector3.Distance(player.transform.position, gameObject.transform.position);
            if (distance < range)
            {
                //In range of explosion
                float playerDamage = damage * (1 - distance / range);
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
