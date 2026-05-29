// Main function: Configures the player, camera, visible hero model, movement scripts, and animation state when the Fae Homes scene starts.

using System.Collections;
using UnityEngine;

public class FaeHomesPlayerBootstrap : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private GameObject player;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform visibleHero;
    [SerializeField] private Camera playerCamera;

    [Header("Camera")]
    [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0f, 2.25f, -1.6f);

    [Header("Hero")]
    [SerializeField] private RuntimeAnimatorController heroAnimatorController;
    [SerializeField] private Vector3 heroLocalPosition = new Vector3(0f, -0.128f, 0.349f);
    [SerializeField] private float heroScale = 0.42f;

    [Header("Animation")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string movingParameter = "IsMoving";
    [SerializeField] private string runningParameter = "IsRunning";
    [SerializeField] private float walkAnimationSpeed = 0.5f;
    [SerializeField] private float moveThreshold = 0.03f;

    [Header("Ground")]
    [SerializeField] private float groundRaycastHeight = 30f;
    [SerializeField] private float groundRaycastDistance = 100f;
    [SerializeField] private float groundOffset = 0.01f;

    private Animator heroAnimator;
    private Vector3 lastPlayerPosition;
    private bool lastPlayerPositionReady;

    // Function: Runs one-time setup after the scene has started.
    private void Start()
    {
        if (player == null)
        {
            return;
        }

        SetupPlayer();
        StartCoroutine(SetupPlayerNextFrame());
    }

    // Function: Updates input handling, interaction checks, and active gameplay flow each frame.
    private void Update()
    {
        UpdateHeroAnimation();
    }

    // Function: Sets up player next frame.
    private IEnumerator SetupPlayerNextFrame()
    {
        yield return null;

        if (player != null)
        {
            MovePlayerToSpawn();
        }
    }

    // Function: Sets up player.
    private void SetupPlayer()
    {
        player.SetActive(true);
        SetupCamera();
        SetupHero();
        SetupMovementScripts();
        MovePlayerToSpawn();
    }

    // Function: Sets up camera.
    private void SetupCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = player.GetComponentInChildren<Camera>(true);
        }

        if (playerCamera == null)
        {
            return;
        }

        playerCamera.transform.localPosition = cameraLocalPosition;
        playerCamera.transform.localRotation = Quaternion.identity;
        playerCamera.gameObject.SetActive(true);
        playerCamera.tag = "MainCamera";

        AudioListener listener = playerCamera.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = true;
        }
    }

    // Function: Sets up hero.
    private void SetupHero()
    {
        if (visibleHero == null)
        {
            return;
        }

        if (visibleHero.parent != player.transform)
        {
            visibleHero.SetParent(player.transform, false);
        }

        visibleHero.localPosition = heroLocalPosition;
        visibleHero.localRotation = Quaternion.identity;
        visibleHero.localScale = Vector3.one * heroScale;
        visibleHero.gameObject.SetActive(true);

        DisableHeroCameras();
        SetupHeroAnimator();
    }

    // Function: Disables hero cameras.
    private void DisableHeroCameras()
    {
        Camera[] cameras = visibleHero.GetComponentsInChildren<Camera>(true);
        foreach (Camera camera in cameras)
        {
            if (camera != null)
            {
                camera.gameObject.SetActive(false);
            }
        }

        AudioListener[] listeners = visibleHero.GetComponentsInChildren<AudioListener>(true);
        foreach (AudioListener listener in listeners)
        {
            if (listener != null)
            {
                listener.enabled = false;
            }
        }
    }

    // Function: Sets up hero animator.
    private void SetupHeroAnimator()
    {
        heroAnimator = visibleHero.GetComponentInChildren<Animator>(true);
        if (heroAnimator == null)
        {
            return;
        }

        if (heroAnimatorController != null)
        {
            heroAnimator.runtimeAnimatorController = heroAnimatorController;
        }

        heroAnimator.applyRootMotion = false;
        heroAnimator.enabled = true;
        lastPlayerPosition = player.transform.position;
        lastPlayerPositionReady = true;
    }

    // Function: Sets up movement scripts.
    private void SetupMovementScripts()
    {
        AquariusMax.Fae.demo.DemoCharacter demoCharacter = player.GetComponentInChildren<AquariusMax.Fae.demo.DemoCharacter>(true);
        if (demoCharacter != null)
        {
            demoCharacter.enabled = true;
        }

        PlayerCharacterController customController = player.GetComponentInChildren<PlayerCharacterController>(true);
        if (customController != null)
        {
            customController.enabled = false;
        }
    }

    // Function: Updates hero animation state, input, or presentation.
    private void UpdateHeroAnimation()
    {
        if (player == null)
        {
            return;
        }

        if (heroAnimator == null && visibleHero != null)
        {
            heroAnimator = visibleHero.GetComponentInChildren<Animator>(true);
        }

        if (heroAnimator == null)
        {
            return;
        }

        Vector3 current = player.transform.position;
        if (!lastPlayerPositionReady)
        {
            lastPlayerPosition = current;
            lastPlayerPositionReady = true;
            return;
        }

        Vector3 delta = current - lastPlayerPosition;
        delta.y = 0f;
        bool moving = delta.magnitude > moveThreshold * Mathf.Max(Time.deltaTime, 0.001f);
        lastPlayerPosition = current;

        SetAnimatorFloat(speedParameter, moving ? walkAnimationSpeed : 0f);
        SetAnimatorBool(movingParameter, moving);
        SetAnimatorBool(runningParameter, false);
    }

    // Function: Sets animator float.
    private void SetAnimatorFloat(string parameterName, float value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
        {
            heroAnimator.SetFloat(parameterName, value, 0.08f, Time.deltaTime);
        }
    }

    // Function: Sets animator bool.
    private void SetAnimatorBool(string parameterName, bool value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            heroAnimator.SetBool(parameterName, value);
        }
    }

    // Function: Checks whether animator parameter already exists or is available.
    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (heroAnimator == null || string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in heroAnimator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == type)
            {
                return true;
            }
        }

        return false;
    }

    // Function: Moves player to spawn toward its target position or state.
    private void MovePlayerToSpawn()
    {
        if (spawnPoint == null)
        {
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controller != null)
        {
            controller.enabled = false;
        }

        Vector3 position = SnapToGround(spawnPoint.position, controller);
        player.transform.SetPositionAndRotation(position, spawnPoint.rotation);

        if (controller != null)
        {
            controller.enabled = controllerWasEnabled;
        }
    }

    // Function: Snaps to ground to the target position or ground.
    private Vector3 SnapToGround(Vector3 position, CharacterController controller)
    {
        float footOffset = controller != null ? controller.center.y - controller.height * 0.5f : 0f;
        Vector3 rayStart = position + Vector3.up * groundRaycastHeight;
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, groundRaycastDistance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.root == player.transform)
            {
                continue;
            }

            position.y = hit.point.y + groundOffset - footOffset;
            return position;
        }

        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            bool insideX = position.x >= terrainPosition.x && position.x <= terrainPosition.x + terrainSize.x;
            bool insideZ = position.z >= terrainPosition.z && position.z <= terrainPosition.z + terrainSize.z;
            if (insideX && insideZ)
            {
                position.y = terrain.SampleHeight(position) + terrainPosition.y + groundOffset - footOffset;
            }
        }

        return position;
    }

}
