using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BanditVehicleAI : MonoBehaviour, IVehicule
{
    public enum FlankPosition { Left, Right }

    [Header("Target")]
    public Transform truck;
    public FlankPosition flankPosition = FlankPosition.Left;
    
    [Header("Positioning")]
    [Tooltip("Distance latérale par rapport au camion")]
    public float flankDistance = 8f;
    [Tooltip("Décalage vers l'avant (pour pas être pile à côté)")]
    public float forwardOffset = 5f;
    [Tooltip("Distance min pour éviter collision avec camion")]
    public float avoidTruckRadius = 5f;
    
    [Header("Speed Settings")]
    public float maxSpeed = 25f;
    public float acceleration = 8f;
    public float deceleration = 10f;
    
    [Header("NavMesh Settings")]
    public float updateDestinationInterval = 0.2f;
    public float stoppingDistance = 2f;

    [Header("Stop Behavior")]
    public float driftSlowdown = 3f;

    private NavMeshAgent agent;
    private bool isStopping = false;
    private float nextUpdateTime = 0f;
    private Vector3 currentVelocity;

    public bool goRight;
    public LookAtTarget lookAtTarget;
    
    public GameObject vfxMort;
    
    public float currentSpeed => agent != null ? agent.velocity.magnitude : 0f;

    void Start()
    {
        SetupNavMeshAgent();
    }

    void SetupNavMeshAgent()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // Configuration NavMesh
        agent.speed = maxSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = 120f; // Vitesse de rotation
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        //agent.radius = 2f; // Ajuste selon taille véhicule
        agent.height = 2f;
    }

    void Update()
    {
        if (truck == null || agent == null) return;

        if (isStopping)
        {
            StopVehicle();
            return;
        }

        // Update destination périodiquement (pas chaque frame = perfs)
        if (Time.time >= nextUpdateTime)
        {
            UpdateDestination();
            nextUpdateTime = Time.time + updateDestinationInterval;
        }

        // Ajuster vitesse dynamiquement selon distance
        AdjustSpeed();
    }

    void UpdateDestination()
    {
        Vector3 targetPos = CalculateFlankPosition();
        
        // Vérifier qu'on est pas trop proche du camion
        if (IsTooCloseToTruck(targetPos))
        {
            targetPos = PushAwayFromTruck(targetPos);
        }

        agent.SetDestination(targetPos);
    }

    Vector3 CalculateFlankPosition()
    {
        // Position sur le flanc
        Vector3 rightDir = truck.right;
        float sideMultiplier = flankPosition == FlankPosition.Left ? -1f : 1f;
        
        Vector3 flankOffset = rightDir * (flankDistance * sideMultiplier);
        Vector3 forwardOffsetVec = truck.forward * forwardOffset;
        
        return truck.position + flankOffset + forwardOffsetVec;
    }

    bool IsTooCloseToTruck(Vector3 position)
    {
        float distanceToTruck = Vector3.Distance(position, truck.position);
        return distanceToTruck < avoidTruckRadius;
    }

    Vector3 PushAwayFromTruck(Vector3 position)
    {
        // Repousser la position pour éviter collision
        Vector3 dirAwayFromTruck = (position - truck.position).normalized;
        return truck.position + dirAwayFromTruck * avoidTruckRadius;
    }

    void AdjustSpeed()
    {
        // Ralentir si proche de la destination
        float distanceToTarget = Vector3.Distance(transform.position, agent.destination);
        
        Rigidbody truckRb = truck.GetComponent<Rigidbody>();
        float truckSpeed = truckRb != null ? truckRb.linearVelocity.magnitude : maxSpeed;

        // Adapter vitesse : ralentir près du point, matcher vitesse camion sinon
        if (distanceToTarget < 10f)
        {
            agent.speed = Mathf.Lerp(agent.speed, truckSpeed * 0.9f, Time.deltaTime * deceleration);
        }
        else
        {
            agent.speed = Mathf.Lerp(agent.speed, maxSpeed, Time.deltaTime * acceleration);
        }
    }

    #region Stop Vehicle

    public void StopVehicle()
    {
        if (!isStopping)
        {
            isStopping = true;
            agent.isStopped = true;
        }

        // Drift/glisse progressif
        if (agent.velocity.magnitude > 0.1f)
        {
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, driftSlowdown * Time.deltaTime);
            agent.velocity = currentVelocity;
        }
    }

    #endregion

    public void Die()
    {
        NetworkObject explosionParticleIntance = Instantiate(vfxMort, transform.position, transform.rotation).GetComponent<NetworkObject>();
        explosionParticleIntance.Spawn();
        
        if (TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Despawn();
        }
        Destroy(gameObject);
    }

    // Debug visuel
    void OnDrawGizmosSelected()
    {
        if (truck == null) return;

        // Position cible
        Vector3 targetPos = CalculateFlankPosition();
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPos, 1f);

        // Zone d'évitement camion
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(truck.position, avoidTruckRadius);

        // Ligne vers cible
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPos);
    }
}