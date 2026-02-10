using System;
using UnityEngine;

public class Chair : MonoBehaviour, IInteractible
{
    public Transform sittingPos;

    private void OnEnable()
    {
        Interact.OnInteract += HitInteract;
    }

    private void OnDisable()
    {
        Interact.OnInteract -= HitInteract;
    }

    private void HitInteract(GameObject obj, GameObject player)
    {
        if (obj.GetInstanceID() != gameObject.GetInstanceID()) return;

        FPSControllerMulti fps = player.GetComponent<FPSControllerMulti>();

        if (fps.isSitting)
        {
            fps.StandUp();
            isSit = false;
        }
        else
        {
            fps.Sit(sittingPos);
            isSit = true;
        }
    }

    private bool isSit = false;

    public string InteractionName
    {
        get { return isSit ? interactionDeboutName : interactionAssoirName; }
        set { }
    }

    public string interactionAssoirName;
    public string interactionDeboutName;
    
    public Outline[] Outline     
    {
        get { return outline; ; }
        set { }
    }

    public Outline[] outline;
}
