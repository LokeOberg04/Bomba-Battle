using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Bomber : Hero
{
    public float firerate = 0.5f;

    public float ECooldown = 3.0f;
    public float ETime = 0;

    public Bomber(Player player) : base(player)
    {
        health = 80;
        m_player.setMaxHealth(health);
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

            bomber.OnUpdate();
            bomber.player.DefaultMovement();
        }
    }
    private class Shoot : BomberAction
    {
        public Shoot(Bomber bomber) : base(bomber)
        {
        }

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            bomber.player.shootSlider.gameObject.SetActive(true);
            bomber.player.shootCooldown = Time.time + bomber.firerate;
            Transform Cameratransform = bomber.player.GetComponentInChildren<Camera>().transform;

            bomber.player.spawnBulletServerRpc(Cameratransform.position + Cameratransform.forward, Cameratransform.rotation, bomber.player.GetComponent<NetworkObject>().OwnerClientId);
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

    private class Bomba : BomberAction
    {
        float cooldown = float.MaxValue;
        public Bomba(Bomber bomber) : base(bomber)
        {
        }

        public override void OnBegin(bool bFirstTime)
        {
            base.OnBegin(bFirstTime);

            bomber.player.bombaSlider.gameObject.SetActive(true);
            bomber.ETime = Time.time + bomber.ECooldown;

            Transform Cameratransform = bomber.player.GetComponentInChildren<Camera>().transform;

            RaycastHit hit;

            Physics.Raycast(Cameratransform.position, Cameratransform.transform.forward, out hit, 500f, bomber.player.whatIsGround);

            bomber.player.spawnBombaServerRpc(bomber.player.transform.position, hit.point);

            //bomber.cooldownSlider.gameObject.SetActive(true);
            bomber.ETime = Time.time + bomber.ECooldown;
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



        float bombaSliderValue = 1 - (ETime - Time.time) / ECooldown;

        player.bombaSlider.value = bombaSliderValue;

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
            player.PushAction(new Bomba(this));
        }
    }

}
