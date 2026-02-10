using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody mainRigidbody;
    
    [Header("Ragdoll Physics")]
    [SerializeField] private float maxVelocity = 10f;
    [SerializeField] private float maxAngularVelocity = 7f;
    [SerializeField] private float ragdollDrag = 2f;
    [SerializeField] private float ragdollAngularDrag = 2f;
    
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private CharacterJoint[] ragdollJoints;
    
    private Vector3[] initialLocalPositions;
    private Quaternion[] initialLocalRotations;
    private Transform[] ragdollTransforms;
    
    private float[] originalDrag;
    private float[] originalAngularDrag;

    private void Awake()
    {
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        ragdollJoints = GetComponentsInChildren<CharacterJoint>();
        
        SaveInitialState();
        SetRagdollState(false);
    }

    private void SaveInitialState()
    {
        int count = ragdollRigidbodies.Length;
        initialLocalPositions = new Vector3[count];
        initialLocalRotations = new Quaternion[count];
        ragdollTransforms = new Transform[count];
        originalDrag = new float[count];
        originalAngularDrag = new float[count];
        
        for (int i = 0; i < count; i++)
        {
            if (ragdollRigidbodies[i] != mainRigidbody && ragdollRigidbodies[i] != null)
            {
                ragdollTransforms[i] = ragdollRigidbodies[i].transform;
                initialLocalPositions[i] = ragdollTransforms[i].localPosition;
                initialLocalRotations[i] = ragdollTransforms[i].localRotation;
                originalDrag[i] = ragdollRigidbodies[i].linearDamping;
                originalAngularDrag[i] = ragdollRigidbodies[i].angularDamping;
            }
        }
    }

    public void EnableRagdoll(Vector3 force)
    {
        SetRagdollState(true);
        
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb != mainRigidbody && rb != null)
            {
                rb.AddForce(force, ForceMode.Impulse);
                
                rb.linearDamping = ragdollDrag;
                rb.angularDamping = ragdollAngularDrag;
                rb.maxLinearVelocity = maxVelocity;
                rb.maxAngularVelocity = maxAngularVelocity;
            }
        }
    }

    public void DisableRagdoll()
    {
        ResetPose();
        SetRagdollState(false);
    }

    private void ResetPose()
    {
        for (int i = 0; i < ragdollRigidbodies.Length; i++)
        {
            if (ragdollRigidbodies[i] != mainRigidbody && ragdollTransforms[i] != null)
            {
                ragdollRigidbodies[i].linearVelocity = Vector3.zero;
                ragdollRigidbodies[i].angularVelocity = Vector3.zero;
                
                ragdollRigidbodies[i].linearDamping = originalDrag[i];
                ragdollRigidbodies[i].angularDamping = originalAngularDrag[i];
                
                ragdollTransforms[i].localPosition = initialLocalPositions[i];
                ragdollTransforms[i].localRotation = initialLocalRotations[i];
            }
        }
    }

    private void SetRagdollState(bool isRagdoll)
    {
        if (animator != null)
            animator.enabled = !isRagdoll;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb != mainRigidbody && rb != null)
            {
                rb.isKinematic = !isRagdoll;
                rb.useGravity = isRagdoll;
            }
        }

        foreach (Collider col in ragdollColliders)
        {
            if (col != null)
                col.enabled = isRagdoll;
        }
    }

    private void FixedUpdate()
    {
        if (ragdollRigidbodies.Length == 0) return;
        if (ragdollRigidbodies[0] == null) return;
        if (ragdollRigidbodies[0].isKinematic) return;
        
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb != mainRigidbody && rb != null && !rb.isKinematic)
            {
                if (rb.linearVelocity.magnitude > maxVelocity)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
                }
                
                if (rb.angularVelocity.magnitude > maxAngularVelocity)
                {
                    rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
                }
            }
        }
    }
}