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
    public float flankDistance = 8f;
    public float forwardOffset = 5f;

    [Header("Truck Safety")]
    [Tooltip("Distance minimale autorisée autour du camion")]
    public float minDistanceFromTruck = 7f;
    [Tooltip("Distance max pour éviter que le bandit parte trop loin")]
    public float maxDistanceFromTruck = 20f;

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

    public LookAtTarget lookAtTarget;
    public GameObject vfxMort;

    public float currentSpeed => agent != null ? agent.velocity.magnitude : 0f;
    public GameObject tourelle;

    void Start()
    {
        SetupNavMeshAgent();

        tourelle.GetComponent<NetworkObject>().Spawn();
    }

    void SetupNavMeshAgent()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.speed = maxSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = 250f;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
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

        if (Time.time >= nextUpdateTime)
        {
            UpdateDestination();
            nextUpdateTime = Time.time + updateDestinationInterval;
        }

        AdjustSpeed();
    }

    void UpdateDestination()
    {
        Vector3 targetPos = CalculateFlankPosition();

        // 🔒 Refus du path s'il entre dans la zone interdite
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(targetPos, path);

        foreach (var corner in path.corners)
        {
            if (Vector3.Distance(corner, truck.position) < minDistanceFromTruck)
            {
                return; // on annule cette destination
            }
        }

        agent.SetDestination(targetPos);
    }

    Vector3 CalculateFlankPosition()
    {
        Vector3 rightDir = truck.right;
        float sideMultiplier = flankPosition == FlankPosition.Left ? -1f : 1f;

        Vector3 desired = truck.position
                        + rightDir * (flankDistance * sideMultiplier)
                        + truck.forward * forwardOffset;

        // 🔒 Clamp dans un anneau sécurisé autour du camion
        Vector3 dir = (desired - truck.position).normalized;
        float dist = Vector3.Distance(desired, truck.position);
        dist = Mathf.Clamp(dist, minDistanceFromTruck, maxDistanceFromTruck);

        return truck.position + dir * dist;
    }

    void AdjustSpeed()
    {
        float distanceToTarget = Vector3.Distance(transform.position, agent.destination);

        Rigidbody truckRb = truck.GetComponent<Rigidbody>();
        float truckSpeed = truckRb != null ? truckRb.linearVelocity.magnitude : maxSpeed;

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

        if (agent.velocity.magnitude > 0.1f)
        {
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, driftSlowdown * Time.deltaTime);
            agent.velocity = currentVelocity;
        }
    }

    #endregion

    public GameObject debris;
    public void Die()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        NetworkObject explosionParticleIntance = Instantiate(vfxMort, transform.position, transform.rotation).GetComponent<NetworkObject>();
        explosionParticleIntance.Spawn();
        SFX_Manager.instance.PlaySFX(9,.4f);
        
        GameObject debriss = Instantiate(debris, transform.position, transform.rotation);
        debriss.GetComponent<NetworkObject>().Spawn();

        if (TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Despawn();
        }
        
        if (tourelle.TryGetComponent<NetworkObject>(out var netObj2))
        {
            netObj2.Despawn();
        }
        
        netObj.Despawn(true);
        netObj2.Despawn(true);
    }

    void OnDrawGizmosSelected()
    {
        if (truck == null) return;

        Vector3 targetPos = CalculateFlankPosition();

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPos, 1f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(truck.position, minDistanceFromTruck);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPos);
    }
}
