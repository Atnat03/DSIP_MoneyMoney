using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    [Header("Target to follow")]
    public Transform target; 

    [Header("Camera settings")]
    public Vector3 offset = new Vector3(0f, 2f, 10f);
    public float followSpeed = 10f; 
    public float rotationSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        Quaternion targetRotation = Quaternion.Euler(20f, target.eulerAngles.y, 0f);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        Vector3 desiredPosition = target.position + transform.rotation * offset;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }
}