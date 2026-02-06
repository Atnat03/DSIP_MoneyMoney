using System;
using Shooting;
using UnityEngine;

public class ListenEventDoor : MonoBehaviour
{
    Animator animator;
    private bool isInteracting;
   private void Start()
    {
        animator = GetComponent<Animator>();
    }

   /* public override void Interact(Transform playerTransform, bool enableCallbacks = true)
    {
        base.Interact(playerTransform, enableCallbacks);
        Debug.Log("Interact");
        
        HitInteract();
    }*/

    private void OnEnable()
    {
        Interact.OnInteract += HitInteract;
    }

    private void OnDisable()
    {
        Interact.OnInteract -= HitInteract;
    }

    private void HitInteract(GameObject obj,  GameObject player)
    {
        Debug.Log("Hit interact");
        if (obj.gameObject.GetInstanceID() == gameObject.GetInstanceID())
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
}