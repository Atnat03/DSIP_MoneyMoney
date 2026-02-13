using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class GodModeController : MonoBehaviour
{
    #region Fields



    [SerializeField] private UnityEvent OnEnterGodMode;
    [SerializeField] private UnityEvent OnExitGodMode;

    [SerializeField] private float speed = 1f;
    [SerializeField] private float sprintModifier = 3f;
    [SerializeField, Range(0.1f, 10)] float mouseSensibility = 2.5f;
    [SerializeField] Vector2 verticalLimit = new Vector2(-90, 90);

    public bool IsEnabled;

    private Vector3 _previousPosition;
    private Quaternion _previousRotation;
    private Transform _previousParent;

    Camera cam;
    public Transform camTransform;
    float yaw;
    float pitch;
    float horizontalInput;
    private float verticalInput;

    #endregion
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetGodMode(IsEnabled);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetGodMode(!IsEnabled);
        if (IsEnabled)
        {
            HandeInputs();
            CameraStuff();

        }
    }

    private void CameraStuff()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensibility;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensibility;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, verticalLimit.x, verticalLimit.y);
        camTransform.rotation = Quaternion.Euler(pitch, yaw, 0);
    }

    public void SetGodMode(bool enabled)
    {
        if (enabled)
            EnterGodMode();
        else
            ExitGodMode();

        HandeInputs();
    }

    private void HandeInputs()
    {
        if (!EnforceReferences())
            return;

        float sprintMultiplier = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            sprintMultiplier = sprintModifier;

        if (Input.GetKey(KeyCode.W))
            camTransform.position = camTransform.position + camTransform.forward * speed * sprintMultiplier;
        if (Input.GetKey(KeyCode.S))
            camTransform.position = camTransform.position - camTransform.forward * speed * sprintMultiplier;
        if (Input.GetKey(KeyCode.A))
            camTransform.position = camTransform.position - camTransform.right * speed * sprintMultiplier;
        if (Input.GetKey(KeyCode.D))
            camTransform.position = camTransform.position + camTransform.right * speed * sprintMultiplier;
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space))
            camTransform.position = camTransform.position + Vector3.up * speed * sprintMultiplier;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl))
            camTransform.position = camTransform.position - Vector3.up * speed * sprintMultiplier;
        if (Input.GetKey(KeyCode.Return))
        {
            TakeScreenshot();
        }
    }

    public void TakeScreenshot()
    {
        string path = Application.persistentDataPath + "/Screenshots";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        DirectoryInfo dir = new DirectoryInfo(path);
        FileInfo[] info = dir.GetFiles("*.*");
        int fileAmount = info.Length;
        ScreenCapture.CaptureScreenshot(path + "/Screenshot " + fileAmount + ".png", 2);
        Debug.Log("Screenshot taken, saved at path " + path + "/Screenshot " + fileAmount);
    }

    public void EnterGodMode()
    {
        if (!EnforceReferences()) return;

        yaw = camTransform.rotation.eulerAngles.y;
        pitch = camTransform.rotation.eulerAngles.x;


        _previousPosition = camTransform.position;
        _previousRotation = camTransform.rotation;
        _previousParent = camTransform.parent;

        camTransform.parent = null;

        IsEnabled = true;
        OnEnterGodMode.Invoke();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void ExitGodMode()
    {
        if (!EnforceReferences()) return;

        camTransform.position = _previousPosition;
        camTransform.rotation = _previousRotation;
        camTransform.SetParent(_previousParent);

        IsEnabled = false;
        OnExitGodMode.Invoke();
    }

    private bool EnforceReferences()
    {
        cam = Camera.main;
        if (cam ==  null)
            return false;
        if (camTransform == null) camTransform = cam.transform;
        if (camTransform == null) return false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        return true;
    }
}
