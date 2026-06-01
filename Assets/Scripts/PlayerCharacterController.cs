// Simple first-person style player controller used in non-fae scenes.
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerCharacterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6.5f;
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -24f;
    [SerializeField] private float groundedForce = -2f;
    [SerializeField] private float turnSmoothTime = 0.08f;

    [Header("Look")]
    [SerializeField] private bool rotateWithMouse = true;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minCameraPitch = -45f;
    [SerializeField] private float maxCameraPitch = 70f;
    [SerializeField] private bool lockCursorOnStart = true;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string moveXParameter = "MoveX";
    [SerializeField] private string moveYParameter = "MoveY";
    [SerializeField] private string movingParameter = "IsMoving";
    [SerializeField] private string runningParameter = "IsRunning";
    [SerializeField] private string groundedParameter = "Grounded";
    [SerializeField] private string verticalVelocityParameter = "VerticalVelocity";
    [SerializeField] private string jumpTriggerParameter = "Jump";
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string jumpStateName = "Jump";
    [SerializeField] private KeyCode crouchKey = KeyCode.V;
    [SerializeField] private string crouchTriggerParameter = "Crouch";
    [SerializeField] private string crouchStateName = "Crouch";
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float maxCrouchSeconds = 3f;
    [SerializeField] private bool toggleCrouch = true;
    [SerializeField] private float actionCrossFadeDuration = 0.08f;

    private readonly HashSet<int> animatorParameters = new HashSet<int>();
    private CharacterController controller;
    private Vector2 moveInput;
    private float verticalVelocity;
    private float yaw;
    private float pitch;
    private float turnVelocity;
    private bool jumpRequested;
    private bool isRunning;
    private bool isCrouching;
    private float crouchEndsAt = -1f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        CacheAnimatorParameters();
        yaw = transform.eulerAngles.y;

        if (playerCamera != null)
        {
            pitch = NormalizeAngle(playerCamera.transform.localEulerAngles.x);
        }
    }

    private void Start()
    {
        if (!lockCursorOnStart)
        {
            return;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        ReadInput();
        UpdateLook();
        UpdateMovement();
        UpdateAnimator();
    }

    private void ReadInput()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
        }

        if (toggleCrouch)
        {
            if (Input.GetKeyDown(crouchKey))
            {
                isCrouching = true;
                crouchEndsAt = Time.time + maxCrouchSeconds;
                PlayAction(crouchTriggerParameter, crouchStateName);
            }
        }
        else
        {
            isCrouching = Input.GetKey(crouchKey);
            crouchEndsAt = isCrouching ? Time.time + maxCrouchSeconds : -1f;
        }

        if (isCrouching && Time.time >= crouchEndsAt)
        {
            isCrouching = false;
            PlayAction(crouchTriggerParameter, idleStateName);
        }

        isRunning = !isCrouching && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
    }

    private void UpdateLook()
    {
        if (!rotateWithMouse)
        {
            return;
        }

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (playerCamera == null)
        {
            return;
        }

        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minCameraPitch, maxCameraPitch);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void UpdateMovement()
    {
        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedForce;
        }

        if (jumpRequested && grounded)
        {
            isCrouching = false;
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            PlayAction(jumpTriggerParameter, jumpStateName);
        }

        jumpRequested = false;

        Vector3 desiredDirection = GetMoveDirection();
        float speed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);
        Vector3 horizontalVelocity = desiredDirection * speed;

        verticalVelocity += gravity * Time.deltaTime;
        horizontalVelocity.y = verticalVelocity;

        controller.Move(horizontalVelocity * Time.deltaTime);
    }

    private Vector3 GetMoveDirection()
    {
        if (moveInput.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        if (playerCamera != null)
        {
            forward = playerCamera.transform.forward;
            right = playerCamera.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
        }

        Vector3 direction = (forward * moveInput.y + right * moveInput.x).normalized;

        if (!rotateWithMouse && visualRoot != null)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(visualRoot.eulerAngles.y, targetAngle, ref turnVelocity, turnSmoothTime);
            visualRoot.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
        }

        return direction;
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        float normalizedSpeed = moveInput.magnitude * (isRunning ? 1f : 0.5f);
        SetFloat(speedParameter, normalizedSpeed);
        SetFloat(moveXParameter, moveInput.x);
        SetFloat(moveYParameter, moveInput.y);
        SetFloat(verticalVelocityParameter, verticalVelocity);
        SetBool(movingParameter, moveInput.sqrMagnitude > 0.001f);
        SetBool(runningParameter, isRunning && moveInput.sqrMagnitude > 0.001f);
        SetBool(groundedParameter, controller.isGrounded);

        if (isCrouching)
        {
            PlayAction(crouchTriggerParameter, crouchStateName);
        }
    }

    private void CacheAnimatorParameters()
    {
        animatorParameters.Clear();

        if (animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            animatorParameters.Add(parameter.nameHash);
        }
    }

    private bool HasParameter(string parameterName)
    {
        return !string.IsNullOrEmpty(parameterName) &&
               animatorParameters.Contains(Animator.StringToHash(parameterName));
    }

    private void SetFloat(string parameterName, float value)
    {
        if (HasParameter(parameterName))
        {
            animator.SetFloat(parameterName, value, 0.1f, Time.deltaTime);
        }
    }

    private void SetBool(string parameterName, bool value)
    {
        if (HasParameter(parameterName))
        {
            animator.SetBool(parameterName, value);
        }
    }

    private void SetTrigger(string parameterName)
    {
        if (HasParameter(parameterName))
        {
            animator.SetTrigger(parameterName);
        }
    }

    private void PlayAction(string triggerParameter, string stateName)
    {
        if (animator == null)
        {
            return;
        }

        if (HasParameter(triggerParameter))
        {
            animator.SetTrigger(triggerParameter);
        }

        if (HasState(stateName))
        {
            animator.CrossFadeInFixedTime(stateName, actionCrossFadeDuration);
        }
    }

    private bool HasState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        return animator.HasState(0, Animator.StringToHash(stateName)) ||
            animator.HasState(0, Animator.StringToHash("Base Layer." + stateName));
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}
