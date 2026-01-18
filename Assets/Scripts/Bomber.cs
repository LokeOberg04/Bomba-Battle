using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Bomber : Hero
{
    
    private Slider cooldownSlider;
    public float firerate = 0.5f;
    public Bomber(Player player) : base(player)
    {
        health = 80;
        m_player.setMaxHealth(health);
        cooldownSlider = m_player.GetComponentInChildren<Slider>(true);
    }

    private abstract class BomberAction : ActionStack.Action
    {
        private Bomber m_bomber;

        public Bomber bomber => m_bomber;

        public BomberAction(Bomber bomber)
        {
            m_bomber = bomber;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            bomber.player.DefaultMovement();
        }
    }
    private class Shoot : BomberAction
    {
        float cooldown = float.MaxValue;
        public Shoot(Bomber bomber) : base(bomber)
        {
        }

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            bomber.cooldownSlider.gameObject.SetActive(true);
            cooldown = Time.time + bomber.firerate;
            Transform Cameratransform = bomber.player.GetComponentInChildren<Camera>().transform;

            bomber.player.spawnBulletServerRpc(Cameratransform.position + Cameratransform.forward, Cameratransform.rotation, bomber.player.GetComponent<NetworkObject>().OwnerClientId);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            float sliderValue = 1 - (cooldown - Time.time) / bomber.firerate;

            bomber.cooldownSlider.value = sliderValue;
        }

        public override void OnEnd()
        {
            base.OnEnd();

            bomber.cooldownSlider.gameObject.SetActive(false);
        }

        public override bool IsDone()
        {
            return Time.time > cooldown;
        }
    }
    public override void OnUpdate()
    {
        base.OnUpdate();

        if (Input.GetKeyDown(KeyCode.Mouse0) && player.IsOwner)
        {
            player.PushAction(new Shoot(this));
        }
    }

}
