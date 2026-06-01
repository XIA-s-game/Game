// Main function: Moves an NPC or object along route waypoints while updating facing direction, walking animation, and ground snapping.

using System;
using UnityEngine;

public class RouteWaypointWalker : MonoBehaviour
{
    [SerializeField] private Transform routeRoot;
    [SerializeField] private Transform[] routePoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float arriveDistance = 0.25f;
    [SerializeField] private bool followGround = true;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRaycastHeight = 20f;
    [SerializeField] private float groundRaycastDistance = 80f;
    [SerializeField] private float groundOffset = 0f;
    [SerializeField] private Animator animator;
    [SerializeField] private RuntimeAnimatorController walkingController;
    [SerializeField] private string walkingStateName = "Walk";

    private int targetIndex;
    private bool hasRoute;

    // Function: Initializes component references, cached state, and default runtime data.
    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    // Function: Runs one-time setup after the scene has started.
    private void Start()
    {
        LoadRoutePointsFromRoot();
        CompactRoutePoints();

        hasRoute = routePoints != null && routePoints.Length > 0;
        if (!hasRoute)
        {
            enabled = false;
            return;
        }

        targetIndex = GetForwardTargetIndex(transform.position);
        SnapToGround();
        PlayWalkingAnimation();
    }

    // Function: Updates input handling, interaction checks, and active gameplay flow each frame.
    private void Update()
    {
        if (!hasRoute || targetIndex >= routePoints.Length)
        {
            return;
        }

        if (routePoints[targetIndex] == null)
        {
            targetIndex++;
            return;
        }

        Vector3 target = routePoints[targetIndex].position;
        target.y = transform.position.y;
        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= arriveDistance)
        {
            targetIndex++;
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        SnapToGround();
    }

    // Function: Gets or calculates forward target index.
    private int GetForwardTargetIndex(Vector3 position)
    {
        if (routePoints.Length == 1)
        {
            return 0;
        }

        float bestDistance = float.PositiveInfinity;
        int bestSegmentStart = 0;

        for (int i = 0; i < routePoints.Length - 1; i++)
        {
            if (routePoints[i] == null || routePoints[i + 1] == null)
            {
                continue;
            }

            Vector3 a = routePoints[i].position;
            Vector3 b = routePoints[i + 1].position;
            a.y = position.y;
            b.y = position.y;

            Vector3 segment = b - a;
            float segmentLengthSquared = segment.sqrMagnitude;
            if (segmentLengthSquared <= Mathf.Epsilon)
            {
                continue;
            }

            float t = Mathf.Clamp01(Vector3.Dot(position - a, segment) / segmentLengthSquared);
            Vector3 closestPoint = a + segment * t;
            float distance = (position - closestPoint).sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestSegmentStart = i;
            }
        }

        return Mathf.Clamp(bestSegmentStart + 1, 0, routePoints.Length - 1);
    }

    // Function: Plays walking animation animation, audio, or cutscene behavior.
    private void PlayWalkingAnimation()
    {
        if (animator == null)
        {
            return;
        }

        if (walkingController != null)
        {
            animator.runtimeAnimatorController = walkingController;
        }

        if (!string.IsNullOrWhiteSpace(walkingStateName))
        {
            int stateHash = Animator.StringToHash("Base Layer." + walkingStateName);
            int shortStateHash = Animator.StringToHash(walkingStateName);
            if (animator.HasState(0, stateHash))
            {
                animator.Play(stateHash, 0, 0f);
            }
            else if (animator.HasState(0, shortStateHash))
            {
                animator.Play(shortStateHash, 0, 0f);
            }
        }
    }

    // Function: Loads route points from root resources or controllers.
    private void LoadRoutePointsFromRoot()
    {
        if (routePoints != null && routePoints.Length > 0)
        {
            return;
        }

        if (routeRoot == null)
        {
            return;
        }

        routePoints = new Transform[routeRoot.childCount];
        for (int i = 0; i < routeRoot.childCount; i++)
        {
            routePoints[i] = routeRoot.GetChild(i);
        }
    }

    // Function: Runs the compact route points logic.
    private void CompactRoutePoints()
    {
        if (routePoints == null || routePoints.Length == 0)
        {
            return;
        }

        int validCount = 0;
        for (int i = 0; i < routePoints.Length; i++)
        {
            if (routePoints[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == routePoints.Length)
        {
            return;
        }

        Transform[] validPoints = new Transform[validCount];
        int writeIndex = 0;
        for (int i = 0; i < routePoints.Length; i++)
        {
            if (routePoints[i] != null)
            {
                validPoints[writeIndex] = routePoints[i];
                writeIndex++;
            }
        }

        routePoints = validPoints;
    }

    // Function: Snaps to ground to the target position or ground.
    private void SnapToGround()
    {
        if (!followGround)
        {
            return;
        }

        Vector3 position = transform.position;
        Vector3 rayStart = position + Vector3.up * groundRaycastHeight;

        if (TrySnapToTerrain(ref position))
        {
            transform.position = position;
            return;
        }

        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, groundRaycastDistance, groundMask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            position.y = hit.point.y + groundOffset;
            transform.position = position;
            return;
        }
    }

    // Function: Tries to process snap to terrain and returns whether it succeeded.
    private bool TrySnapToTerrain(ref Vector3 position)
    {
        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            bool insideX = position.x >= terrainPosition.x && position.x <= terrainPosition.x + terrainSize.x;
            bool insideZ = position.z >= terrainPosition.z && position.z <= terrainPosition.z + terrainSize.z;
            if (!insideX || !insideZ)
            {
                continue;
            }

            position.y = terrain.SampleHeight(position) + terrainPosition.y + groundOffset;
            return true;
        }

        return false;
    }
}
