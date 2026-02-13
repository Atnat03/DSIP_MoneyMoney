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
        // Récupérer les Outlines si non assignés
        if (Outline == null || Outline.Length == 0)
        {
            Outline = GetComponentsInChildren<Outline>();
        }
        
        currentHealth = maxHealth;
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
    }

    #region Damage System

    // ✅ Méthode appelée par BanditTir quand il tire
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

    // ✅ Casser la pièce
    private void Break()
    {
        if (!IsServer) return;

        if (isBroke.Value)
        {
            Debug.Log($"{gameObject.name} is already broken!");
            return;
        }

        isBroke.Value = true;
        Debug.Log($"[Server] {gameObject.name} has been broken by damage!");

        ShowBreakEffectClientRpc();
    }

    [ClientRpc]
    private void ShowBreakEffectClientRpc()
    {
        UpdateVisuals(true);
        GetComponent<Collider>().isTrigger = true;
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
    }

    public void OnRepaired()
    {
        Debug.Log($"[Client] {gameObject.name} visually repaired");
        UpdateVisuals(false);

        if (SFX_Manager.instance != null)
        {
            SFX_Manager.instance.PlaySFX(11);
        }
        
        if (TryGetComponent<Collider>(out var col))
            col.isTrigger = false;
    }

    #endregion

    private void UpdateVisuals(bool broken)
    {
        if (mesh != null)
        {
            mesh.enabled = !broken;

            if (!broken && repairedMaterial != null)
            {
                mesh.material = repairedMaterial;
            }
        }
    }

    // ✅ Méthode pour casser la pièce via ServerRpc (pour d'autres systèmes)
    [ServerRpc(RequireOwnership = false)]
    public void BreakPartServerRpc()
    {
        if (!IsServer) return;
        
        Break();
    }

    // ✅ Méthodes utilitaires pour debug
    [ContextMenu("Force Break")]
    public void ForceBreak()
    {
        if (IsServer)
        {
            Break();
        }
        else
        {
            BreakPartServerRpc();
        }
    }

    [ContextMenu("Force Repair")]
    public void ForceRepair()
    {
        if (IsServer)
        {
            Repair();
        }
    }

    [ContextMenu("Take 50 Damage")]
    public void TestDamage()
    {
        if (IsServer)
        {
            TakeDamage(50f);
        }
    }

    // ✅ Pour afficher la santé dans l'inspector (debug)
    private void OnGUI()
    {
        if (!showHealthBar || !IsServer) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        if (screenPos.z > 0)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y, 100, 20), 
                $"HP: {currentHealth:F0}/{maxHealth:F0}");
        }
    }
}