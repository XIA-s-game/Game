using UnityEngine;

namespace AquariusMax.Fae.demo
{
    // Attach this to a mushroom that should launch the player upward.
    public class BounceMushroom : MonoBehaviour
    {
        // Upward speed applied by the player controller.
        [SerializeField] private float bounceSpeed = 14f;
        // Small delay so one landing does not trigger several bounces.
        [SerializeField] private float bounceCooldown = 0.12f;

        // Last accepted bounce time.
        private float lastBounceTime = -10f;

        // Read by the player controller when a bounce is accepted.
        public float BounceSpeed => bounceSpeed;

        public bool TryBounce()
        {
            if (Time.time - lastBounceTime < bounceCooldown)
            {
                return false;
            }

            lastBounceTime = Time.time;
            return true;
        }
    }
}
