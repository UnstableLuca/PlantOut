using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform greenhouseCenter;
    [SerializeField] private Transform mainCamera;

    [Header("Configuración de la Cámara")]
    [SerializeField] private float cameraRadius = 10.0f;
    [SerializeField] private float cameraHeightOffset = 5.0f;

    [Header("Movimiento del Jugador")]
    [SerializeField] private float maxSpeed = 8.0f;
    [SerializeField] private float acceleration = 25.0f;
    [SerializeField] private float minPlayerRadius = 12.5f;
    [SerializeField] private float maxPlayerRadius = 24.0f;

    [Header("Físicas y gravedad")]
    [SerializeField] private float gravity = -9.8f;
    private Vector3 verticalVelocity;

    private CharacterController controller;
    private InputActions inputActions;
    private Vector2 moveInput;

    private float currentAngle = 0.0f;
    private float currentPlayerRadius = 16.0f;

    private float currentTangentialVelocity = 0.0f;
    private float currentRadialVelocity = 0.0f;

    private bool isExternalControl = false;

    private bool canMove = true;

    #region Inputs
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
    #endregion

    #region Start
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new InputActions();
    }

    private void Start()
    {
        if (greenhouseCenter == null || mainCamera == null) return;

        Vector3 offset = transform.position - greenhouseCenter.position;
        currentPlayerRadius = Mathf.Clamp(new Vector2(offset.x, offset.z).magnitude, minPlayerRadius, maxPlayerRadius);
        currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }
    #endregion

    #region Update
    private void Update()
    {
        if (greenhouseCenter == null || mainCamera == null) return;

        // -- MOVIMIENTO --
        if (canMove && !isExternalControl)
        {
            Vector2 inputDir = canMove ? moveInput : Vector2.zero;

            if (inputDir.magnitude > 1f) inputDir.Normalize();

            float targetTangentialVel = inputDir.x * maxSpeed;
            float targetRadialVel = -inputDir.y * maxSpeed;

            currentTangentialVelocity = Mathf.MoveTowards(currentTangentialVelocity, targetTangentialVel, acceleration * Time.deltaTime);
            currentRadialVelocity = Mathf.MoveTowards(currentRadialVelocity, targetRadialVel, acceleration * Time.deltaTime);

            float currentAngularSpeedDeg = (currentTangentialVelocity / currentPlayerRadius) * Mathf.Rad2Deg;

            currentAngle -= currentAngularSpeedDeg * Time.deltaTime;
            currentPlayerRadius -= currentRadialVelocity * Time.deltaTime;
            currentPlayerRadius = Mathf.Clamp(currentPlayerRadius, minPlayerRadius, maxPlayerRadius);

            float playerRad = currentAngle * Mathf.Deg2Rad;

            Vector3 targetPosition = new Vector3(
                greenhouseCenter.position.x + Mathf.Cos(playerRad) * currentPlayerRadius,
                transform.position.y,
                greenhouseCenter.position.z + Mathf.Sin(playerRad) * currentPlayerRadius
            );

            Vector3 horizontalMove = targetPosition - transform.position;

            if (controller.isGrounded && verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2.0f;
            }
            verticalVelocity.y += gravity * Time.deltaTime;

            Vector3 finalMotion = horizontalMove + (verticalVelocity * Time.deltaTime);
            controller.Move(finalMotion);

            RecalculatePolarFromPosition();
        }

        // -- CAMARA --
        if (canMove && !isExternalControl)
        {
            Vector3 playerOffset = transform.position - greenhouseCenter.position;
            float realPlayerAngleRad = Mathf.Atan2(playerOffset.z, playerOffset.x);

            float cameraRad = realPlayerAngleRad + Mathf.PI;

            Vector3 cameraPos = new Vector3(greenhouseCenter.position.x + Mathf.Cos(cameraRad) * cameraRadius, transform.position.y + cameraHeightOffset, greenhouseCenter.position.z + Mathf.Sin(cameraRad) * cameraRadius);

            mainCamera.position = cameraPos;

            Vector3 cameraTarget = new Vector3(greenhouseCenter.position.x, transform.position.y + 1.0f, greenhouseCenter.position.z);
            mainCamera.LookAt(cameraTarget);

            Vector3 lookDir = new Vector3(playerOffset.x, 0f, playerOffset.z).normalized;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
        }

    }
    #endregion

    private void RecalculatePolarFromPosition()
    {
        Vector3 offset = transform.position - greenhouseCenter.position;
        currentPlayerRadius = Mathf.Clamp(new Vector2(offset.x, offset.z).magnitude, minPlayerRadius, maxPlayerRadius);
        currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }

    public void SetExternalPosition(Vector3 newPos)
    {
        controller.enabled = false;
        transform.position = newPos;
        controller.enabled = true;

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

    public void SetControlActive(bool active)
    {
        canMove = active;

        if (!active)
        {
            moveInput = Vector2.zero;
            currentTangentialVelocity = 0f;
            currentRadialVelocity = 0f;
        }
    }
}