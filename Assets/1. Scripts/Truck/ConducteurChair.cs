using System;
using Unity.Netcode;
using UnityEngine;

public class ConducteurChair : NetworkBehaviour, IInteractible
{
    public Collider col;
    TruckInteraction truckInteraction;

    public override void OnNetworkSpawn()
    {
        truckInteraction = TruckController.instance.GetComponent<TruckInteraction>();
        
        Interact.OnInteract += HitInteract;
    }

    public override void OnNetworkDespawn()
    {
        Interact.OnInteract -= HitInteract;
    }

    private void Update()
    {
        col.enabled = !truckInteraction.hasDriver.Value;
    }

    private void HitInteract(GameObject obj, GameObject player)
    {
        if (obj.GetInstanceID() != gameObject.GetInstanceID()) return;
        
        print("Enter driver");
        
        FPSControllerMulti fps = player.GetComponent<FPSControllerMulti>();
        truckInteraction.TryEnterTruck(fps);
    }
    
    public string InteractionName
    {
        get { return interactionAssoirName; }
        set { }
    }

    public string interactionAssoirName;
    
    public Outline[] Outline     
    {
        get { return outline; ; }
        set { }
    }

    public Outline[] outline;
}
