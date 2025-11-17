using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 50f;
    [SerializeField] private float lookSpeed = 2f;
    [SerializeField] private Transform cameraTransform;

    private float rotationX = 0f;
    private float rotationY = 0f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Mouse Look
        rotationX += Input.GetAxis("Mouse X") * lookSpeed;
        rotationY -= Input.GetAxis("Mouse Y") * lookSpeed;
        rotationY = Mathf.Clamp(rotationY, -90f, 90f);

        transform.localRotation = Quaternion.Euler(0, rotationX, 0);
        cameraTransform.localRotation = Quaternion.Euler(rotationY, 0, 0);
    }

    void FixedUpdate()
    {
        // Movement
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        float moveY = 0f;
        if (Input.GetKey(KeyCode.UpArrow)) moveY += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) moveY -= 1f;

        Vector3 move = (transform.right * moveX +
                        transform.forward * moveZ +
                        Vector3.up * moveY).normalized;

        rb.MovePosition(rb.position + move * movementSpeed * Time.fixedDeltaTime);
    }
}