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
}