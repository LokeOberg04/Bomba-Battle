using UnityEngine;

public abstract class Hero : ActionStack.ActionBehavior
{
    protected float health = 100;

    private Animator m_animator;

    public Player m_player;

    protected Hero(Player player)
    {
        m_player = player;
        m_player.setMaxHealth(health);
    }

    public Player player => m_player;

    public Animator animator => m_animator;

    protected virtual void Awake()
    {
        //m_player = GetComponent<Player>();
        m_animator = GetComponent<Animator>();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        player.DefaultMovement();
    }

    public override bool IsDone()
    {
        return false;
    }
}
