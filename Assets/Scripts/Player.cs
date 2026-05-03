using System;
using System.Collections;
using System.Collections.Generic;
using Bezier;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;

public class Player : ActionStack
{
    private float m_maxHealth = 100;

    public NetworkVariable<bool> ready = new NetworkVariable<bool>();

    public NetworkVariable<float> m_health = new NetworkVariable<float>(100f);

    public NetworkVariable<float> health => m_health;

    public float maxHealth => m_maxHealth;

    public NetworkVariable<bool> spawnProtection = new NetworkVariable<bool>(false,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);

    private float spawnProtectionTime = 0f;

    Rigidbody rb;

    public GameObject capsule;

    private AudioSource audioSource;

    public AudioClip hitmarkerSound;

    private Coroutine hitmarkerCoroutine;

    private GameObject bomb;
    private GameObject bomba;
    private GameObject sleepDart;
    private GameObject LQZap;

    public GameObject bomberWeapon;
    public GameObject LQWeapon;
    public GameObject gunslingerWeapon;
    public GameObject gunslingerShotgun;

    private GameObject bomberHeroModel;
    private GameObject LQHeroModel;
    private GameObject gunslingerHeroModel;

    public GameObject LQDeflectSphere;

    public EHero hero = EHero.None;

    public GameObject weapon;

    public Animator animator;

    public Animator modelAnimator;

    public GameObject model;

    public TextMeshProUGUI sleepingText;

    private FPSCamera FPSCamera;

    public float projectileSpeed = 2000f;

    float horizontalInput;
    float verticalInput;

    public Vector3 moveDirection;

    public UnityEngine.UI.RawImage Hitmarker;

    public UnityEngine.UI.Image healthbarUI;

    public UnityEngine.UI.Image healthbarWorld;

    public float dashes = 2;
    public float dashCooldown = 2.5f;
    public float dashResetTime;

    public TextMeshProUGUI dashText;

    public TextMeshProUGUI scoreText;

    public Transform spawnPoint;

    public UnityEngine.UI.Slider shootSlider;
    public UnityEngine.UI.Slider bombaSlider;
    public UnityEngine.UI.Slider ability2Slider;

    public float moveSpeed = 5.0f;
    public float airMoveSpeed = 15.0f;
    public float jumpHeight = 4.0f;

    public float shootCooldown = 0;

    public NetworkVariable<float> LQCharge = new NetworkVariable<float>(0);

    public NetworkVariable<bool> deflecting = new NetworkVariable<bool>(false);

    public LayerMask whatIsGround;

    public bool grounded;

    bool crouched;

    Vector3 scale;

    public enum EHero
    {
        None,
        Bomber,
        LQ,
        Gunslinger
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //rb = GetComponent<Rigidbody>();
        //rb.freezeRotation = true;
        //scale = transform.localScale;
        //PushAction(new Bomber(this));
        //GameManager.Instance.addPlayer(this);
        //dashText = GetComponentInChildren<TextMeshProUGUI>();
    }

    [ServerRpc]
    public void readyUpServerRpc()
    {
        ready.Value = true;
    }

