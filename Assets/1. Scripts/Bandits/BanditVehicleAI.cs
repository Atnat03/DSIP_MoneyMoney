using UnityEngine;

public class BanditVehicleAI : MonoBehaviour
{
    public enum RelativePosition
    {
        Left,
        Right,
        Front,
        Back
    }

    [Header("References")]
    public Transform truck;
    public RelativePosition position;

    [Header("Distances from truck")]
    public float sideDistance = 8f;
    public float frontBackDistance = 20f;

    [Header("Movement")]
    public float maxSpeed = 25f;
    public float turnSpeed = 3f;
    public float acceleration = 8f;
    public float braking = 12f;

    [Header("Truck speed control")]
    public float truckSpeedThreshold = 5f; // en dessous de cette vitesse, les bandits freinent

    [Header("Avoidance")]
    public LayerMask obstacleMask;
    public LayerMask vehicleMask;
    public float obstacleCheckDistance = 10f;
    public float vehicleAvoidRadius = 6f;

    [Header("Ground Following")]
    public float groundRayDistance = 5f; // distance pour détecter le sol

    float currentSpeed;

    void Update()
    {
        if (truck == null)
        {
            if (GameObject.Find("Trucknathan(Clone)") != null)
            {
                truck = GameObject.Find("Trucknathan(Clone)").transform;
            }
            return;
        }

        Vector3 targetPoint = GetTargetPoint();

        // ===== STEERING (direction) =====
        Vector3 toTarget = targetPoint - transform.position;

        Vector3 avoidObstacles = ComputeObstacleAvoidance();
        Vector3 avoidVehicles = ComputeVehicleAvoidance();

        Vector3 finalDir =
            toTarget + avoidObstacles * 2f + avoidVehicles * 1.5f;

        // Rotation uniquement sur l'axe horizontal
        Vector3 finalDirHorizontal = finalDir;
        finalDirHorizontal.y = 0f;
        if (finalDirHorizontal.sqrMagnitude > 0.001f)
            finalDirHorizontal.Normalize();

        Quaternion lookRot = Quaternion.LookRotation(finalDirHorizontal);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, turnSpeed * Time.deltaTime);

        // ===== SPEED CONTROL =====
        ControlSpeed();

        // ===== MOVE =====
        Vector3 move = transform.forward * currentSpeed * Time.deltaTime;
        transform.position += new Vector3(move.x, 0f, move.z);

        // ===== FOLLOW GROUND =====
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, groundRayDistance))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y;
            transform.position = pos;
        }
    }

    void ControlSpeed()
    {
        Rigidbody truckRb = truck.GetComponent<Rigidbody>();
        float truckSpeed = truckRb != null ? truckRb.velocity.magnitude : maxSpeed;

        // Si le camion est trop lent → freiner progressivement
        if (truckSpeed < truckSpeedThreshold && Vector3.Distance(transform.position, truck.position) < 40)
        {
            currentSpeed -= braking * Time.deltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
            return;
        }

        // Position du bandit dans l'axe AVANT du camion
        Vector3 toBandit = transform.position - truck.position;
        float forwardOffsetCurrent = Vector3.Dot(toBandit, truck.forward);

        float desiredOffset = 0f;

        switch (position)
        {
            case RelativePosition.Front: desiredOffset = frontBackDistance; break;
            case RelativePosition.Back: desiredOffset = -frontBackDistance; break;
            default: desiredOffset = 0f; break;
        }

        float error = desiredOffset - forwardOffsetCurrent;

        if (error > 2f)
        {
            currentSpeed += acceleration * Time.deltaTime;
        }
        else if (error < -2f)
        {
            currentSpeed -= braking * Time.deltaTime;
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, truckSpeed, Time.deltaTime);
        }

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
    }

    Vector3 ComputeObstacleAvoidance()
    {
        Vector3 avoid = Vector3.zero;
        RaycastHit hit;

        Vector3[] dirs = {
            transform.forward,
            Quaternion.AngleAxis(-30, Vector3.up) * transform.forward,
            Quaternion.AngleAxis(30, Vector3.up) * transform.forward
        };

        foreach (var dir in dirs)
        {
            if (Physics.Raycast(transform.position, dir, out hit, obstacleCheckDistance, obstacleMask))
            {
                avoid += hit.normal * (obstacleCheckDistance - hit.distance);
            }
        }

        return avoid;
    }

    Vector3 ComputeVehicleAvoidance()
    {
        Vector3 avoid = Vector3.zero;

        Collider[] nearby = Physics.OverlapSphere(transform.position, vehicleAvoidRadius, vehicleMask);

        foreach (var col in nearby)
        {
            if (col.transform == transform) continue;

            Vector3 away = transform.position - col.transform.position;
            avoid += away.normalized / away.magnitude;
        }

        return avoid;
    }

    Vector3 GetTargetPoint()
    {
        switch (position)
        {
            case RelativePosition.Left:
                return truck.position - truck.right * sideDistance;

            case RelativePosition.Right:
                return truck.position + truck.right * sideDistance;

            case RelativePosition.Front:
                return truck.position + truck.forward * frontBackDistance;

            case RelativePosition.Back:
                return truck.position - truck.forward * frontBackDistance;
        }

        return truck.position;
    }
}
