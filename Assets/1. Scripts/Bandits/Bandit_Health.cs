using System;
using UnityEngine;
using UnityEngine.UI;
using Shooting;

public class Bandit_Health : MonoBehaviour, ITarget
{
    [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    public Image healthBar;
    
    public Action<BulletInfo> OnShot { get; private set; }
    public Collider Collider { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;
        Collider = GetComponent<Collider>();
        OnShot = HandleShot;
    }

    private void HandleShot(BulletInfo bullet)
    {
        TakeDamage(bullet.Damage);
    }

    public void TakeDamage(float dmg)
    {
        currentHealth -= dmg;

        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        GetComponent<BanditVehicleAI>().StopVehicle();
    }
}