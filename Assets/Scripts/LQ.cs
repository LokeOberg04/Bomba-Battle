using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class LQ : Hero
{

    public float firerate = 0.3f;
    public int damage = 20;
    public float ECooldown = 3.0f;
    public float ETime = 0;
    public float ShiftCooldown = 3.0f;
    public float ShiftTime = 0;

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

            Transform Cameratransform = lq.player.GetComponentInChildren<Camera>().transform;
            lq.player.LQShotServerRpc(lq.damage, Cameratransform.position, Cameratransform.forward);
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

    private class Sleep : LQAction
    {
        float cooldown = float.MaxValue;
        public Sleep(LQ lq) : base(lq)
        {
        }

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            //    gunslinger.player.bombaSlider.gameObject.SetActive(true);
            //    gunslinger.ETime = Time.time + gunslinger.ECooldown;

            //    Transform Cameratransform = gunslinger.player.GetComponentInChildren<Camera>().transform;

            //    gunslinger.player.gunslingerSleepServerRpc(Cameratransform.position + Cameratransform.forward, Cameratransform.rotation, gunslinger.sleepDartProjectileSpeed, gunslinger.player.GetComponent<NetworkObject>().OwnerClientId);
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
            return true;
        }
    }

    private class Shotgun : LQAction
    {
        float lockoutTime = 1;
        public Shotgun(LQ lq) : base(lq)
        {
        }

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            //lockoutTime += Time.time;

            //gunslinger.player.gunslingerShotgun.gameObject.SetActive(true);

            //gunslinger.player.animator.Play("Shift");

            //gunslinger.player.ability2Slider.gameObject.SetActive(true);
            //gunslinger.ShiftTime = Time.time + gunslinger.ShiftCooldown;

            //Transform cameraTransform = gunslinger.player.GetComponentInChildren<Camera>().transform;




            //gunslinger.player.gunslingerShotgun.GetComponentInChildren<ParticleSystem>().Play();

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

            //gunslinger.player.gunslingerShotgun.gameObject.SetActive(false);

            //bomber.cooldownSlider.gameObject.SetActive(false);
        }

        public override bool IsDone()
        {
            return Time.time > lockoutTime;
        }
    }

    private void updateUI()
    {

        float sleepSliderValue = 1 - (ETime - Time.time) / ECooldown;

        player.bombaSlider.value = sleepSliderValue;

        if (Time.time > ETime)
        {
            player.bombaSlider.gameObject.SetActive(false);
        }


        float shotgunSliderValue = 1 - (ShiftTime - Time.time) / ShiftCooldown;

        player.ability2Slider.value = shotgunSliderValue;

        if (Time.time > ShiftTime)
        {
            player.ability2Slider.gameObject.SetActive(false);
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
            player.PushAction(new Sleep(this));
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && player.IsOwner && Time.time > ShiftTime)
        {
            player.PushAction(new Shotgun(this));
        }
    }

}
