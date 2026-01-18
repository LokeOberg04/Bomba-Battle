using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Player : ActionStack
{

    private float m_maxHealth = 100;

    public NetworkVariable<float> m_health = new NetworkVariable<float>(100f);

    public NetworkVariable<float> health => m_health;

    public float maxHealth => m_maxHealth;

    Rigidbody rb;

    private GameObject bomb;
    public float projectileSpeed = 2000f;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    public UnityEngine.UI.Image healthbarUI;

    public UnityEngine.UI.Image healthbarWorld;

    private float dashes = 2;
    public float dashCooldown = 2.5f;
    private float dashResetTime;
    private TextMeshProUGUI dashText;

    public float moveSpeed = 5.0f;
    public float airMoveSpeed = 15.0f;
    public float jumpHeight = 4.0f;

    public LayerMask whatIsGround;

    public bool grounded;

    bool crouched;

    Vector3 scale;

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

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        scale = transform.localScale;
        PushAction(new Bomber(this));
        GameManager.Instance.addPlayer(this);
        dashText = GetComponentInChildren<TextMeshProUGUI>(true);
        bomb = Resources.Load<GameObject>("Prefabs/Bomberbomb");
        healthbarUI.fillAmount = 1;
        healthbarWorld.fillAmount = 1;

        if (IsOwner)
        {
            healthbarUI.enabled = true;
            healthbarWorld.GetComponentInParent<Canvas>().enabled = false;
            GetComponentInChildren<Camera>(true).enabled = true;
            GetComponentInChildren<AudioListener>().enabled = true;
            dashText.enabled = true;
        }
    }

    private void OnEnable()
    {
        m_health.OnValueChanged += onHealthChanged;
    }

    private void OnDisable()
    {
        m_health.OnValueChanged -= onHealthChanged;
    }

    [ServerRpc]
    public void spawnBulletServerRpc(Vector3 position, Quaternion rotation, ulong shooterId)
    {
        
        GameObject spawnedBomb = Instantiate(bomb, position, rotation);
        spawnedBomb.GetComponent<Bomberbomb>().shooterId = shooterId;
        Rigidbody bombRb = spawnedBomb.GetComponent<Rigidbody>();
        bombRb.AddForce(spawnedBomb.transform.forward * projectileSpeed);

        ulong myClientId = NetworkManager.Singleton.LocalClientId;

        ulong ownerId = OwnerClientId;

        //spawnedBomb.GetComponent<NetworkObject>().ChangeOwnership(client ? myClientId : ownerId);
        spawnedBomb.GetComponent<NetworkObject>().Spawn();
        
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

        if (Input.GetKeyDown(KeyCode.C))
        {
            transform.position -= Vector3.up * 0.2f;
            transform.localScale += new Vector3(0, -0.2f, 0);
            crouched = true;
        }
        if (Input.GetKeyUp(KeyCode.C))
        {
            transform.position += Vector3.up * 0.2f;
            transform.localScale = scale;
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
