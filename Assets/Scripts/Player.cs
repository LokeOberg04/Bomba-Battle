using System;
using System.Collections.Generic;
using Bezier;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Player : ActionStack
{
    private float m_maxHealth = 100;

    public NetworkVariable<float> m_health = new NetworkVariable<float>(100f);

    public NetworkVariable<float> health => m_health;

    public float maxHealth => m_maxHealth;

    Rigidbody rb;

    public GameObject capsule;

    private GameObject bomb;
    private GameObject bomba;

    public GameObject bomberWeapon;
    public GameObject LQWeapon;
    public GameObject hero3Weapon;

    public EHero hero = EHero.None;
    public GameObject weapon;

    public Animator animator;

    public float projectileSpeed = 2000f;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    public UnityEngine.UI.Image healthbarUI;

    public UnityEngine.UI.Image healthbarWorld;

    private float dashes = 2;
    public float dashCooldown = 2.5f;
    private float dashResetTime;
    public TextMeshProUGUI dashText;

    public TextMeshProUGUI scoreText;

    public Transform spawnPoint;

    public UnityEngine.UI.Slider shootSlider;
    public UnityEngine.UI.Slider bombaSlider;

    public float moveSpeed = 5.0f;
    public float airMoveSpeed = 15.0f;
    public float jumpHeight = 4.0f;

    public float shootCooldown = 0;

    public LayerMask whatIsGround;

    public bool grounded;

    bool crouched;

    Vector3 scale;

    public enum EHero
    {
        None,
        Bomber,
        LQ,
        Hero3
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
        bomb = Resources.Load<GameObject>("Prefabs/Bomberbomb");
        bomba = Resources.Load<GameObject>("Prefabs/Bomba");
        healthbarUI.fillAmount = 1;
        healthbarWorld.fillAmount = 1;
        shootCooldown = 0;

        if (IsOwner)
        {
            healthbarUI.enabled = true;
            healthbarWorld.GetComponentInParent<Canvas>().enabled = false;
            GetComponentInChildren<Camera>(true).enabled = true;
            GetComponentInChildren<AudioListener>().enabled = true;
            dashText.enabled = true;
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

    [ServerRpc]
    public void spawnBulletServerRpc(Vector3 position, Quaternion rotation, ulong shooterId)
    {
        
        GameObject spawnedBomb = Instantiate(bomb, position, rotation);
        spawnedBomb.GetComponent<Bomberbomb>().shooterId = shooterId;
        Rigidbody bombRb = spawnedBomb.GetComponent<Rigidbody>();
        bombRb.AddForce(spawnedBomb.transform.forward * projectileSpeed);
        spawnedBomb.GetComponent<NetworkObject>().Spawn();
        
    }

    [ServerRpc]
    public void spawnBombaServerRpc(Vector3 Player, Vector3 Target)
    {
        GameObject spawnedBomba = Instantiate(bomba, Player, Quaternion.identity);
        BezierCurve.ControlPoint playerControlPoint = new BezierCurve.ControlPoint() { m_vPosition = Player, m_vTangent = Vector3.up * 10 };
        BezierCurve.ControlPoint targetControlPoint = new BezierCurve.ControlPoint() { m_vPosition = Target, m_vTangent = Vector3.down * 10 };
        spawnedBomba.GetComponent<BezierCurve>().m_points.Add(playerControlPoint);
        spawnedBomba.GetComponent<BezierCurve>().m_points.Add(targetControlPoint);
        spawnedBomba.GetComponent<NetworkObject>().Spawn();
    }

    
    public void takeDamage(float dmg)
    {
        m_health.Value -= dmg;
    }

    [ClientRpc]
    public void takeKnockbackClientRpc(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
    }

    public void onHealthChanged(float oldValue, float newValue)
    {
        float percentHealth = newValue / maxHealth;
            healthbarUI.fillAmount = percentHealth;
            healthbarWorld.fillAmount = percentHealth;
        if (newValue < 0)
        {
            die();
        }
    }

    public void die()
    {
        ulong playerId = GetComponent<NetworkObject>().OwnerClientId;

        GameManager.Instance.updateScore(playerId);

        respawnClientRpc();
    }

    [ClientRpc]
    public void respawnClientRpc()
    {
        spawnPoint = GameManager.Instance.getSpawnPoint();

        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;

        m_health.Value = m_maxHealth;
    }

    [ClientRpc]
    public void healClientRpc()
    {
        m_health.Value = m_maxHealth;
    }

    public void setMaxHealth(float health)
    {
        m_maxHealth = health;
        m_health.Value = maxHealth;
    }

    public void DefaultMovement()
    {
        if(!IsOwner)
        {
            Canvas canvas = healthbarWorld.GetComponentInParent<Canvas>();
            canvas.transform.LookAt(Camera.main.transform.position);
            //canvas.transform.Rotate(new Vector3(0, 90, 0));
            return;
        }
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        moveDirection = rb.transform.forward * verticalInput + rb.transform.right * horizontalInput;

        grounded = Physics.Raycast(transform.position, Vector3.down, 1 + 0.01f, whatIsGround);

        if(Time.time > dashResetTime && dashes<2)
        {
            dashes++;
            dashText.text = dashes.ToString();
            dashResetTime = Time.time + dashCooldown;
        }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("player p");
            GameManager.Instance.pauseServerRpc();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            capsule.transform.position -= Vector3.up * 0.2f;
            capsule.transform.localScale += new Vector3(0, -0.2f, 0);
            GetComponentInChildren<Camera>().transform.position -= Vector3.up * 0.2f;
            crouched = true;
        }
        if (Input.GetKeyUp(KeyCode.C))
        {
            capsule.transform.position += Vector3.up * 0.2f;
            capsule.transform.localScale = scale;
            GetComponentInChildren<Camera>().transform.position += Vector3.up * 0.2f;
            crouched = false;
        }

        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            rb.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashes > 0)
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
