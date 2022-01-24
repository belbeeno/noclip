using System;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    #region Defines
    public float maxHealth = 3;
    public float healthPerSecond = 1f;
    public float recoveryCooldown = 1f;

    [SerializeField]
    private UnityEvent OnDamageRecieved;
    [SerializeField]
    private UnityEvent OnDead;

    public bool IsDead => health <= 0;
    bool _wasDead = false;
    public float NormalizedHealth => Mathf.Clamp01(health / maxHealth);
    #endregion

    #region State
    private float health = 3;
    private float cooldown = -1f;
    #endregion

    public void Damage(float amount)
    {
        health -= amount;
        cooldown = recoveryCooldown;
        if (_wasDead) return;
        if (IsDead)
        {
            OnDead.Invoke();
        }
        else
        {
            OnDamageRecieved.Invoke();
        }
    }

    private void OnEnable()
    {
        health = maxHealth;
        cooldown = -1f;
    }

    private void Update()
    {
        if (health > 0)
        {
            if (cooldown > 0f)
            {
                cooldown -= Time.deltaTime;
            }
            else
            {
                health = Mathf.Min(health + healthPerSecond * Time.deltaTime, maxHealth);
            }
        }
        _wasDead = IsDead;
    }
}