    [Rpc(SendTo.Everyone)]
    public void gameStartedRpc()
    {
        //GetComponentInChildren<Camera>(true).enabled = IsOwner ? true : false;
        FPSCamera?.lockCamera();
        foreach(SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            renderer.enabled = false;
        }
        foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.enabled = false;
        }
        foreach (var item in NetworkManager.Singleton.ConnectedClients)
        {
            item.Value.PlayerObject?.GetComponent<Player>().updateHealthbars();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        scale = capsule.transform.localScale;
        //PushAction(new Bomber(this));
        GameManager.Instance.addPlayer(this);
        //dashText = GetComponentInChildren<TextMeshProUGUI>(true);
        bomberHeroModel = Resources.Load<GameObject>("Prefabs/BomberModel");
        LQHeroModel = Resources.Load<GameObject>("Prefabs/LightningQueen");
        gunslingerHeroModel = Resources.Load<GameObject>("Prefabs/Gunslinger");
        bomb = Resources.Load<GameObject>("Prefabs/Bomberbomb");
        bomba = Resources.Load<GameObject>("Prefabs/Bomba");
        sleepDart = Resources.Load<GameObject>("Prefabs/SleepDart");
        LQZap = Resources.Load<GameObject>("Prefabs/LQZap");
        healthbarUI.fillAmount = 1;
        healthbarWorld.fillAmount = 1;
        shootCooldown = 0;

        if (IsOwner)
        {
            audioSource = GetComponent<AudioSource>();
            healthbarUI.enabled = true;
            healthbarWorld.GetComponentInParent<Canvas>().enabled = false;
            GetComponentInChildren<Camera>(true).enabled = true;
            sleepingText.gameObject.SetActive(true);
            FPSCamera = GetComponentInChildren<FPSCamera>();
            GetComponentInChildren<AudioListener>().enabled = true;
            //dashText.enabled = true;
            GameObject prefab = Resources.Load<GameObject>("Prefabs/HeroSelect");
            GameObject heroSelectGO = Instantiate(prefab);
            HeroSelect heroSelect = heroSelectGO.GetComponent<HeroSelect>();
            heroSelect.player = this;
            PushAction(heroSelect);
        }


        respawnClientRpc();
    }

    private void OnEnable()
    {
        m_health.OnValueChanged += onHealthChanged;
        GameManager.Instance.score.OnListChanged += updateScore;
    }

    private void OnDisable()
    {
        m_health.OnValueChanged -= onHealthChanged;
        GameManager.Instance.score.OnListChanged -= updateScore;
    }

    public void updateScore(NetworkListEvent<int> changeEvent)
    {
        scoreText.text = GameManager.Instance.score[0].ToString() + "-" + GameManager.Instance.score[1].ToString();
    }

    [Rpc(SendTo.Owner)]
    public void hitEnemyRpc()
    {
        audioSource.PlayOneShot(hitmarkerSound);

        if (hitmarkerCoroutine != null)
        {
            StopCoroutine(hitmarkerCoroutine);
            hitmarkerCoroutine = null;
        }

        hitmarkerCoroutine = StartCoroutine(hitmarkerAnimation());
    }

    IEnumerator hitmarkerAnimation()
    {
        Hitmarker.gameObject.SetActive(true);
        float t = 0;
        float duration = 0.5f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = 1 - (t / duration);
            Hitmarker.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
        Hitmarker.gameObject.SetActive(false);
        hitmarkerCoroutine = null;
        
    }

    [Rpc(SendTo.Server)]
    public void spawnModelRpc(NetworkObjectReference inPlayer, NetworkBehaviourReference inPlayerScript, int model)
    {
        if (inPlayer.TryGet(out NetworkObject networkPlayer))
        {
            GameObject spawnedModel = Instantiate(model == 1 ? bomberHeroModel : model == 2 ? LQHeroModel : gunslingerHeroModel, networkPlayer.transform);
            spawnedModel.GetComponent<NetworkObject>().Spawn();
            spawnedModel.transform.SetParent(networkPlayer.transform, true);
            if (inPlayerScript.TryGet(out Player playerScript))
            {
                playerScript.setModelRpc(spawnedModel);
                spawnedModel.GetComponent<NetworkObject>().ChangeOwnership(networkPlayer.OwnerClientId);
            }
            else
            {
                Debug.LogError("couldnt get playerscript");
            }
        }
        else
        {
            Debug.LogError("couldnt get player when spawning model");
        }
    }

    [Rpc(SendTo.Owner)]
    public void setModelRpc(NetworkObjectReference inModel)
    {
        if (inModel.TryGet(out NetworkObject nModel))
        {
            model = nModel.gameObject;
            modelAnimator = nModel.GetComponent<Animator>();
        }
    }

    [Rpc(SendTo.Server,InvokePermission = RpcInvokePermission.Everyone)]
    public void spawnBulletServerRpc(NetworkObjectReference inPlayer, NetworkBehaviourReference inPlayerScript, ulong shooterId)
    {
        if(inPlayer.TryGet(out NetworkObject player))
        {
            Transform cameraTransform = player.GetComponentInChildren<Camera>().transform;

            GameObject spawnedBomb = Instantiate(bomb, cameraTransform.position + cameraTransform.forward, cameraTransform.rotation);
            Bomberbomb bombScript = spawnedBomb.GetComponent<Bomberbomb>();
            bombScript.shooterId = player.GetComponent<NetworkObject>().OwnerClientId;
            bombScript.shooter = inPlayerScript;
            Rigidbody bombRb = spawnedBomb.GetComponent<Rigidbody>();
            bombRb.AddForce(spawnedBomb.transform.forward * projectileSpeed);
            spawnedBomb.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            Debug.LogError("playern ot found");
        }
    }

