using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Shooting;

public class TruckLife : NetworkBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    public static TruckLife instance;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        value: 100f,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );


    [Header("UI")]
    public Image healthBar;


    public Collider Collider { get; private set; }

    private void Awake()
    {
        Collider = GetComponent<Collider>();
        instance = this;
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
    
    public void HandleShot(float dmg)
    {
        if (IsServer)
            ApplyDamage(dmg);
        else
            TakeDamageServerRpc(dmg);
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