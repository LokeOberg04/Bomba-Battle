using Bezier;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class ParticleManager : NetworkBehaviour
{
    private GameObject explosionPrefab;

    public static ParticleManager Instance { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        explosionPrefab = Resources.Load<GameObject>("Prefabs/Explosion");
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



    // Update is called once per frame
    void Update()
    {
        
    }
}
