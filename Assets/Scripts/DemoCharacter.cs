// Controls the fae demo player and exposes a few locks used by puzzle cutscenes.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AquariusMax.Fae.demo
{
    // Based on the original FAE demo controller, with project-specific locks added.
    [RequireComponent(typeof(CharacterController))]
    public class DemoCharacter : MonoBehaviour
    {
        // Full input lock used by menus, dialogue, and quizzes.
        public static bool LockPlayerInput;
        // Movement-only lock used while scripted movement is playing.
        public static bool LockMovementInput;
        // Forces walk animation while another script moves the player.
        public static bool ForceWalkAnimation;
        // Optional touch-style look input.
        public static bool UseLookPadInput;
        // Look input supplied by the virtual pad.
        public static Vector2 LookPadInput;
        // True while the first-scene tutorial is running.
        public static bool TutorialActive;
        // Tutorial gate for mouse look.
        public static bool TutorialAllowLook = true;
        // Tutorial gate for movement.
        public static bool TutorialAllowMove = true;
        // Tutorial gate for jumping.
        public static bool TutorialAllowJump = true;
        // Tutorial gate for crouching.
        public static bool TutorialAllowCrouch = true;
        // Tutorial gate for running.
        public static bool TutorialAllowRun = true;
        // Set once the player has successfully used run.
        public static bool TutorialRunObserved;

        // Camera controlled by mouse look.
        [SerializeField]
        Camera cam;

        // Multiplier applied to Unity gravity.
        [SerializeField]
        float gravityModifier = 2f;
        // Normal movement speed.
        [SerializeField]
        float walkSpeed = 5f;
        // Shift movement speed.
        [SerializeField]
        float runSpeed = 10f;
        // Initial upward velocity for jumps.
        [SerializeField]
        float jumpSpeed = 10f;
        // Extra downward force after a jump peaks.
        [SerializeField]
        float landingForce = 10f;

        // Horizontal mouse sensitivity.
        [SerializeField]
        float mouseXSensitivity = 1.25f;
        // Vertical mouse sensitivity.
        [SerializeField]
        float mouseYSensitivity = 1.25f;

        // Animator for the visible player model.
        [SerializeField]
        Animator animator;

        // Animator float parameter for movement speed.
        [SerializeField]
        string speedParameter = "Speed";
        // Animator bool parameter for movement.
        [SerializeField]
        string movingParameter = "IsMoving";
        // Animator bool parameter for running.
        [SerializeField]
        string runningParameter = "IsRunning";
        // Animator bool parameter for grounded state.
        [SerializeField]
        string groundedParameter = "Grounded";
        // Animator trigger parameter for jumping.
        [SerializeField]
        string jumpTriggerParameter = "Jump";
        // Animator idle state name.
        [SerializeField]
        string idleStateName = "Idle";
        // Animator walk state name.
        [SerializeField]
        string walkStateName = "Walk";
        // Animator run state name.
        [SerializeField]
        string runStateName = "Run";
        // Animator jump state name.
        [SerializeField]
        string jumpStateName = "Jump";
        // Key used for crouch.
        [SerializeField]
        KeyCode crouchKey = KeyCode.V;
        // Animator crouch state name.
        [SerializeField]
        string crouchStateName = "Crouch";
        // Movement speed while crouching.
        [SerializeField]
        float crouchSpeed = 2.5f;
        // CharacterController height multiplier while crouched.
        [SerializeField]
        float crouchHeightMultiplier = 0.55f;
        // CharacterController radius multiplier while crouched.
        [SerializeField]
        float crouchRadiusMultiplier = 0.8f;
        // Safety timeout for crouch.
        [SerializeField]
        float maxCrouchSeconds = 3f;
        // If true, one key press toggles crouch on/off.
        [SerializeField]
        bool toggleCrouch = true;
        // Optional forward obstacle blocking.
        [SerializeField]
        bool blockSolidObstacles = false;
        // Uses body probes to reduce clipping.
        [SerializeField]
        bool usePreciseBodyCollision = true;
        // Radius for body probe casts.
        [SerializeField]
        float bodyProbeRadius = 0.12f;
        // Fits CharacterController to renderer bounds on start.
        [SerializeField]
        bool fitControllerToVisibleCharacter = true;
        // Extra space around fitted controller.
        [SerializeField]
        float controllerFitPadding = 0.08f;
        // Ground probe length below the controller.
        [SerializeField]
        float groundedCheckDistance = 0.42f;

        // Cached CharacterController.
        CharacterController charControl;

        // Target yaw for the character body.
        Quaternion characterTargetRot;
        // Target local pitch for the camera.
        Quaternion cameraTargetRot;
        // Animator parameters available on this controller.
        HashSet<int> animatorParameters = new HashSet<int>();

        // Current walk/run choice.
        bool isWalking = true;
        // Current movement input.
        Vector2 moveInput = Vector2.zero;
        // Current movement velocity.
        Vector3 move = Vector3.zero;
        // Jump key state captured this frame.
        bool jumpPressed = false;
        // True while the jump animation/air state is active.
        bool isJumping = false;
        // True while crouch is active.
        bool isCrouching = false;
        // Time when crouch should auto-end.
        float crouchEndsAt = -1f;

        // Last CharacterController collision result.
        CollisionFlags collisionFlags;
        // Animator state hash currently being watched.
        int currentAnimationStateHash;
        // Standing controller height, restored after crouch.
        float standingControllerHeight;
        // Standing controller radius, restored after crouch.
        float standingControllerRadius;
        // Standing controller center, restored after crouch.
        Vector3 standingControllerCenter;

        void Start()
        {
            if (cam == null)
            {
                Debug.LogWarning("DemoCharacter is missing its Camera reference. Drag the player camera into the DemoCharacter Cam field.", this);
            }

            charControl = GetComponent<CharacterController>();
            if (animator == null)
            {
                Debug.LogWarning("DemoCharacter is missing its Animator reference. Drag the player's Walking Animator into the DemoCharacter Animator field.", this);
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
            if (cam != null)
            {
                cameraTargetRot = cam.transform.localRotation;
            }

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
            if (cam == null || LockPlayerInput || (TutorialActive && !TutorialAllowLook))
            {
                return;
            }

            float mouseX;
            float mouseY;
            if (UseLookPadInput)
            {
                mouseX = LookPadInput.x * mouseXSensitivity;
                mouseY = LookPadInput.y * mouseYSensitivity;
            }
            else
            {
                mouseX = Input.GetAxis("Mouse X") * mouseXSensitivity;
                mouseY = Input.GetAxis("Mouse Y") * mouseYSensitivity;
            }

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

            Vector3 desiredMove = transform.forward * moveInput.y + transform.right * moveInput.x;

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
            if (charControl == null)
            {
                charControl = GetComponent<CharacterController>();
            }

            if (charControl == null)
            {
                return false;
            }

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
                currentAnimationStateHash = 0;
            }
            else
            {
                currentAnimationStateHash = 0;
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

            if (animator == null || animator.runtimeAnimatorController == null)
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

        public void SetAnimator(Animator targetAnimator)
        {
            animator = targetAnimator;
            CacheAnimatorParameters();
        }

        public Animator GetCurrentAnimator()
        {
            return animator;
        }

        public void SetCamera(Camera targetCamera)
        {
            if (targetCamera == null)
            {
                return;
            }

            cam = targetCamera;
            cameraTargetRot = cam.transform.localRotation;
        }

        public void SetCollisionOptions(bool ignoreRigidbodies, bool ignoreTriggers)
        {
            blockSolidObstacles = ignoreRigidbodies;
        }

        public void ClearMotionState()
        {
            if (charControl == null)
            {
                charControl = GetComponent<CharacterController>();
            }

            moveInput = Vector2.zero;
            move = Vector3.zero;
            jumpPressed = false;
            isJumping = false;
            currentAnimationStateHash = 0;

            if (charControl != null)
            {
                UpdateAnimator();
            }
        }

        public static void ResetControlFlags()
        {
            LockPlayerInput = false;
            LockMovementInput = false;
            ForceWalkAnimation = false;
            UseLookPadInput = false;
            LookPadInput = Vector2.zero;
            TutorialActive = false;
            TutorialAllowLook = true;
            TutorialAllowMove = true;
            TutorialAllowJump = true;
            TutorialAllowCrouch = true;
            TutorialAllowRun = true;
            TutorialRunObserved = false;
        }

        public static void SetControlLocked(bool locked)
        {
            LockPlayerInput = locked;
        }
    }
}
