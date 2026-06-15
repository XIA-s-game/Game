using UnityEngine;

namespace AquariusMax.Fae.demo
{
    public class BounceMushroom : MonoBehaviour
    {
        [SerializeField] private float bounceSpeed = 14f;
        [SerializeField] private float bounceCooldown = 0.12f;

        private float lastBounceTime = -10f;

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