    [Rpc(SendTo.Server)]
    public void gunslingerShotRpc(int damage, NetworkObjectReference target)
    {
        if (target.TryGet(out NetworkObject player))
        {
            // deal damage or something to target object.
            Transform cameraTransform = player.GetComponentInChildren<Camera>().transform;
            RaycastHit hit;
            if (Physics.Raycast(cameraTransform.position + cameraTransform.forward * 0.1f, cameraTransform.forward, out hit))
            {
                ParticleManager.Instance.spawnBulletTrailRpc(cameraTransform.position, hit.point);
                Debug.DrawLine(cameraTransform.position, hit.point, Color.green, 10);
                Player enemy = hit.collider.GetComponentInParent<Player>();
                if (enemy == null)
                {
                    //hit wall
                    return;
                }
                if (enemy.deflecting.Value)
                {
                    //hit deflecting enemy
                    enemy.gunslingerShotRpc(damage, enemy.gameObject);
                    return;
                }
                //hit enemy
                enemy.takeDamageRpc(damage);
                hitEnemyRpc();
            }
        }
        else
        {
            // Target not found on server, likely because it already has been destroyed/despawned.
            Debug.LogError("target not found!");
        }


    }

    [Rpc(SendTo.Everyone)]
    public void spawnLQZapRpc(NetworkObjectReference networkPlayer)
    {
        if (networkPlayer.TryGet(out NetworkObject player))
        {
            Transform cameraTransform = player.GetComponentInChildren<Camera>().transform;
            GameObject zap = Instantiate(LQZap, IsOwner ? LQWeapon.transform.position : player.transform.position + Vector3.up * 0.75f, Quaternion.identity);
            zap.transform.forward = cameraTransform.forward;
            zap.transform.SetParent(cameraTransform, true);
            zap.name = "zap";
        }
    }

