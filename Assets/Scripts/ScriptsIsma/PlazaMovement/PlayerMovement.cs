using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform greenhouseCenter;
    [SerializeField] private Transform mainCamera;

    [Header("Configuración de la Cámara")]
    [SerializeField] private float cameraRadius = 0.0f;
    [SerializeField] private float cameraHeightOffset = 4.0f;

    [Header("Movimiento del Jugador")]
    [SerializeField] private float maxSpeed = 8.0f;
    [SerializeField] private float acceleration = 25.0f;
    [SerializeField] private float minPlayerRadius = 12.5f;
    [SerializeField] private float maxPlayerRadius = 24.0f;

    private InputActions inputActions;
    private Vector2 moveInput;

    private float currentAngle = 0.0f;
    private float currentPlayerRadius = 16.0f;

    private float currentTangentialVelocity = 0.0f;
    private float currentRadialVelocity = 0.0f;

    private bool isExternalControl = false;

    private bool canMove = true;

    private void Awake()
    {
        inputActions = new InputActions();
    }

    private void OnEnable()
    {
        inputActions.Plaza.Enable();

        inputActions.Plaza.Move.performed += OnMovePerformed;
        inputActions.Plaza.Move.canceled += OnMoveCanceled;
    }

    private void OnDisable()
    {
        inputActions.Plaza.Move.performed -= OnMovePerformed;
        inputActions.Plaza.Move.canceled -= OnMoveCanceled;

        inputActions.Plaza.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext ct)
    {
        moveInput = ct.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ct)
    {
        moveInput = Vector2.zero;
    }

    private void Start()
    {
        if (greenhouseCenter == null || mainCamera == null) return;

        Vector3 offset = transform.position - greenhouseCenter.position;
        currentPlayerRadius = Mathf.Clamp(new Vector2(offset.x, offset.z).magnitude, minPlayerRadius, maxPlayerRadius);
        currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }

    private void Update()
    {
        if (greenhouseCenter == null || mainCamera == null) return;

        if (!isExternalControl)
        {
            Vector2 inputDir = canMove ? moveInput : Vector2.zero;

            if (inputDir.magnitude > 1f) inputDir.Normalize();

            float targetTangentialVel = inputDir.x * maxSpeed;
            float targetRadialVel = inputDir.y * maxSpeed;

            currentTangentialVelocity = Mathf.MoveTowards(currentTangentialVelocity, targetTangentialVel, acceleration * Time.deltaTime);
            currentRadialVelocity = Mathf.MoveTowards(currentRadialVelocity, targetRadialVel, acceleration * Time.deltaTime);

            float currentAngularSpeedDeg = (currentTangentialVelocity / currentPlayerRadius) * Mathf.Rad2Deg;

            currentAngle -= currentAngularSpeedDeg * Time.deltaTime;
            currentPlayerRadius += currentRadialVelocity * Time.deltaTime;
            currentPlayerRadius = Mathf.Clamp(currentPlayerRadius, minPlayerRadius, maxPlayerRadius);

            float playerRad = currentAngle * Mathf.Deg2Rad;

            Vector3 playerPos = new Vector3(greenhouseCenter.position.x + Mathf.Cos(playerRad) * currentPlayerRadius, transform.position.y, greenhouseCenter.position.z + Mathf.Sin(playerRad) * currentPlayerRadius);
            transform.position = playerPos;
        }

        float cameraRad;
        Vector3 cameraTarget;

        if (isExternalControl)
        {
            Vector3 playerOffset = transform.position - greenhouseCenter.position;
            float exactPlayerAngleRad = Mathf.Atan2(playerOffset.z, playerOffset.x);

            cameraRad = exactPlayerAngleRad + Mathf.PI;

            cameraTarget = transform.position + Vector3.up * 1.0f;
        }
        else
        {
            cameraRad = (currentAngle * Mathf.Deg2Rad) + Mathf.PI;
            cameraTarget = new Vector3(greenhouseCenter.position.x, transform.position.y + 1.0f, greenhouseCenter.position.z);
        }

        Vector3 cameraPos = new Vector3(greenhouseCenter.position.x + Mathf.Cos(cameraRad) * cameraRadius, transform.position.y + cameraHeightOffset, greenhouseCenter.position.z + Mathf.Sin(cameraRad) * cameraRadius);
        mainCamera.position = cameraPos;

        mainCamera.LookAt(cameraTarget);

        Vector3 lookDir = (transform.position - new Vector3(greenhouseCenter.position.x, transform.position.y, greenhouseCenter.position.z)).normalized;
        if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
    }

    public void SetExternalPosition(Vector3 newPos)
    {
        transform.position = newPos;

        Vector3 offset = transform.position - greenhouseCenter.position;
        currentPlayerRadius = Mathf.Clamp(new Vector2(offset.x, offset.z).magnitude, minPlayerRadius, maxPlayerRadius);
        currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }

    public void DisableMovement()
    {
        canMove = false;
        isExternalControl = true;
        moveInput = Vector2.zero;
        currentTangentialVelocity = 0f;
        currentRadialVelocity = 0f;
    }

    public void EnableMovement()
    {
        canMove = true;
        isExternalControl = false;

        Vector3 offset = transform.position - greenhouseCenter.position;
        currentPlayerRadius = Mathf.Clamp(new Vector2(offset.x, offset.z).magnitude, minPlayerRadius, maxPlayerRadius);
        currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }
}