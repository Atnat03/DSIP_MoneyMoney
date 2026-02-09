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
        }
        else
        {
            fps.Sit(sittingPos);
        }
    }

    public string InteractionName
    {
        get { return interactionName; }
        set { interactionName = value; }
    }

    public string interactionName;}
