using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(FPSControllerMulti))]
public class DriverInputHandler : MonoBehaviour
{
    private FPSControllerMulti playerController;
    private TruckController truckController;

    private void Awake()
    {
        playerController = GetComponent<FPSControllerMulti>();
    }

    private void Update()
    {
        if (!playerController.isDriver || !playerController.isInTruck)
            return;

        if (TruckController.instance == null)
            return;

        if (truckController == null)
            truckController = TruckController.instance;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool breaking = Input.GetKey(KeyCode.Space);
        bool horn = Input.GetKeyDown(KeyCode.K);

        truckController.SendInputsServerRpc(horizontal, vertical, breaking, horn);
    }
}