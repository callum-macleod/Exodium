using Unity.Netcode;
using UnityEngine;

public class Rotator : NetworkBehaviour
{
    [SerializeField] Transform verticalRotator;
    private float xRotation;
    private float yRotation;
    float sensitivity = 1f;


    void Update()
    {
        if (!IsOwner) return;

        float xRot = Input.GetAxisRaw("Mouse Y");
        float yRot = Input.GetAxisRaw("Mouse X");

        // limits vertical rotations (stops when you are completely pointing up or down)
        if (Mathf.Abs(xRotation - xRot) <= 90)
            xRotation -= xRot;
        else
            xRotation = 90 * (xRotation/Mathf.Abs(xRotation));

        yRotation += yRot;

        verticalRotator.localRotation = Quaternion.Euler(xRotation * sensitivity, 0, 0);
        transform.localRotation = Quaternion.Euler(0, yRotation * sensitivity, 0);
    }
}