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
    public float maxSpeedFar = 25f;
    public float turnSpeed = 3f;
    public float acceleration = 5f;
    public float stoppingDistance = 2f;

    [Header("Esquive des obstacles")]
    public LayerMask obstacleMask;
    public float obstacleCheckDistance = 10f;
    public float obstacleAvoidStrength = 5f; // Intensité de l'évitement

    [Header("Ground Following")]
    public float groundRayDistance = 5f;

    [Header("Arrêt stylé")]
    public float stopDeceleration = 5f;
    public float driftIntensity = 1f;

    public float currentSpeed;
    public bool isStopping = false;

    public LookAtTarget lookAtTarget;

    void Update()
    {
        if (truck == null) return;

        if (isStopping)
        {
            StopVehicleMovement();
            return;
        }

        Rigidbody truckRb = truck.GetComponent<Rigidbody>();
        float truckSpeed = truckRb != null ? truckRb.linearVelocity.magnitude : 0f;

        // === Target point
        Vector3 targetPoint = GetTargetPoint();
        Vector3 toTarget = targetPoint - transform.position;

        // === Final direction
        Vector3 finalDir = toTarget;
        finalDir.y = 0f;
        if (finalDir.sqrMagnitude > 0.001f) finalDir.Normalize();

        // === Obstacle avoidance
        finalDir = AvoidObstacles(finalDir);

        // === Rotation
        if (finalDir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(finalDir);
            Vector3 euler = lookRot.eulerAngles;
            euler.x = 0f;
            euler.z = 0f;
            lookRot = Quaternion.Euler(euler);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
        }

        // === Speed management
        float distance = toTarget.magnitude;
        float desiredMaxSpeed = distance < 10f ? truckSpeed : maxSpeedFar;
        float desiredSpeed = Mathf.Clamp(distance / stoppingDistance * desiredMaxSpeed, 0.5f, desiredMaxSpeed);
        currentSpeed = Mathf.Lerp(currentSpeed, desiredSpeed, acceleration * Time.deltaTime);

        // === Safe Move
        float moveDistance = currentSpeed * Time.deltaTime;
        Vector3 moveDir = finalDir;

        RaycastHit moveHit;
        if (Physics.Raycast(transform.position + Vector3.up, moveDir, out moveHit, moveDistance, obstacleMask))
        {
            moveDistance = Mathf.Max(0f, moveHit.distance - 0.1f);
        }

        transform.position += moveDir * moveDistance;

        // === Ground follow
        RaycastHit hit;
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
            case RelativePosition.Left:
                return truck.position - truck.right * sideDistance + truck.forward * (sideDistance / 2);
            case RelativePosition.Right:
                return truck.position + truck.right * sideDistance + truck.forward * (sideDistance / 2);
            case RelativePosition.Front:
                return truck.position + truck.forward * frontBackDistance;
            case RelativePosition.Back:
                return truck.position - truck.forward * frontBackDistance;
        }
        return truck.position;
    }

    #region Stop Vehicle

    public void StopVehicle()
    {
        isStopping = true;
    }

    void StopVehicleMovement()
    {
        if (currentSpeed > 0.01f)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, stopDeceleration * Time.deltaTime);

            Vector3 drift = new Vector3(
                Random.Range(-driftIntensity, driftIntensity),
                0f,
                Random.Range(-driftIntensity, driftIntensity));

            transform.position += transform.forward * currentSpeed * Time.deltaTime + drift * Time.deltaTime;
            transform.Rotate(0f, Random.Range(-driftIntensity, driftIntensity) * Time.deltaTime * 10f, 0f);
        }
        else
        {
            currentSpeed = 0f;
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, groundRayDistance))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y;
            transform.position = pos;
        }
    }

    #endregion

    #region Obstacle Avoidance

    Vector3 AvoidObstacles(Vector3 desiredDirection)
    {
        Vector3 avoidance = Vector3.zero;
        float rayLength = obstacleCheckDistance;
        int rays = 5; // nombre de rayons en éventail devant le véhicule
        float angleSpread = 60f; // angle total en degrés pour l'éventail

        for (int i = 0; i < rays; i++)
        {
            float angle = -angleSpread / 2 + (angleSpread / (rays - 1)) * i;
            Vector3 rayDir = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            if (Physics.Raycast(transform.position + Vector3.up, rayDir, out RaycastHit hit, rayLength, obstacleMask))
            {
                // Plus l'obstacle est proche, plus la force d'évitement est forte
                float avoidStrength = (rayLength - hit.distance) / rayLength;
                avoidance -= rayDir * avoidStrength;
            }
        }

        Vector3 finalDir = desiredDirection + avoidance * obstacleAvoidStrength;
        finalDir.y = 0f;
        if (finalDir.sqrMagnitude > 0.001f) finalDir.Normalize();
        return finalDir;
    }

    #endregion

}
