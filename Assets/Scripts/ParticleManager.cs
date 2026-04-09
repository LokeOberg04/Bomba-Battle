using System.Collections;
using Bezier;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class ParticleManager : NetworkBehaviour
{
    private GameObject explosionPrefab;
    private GameObject LQZap;

    public static ParticleManager Instance { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        explosionPrefab = Resources.Load<GameObject>("Prefabs/Explosion");
        LQZap = Resources.Load<GameObject>("Prefabs/LQZap");
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

    [ClientRpc]
    public void spawnExplosionClientRpc(Vector3 inPos)
    {
        Instantiate(explosionPrefab, inPos, Quaternion.identity);
    }

    [ClientRpc]
    public void spawnRailClientRpc(Vector3 position, Vector3 direction)
    {
        StartCoroutine(spawnLQRail(position, direction));
    }

    IEnumerator spawnLQRail(Vector3 position, Vector3 direction)
    {
        GameObject zap = Instantiate(LQZap, position, Quaternion.identity);
        zap.transform.localScale = Vector3.one * 1;
        zap.transform.SetParent(transform, true);
        zap.transform.forward = direction;
        zap.name = "rail";
        //zap.GetComponent<NetworkObject>().Spawn();
        yield return new WaitForSeconds(1);
        despawnLQRail();
    }

    public void despawnLQRail()
    {
        GameObject zap = transform.Find("rail").gameObject;
        Destroy(zap);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
