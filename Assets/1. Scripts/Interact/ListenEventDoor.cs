using System;
using UnityEngine;

public class ListenEventDoor : MonoBehaviour
{
    Animator animator;
    private bool isInteracting;
    private void Start()
    {
        animator = GetComponent<Animator>();
    }

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
        if (obj.name == gameObject.name)
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