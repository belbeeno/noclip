using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cinemachine;
using Cinemachine.PostFX;

[RequireComponent(typeof(Health))]
public class HeroDamageReceiver : MonoBehaviour
{
    public CinemachineVirtualCamera deathEyes = null;
    public Squid particlePrefab = null;
    public Health health = null;
    public HeroMovement movement = null;

    [SerializeField]
    private CinemachineVolumeSettings _volume = null;
    private Vignette _vignette = null;
    public float vignetteEffectMax = 0.2f;
    public float vignetteBeatAmplitude = 0.05f;

    private void OnEnable()
    {
        if (health == null) health = GetComponent<Health>();
        if (!ObjectPool.HasPool(particlePrefab))
        {
            particlePrefab.CreatePool();
        }

        if (_vignette == null && _volume != null)
        {
            Vignette temp;
            if (_volume.m_Profile.TryGet<Vignette>(out temp))
            {
                _vignette = temp;
            }
        }
    }

    private void OnDisable()
    {
        if (_vignette) _vignette.intensity.value = 0f;
    }

    public void ReceiveDamage(BulletBehaviour bullet, RaycastHit hitInfo)
    {
        health.Damage(bullet.define.Damage);
        Quaternion quat = Quaternion.LookRotation(bullet.transform.forward, Vector3.up);
        if (health.IsDead && deathEyes)
        {
            deathEyes.Priority = 100;
            return;
        }
        else
        {
            Squid newSquid = particlePrefab.Spawn(hitInfo.point, quat);
            newSquid.transform.SetParent(transform, true);
        }
    }

    private void Update()
    {
        if (_vignette)
        {
            float intensity = (1f - health.NormalizedHealth);
            _vignette.intensity.value = intensity * (vignetteEffectMax + ((Mathf.Cos(Time.time * Mathf.PI * 2f) + 1f) / 2f) * vignetteBeatAmplitude);
        }
    }
}
