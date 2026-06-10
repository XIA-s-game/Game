using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class OldTreeInteraction
{
    private IEnumerator TurnTowardPlayer()
    {
        yield return RotateLookRoot(GetLookAtPlayerRotation());
    }

    private IEnumerator ReturnToOriginalRotation()
    {
        yield return RotateLookRoot(originalRotation);
    }

    private IEnumerator RotateLookRoot(Quaternion targetRotation)
    {
        Quaternion startRotation = lookRoot.rotation;
        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / turnDuration);
            lookRoot.rotation = Quaternion.Slerp(startRotation, originalRotation, t);
            yield return null;
        }

        lookRoot.rotation = originalRotation;
        lookCoroutine = null;
    }

    private Quaternion GetLookAtPlayerRotation()
    {
        Vector3 direction = player.position - lookRoot.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return originalRotation;
        }

        Quaternion yawRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        return yawRotation * Quaternion.Euler(lookDownAngle, 0f, 0f);
    }
}
