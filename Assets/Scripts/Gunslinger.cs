using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Gunslinger : Hero
{

    public float firerate = 0.3f;
    public int damage = 20;
    public float ECooldown = 3.0f;
    public float ETime = 0;

    public Gunslinger(Player player) : base(player)
    {
        health = 80;
        m_player.setMaxHealth(health);
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

            gunslinger.OnUpdate();
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

            gunslinger.player.gunslingerShotServerRpc(gunslinger.damage, Cameratransform.position, Cameratransform.forward);
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

    private class Deflect : GunslingerAction
    {
        float cooldown = float.MaxValue;
        public Deflect(Gunslinger gunslinger) : base(gunslinger)
        {
        }

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            gunslinger.player.bombaSlider.gameObject.SetActive(true);
            gunslinger.ETime = Time.time + gunslinger.ECooldown;

            Transform Cameratransform = gunslinger.player.GetComponentInChildren<Camera>().transform;

            RaycastHit hit;

            Physics.Raycast(Cameratransform.position, Cameratransform.transform.forward, out hit, 500f, gunslinger.player.whatIsGround);

            gunslinger.player.spawnBombaServerRpc(gunslinger.player.transform.position, hit.point);

            //bomber.cooldownSlider.gameObject.SetActive(true);
            gunslinger.ETime = Time.time + gunslinger.ECooldown;
            //Transform Cameratransform = bomber.player.GetComponentInChildren<Camera>().transform;

            //bomber.player.spawnBulletServerRpc(Cameratransform.position + Cameratransform.forward, Cameratransform.rotation, bomber.player.GetComponent<NetworkObject>().OwnerClientId);
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

    private void updateUI()
    {
        float shooterSliderValue = 1 - (player.shootCooldown - Time.time) / firerate;

        player.shootSlider.value = shooterSliderValue;

        if (Time.time > player.shootCooldown)
        {
            player.shootSlider.gameObject.SetActive(false);
        }



        float deflectSliderValue = 1 - (ETime - Time.time) / ECooldown;

        player.bombaSlider.value = deflectSliderValue;

        if (Time.time > ETime)
        {
            player.bombaSlider.gameObject.SetActive(false);
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
    }

}
