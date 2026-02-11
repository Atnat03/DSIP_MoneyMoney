using System;
using Shooting;
using UnityEngine;
using UnityEngine.Events;

public class ReloadStation : MonoBehaviour, IInteractible
{
    public string InteractionName
    {
        get { return interactionName ; }
        set { }
    }
    
    public Outline[] Outline
    {
        get { return outline; ; }
        set { }
    }

    public Outline[] outline;
    public string interactionName;

    public void OnEnable()
    {
        Interact.OnInteract += HitInteract;
    }

    private void HitInteract(GameObject obj, GameObject player)
    {
        if (obj.GetInstanceID() != gameObject.GetInstanceID()) return;


        ShooterComponent shooter = player.GetComponent<ShooterComponent>();

        if(shooter)
        {
            shooter.StartToReload();
        }
    }
}