using Unity.Netcode;
using UnityEngine;

public class ButtonCheckPiscine : NetworkBehaviour, IInteractible
{
    public string InteractionName
    {
        get { return interactionName ; }
        set { }
    }   

    public string interactionName;    
    public Outline[] Outline     
    {
        get { return outline; ; }
        set { }
    }

    public Outline[] outline;
    public PiscineDeBillet piscine;
    
    public override void OnNetworkSpawn()
    {
        Interact.OnInteract += TouchButton;
    }

    public override void OnNetworkDespawn()
    {
        Interact.OnInteract -= TouchButton;
    }
    
    private void TouchButton(GameObject obj, GameObject player)
    {
        if (obj.GetInstanceID() != gameObject.GetInstanceID()) return;

        if (NetworkManager.Singleton.IsServer)
        {
            EndTheGameClientRpc();
        }
        else
        {
            EndTheGameServerRpc();
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void EndTheGameServerRpc()
    {
        EndTheGameClientRpc();
    }
    
    [ClientRpc]
    private void EndTheGameClientRpc()
    {
        EndGame player = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<EndGame>();
        player.SetScore(piscine.numberOfSac.Value, piscine.numberOfLingot.Value);
        player.isEnd = true;
    }
}
