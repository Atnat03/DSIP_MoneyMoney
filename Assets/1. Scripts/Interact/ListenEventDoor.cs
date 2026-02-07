using Unity.Netcode;
using UnityEngine;
using Shooting;
using Unity.Netcode.Components;

public class ListenEventDoor : NetworkBehaviour
{
    private Animator animator;
    private NetworkAnimator networkAnimator;
    
    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Start()
    {
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();

        isOpen.OnValueChanged += OnDoorStateChanged;
    }

    private void OnEnable()
    {
        Interact.OnInteract += HitInteract;
    }

    private void OnDisable()
    {
        Interact.OnInteract -= HitInteract;
        isOpen.OnValueChanged -= OnDoorStateChanged;
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
        isOpen.Value = !isOpen.Value;
    }

    private void OnDoorStateChanged(bool previousValue, bool newValue)
    {
        animator.SetBool("Open", newValue);
    }
}