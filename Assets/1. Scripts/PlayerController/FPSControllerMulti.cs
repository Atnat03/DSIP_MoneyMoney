using Shooting;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
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
    [SerializeField] CapsuleCollider capsuleCollider;
    
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
    public float horizontalInput;
    public float verticalInput;
    
    public bool isInTruck = false;
    private Transform truckParent;
    public bool isPassenger = false; // NOUVEAU: pour distinguer passager du conducteur

    [Header("Truck Interaction")]
    [SerializeField] float interactDistance = 5f;
    [SerializeField] LayerMask truckLayer;
    [SerializeField, Range(0, 1f)] float truckFollowStrength = 0.5f;
    [SerializeField] float passengerMoveSpeed = 2f; // NOUVEAU: vitesse de déplacement dans le camion
    
    private TruckInteraction truckInteraction;
    
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
    public GameObject textGoOUTCamion;
    public GameObject textReload;
    public LayerMask maskCameraPlayer;

    private bool canReload = false;
    ShooterComponent shooter;
    
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
    [SerializeField] private bool isOnLadder = false;
    
    [Header("Map")]
    [SerializeField] private GameObject map;
    private KeyCode mapKey = KeyCode.Semicolon;
    public bool isMapActive = false;

    private Quaternion freezeSaveRotation = Quaternion.identity;

    public Skins skinManager;

    [HideInInspector]public Animator animator;
    private NetworkAnimator networkAnimator;
    
    public void SetVisibleGun()
    {
        gunOther.SetActive(!hasSomethingInHand && !isMapActive && !isDriver);
        gunOwner.SetActive(!hasSomethingInHand && !isMapActive && !isDriver);
    }
    
    public override void OnNetworkSpawn()
    {
        currentSkinId.OnValueChanged += OnSkinChanged;

        if(IsOwner)
        {
            SubmitSkinServerRpc(AutoJoinedLobby.Instance.LocalPlayerSkin);
        }

        skinManager.SetSkin(currentSkinId.Value);
        animator = skinManager.GetAnimator(currentSkinId.Value);
        networkAnimator = animator.gameObject.GetComponent<NetworkAnimator>();
        
        if (!IsOwner)
        {
            ConfigureOtherPlayerLayers(currentSkinId.Value);
            
            myCamera.gameObject.SetActive(false);
            
            gunOwner.gameObject.layer = LayerMask.NameToLayer("Other");
            map.gameObject.layer = LayerMask.NameToLayer("Other");
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
        map.gameObject.layer = LayerMask.NameToLayer("Owner");
        
        myCamera.cullingMask = maskCameraPlayer;
        startPos = cameraTransform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        truckInteraction = TruckController.instance.GetComponent<TruckInteraction>();

        yaw = transform.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;
        
        speed = moveSpeed;
        
        shooter = gameObject.GetComponent<ShooterComponent>();

        capsuleCollider = GetComponent<CapsuleCollider>();
        
        cameraShake = MyCamera().GetComponent<CameraShake>();
    }

    private void ConfigureOtherPlayerLayers(int skinId)
    {
        GameObject[] listSkins = skinManager.GetSkinnedMeshRenderers(skinId);
        
        if (listSkins == null || listSkins.Length < 2)
        {
            Debug.LogWarning($"Impossible de récupérer les meshes pour le skin {skinId}");
            return;
        }
        
        int ragdollLayer = LayerMask.NameToLayer("Default");
        SetLayerRecursively(listSkins[0].gameObject, ragdollLayer);
        SetLayerRecursively(listSkins[1].gameObject, ragdollLayer);
    }

    public override void OnNetworkDespawn()
    {
        currentSkinId.OnValueChanged -= OnSkinChanged;
    }

    private void OnEnable()
    {
        TruckController.instance.AddInParent(this);
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
    
    public CharacterController controller;
    public bool canEnterInTruck = false;
    
    void Update()
    {
        if (!IsOwner) return;
        
        float isMoving = controller.isGrounded ? controller.velocity.magnitude : 0;
        
        animator.SetFloat("Speed", isMoving);
        
        textGoInCamion.SetActive(canEnterInTruck);
        textGoOUTCamion.SetActive(isDriver || isPassenger); // MODIFIÉ: afficher aussi pour les passagers
        textReload.SetActive(canReload);

        isInTruck = transform.parent == TruckController.instance.transform;
        
        SetVisibleGun();
        
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
                truckInteraction.TryExitTruck(this);
            }
            else if (isInTruck && isPassenger)
            {
                truckInteraction.TryExitTruck(this);
            }
            else if (isInTruck && !isDriver && canEnterInTruck)
            {
                truckInteraction.TryEnterTruck(this);
            }
            else if (!isInTruck && canEnterInTruck)
            {
                canEnterInTruck = false;
                truckInteraction.TryEnterTruck(this);
            }
        }

        if (TruckController.instance.isFallen.Value)
        {
            if (Input.GetKeyDown(resetTruckKey))
            {
                TruckController.instance.AddValueToReset();
            }
        }

        if (Input.GetKeyDown(mapKey) && !isDriver)
        {
            if (isMapActive)
            {
                isMapActive = false;
            }else
            {
                isMapActive = true;
            }
        }
        
        map.SetActive(isMapActive && !hasSomethingInHand && !isDriver);
        
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
                        TruckController.instance.ToggleFrontLightsServerRpc();
                    }
                }
            }
            
            HandleCameraInput();
            return;
        }
        
        if (isPassenger && isInTruck && !isDriver)
        {
            HandleCameraInput();
        
            if (isOnLadder)
            {
                HandlePassengerLadder();
            }
            else
            {
                HandlePassengerMovement();
            }
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
        GetComponent<FPSControllerMulti>().animator.SetBool("Sit", true);
        isSitting = true;
        canSit = false;
        sittingPos = sitPos;
    }

    public void StandUp()
    {
        GetComponent<FPSControllerMulti>().animator.SetBool("Sit", false);
        sittingPos = null;
        isSitting = false;
    }

    [SerializeField] private bool isFreeze;
    public bool IsFreeze => isFreeze;
    private Vector3 freezeCameraPos;
    
    public void StartFreeze()
    {
        cameraTransform.GetComponent<Animator>().enabled = false;
        
        freezeSaveRotation = cameraTransform.rotation;
        isFreeze = true;
    }

    public void StopFreeze()
    {
        cameraTransform.GetComponent<Animator>().enabled = true;
        
        isFreeze = false;
    }
    
    void HandleCameraInput()
    {
        if (isFreeze) return;
        
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensibility;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensibility;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, verticalLimit.x, verticalLimit.y);
    }

    private void HandleHeadbob()
    {
        if (isOnLadder || isDriver || isPassenger) return; // MODIFIÉ: pas de headbob pour les passagers non plus
        
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
            
            // Vérifier saut depuis l'échelle AVANT de traiter le mouvement
            if (isOnLadder && Input.GetKeyDown(KeyCode.Space))
            {
                isOnLadder = false;
                verticalVelocity = jumpForce * 0.5f;
            }
            
            if (isOnLadder && !controller.isGrounded)
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
                    {
                        animator.SetTrigger("Jump");
                        verticalVelocity = jumpForce;
                    }
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
    
    private CameraShake cameraShake;
    
    void LateUpdate()
    {
        if (!IsOwner) return;
        if (cameraShake.shaking) return;
        if(Time.timeScale == 0) return;
        
        if(!isFreeze)
        {
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            
            cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0);

            cameraTransform.position = Vector3.Lerp(cameraTransform.position, cameraTarget.position,
                Time.deltaTime * cameraSmoothFollow);

            if (!isInTruck || !isDriver)
                HandleHeadbob();
        }
        else
        {
            if (freezeSaveRotation != Quaternion.identity && freezeSaveRotation.w != 0)
            {
                cameraTransform.rotation = Quaternion.Lerp(
                    cameraTransform.rotation, 
                    freezeSaveRotation, 
                    Time.deltaTime * cameraSmoothFollow
                ); 
            }
        }
    }
    
    void HandlePassengerLadder()
    {
        if (!isPassenger || !isInTruck) return;

        float vertInput = Input.GetAxisRaw("Vertical");
        float horizInput = Input.GetAxisRaw("Horizontal");

        // Mouvement vertical sur l'échelle
        Vector3 climb = new Vector3(horizInput * 0.3f, vertInput, 0f); // Petit mouvement horizontal autorisé
        Vector3 localClimb = transform.parent.InverseTransformDirection(
            transform.TransformDirection(climb)
        );
        
        transform.localPosition += localClimb * ladderClimbSpeed * Time.deltaTime;

        // Vérifier les limites du camion
        if (TruckController.instance != null)
        {
            Vector3 localPos = transform.localPosition;
            Vector3 boundsCenter = TruckController.instance.boundsCenter;
            Vector3 boundsSize = TruckController.instance.boundsSize;
            Vector3 halfSize = boundsSize * 0.5f;
            
            localPos.x = Mathf.Clamp(localPos.x, boundsCenter.x - halfSize.x, boundsCenter.x + halfSize.x);
            localPos.y = Mathf.Clamp(localPos.y, boundsCenter.y - halfSize.y, boundsCenter.y + halfSize.y);
            localPos.z = Mathf.Clamp(localPos.z, boundsCenter.z - halfSize.z, boundsCenter.z + halfSize.z);
            
            transform.localPosition = localPos;
        }

        // Sortir de l'échelle avec Espace
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isOnLadder = false;
        }
    }

    // ✅ MODIFIÉ: Fonction de mouvement passager (sans échelle)
    void HandlePassengerMovement()
    {
        if (isFreeze) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Mouvement local dans le camion
        Vector3 localMove = new Vector3(horizontalInput, 0, verticalInput).normalized;
        Vector3 worldMove = transform.TransformDirection(localMove) * passengerMoveSpeed * Time.deltaTime;
        
        // Déplacer en position locale pour rester attaché au camion
        transform.localPosition += transform.parent.InverseTransformDirection(worldMove);
        
        // Vérifier les limites du camion
        if (TruckController.instance != null)
        {
            Vector3 localPos = transform.localPosition;
            Vector3 boundsCenter = TruckController.instance.boundsCenter;
            Vector3 boundsSize = TruckController.instance.boundsSize;
            Vector3 halfSize = boundsSize * 0.5f;
            
            localPos.x = Mathf.Clamp(localPos.x, boundsCenter.x - halfSize.x, boundsCenter.x + halfSize.x);
            localPos.y = Mathf.Clamp(localPos.y, boundsCenter.y - halfSize.y, boundsCenter.y + halfSize.y);
            localPos.z = Mathf.Clamp(localPos.z, boundsCenter.z - halfSize.z, boundsCenter.z + halfSize.z);
            
            transform.localPosition = localPos;
        }
    }


    public void EnterTruck(bool asDriver, Vector3 spawnPosition) 
    {
        capsuleCollider.enabled = false;
        
        truckRb = TruckController.instance.GetComponent<Rigidbody>();
        
        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null) {
            netTransform.InLocalSpace = true;
        }
        
        animator.SetBool("Sit", asDriver); // MODIFIÉ: seulement le conducteur est assis
        
        isPassenger = !asDriver; // NOUVEAU: marquer comme passager si pas conducteur
        
        controller.enabled = false;
        
        SetVisibleGun();
        
        if (IsOwner) {
            Transform targetSeat = asDriver ? TruckController.instance.driverPos : TruckController.instance.spawnPassager;
            transform.localPosition = targetSeat.localPosition;
            transform.localRotation = Quaternion.identity;
        }
    }
    
    public void ExitTruck(Vector3 exitPosition)
    {
        print("ExitTruck");
        
        capsuleCollider.enabled = true;
        controller.enabled = true;
        
        animator.SetBool("Sit", false);
        
        isPassenger = false; // NOUVEAU: ne plus être passager
        
        SetVisibleGun();
        
        transform.position = exitPosition;
        
        truckRb = null;
    }

    
    
    [ServerRpc(RequireOwnership = false)]
    public void SetPassengerModeServerRpc(bool isPassenger, Vector3 desiredLocalPos)
    {
        SetPassengerModeClientRpc(isPassenger, desiredLocalPos);
    }

    [ClientRpc]
    private void SetPassengerModeClientRpc(bool isPassengerMode, Vector3 desiredLocalPos)
    {
        isInTruck = isPassengerMode;

        if (isPassengerMode)
        {

            var netTransform = GetComponent<NetworkTransform>();
            if (netTransform != null)
            {
                netTransform.InLocalSpace = true; 
            }

            Debug.Log("Client: Mode passager activé - CC disabled + InLocalSpace=true");
        }
        else
        {
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
        if (!IsOwner) return;
        
        if (other.transform.CompareTag("PorteConducteur") && 
            truckInteraction.hasDriver.Value == false &&
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
        if (!IsOwner) return;
        
        if (other.transform.CompareTag("PorteConducteur"))
        {
            canEnterInTruck = false;
        }
        
        if (other.CompareTag("Echelle"))
        {
            print("plus dans echelle");
            isOnLadder = false;
            verticalVelocity = 0f;
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

    #region Skins

    public NetworkVariable<int> currentSkinId = new NetworkVariable<int>(0);
    
    public Ragdoll GetRagdoll()
    {
        return skinManager.GetRagdoll(currentSkinId.Value);
    }
    
    public Animator Animator => skinManager.GetAnimator(currentSkinId.Value);

        
    [ServerRpc]
    void SubmitSkinServerRpc(int skinID)
    {
        print("Change skin : " + skinID);
        currentSkinId.Value = skinID;
    }
    
    private void OnSkinChanged(int previousValue, int newValue)
    {
        skinManager.SetSkin(newValue);
        animator = skinManager.GetAnimator(newValue);
    
        if (!IsOwner)
        {
            ConfigureOtherPlayerLayers(newValue);
        }
    
        print("NEW VALUE SKIN : " + newValue);
    }

    #endregion
}

public interface IParentable
{
    Transform Transform { get; }
    NetworkObject NetworkObject { get; }

    void OnParented(Transform parent);
    void OnUnparented();
}