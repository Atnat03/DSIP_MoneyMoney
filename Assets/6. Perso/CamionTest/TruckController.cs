using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
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

    public GameObject backLights;
    public NetworkVariable<bool> BackLightOn = new NetworkVariable<bool>();

    public GameObject frontLights;
    public NetworkVariable<bool> FrontLightOn = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public MeshRenderer lightsButtons;
    
    float horizontalInput;
    float verticalInput;
    float currentSteerAngle;
    float currentBreakForce;
    bool isBreaking;

    private Rigidbody rb;

    public AudioClip klaxon;
    
    private float t = 0f;
    [SerializeField, Tooltip("Value pour 1 joueur, qui sera multiplier par le nombre de joueuur")] 
    private NetworkVariable<float> targetValueToReset = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);    private NetworkVariable<float> currentValueToResetNet = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isFallen = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private Image jaugeMashing;
    
    [Header("Truck Bounds")]
    [SerializeField] public Vector3 boundsCenter = Vector3.zero; 
    [SerializeField] public Vector3 boundsSize = new Vector3(5f, 3f, 10f); 
    [SerializeField] public float boundsPushForce = 10f;

    public Transform driverPos;
    public Transform spawnPassager;

    private TruckInteraction truckInteraction;
    
    private readonly HashSet<NetworkObject> trackedPassengers = new();

    public Transform reload;
    public float raduisToReload = 2f;

    public Transform steeringWheel;
    
    [Header("Quand il se prend un mur")]
    [SerializeField] float velocityToTriggerShake = 25f;
    [SerializeField] float cameraShakeDuration = 0.25f;
    [SerializeField] float cameraShakeMagnitude = 0.1f;
    
    public List<IParentable> parentable;
    
    [Header("SFX")]
    public AudioSource engineAudioSource;
    public AudioSource klaxonAudioSource;
    
    private void Awake()
    {
        instance = this;

        truckInteraction = GetComponent<TruckInteraction>();
        
        jaugeMashing.transform.parent.gameObject.SetActive(false);
        
        parentable = new List<IParentable>();
    }

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularDamping = 1.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        currentValueToResetNet.OnValueChanged += OnCurrentValueChanged;
        isFallen.OnValueChanged += OnIsFallenChanged;
        BackLightOn.OnValueChanged += OnBackLightsChanged;
        FrontLightOn.OnValueChanged += OnFrontLigthChanged;
        
        engineAudioSource.Play();
        
        UpdateJauge();    
    }

    private void OnDestroy()
    {
        currentValueToResetNet.OnValueChanged -= OnCurrentValueChanged;
        isFallen.OnValueChanged -= OnIsFallenChanged;
        BackLightOn.OnValueChanged -= OnBackLightsChanged;
        FrontLightOn.OnValueChanged -= OnFrontLigthChanged;
    }

    void Update()
    {
        if (!IsServer) 
        {
            UpdateWheelsVisual();
            return;
        }
        
        CheckPassengersBounds();
    
        rb.centerOfMass = centerOfMass;

        rb.isKinematic = !truckInteraction.hasDriver.Value;
        
        if (truckInteraction != null && truckInteraction.HasDriver())
        {
            HandleMotor();
            HandleSteering();
        }
        else
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            isBreaking = false;
        }
        
        engineAudioSource.volume = rb.linearVelocity.magnitude/100;
    
        UpdateWheels();
        CheckFall();
    }

    [ClientRpc]
    private void TriggerCameraShakeClientRpc()
    {
        if (NetworkManager.Singleton.LocalClient == null) return;

        NetworkObject playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (playerObject == null) return;

        FPSControllerMulti fps = playerObject.GetComponent<FPSControllerMulti>();
        if (fps == null || !fps.isInTruck) return;

        CameraShake cameraShake = fps.MyCamera().GetComponent<CameraShake>();
        if (cameraShake != null)
        {
            cameraShake.TriggerShake(cameraShakeDuration, cameraShakeMagnitude);
        }
    }


    
    public void CheckVelecityToApplyShake()
    {
        if (rb.linearVelocity.magnitude > velocityToTriggerShake)
        {
            TriggerCameraShakeClientRpc();
        }
    }

    private void OnBackLightsChanged(bool previousValue, bool newValue)
    {
        backLights.SetActive(newValue);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ToggleFrontLightsServerRpc()
    {
        FrontLightOn.Value = !FrontLightOn.Value;
    }
    
    private void OnFrontLigthChanged(bool previousValue, bool newValue)
    {
        frontLights.SetActive(newValue);
        
        lightsButtons.material.color = frontLights.activeSelf ? Color.yellow : Color.gray;
    }
    
    private readonly HashSet<NetworkObject> trackedParentables = new();
    
    void CheckPassengersBounds()
    {
        if (!IsServer) return;
        
        foreach (IParentable p in parentable)
        {
            NetworkObject netObj = p.NetworkObject;
            if (netObj == null) continue;

            Transform t = p.Transform;

            bool inside = !IsOutsideTruckBounds(t);
            bool alreadyParented = trackedParentables.Contains(netObj);

            if (inside && !alreadyParented)
            {
                ParentObject(p);
            }
            else if (!inside && alreadyParented)
            {
                UnparentObject(p);
            }
        }
    }

    
    void ParentObject(IParentable parentable)
    {
        NetworkObject netObj = parentable.NetworkObject;

        netObj.TrySetParent(transform, true);
        trackedParentables.Add(netObj);

        parentable.OnParented(transform);

        netObj.GetComponent<NetworkTransform>().InLocalSpace = true;

        Debug.Log($"{netObj.name} parenté au camion");
    }

    void UnparentObject(IParentable parentable)
    {
        NetworkObject netObj = parentable.NetworkObject;

        netObj.TrySetParent((Transform)null, true);
        trackedParentables.Remove(netObj);

        parentable.OnUnparented();
        
        netObj.GetComponent<NetworkTransform>().InLocalSpace = false;

        Debug.Log($"{netObj.name} déparenté du camion");
    }

    

    [ServerRpc(RequireOwnership = false)]
    public void SendInputsServerRpc(float horizontal, float vertical, bool breaking, ServerRpcParams rpcParams = default)
    {
        if (truckInteraction != null && truckInteraction.IsDriver(rpcParams.Receive.SenderClientId))
        {
            horizontalInput = horizontal;
            verticalInput = vertical;
            isBreaking = breaking;
            BackLightOn.Value = breaking;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayHornServerRpc()
    {
        PlayHornClientRpc();
    }

    [ClientRpc]
    private void PlayHornClientRpc()
    {
        klaxonAudioSource.PlayOneShot(klaxon);
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
        steeringWheel.localRotation = Quaternion.Euler(0, 0, -currentSteerAngle);
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
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(backLeftWheelCollider, backLeftWheelTransform);
        UpdateSingleWheel(backRightWheelCollider, backRightWheelTransform);
    }

    private bool multiply = false;
    
    private void CheckFall()
    {
        Vector3 rot = transform.rotation.eulerAngles;
        float x = NormalizeAngle(rot.x);
        float z = NormalizeAngle(rot.z);

        if (!isFallen.Value && (Mathf.Abs(x) > 75f || Mathf.Abs(z) > 75f))
        {
            Debug.Log("Camion tombé");
            isFallen.Value = true;
        }

        if (isFallen.Value)
        {
            jaugeMashing.transform.parent.gameObject.SetActive(true);
            
            if(!multiply)
            {
                targetValueToReset.Value *= NetworkManager.Singleton.ConnectedClientsList.Count;
                multiply = true;
            }

            if (currentValueToResetNet.Value >= targetValueToReset.Value)
            {
                ResetTruckServerRpc(transform.position);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddValueToResetServerRpc()
    {
        currentValueToResetNet.Value++;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetTruckServerRpc(Vector3 newPosition)
    {
        print("Reset position : " + newPosition);
        
        Vector3 rot = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, rot.y, 0f);
        transform.position = newPosition + Vector3.up * 2f;
        
        jaugeMashing.transform.parent.gameObject.SetActive(false);

        currentValueToResetNet.Value = 0f;
        isFallen.Value = false;

        multiply = false;
    }

    public float raduisReset = 50;
    
    public void ResetCamionToNearPoint()
    {
        float minDist = Mathf.Infinity;
        Transform closest = null;

        Collider[] hits = Physics.OverlapSphere(transform.position, raduisReset);

        print(hits.Length);
        
        foreach (Collider hit in hits)
        {
            if (hit.gameObject.layer != LayerMask.NameToLayer("SpawnPointBandit"))
                continue;

            float dist = (hit.transform.position - transform.position).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                closest = hit.transform;
            }
        }

        if (closest != null)
            ResetTruckServerRpc(closest.position);
    }


    public void AddValueToReset()
    {
        if (IsServer)
            currentValueToResetNet.Value++;
        else
            AddValueToResetServerRpc();
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
    
    bool IsOutsideTruckBounds(Transform playerTransform)
    {
        Vector3 localPos = transform.InverseTransformPoint(playerTransform.position);
    
        Vector3 halfSize = boundsSize * 0.5f;
        Vector3 center = boundsCenter;

        return
            localPos.x < center.x - halfSize.x || localPos.x > center.x + halfSize.x ||
            localPos.y < center.y - halfSize.y || localPos.y > center.y + halfSize.y ||
            localPos.z < center.z - halfSize.z || localPos.z > center.z + halfSize.z;
    }
    
    private void OnCurrentValueChanged(float oldValue, float newValue)
    {
        UpdateJauge();
    }

    private void OnIsFallenChanged(bool oldValue, bool newValue)
    {
        UpdateJauge();
    }

    private void UpdateJauge()
    {
        if (jaugeMashing == null) return;

        jaugeMashing.transform.parent.gameObject.SetActive(isFallen.Value);

        if (isFallen.Value && targetValueToReset.Value > 0f)
            jaugeMashing.fillAmount = Mathf.Clamp01(currentValueToResetNet.Value / targetValueToReset.Value);
        else
            jaugeMashing.fillAmount = 0f;
    }
    
    public void ResetDriverInputs()
    {
        horizontalInput = 0f;
        verticalInput = 0f;
        isBreaking = false;
        BackLightOn.Value = false;
    }
    
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle") || other.gameObject.layer == LayerMask.NameToLayer("Bandits"))
        {
            CheckVelecityToApplyShake();
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 worldCenter = transform.TransformPoint(boundsCenter);
        
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boundsSize);
        Gizmos.matrix = oldMatrix;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, raduisReset);
    }

    public void AddInParent(IParentable ob)
    {
        print("AddInParent :  " + parentable.Count);
        parentable.Add(ob);
    }
}