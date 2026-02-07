using System;
using UnityEngine;

public class Helicopter : MonoBehaviour
{
    [SerializeField] private Transform rotor;
    [SerializeField] private float rotorRotateSpeed = 5f;

    private void Update()
    {
        rotor.transform.Rotate(Vector3.up, rotorRotateSpeed * Time.deltaTime);
    }
}
