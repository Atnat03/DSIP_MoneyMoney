using System;
using System.Numerics;
using Shooting;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
using Vector2 = UnityEngine.Vector2;

[DefaultExecutionOrder(-1)]
public class FPSControllerMulti : NetworkBehaviour, IParentable
{
    [Header("References")]
    private Rigidbody truckRb;
    [SerializeField] Transform cameraTarget;
    [SerializeField] Transform cameraTransform;
    [SerializeField] Camera myCamera;
    [SerializeField] private CapsuleCollider capsule;
    
    [Header("Move Settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float sprintSpeed = 12f;
    private float speed;
    [SerializeField, Range(0.1f, 10)] float mouseSensibility;
    [SerializeField] Vector2 verticalLimit = new Vector2(-80, 80);
    [SerializeField] float cameraSmoothFollow = 15f;
    [SerializeField] float gravity = -9.8f;
    
    [Header("Jump Settings")]
    [SerializeField] private Transform groundedPoint;
    [SerializeField] private float distanceToGround;
    [SerializeField] private float jumpForce = 10;
    [SerializeField] bool isGrounded = false;
    [SerializeField] LayerMask groundLayer;
    private float verticalVelocity;

    [Header("Headbob Settings")]
    [SerializeField, Range(0, 0.2f)] float _amplitude = 0.05f;
    [SerializeField, Range(0, 30)] float frequency = 10f;
    [SerializeField] private Vector3 startPos;
    [SerializeField, Range(0, 2f)] float sprintAmplitudeMultiplier = 1.5f;
    [SerializeField, Range(0, 2f)] float sprintFrequencyMultiplier = 1.5f;

    [Header("Truck Physics")]
    [SerializeField] float truckDamping = 5f;
    
    float yaw;
    float pitch;
    float horizontalInput;
    private float verticalInput;
    
    public bool isInTruck = false;
    private Transform truckParent;

    [Header("Truck Interaction")]
    [SerializeField] float interactDistance = 5f;
    [SerializeField] LayerMask truckLayer;
    [SerializeField, Range(0, 1f)] float truckFollowStrength = 0.5f;
    
    private TruckInteraction nearbyTruck;
    public bool isDriver
    {
        get
        {
            if (!IsOwner || TruckController.instance == null) return false;
        
            var truckInteraction = TruckController.instance.GetComponent<TruckInteraction>();
            if (truckInteraction == null) return false;
        
            return truckInteraction.driverClientId.Value == OwnerClientId && isInTruck;
        }
    }    
    
    private Vector3 driverLocalPosition;
    
    [Header("UI")]
    public GameObject ui;

    public GameObject textGoInCamion;
    public GameObject textReload;
    public LayerMask maskCameraPlayer;

    private bool canReload = false;
    ShooterComponent shooter;
    
    public GameObject meshRenderer;

    public bool canSit = false;
    public bool isSitting = false;
    private Transform sittingPos;
    
    [Header("Reset Truck")]
    public KeyCode resetTruckKey = KeyCode.C;

    [Header("Gun Visual")] 
    public GameObject gunOwner;
    public GameObject gunOther;
    public bool hasSomethingInHand = false;
    
    public Camera MyCamera()
    {
        return myCamera;
    }

    [Header("Ladder Settings")]
    [SerializeField] private float ladderClimbSpeed = 3f;
    private bool isOnLadder = false;

    public void SetVisibleGun()
    {
        gunOther.SetActive(hasSomethingInHand);
        gunOwner.SetActive(!hasSomethingInHand);
    }
    
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            meshRenderer.gameObject.layer = LayerMask.NameToLayer("Default");
            
            meshRenderer.GetComponent<MeshRenderer>().material.color = GetComponent<PlayerCustom>().colorPlayer.Value;
            meshRenderer.gameObject.layer = LayerMask.NameToLayer("Default");
            
            myCamera.gameObject.SetActive(false);
            
            gunOwner.gameObject.layer = LayerMask.NameToLayer("Other");
            SetLayerRecursively(gunOther, LayerMask.NameToLayer("Default"));
            
            ui.SetActive(false);
            return;
        }

        gunOther.gameObject.layer = LayerMask.NameToLayer("Default");

        foreach (Transform t in gunOther.transform)
        {
            t.gameObject.layer = LayerMask.NameToLayer("Default");
        }
        
        gunOwner.gameObject.layer = LayerMask.NameToLayer("Owner");  
        
        myCamera.cullingMask = maskCameraPlayer;
        startPos = cameraTransform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        nearbyTruck = TruckController.instance.GetComponent<TruckInteraction>();

        yaw = transform.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;
        
        speed = moveSpeed;
        
        shooter = gameObject.GetComponent<ShooterComponent>();
    }
    
    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private Vector3 lastTruckPosition;

    public CharacterController controller;
    public bool canEnterInTruck = false;

