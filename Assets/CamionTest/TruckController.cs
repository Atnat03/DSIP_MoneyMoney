using System;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class TruckController : MonoBehaviour
{
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

    [Header("Movement settings")] 
    [SerializeField, Range(0, 1f)] private float freinMoteur;
    [SerializeField, Range(0.5f, 1.5f)] private float adherence;
    
    float horizontalInput;
    float verticalInput;
    float currentSteerAngle;
    float currentBreakForce;
    bool isBreaking;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularDamping = 1.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        rb.centerOfMass = centerOfMass;

        frontLeftWheelCollider.wheelDampingRate  = freinMoteur;
        frontRightWheelCollider.wheelDampingRate  = freinMoteur;
        backLeftWheelCollider.wheelDampingRate  = freinMoteur;
        backRightWheelCollider.wheelDampingRate  = freinMoteur;
        
        WheelFrictionCurve fl = frontLeftWheelCollider.forwardFriction;
        fl.stiffness = adherence;
        frontLeftWheelCollider.forwardFriction = fl;
        
        WheelFrictionCurve fr = frontRightWheelCollider.forwardFriction;
        fr.stiffness = adherence;
        frontRightWheelCollider.forwardFriction = fr;
        
        WheelFrictionCurve bl = backLeftWheelCollider.forwardFriction;
        bl.stiffness = adherence;
        backLeftWheelCollider.forwardFriction = bl;
        
        WheelFrictionCurve br = backRightWheelCollider.forwardFriction;
        br.stiffness = adherence;
        backRightWheelCollider.forwardFriction = br;
        
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
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
}