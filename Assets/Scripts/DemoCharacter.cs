// Controls the fae demo player and exposes a few locks used by puzzle cutscenes.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AquariusMax.Fae.demo
{
    // influenced by Unity
    [RequireComponent(typeof(CharacterController))]
    public class DemoCharacter : MonoBehaviour
    {
        public static bool LockPlayerInput;
        public static bool LockMovementInput;
        public static bool ForceWalkAnimation;
        public static bool UseLookPadInput;
        public static Vector2 LookPadInput;
        public static bool TutorialActive;
        public static bool TutorialAllowLook;
        public static bool TutorialAllowMove;
        public static bool TutorialAllowJump;
        public static bool TutorialAllowCrouch;
        public static bool TutorialAllowRun;
        public static bool TutorialRunObserved;

        [SerializeField]
        Camera cam;

        [SerializeField]
        float gravityModifier = 2f;
        [SerializeField]
        float walkSpeed = 5f;
        [SerializeField]
        float runSpeed = 10f;
        [SerializeField]
        float jumpSpeed = 10f;
        [SerializeField]
        float landingForce = 10f;

        [SerializeField]
        float mouseXSensitivity = 1.25f;
        [SerializeField]
        float mouseYSensitivity = 1.25f;

        [SerializeField]
        Animator animator;

        [SerializeField]
        string speedParameter = "Speed";
        [SerializeField]
        string movingParameter = "IsMoving";
        [SerializeField]
        string runningParameter = "IsRunning";
        [SerializeField]
        string groundedParameter = "Grounded";
        [SerializeField]
        string jumpTriggerParameter = "Jump";
        [SerializeField]
        string idleStateName = "Idle";
        [SerializeField]
        string walkStateName = "Walk";
        [SerializeField]
        string runStateName = "Run";
        [SerializeField]
        string jumpStateName = "Jump";
        [SerializeField]
        KeyCode crouchKey = KeyCode.V;
        [SerializeField]
        string crouchStateName = "Crouch";
        [SerializeField]
        float crouchSpeed = 2.5f;
        [SerializeField]
        float crouchHeightMultiplier = 0.55f;
        [SerializeField]
        float crouchRadiusMultiplier = 0.8f;
        [SerializeField]
        float maxCrouchSeconds = 3f;
        [SerializeField]
        bool toggleCrouch = true;
        [SerializeField]
        bool blockSolidObstacles = false;
        [SerializeField]
        bool usePreciseBodyCollision = true;
        [SerializeField]
        float bodyProbeRadius = 0.12f;
        [SerializeField]
        bool fitControllerToVisibleCharacter = true;
        [SerializeField]
        float controllerFitPadding = 0.08f;
        [SerializeField]
        float groundedCheckDistance = 0.42f;

        CharacterController charControl;

        Quaternion characterTargetRot;
        Quaternion cameraTargetRot;
        HashSet<int> animatorParameters = new HashSet<int>();

       // bool isGrounded = true;
        bool isWalking = true;
        Vector2 moveInput = Vector2.zero;
        Vector3 move = Vector3.zero;
        bool jumpPressed = false;
        bool isJumping = false;
        bool isCrouching = false;
        float crouchEndsAt = -1f;

        CollisionFlags collisionFlags;
        int currentAnimationStateHash;
        float standingControllerHeight;
        float standingControllerRadius;
        Vector3 standingControllerCenter;

        void Start()
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            charControl = GetComponent<CharacterController>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (fitControllerToVisibleCharacter)
            {
                FitControllerToVisibleCharacter();
            }

            standingControllerHeight = charControl.height;
            standingControllerRadius = charControl.radius;
            standingControllerCenter = charControl.center;
            CacheAnimatorParameters();

            characterTargetRot = transform.localRotation;
            cameraTargetRot = cam.transform.localRotation;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void FitControllerToVisibleCharacter()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            bool hasBounds = false;
            Bounds worldBounds = new Bounds(transform.position, Vector3.zero);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.transform.GetComponentInParent<Camera>() != null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    worldBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                return;
            }

            Vector3 localMin = transform.InverseTransformPoint(worldBounds.min);
            Vector3 localMax = transform.InverseTransformPoint(worldBounds.max);
            float bottom = Mathf.Min(localMin.y, localMax.y);
            float top = Mathf.Max(localMin.y, localMax.y);
            float height = Mathf.Max(0.7f, top - bottom + controllerFitPadding * 2f);
            float centerY = bottom + height * 0.5f - controllerFitPadding;

            Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
            float radius = Mathf.Max(worldBounds.extents.x, worldBounds.extents.z) + controllerFitPadding;
            radius = Mathf.Clamp(radius, 0.25f, 0.85f);

            charControl.height = height;
            charControl.center = new Vector3(localCenter.x, centerY, localCenter.z);
            charControl.radius = Mathf.Min(radius, height * 0.45f);
        }

        void GetMoveInput(out float speed)
        {
            if (LockPlayerInput || LockMovementInput || (TutorialActive && !TutorialAllowMove))
            {
                moveInput = Vector2.zero;
                speed = 0f;
                return;
            }

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            moveInput = new Vector2(horizontal, vertical);
            // normalize input if it exceeds 1 in combined length:
            if (moveInput.sqrMagnitude > 1)
            {
                moveInput.Normalize();
            }

            isWalking = isCrouching ||
                !Input.GetKey(KeyCode.LeftShift) ||
                (TutorialActive && !TutorialAllowRun);
            if (TutorialActive && !isWalking && moveInput.sqrMagnitude > 0.001f)
            {
                TutorialRunObserved = true;
            }

            speed = isWalking ? walkSpeed : runSpeed;
        }

        void CameraLook()
        {
            if (LockPlayerInput || (TutorialActive && !TutorialAllowLook))
            {
                return;
            }

            float mouseX = Input.GetAxis("Mouse X") * mouseXSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseYSensitivity;

            characterTargetRot *= Quaternion.Euler(0f, mouseX, 0f);
            cameraTargetRot *= Quaternion.Euler(-mouseY, 0f, 0f);

            cameraTargetRot = ClampRotationAroundXAxis(cameraTargetRot);

            transform.localRotation = characterTargetRot;
            cam.transform.localRotation = cameraTargetRot;
        }

        void Update()
        {
            CameraLook();

            if (LockPlayerInput || LockMovementInput)
            {
                jumpPressed = false;
                return;
            }

            jumpPressed = (!TutorialActive || TutorialAllowJump) && Input.GetKeyDown(KeyCode.Space);
            if (jumpPressed && IsGrounded())
            {
                if (isCrouching && CanStandUp())
                {
                    isCrouching = false;
                }

                move.y = jumpSpeed;
                jumpPressed = false;
                isJumping = true;
                SetTrigger(jumpTriggerParameter);
                PlayAnimationState(jumpStateName, 0.05f);
            }

            if (!TutorialActive || TutorialAllowCrouch)
            {
                if (toggleCrouch)
                {
                    if (Input.GetKeyDown(crouchKey))
                    {
                        isCrouching = true;
                        crouchEndsAt = Time.time + maxCrouchSeconds;
                        currentAnimationStateHash = 0;
                        PlayAnimationState(crouchStateName, 0.05f);
                    }
                }
                else
                {
                    isCrouching = Input.GetKey(crouchKey);
                    crouchEndsAt = isCrouching ? Time.time + maxCrouchSeconds : -1f;
                }
            }

            if (isCrouching && Time.time >= crouchEndsAt && CanStandUp())
            {
                isCrouching = false;
                currentAnimationStateHash = 0;
                PlayAnimationState(idleStateName, 0.05f);
            }
        }

        private void FixedUpdate()
        {
            ApplyCrouchControllerSize();

            if (LockPlayerInput || LockMovementInput)
            {
                moveInput = Vector2.zero;
                move = Vector3.zero;
                UpdateAnimator();
                return;
            }

            float speed;
            GetMoveInput(out speed);
            if (isCrouching)
            {
                speed = crouchSpeed;
            }

            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * moveInput.y + transform.right * moveInput.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, charControl.radius, Vector3.down, out hitInfo,
                               charControl.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            move.x = desiredMove.x * speed;
            move.z = desiredMove.z * speed;

            bool grounded = IsGrounded();

            if (grounded && !isJumping)
            {
                move.y = -landingForce;
            }
            else
            {
                move += Physics.gravity * gravityModifier * Time.fixedDeltaTime;
            }

            if (grounded && move.y <= 0f)
            {
                isJumping = false;
            }

            Vector3 frameMove = move * Time.fixedDeltaTime;
            if (blockSolidObstacles && IsBlockedBySolidObstacle(frameMove))
            {
                frameMove.x = 0f;
                frameMove.z = 0f;
                move.x = 0f;
                move.z = 0f;
            }

            collisionFlags = charControl.Move(frameMove);
            UpdateAnimator();
        }

        private void ApplyCrouchControllerSize()
        {
            if (charControl == null || standingControllerHeight <= 0f)
            {
                return;
            }

            float targetHeight = isCrouching
                ? Mathf.Max(standingControllerRadius * crouchRadiusMultiplier * 2f, standingControllerHeight * crouchHeightMultiplier)
                : standingControllerHeight;
            Vector3 targetCenter = standingControllerCenter;
            targetCenter.y -= (standingControllerHeight - targetHeight) * 0.5f;

            charControl.radius = isCrouching ? standingControllerRadius * crouchRadiusMultiplier : standingControllerRadius;
            charControl.height = targetHeight;
            charControl.center = targetCenter;
        }

        private bool CanStandUp()
        {
            if (charControl == null)
            {
                return true;
            }

            float currentHeight = charControl.height * Mathf.Abs(transform.lossyScale.y);
            Vector3 currentCenter = transform.TransformPoint(charControl.center);
            Vector3 standingCenter = transform.TransformPoint(standingControllerCenter);
            float radius = standingControllerRadius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z)) * 0.95f;
            float standingHeight = standingControllerHeight * Mathf.Abs(transform.lossyScale.y);
            float currentTop = Mathf.Max(0f, currentHeight * 0.5f - radius);
            float standingTop = Mathf.Max(0f, standingHeight * 0.5f - radius);
            Vector3 bottom = currentCenter + transform.up * (currentTop + 0.03f);
            Vector3 top = standingCenter + transform.up * standingTop;

            if (Vector3.Dot(top - bottom, transform.up) <= 0f)
            {
                return true;
            }

            Collider[] hits = Physics.OverlapCapsule(bottom, top, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider hit in hits)
            {
                if (hit != null && hit.transform.root != transform.root)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsGrounded()
        {
            Vector3 center = transform.TransformPoint(charControl.center);
            float radius = charControl.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
            float height = Mathf.Max(charControl.height * Mathf.Abs(transform.lossyScale.y), radius * 2f);
            Vector3 footCenter = center - transform.up * (height * 0.5f) + transform.up * 0.16f;

            RaycastHit[] hits = Physics.RaycastAll(
                footCenter,
                Vector3.down,
                groundedCheckDistance,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider != null &&
                    hit.collider.transform.root != transform.root &&
                    hit.normal.y > 0.45f)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsBlockedBySolidObstacle(Vector3 frameMove)
        {
            Vector3 horizontalMove = new Vector3(frameMove.x, 0f, frameMove.z);
            float distance = horizontalMove.magnitude;
            if (distance <= 0.001f)
            {
                return false;
            }

            Vector3 direction = horizontalMove / distance;
            if (usePreciseBodyCollision && IsBlockedByBodyProbes(direction, distance))
            {
                return true;
            }

            float radius = charControl.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z)) * 0.96f;
            float height = Mathf.Max(charControl.height * Mathf.Abs(transform.lossyScale.y), radius * 2f);
            Vector3 center = transform.TransformPoint(charControl.center);
            Vector3 up = transform.up;
            float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 top = center + up * halfSegment;
            Vector3 bottom = center - up * halfSegment;

            RaycastHit[] hits = Physics.CapsuleCastAll(
                bottom,
                top,
                radius,
                direction,
                distance + charControl.skinWidth,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.root == transform.root)
                {
                    continue;
                }

                if (hit.normal.y > 0.55f)
                {
                    continue;
                }

                if (hit.distance <= distance + charControl.skinWidth)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsBlockedByBodyProbes(Vector3 direction, float distance)
        {
            if (!TryGetVisibleBounds(out Bounds bounds))
            {
                return false;
            }

            float probeRadius = Mathf.Min(bodyProbeRadius, Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.45f);
            probeRadius = Mathf.Max(0.08f, probeRadius);
            Vector3 center = bounds.center;
            Vector3[] probes =
            {
                new Vector3(center.x, Mathf.Lerp(bounds.min.y, bounds.max.y, 0.12f), center.z),
                new Vector3(center.x, Mathf.Lerp(bounds.min.y, bounds.max.y, 0.38f), center.z),
                new Vector3(center.x, Mathf.Lerp(bounds.min.y, bounds.max.y, 0.62f), center.z),
                new Vector3(center.x, Mathf.Lerp(bounds.min.y, bounds.max.y, 0.86f), center.z),
                new Vector3(center.x, Mathf.Lerp(bounds.min.y, bounds.max.y, 0.97f), center.z)
            };

            foreach (Vector3 probe in probes)
            {
                if (Physics.SphereCast(
                        probe,
                        probeRadius,
                        direction,
                        out RaycastHit hit,
                        distance + charControl.skinWidth,
                        Physics.AllLayers,
                        QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != null &&
                        hit.collider.transform.root != transform.root &&
                        hit.normal.y <= 0.55f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryGetVisibleBounds(out Bounds bounds)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            bool hasBounds = false;
            bounds = new Bounds(transform.position, Vector3.zero);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.transform.GetComponentInParent<Camera>() != null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        Quaternion ClampRotationAroundXAxis(Quaternion q)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

            angleX = Mathf.Clamp(angleX, -90f, 90f);

            q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            return q;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            BounceMushroom bounceMushroom = hit.collider.GetComponentInParent<BounceMushroom>();
            if (bounceMushroom != null && hit.normal.y > 0.45f && move.y <= 0f && bounceMushroom.TryBounce())
            {
                move.y = bounceMushroom.BounceSpeed;
                isJumping = true;
                return;
            }

            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (collisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(charControl.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }

        private void UpdateAnimator()
        {
            if (animator == null)
            {
                return;
            }

            bool forcedWalk = ForceWalkAnimation;
            bool isMoving = forcedWalk || moveInput.sqrMagnitude > 0.001f;
            SetFloat(speedParameter, isMoving ? (forcedWalk || isWalking ? 0.5f : 1f) : 0f);
            SetBool(movingParameter, isMoving);
            SetBool(runningParameter, isMoving && !forcedWalk && !isWalking);
            SetBool(groundedParameter, IsGrounded());

            if (isCrouching)
            {
                PlayAnimationState(crouchStateName, 0.05f);
            }
            else if (isJumping && !IsGrounded())
            {
                PlayAnimationState(jumpStateName, 0.05f);
            }
            else if (isMoving)
            {
                PlayAnimationState(forcedWalk || isWalking ? walkStateName : runStateName, 0.08f);
            }
            else
            {
                PlayAnimationState(idleStateName, 0.08f);
            }
        }

        private void PlayAnimationState(string stateName, float fadeTime)
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
            {
                return;
            }

            int fullPathHash = Animator.StringToHash("Base Layer." + stateName);
            int shortNameHash = Animator.StringToHash(stateName);
            int playableHash = animator.HasState(0, fullPathHash) ? fullPathHash : shortNameHash;

            if (!animator.HasState(0, playableHash) || currentAnimationStateHash == playableHash)
            {
                return;
            }

            currentAnimationStateHash = playableHash;
            animator.CrossFade(playableHash, fadeTime, 0);
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
    }
}
