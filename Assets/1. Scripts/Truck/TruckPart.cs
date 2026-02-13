using Unity.Netcode;
using UnityEngine;

public class TruckPart : NetworkBehaviour, IInteractible
{
    #region Inspector
    [Header("Health")]
    [SerializeField] private float maxHealth = 50f;

    [Header("Detached Prefab")]
    [SerializeField] private GameObject detachedPrefab;
    [SerializeField] private float ejectForce = 5f;
    [SerializeField] private float ejectUpwardForce = 2f;

    [Header("Debug")]
    [SerializeField] private bool startDestroyed = false;
    #endregion

    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        value: 100f,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<ulong> detachedInstanceId = new NetworkVariable<ulong>(
        value: 0,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    public float mult;
    public bool lifeMode;
    
    public NetworkVariable<bool> isBroke = new NetworkVariable<bool>(
        value: false,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    public MeshRenderer mesh;
    public Material baseMaterial;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;

        mesh = GetComponent<MeshRenderer>();
        
        Interact.OnInteract += HitInteract;
        
        isBroke.OnValueChanged += OnBrokeStateChanged;
        
        outline[0] = GetComponent<Outline>();
        
        UpdateVisuals();
    }

    private void OnDisable()
    {
        Interact.OnInteract -= HitInteract;
        if (isBroke != null)
            isBroke.OnValueChanged -= OnBrokeStateChanged;
    }

    public void Repair()
    {
        if (!IsServer) return;
        
        isBroke.Value = false;
        Debug.Log($"[Server] {gameObject.name} réparé");
    }
    
    public void OnRepaired()
    {
        // Mettre à jour les visuels, sons, etc.
        if (mesh != null)
        {
            mesh.enabled = false;
        }
        
        // Autres effets visuels/sonores
        Debug.Log($"[Client] {gameObject.name} visuellement réparé");
    }

    
    private void OnBrokeStateChanged(bool previousValue, bool newValue)
    {
        UpdateVisuals();
    }

    void Update()
    {
        if (mesh == null) return;
        
        if(!isBroke.Value && !mesh.enabled)
            UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        mesh.enabled = !isBroke.Value;
        GetComponent<BoxCollider>().isTrigger = isBroke.Value;
    }

    #region Damage

    public void TakeDamage(float amount)
    {
        TakeDamageServerRpc(amount);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float amount)
    {
        print("Take Dmg (Server)");
        
        if (!lifeMode)
        {
            if (currentHealth.Value <= 0) return;

            currentHealth.Value -= amount;

            if (currentHealth.Value <= 0)
            {
                BreakOnServer();
            }
        }
    }

    #endregion

    #region Break / Detach

    private void BreakOnServer()
    {
        if (!IsServer) return;
        
        print("break (Server)");
        
        if (detachedPrefab != null)
        {
            GameObject detachedInstance = Instantiate(detachedPrefab, transform.position, transform.rotation);
            NetworkObject netObj = detachedInstance.GetComponent<NetworkObject>();
            netObj.Spawn();
            
            // Stocker l'ID pour pouvoir le détruire plus tard
            detachedInstanceId.Value = netObj.NetworkObjectId;
            
            Rigidbody rb = detachedInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 localDir = transform.localPosition;
                Vector3 ejectDir = new Vector3(0f,0f,-Mathf.Sign(localDir.z)).normalized; 
                ejectDir += Vector3.up * 0.2f; 
                rb.AddForce(ejectDir * (ejectForce * mult) + Vector3.up * ejectUpwardForce, ForceMode.Impulse);
                rb.AddTorque(30,0,0);
            }
        }

        SFX_Manager.instance.PlaySFX(9, .4f);
        
        isBroke.Value = true;
        TruckLife.instance.DetermineIfShootable();
    }
    
    #endregion

    #region Repair

    private void HitInteract(GameObject obj, GameObject player)
    {
        if (obj.gameObject.GetInstanceID() == gameObject.GetInstanceID())
        {
            RepairServerRpc();
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RepairServerRpc()
    {
        if (!IsServer) return;
        
        isBroke.Value = false;
        currentHealth.Value = maxHealth;

        // Supprime la partie tombée
        if (detachedInstanceId.Value != 0)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(detachedInstanceId.Value, out NetworkObject netObj))
            {
                netObj.Despawn();
                Destroy(netObj.gameObject);
            }
            detachedInstanceId.Value = 0;
        }
        
        mesh.material = baseMaterial;
    }
    
    #endregion

    public string InteractionName
    {
        get { return interactionName;}
        set { }
    }

    public string interactionName;
    
    public Outline[] Outline     
    {
        get { return outline; ; }
        set { }
    }

    public Outline[] outline;
}