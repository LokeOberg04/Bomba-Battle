using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class LQ : Hero
{

    public float firerate = 0.3f;
    public int damage = 40;
    public float ECooldown = 10.0f;
    public float ETime = 0;
    public float railDmgPerCharge = .75f;
    public float railChargePerDmg = 1f;

    public LQ(Player player) : base(player)
    {
        health = 80;
        m_player.setMaxHealthServerRpc(health);
        m_player.hero = Player.EHero.LQ;
        m_player.weapon = m_player.LQWeapon;
        m_player.weapon.gameObject.SetActive(true);
    }

    private abstract class LQAction : ActionStack.Action
    {
        private LQ m_lq;

        public LQ lq => m_lq;

        public LQAction(LQ lq)
        {
            m_lq = lq;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            lq.updateUI();
            lq.player.DefaultMovement();
        }
    }
    private class Shoot : LQAction
    {
        public Shoot(LQ lq) : base(lq)
        {
        }

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            lq.player.animator.SetBool("Shooting", true);
            Transform Cameratransform = lq.player.GetComponentInChildren<Camera>().transform;

            lq.player.spawnLQZap(lq.player.weapon.transform.position, Cameratransform.forward);
            


            //gunslinger.player.gunslingerWeapon.GetComponentInChildren<ParticleSystem>().Play();

        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            lq.player.LQShotRpc(lq.damage * Time.deltaTime, lq.railChargePerDmg, lq.player.gameObject,lq.player);
            //, lq.player.GetComponent<NetworkObject>().OwnerClientId

        }

        public override void OnEnd()
        {
            base.OnEnd();

            lq.player.animator.SetBool("Shooting", false);

            lq.player.despawnLQZap();

            //bomber.cooldownSlider.gameObject.SetActive(false);
        }

        public override bool IsDone()
        {
            return Input.GetKeyUp(KeyCode.Mouse0) && lq.player.IsOwner;
        }
    }

    private class Rail : LQAction
    {
        float lockoutTime = 1;
        public Rail(LQ lq) : base(lq)
        {
        }

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            lockoutTime += Time.time;

            float currentCharge = lq.player.LQCharge.Value;

            lq.player.animator.Play("Shift");

            lq.player.LQRailRpc(currentCharge * lq.railDmgPerCharge, lq.player.gameObject, lq.player);

        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            //float sliderValue = 1 - (cooldown - Time.time) / bomber.firerate;

            //bomber.cooldownSlider.value = sliderValue;
        }

        public override void OnEnd()
        {
            base.OnEnd();

            //bomber.cooldownSlider.gameObject.SetActive(false);
        }

        public override bool IsDone()
        {
            return Time.time > lockoutTime;
        }
    }

    private class Deflect : LQAction
    {
        float Duration = 3;

        float lockoutTime = 1;
        public Deflect(LQ lq) : base(lq)
        {
        }

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            lockoutTime += Time.time;

            Duration += Time.time;

            lq.ETime = Time.time + lq.ECooldown;

            lq.player.bombaSlider.gameObject.SetActive(true);

            lq.player.animator.SetBool("E", true);

            lq.player.toggleLQDeflectRpc(true);

        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            //float sliderValue = 1 - (cooldown - Time.time) / bomber.firerate;

            //bomber.cooldownSlider.value = sliderValue;
        }

        public override void OnEnd()
        {
            base.OnEnd();

            lq.player.animator.SetBool("E", false);

            lq.player.toggleLQDeflectRpc(false);
        }

        public override bool IsDone()
        {
            return Time.time > Duration || Input.GetKeyDown(KeyCode.E) && Time.time > lockoutTime;
        }
    }

    private void updateUI()
    {

        float DeflectSliderValue = 1 - (ETime - Time.time) / ECooldown;

        player.bombaSlider.value = DeflectSliderValue;

        if (Time.time > ETime)
        {
            player.bombaSlider.gameObject.SetActive(false);
        }


        player.ability2Slider.value = player.LQCharge.Value / 100;

        if (player.LQCharge.Value > 0)
        {
            player.ability2Slider.gameObject.SetActive(true);
        }
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        updateUI();

        if (Input.GetKeyDown(KeyCode.Mouse0) && player.IsOwner && Time.time > player.shootCooldown)
        {
            player.PushAction(new Shoot(this));
        }

        if (Input.GetKeyDown(KeyCode.E) && player.IsOwner && Time.time > ETime)
        {
            player.PushAction(new Deflect(this));
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && player.IsOwner && player.LQCharge.Value > 10)
        {
            player.PushAction(new Rail(this));
        }
    }

}
