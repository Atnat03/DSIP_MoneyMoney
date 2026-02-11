using System;
using Shooting;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class GunSway : MonoBehaviour
{
    [Header("Sway Settings")] 
    [SerializeField] private float smooth;
    [SerializeField] private float swayMultiplier;
    [SerializeField] private FPSControllerMulti fps;
    
    [Header("Headbob Settings")]
    [SerializeField, Range(0, 0.1f)] float _amplitude = 0.05f;
    [SerializeField, Range(0, 30)] float frequency = 10f;
    [SerializeField] private Vector3 startPos;
    [SerializeField] private Quaternion startRot;
    [SerializeField, Range(0, 2f)] float sprintAmplitudeMultiplier = 1.5f;
    [SerializeField, Range(0, 2f)] float sprintFrequencyMultiplier = 1.5f;

    private void Update()
    {
        if(fps.IsFreeze)
            return;
        
        float mouseX = Input.GetAxisRaw("Mouse X") * swayMultiplier;
        float mouseY = Input.GetAxisRaw("Mouse Y") * swayMultiplier;
        
        Quaternion rotationX = Quaternion.AngleAxis(-mouseY, Vector3.right);
        Quaternion rotationY = Quaternion.AngleAxis(mouseX, Vector3.up);
        
        Quaternion targetRotation =  rotationX * rotationY;
        
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, smooth * Time.deltaTime);

        HandleHeadbob();
    }
    
    private void HandleHeadbob()
    {
        if (!fps.controller.isGrounded || new Vector2(fps.horizontalInput, fps.verticalInput).magnitude < 0.1f || !fps.GetComponent<ShooterComponent>().canShoot)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                startPos,
                Time.deltaTime);
            
            return;
        }
        
        // Sprint ?
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        float amp = _amplitude * (isSprinting ? sprintAmplitudeMultiplier : 1f);
        float freq = frequency * (isSprinting ? sprintFrequencyMultiplier : 1f);

        Vector3 bobOffset;
        bobOffset.y = Mathf.Sin(Time.time * freq) * amp;
        bobOffset.x = Mathf.Cos(Time.time * freq * 0.5f) * amp * 2f;
        bobOffset.z = 0f;

        Quaternion bobj;

        transform.localPosition = startPos + bobOffset;
        transform.localRotation = startRot;
    }

}
