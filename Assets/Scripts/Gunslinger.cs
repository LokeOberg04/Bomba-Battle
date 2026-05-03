using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Gunslinger : Hero
{

    public float firerate = 0.5f;
    public int damage = 20;
    public float ECooldown = 3.0f;
    public float ETime = 0;
    public float ShiftCooldown = 3.0f;
    public float ShiftTime = 0;
    public float sleepDartProjectileSpeed = 2000f;
    public int shotgunDamage = 5;
    public float shotgunSpread = 0.1f;
    public int shotgunShots = 10;

    public Gunslinger(Player player) : base(player)
    {
        health = 80;
        m_player.setMaxHealthServerRpc(health);
        m_player.hero = Player.EHero.Gunslinger;
        m_player.weapon = m_player.gunslingerWeapon;
        m_player.weapon.gameObject.SetActive(true);
    }

    private abstract class GunslingerAction : ActionStack.Action
    {
        private Gunslinger m_gunslinger;

        public Gunslinger gunslinger => m_gunslinger;

        public GunslingerAction(Gunslinger gunslinger)
        {
            m_gunslinger = gunslinger;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            gunslinger.updateUI();
            gunslinger.player.DefaultMovement();
        }
    }
    private class Shoot : GunslingerAction
    {
        public Shoot(Gunslinger gunslinger) : base(gunslinger)
        {
        }

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            gunslinger.player.animator.SetTrigger("Shoot");
            gunslinger.player.shootSlider.gameObject.SetActive(true);
            gunslinger.player.shootCooldown = Time.time + gunslinger.firerate;
            Transform Cameratransform = gunslinger.player.GetComponentInChildren<Camera>().transform;

            gunslinger.player.gunslingerShotRpc(gunslinger.damage, gunslinger.player.gameObject);

            gunslinger.player.gunslingerWeapon.GetComponentInChildren<ParticleSystem>().Play();

        }

        public override void OnUpdate()
        {
            base.OnUpdate();
        }

        public override void OnEnd()
        {
            base.OnEnd();

            //bomber.cooldownSlider.gameObject.SetActive(false);
        }

        public override bool IsDone()
        {
            return true; // Time.time > cooldown;
        }
    }

    private class Sleep : GunslingerAction
    {
        public Sleep(Gunslinger gunslinger) : base(gunslinger)
        {
        }

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            gunslinger.player.bombaSlider.gameObject.SetActive(true);
            gunslinger.ETime = Time.time + gunslinger.ECooldown;

            Transform Cameratransform = gunslinger.player.GetComponentInChildren<Camera>().transform;

            gunslinger.player.gunslingerSleepServerRpc(gunslinger.player.gameObject, gunslinger.player, gunslinger.sleepDartProjectileSpeed, gunslinger.player.GetComponent<NetworkObject>().OwnerClientId);
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

    private class Shotgun : GunslingerAction
    {
        float lockoutTime = 1;
        public Shotgun(Gunslinger gunslinger) : base(gunslinger)
        {
        }

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            lockoutTime += Time.time;

            gunslinger.player.gunslingerShotgun.gameObject.SetActive(true);

            gunslinger.player.animator.Play("Shift");

            gunslinger.player.ability2Slider.gameObject.SetActive(true);
            gunslinger.ShiftTime = Time.time + gunslinger.ShiftCooldown;

            gunslinger.player.gunslingerShotgunShotRpc(gunslinger.shotgunDamage, gunslinger.player.gameObject, gunslinger.shotgunShots, gunslinger.shotgunSpread);

            gunslinger.player.gunslingerShotgun.GetComponentInChildren<ParticleSystem>().Play();

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

            gunslinger.player.gunslingerShotgun.gameObject.SetActive(false);

            //bomber.cooldownSlider.gameObject.SetActive(false);
        }

        public override bool IsDone()
        {
            return Time.time > lockoutTime;
        }
    }

    private void updateUI()
    {
        float shooterSliderValue = 1 - (player.shootCooldown - Time.time) / firerate;

        player.shootSlider.value = shooterSliderValue;

        if (Time.time > player.shootCooldown)
        {
            player.shootSlider.gameObject.SetActive(false);
        }



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
