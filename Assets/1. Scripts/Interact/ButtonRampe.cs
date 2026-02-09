using Unity.Netcode;
using UnityEngine;

public class ButtonRampe : NetworkBehaviour, IInteractible
{
    public string InteractionName
    {
        get { return isOpenRampe.Value ? interactionSortirName : interactionRentreName ; }
        set { }
    }   

    public string interactionSortirName;
    public string interactionRentreName;
    
    private NetworkVariable<bool> isOpenRampe = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public Animator rampeAnimator;

    public override void OnNetworkSpawn()
    {
        Interact.OnInteract += TouchButtonRampe;
        isOpenRampe.OnValueChanged += OnRampeStateChanged;
    }
    
    public override void OnNetworkDespawn()
    {
        Interact.OnInteract -= TouchButtonRampe;
        isOpenRampe.OnValueChanged -= OnRampeStateChanged;
    }

    public void TouchButtonRampe(GameObject obj, GameObject player)
    {
        if (obj.GetInstanceID() != gameObject.GetInstanceID()) return;
        
        if (NetworkManager.Singleton.IsServer)
        {
            ToggleRampe();
        }
        else
        {
            ToggleRampeServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleRampeServerRpc()
    {
        ToggleRampe();
    }

    private void ToggleRampe()
    {
        isOpenRampe.Value = !isOpenRampe.Value;
    }
    
    private void OnRampeStateChanged(bool previousValue, bool newValue)
    {
        rampeAnimator.SetBool("IsOpen", newValue);
    }
}
