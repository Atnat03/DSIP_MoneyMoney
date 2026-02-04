using Shooting;
using UnityEngine;

public class BanditVehicleAI : MonoBehaviour
{
    public enum RelativePosition { Left, Right, Front, Back }

    [Header("Références")]
    public Transform truck;
    public RelativePosition position;

    [Header("Distances visée avec le camion")]
    public float sideDistance = 8f;
    public float frontBackDistance = 20f;

    [Header("Mouvement Stats")]
    public float maxSpeedFar = 25f;      // vitesse max quand loin
    public float turnSpeed = 3f;
    public float acceleration = 5f;
    public float stoppingDistance = 2f;  // distance pour s'arrêter doucement

    [Header("Esquive des obstacles")]
    public LayerMask obstacleMask;
    public LayerMask vehicleMask;
    public float obstacleCheckDistance = 10f;
    public float vehicleAvoidRadius = 6f;

    [Header("Ground Following")]
    public float groundRayDistance = 5f;

    [Header("Arrêt stylé")]
    public float stopDeceleration = 5f;   // vitesse à laquelle la voiture ralentit
    public float driftIntensity = 1f;     // intensité du drift aléatoire à l'arrêt

    public float currentSpeed;
    public bool isStopping = false; // flag pour savoir si la voiture doit s'arrêter

    public LookAtTarget lookAtTarget;
    void Update()
    {
        if (truck == null)
        {
            /*
            var t = GameObject.Find("Trucknathan(Clone)");
            if (t != null) truck = t.transform;

            lookAtTarget.target = t.transform;
            */
            return;
        }

        // === Gestion arrêt stylé si activé
        if (isStopping)
        {
            StopVehicleMovement();
            return;
        }

        Rigidbody truckRb = truck.GetComponent<Rigidbody>();
        float truckSpeed = truckRb != null ? truckRb.velocity.magnitude : 0f;

        Vector3 targetPoint = GetTargetPoint();
        Vector3 toTarget = targetPoint - transform.position;

        // === Avoid obstacles (check devant les obstacles)
        Vector3 avoidObstacles = Vector3.zero;
        RaycastHit hit;
        Vector3[] dirs = { transform.forward, Quaternion.AngleAxis(-30, Vector3.up) * transform.forward, Quaternion.AngleAxis(30, Vector3.up) * transform.forward };
        
        foreach (var dir in dirs)
        {
            if (Physics.Raycast(transform.position + Vector3.up, dir, out hit, obstacleCheckDistance, obstacleMask))
            {
                Vector3 hNormal = hit.normal;
                hNormal.y = 0f;
                avoidObstacles += hNormal * (obstacleCheckDistance - hit.distance);
            }
        }

        // === Évitement autres véhicules
        Vector3 avoidVehicles = Vector3.zero;
        
        Collider[] nearby = Physics.OverlapSphere(transform.position, vehicleAvoidRadius, vehicleMask);
        foreach (var col in nearby)
        {
            if (col.transform == transform) continue;
            Vector3 away = transform.position - col.transform.position;
            away.y = 0f;
            avoidVehicles += away.normalized / away.magnitude;
        }

        // === Direction finale
        Vector3 finalDir = toTarget + avoidObstacles * 2f + avoidVehicles * 1.5f;
        finalDir.y = 0f;
        if (finalDir.sqrMagnitude > 0.001f) finalDir.Normalize();

        // === Rotation (axe Y seulement) lissée
        if (finalDir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(finalDir);
            Vector3 euler = lookRot.eulerAngles;
            euler.x = 0f;
            euler.z = 0f;
            lookRot = Quaternion.Euler(euler);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
        }

        // === Gestion vitesse fluide avec ralentissement progressif
        float distance = toTarget.magnitude;
        float desiredMaxSpeed = distance < 10f ? truckSpeed : maxSpeedFar;

        // On réduit la vitesse proportionnellement à la distance
        float desiredSpeed = Mathf.Clamp(distance / stoppingDistance * desiredMaxSpeed, 0.5f, desiredMaxSpeed);

        // Lissage
        currentSpeed = Mathf.Lerp(currentSpeed, desiredSpeed, acceleration * Time.deltaTime);

        // === Déplacement fluide vers la cible
        transform.position = Vector3.MoveTowards(transform.position, targetPoint, currentSpeed * Time.deltaTime);

        // === Suivi du sol
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, groundRayDistance))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y;
            transform.position = pos;
        }
    }

    Vector3 GetTargetPoint()
    {
        switch (position)
        {
            case RelativePosition.Left: return truck.position - truck.right * sideDistance + truck.forward * (sideDistance/2);
            case RelativePosition.Right: return truck.position + truck.right * sideDistance + truck.forward * (sideDistance/2);
            case RelativePosition.Front: return truck.position + truck.forward * frontBackDistance;
            case RelativePosition.Back: return truck.position - truck.forward * frontBackDistance;
        }
        return truck.position;
    }

    #region Stop Vehicle

    // Appeler cette fonction quand le bandit meurt
    public void StopVehicle()
    {
        isStopping = true;
    }

    void StopVehicleMovement()
    {
        if (currentSpeed > 0.01f)
        {
            // Décélération progressive
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, stopDeceleration * Time.deltaTime);

            // Déplacement avec drift aléatoire pour le style
            Vector3 drift = new Vector3(Random.Range(-driftIntensity, driftIntensity), 0f, Random.Range(-driftIntensity, driftIntensity));
            transform.position += transform.forward * currentSpeed * Time.deltaTime + drift * Time.deltaTime;

            // Rotation légère aléatoire pendant le drift
            transform.Rotate(0f, Random.Range(-driftIntensity, driftIntensity) * Time.deltaTime * 10f, 0f);
        }
        else
        {
            currentSpeed = 0f;
            //isStopping = false;
        }

        // Suivi du sol
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, groundRayDistance))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y;
            transform.position = pos;
        }
    }

    #endregion
}
