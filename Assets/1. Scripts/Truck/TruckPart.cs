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

    private float currentHealth;
    private GameObject detachedInstance;

    public float mult;

    private void Start()
    {
        currentHealth = maxHealth;

        if (startDestroyed)
            Break();
    }

    #region Damage

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Break();
        }
    }

    #endregion

    #region Break / Detach

    private void Break()
    {
        // Désactive la partie originale
        gameObject.SetActive(false);

        // Instancie la version physiquée
        if (detachedPrefab != null)
        {
            detachedInstance = Instantiate(detachedPrefab, transform.position, transform.rotation);

            Rigidbody rb = detachedInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Calcul de la direction opposée au centre du camion
                Vector3 localDir = transform.localPosition;
                Vector3 ejectDir = new Vector3(Mathf.Sign(localDir.x), 0f, 0f).normalized; // X = droite/gauche
                ejectDir += Vector3.up * 0.2f; // un petit peu vers le haut pour tomber visuellement
                rb.AddForce(ejectDir * (ejectForce*mult) + Vector3.up * ejectUpwardForce, ForceMode.Impulse);
            }
        }
    }

    #endregion

    #region Repair

    public void Repair()
    {
        // Réactive la partie originale
        gameObject.SetActive(true);
        currentHealth = maxHealth;

        // Supprime la partie tombée
        if (detachedInstance != null)
        {
            Destroy(detachedInstance);
        }
    }

    #endregion
}
