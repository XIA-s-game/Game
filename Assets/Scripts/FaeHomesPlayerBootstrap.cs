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

    [Header("Ground")]
    [SerializeField] private float groundRaycastHeight = 30f;
    [SerializeField] private float groundRaycastDistance = 100f;
    [SerializeField] private float groundOffset = 0.01f;

    private Animator heroAnimator;

    private void Start()
    {
        if (player == null)
        {
            return;
        }

        SetupPlayer();
    }

    private void SetupPlayer()
    {
        player.SetActive(true);
        SetupCamera();
        SetupHero();
        SetupMovementScripts();
        DisableSpawnMarkerCollision();
        MovePlayerToSpawn();
    }

    private void DisableSpawnMarkerCollision()
    {
        if (spawnPoint == null)
        {
            return;
        }

        Collider[] colliders = spawnPoint.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        Renderer[] renderers = spawnPoint.GetComponents<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }
    }

    private void SetupCamera()
    {
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

        AquariusMax.Fae.demo.DemoCharacter demoCharacter = player.GetComponentInChildren<AquariusMax.Fae.demo.DemoCharacter>(true);
        if (demoCharacter != null)
        {
            demoCharacter.SetAnimator(heroAnimator);
        }
    }

    private void SetupMovementScripts()
    {
        AquariusMax.Fae.demo.DemoCharacter demoCharacter = player.GetComponentInChildren<AquariusMax.Fae.demo.DemoCharacter>(true);
        if (demoCharacter != null)
        {
            demoCharacter.enabled = true;
            demoCharacter.SetCollisionOptions(false, false);
            demoCharacter.ClearMotionState();
            AquariusMax.Fae.demo.DemoCharacter.ResetControlFlags();
        }

    }

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

        return position;
    }

}
