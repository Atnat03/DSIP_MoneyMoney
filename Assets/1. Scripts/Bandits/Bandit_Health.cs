using System;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.UI;

public class Bandit_Health : MonoBehaviour
{
    public float maxHealth;
    public float tickDmg;
    public float currentHealth;
    public Image healthBar;

    public void Start()
    {
        currentHealth = maxHealth;
    }

    public void Update()
    {
        TakeDamage(tickDmg);
    }


    public void TakeDamage(float dmg)
    {
        Debug.Log("plz");
        currentHealth -= dmg;
        healthBar.fillAmount = currentHealth / maxHealth;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        GetComponent<BanditVehicleAI>().StopVehicle();
    }
}
