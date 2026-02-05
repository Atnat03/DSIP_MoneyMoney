using System;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class TruckController : NetworkBehaviour
{
    public static TruckController instance;
    
    [SerializeField] float motorForce = 100f;
    [SerializeField] float breakForce = 1000f;
    [SerializeField] float maxSteerAngle = 30f;

    [SerializeField] Vector3 centerOfMass;
    
    [Header("Wheels Colliders")]
    public WheelCollider frontLeftWheelCollider;
    public WheelCollider frontRightWheelCollider;
    public WheelCollider backLeftWheelCollider;
    public WheelCollider backRightWheelCollider;
    
    [Header("Wheels Transforms")]
    public Transform frontLeftWheelTransform;
    public Transform frontRightWheelTransform;
    public Transform backLeftWheelTransform;
    public Transform backRightWheelTransform;

    
    float horizontalInput;
    float verticalInput;
    float currentSteerAngle;
    float currentBreakForce;
    bool isBreaking;

    private Rigidbody rb;

    public AudioClip klaxon;
    
    public float durationBeforeReset = 2f;
    private float t = 0f;
    private bool isFallen = false;
    
    [Header("Truck Bounds")]
    [SerializeField] public Vector3 boundsCenter = Vector3.zero; 
    [SerializeField] public Vector3 boundsSize = new Vector3(5f, 3f, 10f); 
    [SerializeField] public float boundsPushForce = 10f;

    public Transform driverPos;
    public Transform spawnPassager;

    private TruckInteraction truckInteraction;

    private void Awake()
    {
        instance = this;
        truckInteraction = GetComponent<TruckInteraction>();
    }

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularDamping = 1.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (!IsServer) return;
    }

    void Update()
    {
        if (!IsServer) 
        {
            UpdateWheelsVisual();
            return;
        }
        
        rb.centerOfMass = centerOfMass;
        
        if (truckInteraction != null && truckInteraction.HasDriver())
        {

        }
        
        HandleMotor();
        HandleSteering();
        UpdateWheels();
        CheckFall();
    }


    [ServerRpc(RequireOwnership = false)]
    public void SendInputsServerRpc(float horizontal, float vertical, bool breaking, bool horn, ServerRpcParams rpcParams = default)
    {
        if (truckInteraction != null && truckInteraction.IsDriver(rpcParams.Receive.SenderClientId))
        {
            horizontalInput = horizontal;
            verticalInput = vertical;
            isBreaking = breaking;
            
            if (horn)
            {
                PlayHornClientRpc();
            }
        }
    }

    [ClientRpc]
    void PlayHornClientRpc()
    {
        GetComponent<AudioSource>().PlayOneShot(klaxon);
    }

    public void GetInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        isBreaking = Input.GetKey(KeyCode.Space);
    }

    void HandleMotor()
    {
        frontLeftWheelCollider.motorTorque = verticalInput * motorForce;
        frontRightWheelCollider.motorTorque = verticalInput * motorForce;
        backLeftWheelCollider.motorTorque = verticalInput * motorForce;
        backRightWheelCollider.motorTorque = verticalInput * motorForce;

        currentBreakForce = isBreaking ? breakForce : 0f;
        ApplyBraking();
    }

    private void ApplyBraking()
    {
        frontLeftWheelCollider.brakeTorque = currentBreakForce;
        frontRightWheelCollider.brakeTorque = currentBreakForce;
        backLeftWheelCollider.brakeTorque = currentBreakForce;
        backRightWheelCollider.brakeTorque = currentBreakForce;
    }

    void HandleSteering()
    {
        currentSteerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

    void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }
    
    private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(backLeftWheelCollider, backLeftWheelTransform);
        UpdateSingleWheel(backRightWheelCollider, backRightWheelTransform);
    }
    
    private void UpdateWheelsVisual()
    {
        // Clients non-serveur mettent juste à jour les visuels
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(backLeftWheelCollider, backLeftWheelTransform);
        UpdateSingleWheel(backRightWheelCollider, backRightWheelTransform);
    }

    private void CheckFall()
    {
        Vector3 rot = transform.rotation.eulerAngles;

        float x = NormalizeAngle(rot.x);
        float z = NormalizeAngle(rot.z);

        if (!isFallen && (Mathf.Abs(x) > 75f || Mathf.Abs(z) > 75f))
        {
            Debug.Log("Camion tombé");
            isFallen = true;
            t = durationBeforeReset;
        }

        if (isFallen)
        {
            t -= Time.deltaTime;

            if (t <= 0f)
            {
                ResetTruck();
            }
        }
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }

    void ResetTruck()
    {
        Debug.Log("Camion redressé");

        Vector3 rot = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, rot.y, 0f);

        transform.position += Vector3.up * 2f;
        isFallen = false;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 worldCenter = transform.TransformPoint(boundsCenter);
        
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boundsSize);
        Gizmos.matrix = oldMatrix;
    }
}