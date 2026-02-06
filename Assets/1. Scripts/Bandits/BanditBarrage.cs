using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BanditBarrage : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;

    public float maxSpeed = 10f;
    public float acceleration = 10f;
    public float rotationSpeed = 5f;
    public float uprightStrength = 500f; // force qui maintient la voiture droite

    private Rigidbody rb;
    private Transform currentTarget;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // ❌ PLUS DE CONSTRAINTS
        rb.centerOfMass = new Vector3(0f, -0.8f, 0f);
    }

    void Start()
    {
        currentTarget = pointB;
    }

    void FixedUpdate()
    {
        KeepUpright();

        Vector3 toTarget = currentTarget.position - rb.position;
        float distance = toTarget.magnitude;

        if (distance < 0.1f)
        {
            rb.linearVelocity = Vector3.zero;
            this.enabled = false;
            return;
        }

        Vector3 direction = toTarget.normalized;

        float brakingDistance = (maxSpeed * maxSpeed) / (2f * acceleration);
        float speed = (distance < brakingDistance)
            ? Mathf.Sqrt(2f * acceleration * distance)
            : maxSpeed;

        Vector3 desiredVelocity = direction * speed;

        Vector3 force = (desiredVelocity - rb.linearVelocity) * acceleration;
        force.y = 0f;
        rb.AddForce(force, ForceMode.Acceleration);

        // Rotation vers la cible (uniquement Y)
        Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
        Quaternion yOnly = Quaternion.Euler(0, targetRot.eulerAngles.y, 0);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, yOnly, rotationSpeed * Time.fixedDeltaTime));
    }

    void KeepUpright()
    {
        Vector3 torque = Vector3.Cross(transform.up, Vector3.up);
        rb.AddTorque(torque * uprightStrength);
    }
}