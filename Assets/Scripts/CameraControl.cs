using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private float targetHeightOffset = 1.5f;
    private Transform character;
    private float rotationSpeed = 500.0f;
    private float distance = 4f;
    public float minDistance = 2f;
    public float maxDistance = 10f;
    public float zoomSpeed = 50f;

    void Awake()
    {
        character = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void LateUpdate()
    {
        if (character == null) return;
        CamOrbit();
        Zoom();
        Vector3 target = character.position + Vector3.up * targetHeightOffset;
        transform.position = target - (transform.forward * distance);
        FaceCharacterToCamera();
    }

    void CamOrbit()
    {
        if (Mouse.current == null) return;
        if (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed) {
            Vector2 delta = Mouse.current.delta.ReadValue();
            if (delta.sqrMagnitude < 0.0001f) return;

            float verticalInput = delta.y * rotationSpeed * Time.deltaTime;
            float horizontalInput = delta.x * rotationSpeed * Time.deltaTime;

            transform.Rotate(Vector3.right, -verticalInput, Space.Self);
            transform.Rotate(Vector3.up, horizontalInput, Space.World);
        };
    }

    void Zoom()
    {
        if (Mouse.current == null) return;
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSpeed * Time.deltaTime;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    void FaceCharacterToCamera()
    {
        if (character == null || Mouse.current == null) return;
        if (!Mouse.current.rightButton.isPressed) return;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.001f)
            character.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}
