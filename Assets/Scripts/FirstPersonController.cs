using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    public Transform lookRoot;
    public Transform cam;

    [Header("Settings")]
    public float mouseSensitivity = 2f;
    public float moveSpeed = 8f;

    private float pitch = 0f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;   // Important!

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        lookRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void FixedUpdate()
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