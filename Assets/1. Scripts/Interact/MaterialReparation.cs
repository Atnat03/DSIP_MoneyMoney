using UnityEngine;

public class MaterialReparation : MonoBehaviour, IInteractible
{
    public string InteractionName
    {
        get { return interactionName; }
        set { interactionName = value; }
    }
    
    public Outline[] Outline     
    {
        get { return outline; ; }
        set { }
    }

    public Outline[] outline;

    public string interactionName;}
