using UnityEngine;

public class ListenEventDoor : Interactable
{
    Animator animator;
    private bool isInteracting;
    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    protected override void Interact(Transform playerTransform, bool enableCallbacks = true)
    {
        base.Interact(playerTransform, enableCallbacks);
        Debug.Log("Interact");
        
        HitInteract();
        
    }
    
    private void HitInteract()
    {
        if (isInteracting)
        {
            animator.SetBool("Open", false);
            isInteracting = false;
        }
        else
        {
            animator.SetBool("Open", true);
            isInteracting = true;
        }
    }
}