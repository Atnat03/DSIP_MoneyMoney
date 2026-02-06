using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Shooting;

public class Bandit_Health : NetworkBehaviour, ITarget
{
    [Header("Health")]
    public float maxHealth = 100f;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        value: 100f,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );


    [Header("UI")]
    public Image healthBar;

    public Action<BulletInfo> OnShot { get; private set; }
    public Collider Collider { get; private set; }

    private void Awake()
    {
        Collider = GetComponent<Collider>();
        OnShot = HandleShot;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;
        
        currentHealth.OnValueChanged += OnHealthChanged;
    }
    
    private void OnDestroy()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float oldVal, float newVal)
    {
        if (healthBar != null)
            healthBar.fillAmount = newVal / maxHealth;

        if (newVal <= 0f)
            Die();
    }
    
    private void HandleShot(BulletInfo bullet)
    {
        if (IsServer)
            ApplyDamage(bullet.Damage);
        else
            TakeDamageServerRpc(bullet.Damage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float dmg)
    {
        print("TakeDamageServerRpc");
        ApplyDamage(dmg);
    }

    private void ApplyDamage(float dmg)
    {
        print("ApplyDamage : " + currentHealth.Value);
        
        if (currentHealth.Value <= 0f)
            return;

        currentHealth.Value -= dmg;
        currentHealth.Value = Mathf.Max(currentHealth.Value, 0f);
    }

    private void Die()
    {
        if (TryGetComponent(out BanditVehicleAI ai))
            ai.StopVehicle();
    }
}