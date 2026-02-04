using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BanditBarrage : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;

    public float maxSpeed = 10f;
    public float acceleration = 10f;
    public float rotationSpeed = 5f;

    private Rigidbody rb;
    private Transform currentTarget;
    private Vector3 desiredVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Start()
    {
        currentTarget = pointB;
    }

    void FixedUpdate()
    {
        Vector3 toTarget = currentTarget.position - rb.position;
        float distance = toTarget.magnitude;
        Vector3 direction = toTarget.normalized;

        // Calcul de la vitesse souhaitée avec freinage progressif
        float brakingDistance = (maxSpeed * maxSpeed) / (2f * acceleration);
        float speed = (distance < brakingDistance) ? Mathf.Sqrt(2f * acceleration * distance) : maxSpeed;

        desiredVelocity = direction * speed;

        // Appliquer directement la vélocité
        Vector3 vel = desiredVelocity;
        vel.y = rb.velocity.y; // conserve gravité si nécessaire
        rb.velocity = vel;

        // Rotation vers la cible
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
        }

        // Changer de point quand proche
        if (distance < 0.1f)
            this.enabled = false;
    }
}