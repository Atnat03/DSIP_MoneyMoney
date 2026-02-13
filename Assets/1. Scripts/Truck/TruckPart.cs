using Unity.Netcode;
using UnityEngine;

public class TruckPart : NetworkBehaviour, IInteractible
{
    [Header("Network State")]
    public NetworkVariable<bool> isBroke = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Visual")]
    public MeshRenderer mesh;
    public Material brokenMaterial;
    public Material repairedMaterial;

    [Header("Interactible")]
    public string InteractionName { get; set; } = "Réparer";
    public Outline[] Outline { get; set; }

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private bool showHealthBar = false;

    private void Awake()
    {
        if (Outline == null || Outline.Length == 0)
        {
            Outline = GetComponentsInChildren<Outline>();
        }
        currentHealth = maxHealth;

        mesh = GetComponent<MeshRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        isBroke.OnValueChanged += OnBrokeStateChanged;
        UpdateVisuals(isBroke.Value);
    }

    public override void OnNetworkDespawn()
    {
        isBroke.OnValueChanged -= OnBrokeStateChanged;
    }

    private void OnBrokeStateChanged(bool previousValue, bool newValue)
    {
        UpdateVisuals(newValue);

        if (!newValue) // vient d'être réparé
        {
            Debug.Log($"[Client] {gameObject.name} visually repaired");
            if (SFX_Manager.instance != null)
            {
                SFX_Manager.instance.PlaySFX(11);
            }
        }
        else if (newValue)
        {
            if (SFX_Manager.instance != null)
            {
                SFX_Manager.instance.PlaySFX(13); // son de cassure si tu veux
            }
        }
    }

    #region Damage System
    public void TakeDamage(float damage)
    {
        if (!IsServer)
        {
            Debug.LogWarning("TakeDamage should only be called on the server!");
            return;
        }

        if (isBroke.Value)
        {
            Debug.Log($"{gameObject.name} is already broken, ignoring damage");
            return;
        }

        currentHealth -= damage;
        Debug.Log($"[Server] {gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Break();
        }
    }

    private void Break()
    {
        if (!IsServer) return;
        if (isBroke.Value) return;

        isBroke.Value = true;
        Debug.Log($"[Server] {gameObject.name} has been broken by damage!");
    }
    #endregion

    #region Repair System
    public void Repair()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Repair() should only be called on the server!");
            return;
        }

        if (!isBroke.Value)
        {
            Debug.Log($"{gameObject.name} is already repaired!");
            return;
        }

        isBroke.Value = false;
        currentHealth = maxHealth;
        Debug.Log($"[Server] {gameObject.name} has been repaired");

        // Remettre le collider en mode solide (si tu l'avais mis en trigger)
        if (TryGetComponent<Collider>(out var col))
        {
            col.isTrigger = false;
        }
    }
    #endregion

    private void UpdateVisuals(bool broken)
    {
        if (mesh == null) return;

        if (broken)
        {
            mesh.enabled = false; // ou true + brokenMaterial si tu veux montrer des débris
            if (brokenMaterial != null)
                mesh.material = brokenMaterial;
        }
        else
        {
            mesh.enabled = true; // ← IMPORTANT : visible quand réparé
            if (repairedMaterial != null)
                mesh.material = repairedMaterial;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void BreakPartServerRpc()
    {
        Break();
    }

    // Debug utils
    [ContextMenu("Force Break")]
    public void ForceBreak()
    {
        if (IsServer) Break();
        else BreakPartServerRpc();
    }

    [ContextMenu("Force Repair")]
    public void ForceRepair()
    {
        if (IsServer) Repair();
    }

    [ContextMenu("Take 50 Damage")]
    public void TestDamage()
    {
        if (IsServer) TakeDamage(50f);
    }
}