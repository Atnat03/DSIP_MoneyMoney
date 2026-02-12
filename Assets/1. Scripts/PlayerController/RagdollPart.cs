using UnityEngine;

public class RagdollPart : MonoBehaviour, IInteractible
{


    public string InteractionName { get; set; }
    public Outline[] Outline { get; set; }
}
