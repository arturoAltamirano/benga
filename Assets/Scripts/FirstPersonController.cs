using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    public Transform lookRoot;

    [Header("Settings")]
    public float moveSpeed = 8f;
    public float mouseSensitivity = 2f;

    [Header("Rigidbody")]
    private Rigidbody rb;
    private float pitch = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    /*
    Look movement every frame, mouse to look around
    */
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        lookRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void FixedUpdate()
    /*
    Fixed movement, how you move through the world and up and down
    */
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

    
        float y = 0f;
        if (Input.GetKey(KeyCode.UpArrow))   y += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) y -= 1f;

        Vector3 move =
            (transform.right * x +
            transform.forward * z +
            Vector3.up * y).normalized;

        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);
    }
}