    [Rpc(SendTo.Everyone)]
    public void despawnLQZapRpc(NetworkObjectReference networkPlayer)
    {
        if (networkPlayer.TryGet(out NetworkObject player))
        {
            Transform cameraTransform = player.GetComponentInChildren<Camera>().transform;
            GameObject zap = cameraTransform.Find("zap").gameObject;
            Destroy(zap);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void toggleLQDeflectRpc(bool on)
    {
        if(IsServer)
        {
            deflecting.Value = on;
        }
        
        LQDeflectSphere.gameObject.SetActive(on);
    }

[Rpc(SendTo.Server)]
    public void LQShotRpc(float damage, float chargePerDmg, NetworkObjectReference target, NetworkBehaviourReference inPlayerScript)
    {
        if (target.TryGet(out NetworkObject player))
        {
            Transform cameraTransform = player.GetComponentInChildren<Camera>().transform;

            RaycastHit hit;
            if (Physics.Raycast(cameraTransform.position + cameraTransform.forward * 0.1f, cameraTransform.forward, out hit))
            {
                Debug.DrawLine(cameraTransform.position, hit.point, Color.green, 10);
                Player enemy = hit.collider.GetComponentInParent<Player>();
                if (enemy == null)
                {
                    //hit wall
                    return;
                }
                if (enemy.deflecting.Value)
                {
                    //hit deflecting enemy
                    enemy.LQShotRpc(damage,chargePerDmg,enemy.gameObject,enemy);
                    return;
                }
                //hit enemy
                enemy.takeDamageRpc(damage);
                hitEnemyRpc();
                if (inPlayerScript.TryGet(out Player playerScript))
                {
                    playerScript.LQCharge.Value += chargePerDmg * damage;
                    playerScript.LQCharge.Value = Mathf.Clamp(playerScript.LQCharge.Value, 0, 100);
                }
            }
        }
        else
        {
            // Target not found on server, likely because it already has been destroyed/despawned.
            Debug.LogError("target not found!");
        }
    }

    [Rpc(SendTo.Server)]
    public void LQRailRpc(float damage, NetworkObjectReference inPlayer, NetworkBehaviourReference inPlayerScript)
    {
        if(inPlayerScript.TryGet(out Player playerScript ))
        {
            playerScript.LQCharge.Value = 0;
        }
        if (inPlayer.TryGet(out NetworkObject player))
        {
            Transform cameraTransform = player.GetComponentInChildren<Camera>().transform;

            ParticleManager.Instance.spawnRailClientRpc(LQWeapon.transform.position, cameraTransform.forward);

            RaycastHit hit;
            if (Physics.Raycast(cameraTransform.position + cameraTransform.forward * 0.1f, cameraTransform.forward, out hit))
            {
                Debug.DrawLine(cameraTransform.position, hit.point, Color.green, 10);
                Player enemy = hit.collider.GetComponentInParent<Player>();
                if (enemy == null)
                {
                    //hit wall
                    return;
                }
                if (enemy.deflecting.Value)
                {
                    //hit deflecting enemy
                    enemy.LQRailRpc(damage, enemy.gameObject, enemy);
                    return;
                }
                //hit enemy
                enemy.takeDamageRpc(damage);
                hitEnemyRpc();
            }
        }
        else
        {
            // Target not found on server, likely because it already has been destroyed/despawned.
            Debug.LogError("target not found!");
        }
    }

    [Rpc(SendTo.Server)]
    public void gunslingerShotgunShotRpc(int damage, NetworkObjectReference inPlayer, int shots, float spread)
    {
        Debug.Log(shots + " shots");
        if(inPlayer.TryGet(out NetworkObject player))
        {
            Transform cameraTransform = player.GetComponentInChildren<Camera>().transform;
            for (int i = 0; i < shots; i++)
            {
                Debug.Log("shotgun pellet");
                Vector3 direction = cameraTransform.forward;
                direction += cameraTransform.right * UnityEngine.Random.Range(-spread, spread);
                direction += cameraTransform.up * UnityEngine.Random.Range(-spread, spread);
                RaycastHit hit;
                if (Physics.Raycast(cameraTransform.position + cameraTransform.forward * 0.1f, direction, out hit))
                {
                    ParticleManager.Instance.spawnBulletTrailRpc(cameraTransform.position, hit.point);
                    Debug.DrawLine(cameraTransform.position, hit.point, Color.green, 10);
                    Player enemy = hit.collider.GetComponentInParent<Player>();
                    if(enemy == null)
                    {
                        //hit wall
                        continue;
                    }
                    if(enemy.deflecting.Value)
                    {
                        //hit deflecting enemy
                        gunslingerShotRpc(damage, enemy.gameObject);
                        continue;
                    }
                    //hit enemy
                    enemy?.takeDamageRpc(damage);
                    hitEnemyRpc();
                }
            }

        }
    }

    [Rpc(SendTo.Server,InvokePermission = RpcInvokePermission.Everyone)]
    public void gunslingerSleepServerRpc(NetworkObjectReference inPlayer, NetworkBehaviourReference inPlayerScript, float speed, ulong shooterId)
    {
        if(inPlayer.TryGet(out NetworkObject player))
        {
            Transform cameraTransform = player.GetComponentInChildren<Camera>().transform;
            GameObject spawnedDart = Instantiate(sleepDart, cameraTransform.position + cameraTransform.forward * 2, cameraTransform.rotation);
            SleepDart dart = spawnedDart.GetComponent<SleepDart>();
            dart.shooterId = shooterId;
            dart.speed = speed;
            dart.shooter = inPlayerScript;
            Rigidbody DartRb = spawnedDart.GetComponent<Rigidbody>();
            DartRb.AddForce(spawnedDart.transform.forward * speed);
            spawnedDart.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            Debug.LogError("couldnt find player");
        }

    }

    [ServerRpc]
    public void spawnBombaServerRpc(NetworkBehaviourReference inPlayerScript, Vector3 Player, Vector3 Target)
    {
        GameObject spawnedBomba = Instantiate(bomba, Player, Quaternion.identity);
        spawnedBomba.GetComponent<Bomba>().shooter = inPlayerScript;
        BezierCurve.ControlPoint playerControlPoint = new BezierCurve.ControlPoint() { m_vPosition = Player, m_vTangent = Vector3.up * 10 };
        BezierCurve.ControlPoint targetControlPoint = new BezierCurve.ControlPoint() { m_vPosition = Target, m_vTangent = Vector3.down * 10 };
        spawnedBomba.GetComponent<BezierCurve>().m_points.Add(playerControlPoint);
        spawnedBomba.GetComponent<BezierCurve>().m_points.Add(targetControlPoint);
        spawnedBomba.GetComponent<NetworkObject>().Spawn();
    }

    [Rpc(SendTo.Authority)]
    public void takeDamageRpc(float dmg)
    {
        if(spawnProtection.Value)
        {
            return;
        }
        m_health.Value -= dmg;
    }

    [ClientRpc]
    public void takeKnockbackClientRpc(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
    }

    public void onHealthChanged(float oldValue, float newValue)
    {
        updateHealthbars();
        if (newValue <= 0)
        {
            die();
        }
    }

    public void updateHealthbars()
    {
        float percentHealth = health.Value / maxHealth;
        healthbarUI.fillAmount = percentHealth;
        healthbarWorld.fillAmount = percentHealth;
    }

    public void die()
    {
        ulong playerId = GetComponent<NetworkObject>().OwnerClientId;

        GameManager.Instance.updateScore(playerId);

        respawnClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void respawnClientRpc()
    {
        if(IsOwner)
        {
            spawnProtection.Value = true;
            spawnProtectionTime = 1 + Time.time;

            spawnPoint = GameManager.Instance.getSpawnPoint();

            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        if(IsServer)
        {
            m_health.Value = m_maxHealth;
        }
    }

    [ClientRpc]
    public void healClientRpc()
    {
        m_health.Value = m_maxHealth;
    }

    [ServerRpc]
    public void setMaxHealthServerRpc(float health)
    {
        m_maxHealth = health;
        m_health.Value = maxHealth;
    }

    public void bomberDash()
    {
        rb.linearVelocity = Vector3.zero;

        Vector3 forwardForce = transform.forward * verticalInput * 7;
        Vector3 rightForce = transform.right * horizontalInput * 7;
        Vector3 force = forwardForce + rightForce + Vector3.up * 5;

        rb.AddForce(force, ForceMode.Impulse);

        dashes--;
        dashText.text = dashes.ToString();
        dashResetTime = Time.time + dashCooldown;
    }

    public void movingAnimations()
    {
        modelAnimator.SetBool("Walking", verticalInput == 1 ? true : false);

        modelAnimator.SetBool("WalkingBackwards", verticalInput == -1 ? true : false);

        modelAnimator.SetBool("Strafing", horizontalInput == 1 ? true : false);

        modelAnimator.SetBool("LeftStrafe", horizontalInput == -1 ? true : false);

        //Vector3 rotation = new Vector3(0, horizontalInput == -1 ? -55 : horizontalInput == 0 ? 25 : 55, 0);

        //model.transform.localEulerAngles = rotation;

        //model.transform.GetChild(2).localEulerAngles -= horizontalInput == -1 ? rotation : Vector3.zero;
    }

    public void DefaultMovement()
    {
        if(!IsOwner)
        {
            return;
        }

        FPSCamera.OnUpdate();

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        moveDirection = rb.transform.forward * verticalInput + rb.transform.right * horizontalInput;

        if(model != null)
        {
            movingAnimations();
        }

        grounded = Physics.Raycast(transform.position, Vector3.down, 1 + 0.01f, whatIsGround);

        if(Time.time > dashResetTime && dashes<2)
        {
            dashes++;
            dashText.text = dashes.ToString();
            dashResetTime = Time.time + dashCooldown;
        }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            GameManager.Instance.pauseServerRpc();
        }

        //if (Input.GetKeyDown(KeyCode.C))
        //{
        //    capsule.transform.position -= Vector3.up * 0.2f;
        //    capsule.transform.localScale += new Vector3(0, -0.2f, 0);
        //    GetComponentInChildren<Camera>().transform.position -= Vector3.up * 0.2f;
        //    crouched = true;
        //}
        //if (Input.GetKeyUp(KeyCode.C))
        //{
        //    capsule.transform.position += Vector3.up * 0.2f;
        //    capsule.transform.localScale = scale;
        //    GetComponentInChildren<Camera>().transform.position += Vector3.up * 0.2f;
        //    crouched = false;
        //}

        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            rb.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
        }

        Vector3 center = transform.position;
        for (int o = 0; o < 10; o++)
        {
            float angle = (float)o / 10 * 360f;
            // Convert angle to radians for Mathf functions
            float rad = angle * Mathf.Deg2Rad;

            // Calculate point on circle in XZ plane (for 3D)
            float x = center.x + 0.5f * Mathf.Cos(rad);
            float z = center.z + 0.5f * Mathf.Sin(rad);
            Vector3 targetPoint = new Vector3(x, center.y, z);

            // Get direction from center to target point
            Vector3 direction = (targetPoint - center).normalized;

            float crouchOffset = crouched ? -0.2f : 0;

            bool feetRay = Physics.Raycast(transform.position + Vector3.down * (0.9f + crouchOffset), direction, 0.4f);

            bool ankleRay = Physics.Raycast(transform.position + Vector3.down * (0.4f + crouchOffset), direction, 0.4f);

            Debug.DrawRay(transform.position + Vector3.down * (0.4f + crouchOffset), direction);

            //Debug.Log(feetRay + " and " +  ankleRay);

            if (feetRay && !ankleRay && grounded)
            {
                transform.position += Vector3.up * 0.25f;
                return;
            }
        }
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if (IsOwner)
        {
            if (Time.time > spawnProtectionTime && spawnProtection.Value)
            {
                spawnProtection.Value = false;
            }
        }
    }

    public void OnDrawGizmosSelected()
    {
        float crouchOffset = crouched ? -0.2f : 0;
            Vector3 center = transform.position;
            for (int o = 0; o < 10; o++)
            {
                float angle = (float)o / 10 * 360f;
                // Convert angle to radians for Mathf functions
                float rad = angle * Mathf.Deg2Rad;

                // Calculate point on circle in XZ plane (for 3D)
                float x = center.x + 2.5f * Mathf.Cos(rad);
                float z = center.z + 2.5f * Mathf.Sin(rad);
                Vector3 targetPoint = new Vector3(x, center.y, z);

                // Get direction from center to target point
                Vector3 direction = (targetPoint - center).normalized;

                Gizmos.DrawLine(transform.position + Vector3.down * (0.9f + crouchOffset), transform.position + Vector3.down * (0.9f + crouchOffset) + direction);
                Gizmos.DrawLine(transform.position + Vector3.down * (0.4f + crouchOffset), transform.position + Vector3.down * (0.4f + crouchOffset) + direction);

            }

        

        //Gizmos.DrawLine(transform.position + Vector3.down * (0.9f + crouchOffset), transform.position + transform.forward * 0.6f + Vector3.down * 0.9f);
        //Gizmos.DrawLine(transform.position + Vector3.down * (0.4f + crouchOffset), transform.position + transform.forward * 0.6f + Vector3.down * 0.4f);
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            Canvas canvas = healthbarWorld.GetComponentInParent<Canvas>();
            canvas.transform.LookAt(Camera.main.transform.position);
            //canvas.transform.Rotate(new Vector3(0, 90, 0));
            return;
        }
        float yVelocity = rb.linearVelocity.y;
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (grounded)
        {
            rb.linearVelocity = moveDirection.normalized * moveSpeed;
        }
        else
        {
            if (flatVel.magnitude < moveSpeed)
            {
                rb.AddForce(moveDirection.normalized * airMoveSpeed);
                //old limit max speed
                //Vector3 limitedVelocity = flatVel.normalized * moveSpeed;
                //rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
            }
            else
            {
                rb.AddForce((moveDirection.normalized - flatVel.normalized) * airMoveSpeed);
            }
        }
        
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, yVelocity, rb.linearVelocity.z);
    }

    private void step()
    {

    }

}