    void Update()
    {
        if (!IsOwner) return;
        
        textGoInCamion.SetActive(canEnterInTruck);
        textReload.SetActive(canReload);

        isInTruck = transform.parent == TruckController.instance.transform;
        
        SetVisibleGun();
        
        capsule.enabled = !isDriver;
        
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            speed = sprintSpeed;
        }else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            speed = moveSpeed;
        }
        
        if (Input.GetKeyDown(KeyCode.E) && (canEnterInTruck || isInTruck))
        {
            if (isInTruck && isDriver)
            {
                nearbyTruck.TryExitTruck(this);
            }
            else if (isInTruck && !isDriver && canEnterInTruck)
            {
                TruckController.instance.GetComponent<TruckInteraction>().TryEnterTruck(this);
            }
            else if (!isInTruck && canEnterInTruck)
            {
                canEnterInTruck = false;
                TruckController.instance.GetComponent<TruckInteraction>().TryEnterTruck(this);
            }
        }

        if (TruckController.instance.isFallen.Value)
        {
            if (Input.GetKeyDown(resetTruckKey))
            {
                print("KEY RESET");
                TruckController.instance.AddValueToReset();
            }
        }

        canReload = CheckCanReload();

        if (Input.GetKeyUp(KeyCode.E) && !isDriver)
        {
            if(canReload)
            {
                shooter.StartToReload();
                canReload = false;
            }

            if (TruckController.instance.GetComponent<TruckInteraction>().hasDriver.Value == false)
                return;
        }

        if (isDriver && isInTruck)
        {
            if (TruckController.instance != null)
            {
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");
                bool brake = Input.GetKey(KeyCode.Space);
                TruckController.instance.SendInputsServerRpc(h, v, brake);
            }

            if (Input.GetButtonDown("Fire2"))
            {
                RaycastHit hit;
                Vector3 origin = MyCamera().transform.position;
                Vector3 direction = MyCamera().transform.forward;
                
                if (Physics.Raycast(origin, direction, out hit, 5f))
                {
                    if (hit.collider.CompareTag("Klaxon"))
                    {
                        TruckController.instance.PlayHornServerRpc();
                    }

                    if (hit.collider.CompareTag("RadioButton"))
                    {
                        print("Appuis sur radio button");
                        Radio.instance.CheckButton(hit.collider.gameObject);
                    }
                    
                    if (hit.collider.CompareTag("Phares"))
                    {
                        TruckController.instance.FrontLightOn.Value = !TruckController.instance.FrontLightOn.Value;
                    }
                }
            }
            
            HandleCameraInput();
            return;
        }

        if (isSitting && sittingPos != null)
        {
            transform.position = sittingPos.position;
                
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            
            HandleCameraInput();
            return;
        }

        if (!isInTruck || !isDriver)
        {
            HandleMovement();
        }
    }

    public void Sit(Transform sitPos)
    {
        isSitting = true;
        canSit = false;
        sittingPos = sitPos;
        capsule.enabled = false;
    }

    public void StandUp()
    {
        sittingPos = null;
        isSitting = false;
        capsule.enabled = true;
    }

    bool CheckCanReload()
    {
        RaycastHit hit;
        bool final = false;

        if (Physics.Raycast(MyCamera().transform.position, MyCamera().transform.forward, out hit, 5f))
        {
            if (hit.collider.CompareTag("ReloadStation"))
            {
                final = true;
            }
        }
        
        return (Vector3.Distance(transform.position, TruckController.instance.reload.position) < TruckController.instance.raduisToReload) && final;
    }

    private bool isFreeze;
    public bool IsFreeze => isFreeze;
    private Vector3 freezeCameraPos;
    
    public void StartFreeze()
    {
        isFreeze = true;
    }

    public void StopFreeze()
    {
        isFreeze = false;
    }
    
    void HandleCameraInput()
    {
        if (!isFreeze)
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensibility;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensibility;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, verticalLimit.x, verticalLimit.y);
        }
    }

    private void HandleHeadbob()
    {
        if (isOnLadder) return;
        
        if (!controller.isGrounded || new Vector2(horizontalInput, verticalInput).magnitude < 0.1f)
        {
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition,
                startPos,
                Time.deltaTime
            );
            return;
        }

        // Sprint ?
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        float amp = _amplitude * (isSprinting ? sprintAmplitudeMultiplier : 1f);
        float freq = frequency * (isSprinting ? sprintFrequencyMultiplier : 1f);

        Vector3 bobOffset;
        bobOffset.y = Mathf.Sin(Time.time * freq) * amp;
        bobOffset.x = Mathf.Cos(Time.time * freq * 0.5f) * amp * 2f;
        bobOffset.z = 0f;

        cameraTransform.localPosition = startPos + bobOffset;
    }

    void HandleMovement()
    {
        if (!isFreeze)
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");

            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensibility;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensibility;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, verticalLimit.x, verticalLimit.y);

            Vector3 move = Vector3.zero;
            
            if (isOnLadder)
            {
                Vector3 climb = new Vector3(horizontalInput, verticalInput, 0f);
                move = transform.TransformDirection(climb) * ladderClimbSpeed;

                verticalVelocity = 0f;
            }
            else
            {
                if (controller.isGrounded)
                {
                    if (verticalVelocity < 0)
                        verticalVelocity = -2f;

                    if (Input.GetKeyDown(KeyCode.Space) && !isOnLadder)
                        verticalVelocity = jumpForce;
                }
                else
                {
                    verticalVelocity += gravity * Time.deltaTime;
                }

                Vector3 localMove = new Vector3(horizontalInput, 0, verticalInput).normalized;
                move = transform.TransformDirection(localMove) * speed;
                move.y = verticalVelocity;
            }

            controller.enabled = true;
            controller.Move(move * Time.deltaTime);
            controller.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (!IsOwner) return;
        if (isFreeze) return;
        if (MyCamera().GetComponent<CameraShake>().shaking) return;
        
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0);
        
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, cameraTarget.position, Time.deltaTime * cameraSmoothFollow);
    
        if(!isInTruck || !isDriver)
            HandleHeadbob();
    }

    public void EnterTruck(bool asDriver, Vector3 spawnPosition) 
    {
        if (controller != null) controller.enabled = false;
        truckRb = TruckController.instance.GetComponent<Rigidbody>();
        lastTruckPosition = truckRb.position;
        
        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null) {
            netTransform.InLocalSpace = true;
        }

        if (IsOwner) {
            Transform targetSeat = asDriver ? TruckController.instance.driverPos : TruckController.instance.spawnPassager;
            transform.localPosition = targetSeat.localPosition;
            transform.localRotation = Quaternion.identity;
        }
    }
    
    public void ExitTruck(Vector3 exitPosition)
    {
        print("ExitTruck");
        
        transform.position = exitPosition;
        
        truckRb = null;
        
        if (controller != null)
            controller.enabled = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetParentServerRpc(bool setParent)
    {
        if (setParent)
        {
            Transform truckTransform = TruckController.instance.transform;
            if (truckParent == null)
            {
                truckParent = truckTransform;
            }
        
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.TrySetParent(truckParent);
            }
            
            controller.enabled = false;
        }
        else
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.TrySetParent((Transform)null);
            }
            
            controller.enabled = true;
        }
    }
    
        [ServerRpc(RequireOwnership = false)]
    public void SetPassengerModeServerRpc(bool isPassenger, Vector3 desiredLocalPos)
    {
        // Le serveur peut valider si besoin (ex: vraiment dans le camion ?)
        SetPassengerModeClientRpc(isPassenger, desiredLocalPos);
    }

    [ClientRpc]
    private void SetPassengerModeClientRpc(bool isPassenger, Vector3 desiredLocalPos)
    {
        isInTruck = isPassenger;

        if (isPassenger)
        {
            if (controller != null)
            {
                controller.enabled = false;
            }

            var netTransform = GetComponent<NetworkTransform>();
            if (netTransform != null)
            {
                netTransform.InLocalSpace = true; 
            }

            Debug.Log("Client: Mode passager activé - CC disabled + InLocalSpace=true");
        }
        else
        {
            if (controller != null)
            {
                controller.enabled = true;
            }

            var netTransform = GetComponent<NetworkTransform>();
            if (netTransform != null)
            {
                netTransform.InLocalSpace = false;
            }

            Debug.Log("Client: Mode passager désactivé");
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("PorteConducteur") && 
            TruckController.instance.GetComponent<TruckInteraction>().hasDriver.Value == false &&
            !hasSomethingInHand)
        {
            canEnterInTruck = true;
        }

        if (other.CompareTag("Echelle"))
        {
            isOnLadder = true;
            verticalVelocity = 0f;        
        }
    }
    
    public void OnTriggerExit(Collider other)
    {
        if (other.transform.CompareTag("PorteConducteur"))
        {
            canEnterInTruck = false;
        }
        
        if (other.CompareTag("Echelle"))
        {
            isOnLadder = false;
        }
    }

    public Transform Transform => transform;
    public NetworkObject NetworkObject => GetComponent<NetworkObject>();
    
    public void OnParented(Transform parent)
    {
        SetPassengerModeServerRpc(true, parent.InverseTransformPoint(transform.position));
    }

    public void OnUnparented()
    {
        SetPassengerModeServerRpc(false, Vector3.zero);
    }
}

public interface IParentable
{
    Transform Transform { get; }
    NetworkObject NetworkObject { get; }

    void OnParented(Transform parent);
    void OnUnparented();
}