using UnityEngine;
using UnityEngine.AI;

public class BanditBarrage : MonoBehaviour
{
    [Header("Navigation Points")]
    public Transform pointA;
    public Transform pointB;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float arrivalDistance = 3f;
    
    [Header("Components")]
    private NavMeshAgent navAgent;
    private Rigidbody rb;
    
    private bool hasReachedDestination = false;

    void Start()
    {
        // Setup components
        rb = GetComponent<Rigidbody>();
        navAgent = GetComponent<NavMeshAgent>();
        
        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
        }
        
        // Configure NavMeshAgent
        navAgent.speed = moveSpeed;
        navAgent.angularSpeed = rotationSpeed * 60f;
        navAgent.acceleration = 50f;
        navAgent.stoppingDistance = 0.1f; // Très petit pour éviter les oscillations
        navAgent.autoBraking = true;
        navAgent.updateRotation = true;
        
        // Configure Rigidbody pour compatibilité NavMesh
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        
        // Positionner au point A et aller vers point B
        if (pointA != null)
        {
            transform.position = pointA.position;
            transform.rotation = pointA.rotation;
        }
        
        if (pointB != null)
        {
            navAgent.SetDestination(pointB.position);
        }
    }

    void Update()
    {
        if (hasReachedDestination || navAgent == null || pointB == null)
            return;

        // Vérifier la distance réelle au point B (pas le remainingDistance du NavMesh)
        float distanceToTarget = Vector3.Distance(transform.position, pointB.position);
        
        if (distanceToTarget <= arrivalDistance)
        {
            OnReachedDestination();
        }
    }

    void FixedUpdate()
    {
        // Synchroniser la physique avec le NavMeshAgent
        if (navAgent != null && navAgent.enabled && !hasReachedDestination)
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.velocity = navAgent.velocity;
            }
        }
    }

    private void OnReachedDestination()
    {
        hasReachedDestination = true;
        
        // PAS de snap de position - on laisse la voiture où elle est naturellement
        
        // Arrêter le NavMeshAgent
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.enabled = false;
        }
        
        // Arrêter le Rigidbody progressivement
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Désactiver le script
        this.enabled = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // La physique du Rigidbody gère automatiquement la collision
        }
    }

    void OnDrawGizmos()
    {
        if (pointA != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pointA.position, 0.5f);
            Gizmos.DrawLine(pointA.position, pointA.position + pointA.forward * 2f);
        }
        
        if (pointB != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pointB.position, 0.5f);
            Gizmos.DrawLine(pointB.position, pointB.position + pointB.forward * 2f);
        }
        
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
    }
}