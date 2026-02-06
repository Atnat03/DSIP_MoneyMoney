using Unity.Netcode;
using UnityEngine;

public class TruckPart : MonoBehaviour
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

    [SerializeField]private float currentHealth;
    private GameObject detachedInstance;

    public float mult;

    public bool lifeMode;

    private void Start()
    {
        currentHealth = maxHealth;

        if (startDestroyed)
            Break();
    }
    
    private void OnEnable()
    {
        Interact.OnInteract += HitInteract;
    }

    private void OnDisable()
    {
        Interact.OnInteract -= HitInteract;
    }
    
    
    

    #region Damage

    public void TakeDamage(float amount)
    {
        if (!lifeMode)
        {
            if (currentHealth <= 0) return;

            currentHealth -= amount;

            if (currentHealth <= 0)
            {
                Break();
            }
        }
        else
        {
            TruckLife.instance.HandleShot(amount);
        }
       
    }

    #endregion

    #region Break / Detach

    private void Break()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;
        
        if (detachedPrefab != null)
        {
            detachedInstance = Instantiate(detachedPrefab, transform.position, transform.rotation);
            detachedInstance.GetComponent<NetworkObject>().Spawn();
            
            Rigidbody rb = detachedInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 localDir = transform.localPosition;
                Vector3 ejectDir = new Vector3(Mathf.Sign(localDir.x), 0f, 0f).normalized; 
                ejectDir += Vector3.up * 0.2f; 
                rb.AddForce(ejectDir * (ejectForce*mult) + Vector3.up * ejectUpwardForce, ForceMode.Impulse);
            }
        }

        UpdateBreakPartServerRpc();      
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateBreakPartServerRpc()
    {
        UpdateBreakPartClientRpc();
    }

    [ClientRpc]
    private void UpdateBreakPartClientRpc()
    {
        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<BoxCollider>().isTrigger = true;
    }
    
    #endregion

    #region Repair

    private void HitInteract(GameObject obj,  GameObject player)
    {
        Debug.Log("Hit interact");
        if (obj.gameObject.GetInstanceID() == gameObject.GetInstanceID())
        {
            Repair();
        }
    }
    
    
    public void Repair()
    {
        currentHealth = maxHealth;

        // Supprime la partie tombée
        if (detachedInstance != null)
        {
            Destroy(detachedInstance);
        }

        UpdateBreakPartServerRpcRepair();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void UpdateBreakPartServerRpcRepair()
    {
        UpdateBreakPartClientRpcPart();
    }

    [ClientRpc]
    private void UpdateBreakPartClientRpcPart()
    {
        GetComponent<MeshRenderer>().enabled = true;
        GetComponent<BoxCollider>().isTrigger = false;
    }
    
    #endregion
}
