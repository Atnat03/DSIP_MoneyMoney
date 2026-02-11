using System;
using UnityEngine;

public class ConducteurChair : MonoBehaviour, IInteractible
{
    public Collider col;
    TruckInteraction truckInteraction;

    private void OnEnable()
    {
        truckInteraction = TruckController.instance.GetComponent<TruckInteraction>();
        
        Interact.OnInteract += HitInteract;
    }

    private void OnDisable()
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
