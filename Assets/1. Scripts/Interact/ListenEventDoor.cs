using Unity.Netcode;
using UnityEngine;
using Shooting;
using Unity.Netcode.Components;
using UnityEngine.Serialization;

public class ListenEventDoor : NetworkBehaviour, IInteractible
{
    private Animator animator;
    private NetworkAnimator networkAnimator;
    
    private NetworkVariable<bool> isOpenDoor = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Start()
    {
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();

        isOpenDoor.OnValueChanged += OnDoorStateChanged;
    }

    private void OnEnable()
    {
        Interact.OnInteract += HitInteract;
    }

    private void OnDisable()
    {
        Interact.OnInteract -= HitInteract;
        isOpenDoor.OnValueChanged -= OnDoorStateChanged;
    }

    private void HitInteract(GameObject obj, GameObject player)
    {
        if (obj.GetInstanceID() != gameObject.GetInstanceID()) return;
        
        if (IsServer)
        {
            ToggleDoor();
        }
        else
        {
            ToggleDoorServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleDoorServerRpc()
    {
        ToggleDoor();
    }

    private void ToggleDoor()
    {
        isOpenDoor.Value = !isOpenDoor.Value;
    }

    private void OnDoorStateChanged(bool previousValue, bool newValue)
    {
        animator.SetBool("Open", newValue);
    }

    public string InteractionName
    {
        get { return interactionName; }
        set { interactionName = value; }
    }

    public string interactionName;
}