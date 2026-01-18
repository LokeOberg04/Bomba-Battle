using Unity.Netcode;
using UnityEngine;

public class FPSCamera : NetworkBehaviour
{

    public Transform player;
    public float sensitivity = 5.0f;
    float verticalRotation = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner)
        {
            return;
        }
        float inputX = Input.GetAxis("Mouse X") * sensitivity;
        float inputY = Input.GetAxis("Mouse Y") * sensitivity;

        verticalRotation -= inputY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        transform.localEulerAngles = Vector3.right * verticalRotation;
        
        player.Rotate(Vector3.up * inputX);
    }
}
