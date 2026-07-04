using UnityEngine;

namespace GameJam.Gameplay.PlatformObjectives
{
    internal static class PlatformObjectivePlayerDetector
    {
        public static bool IsPlayer(Collider2D other, string playerTag)
        {
            if (other == null)
                return false;

            if (IsPlayerObject(other.gameObject, playerTag))
                return true;

            if (other.attachedRigidbody != null && IsPlayerObject(other.attachedRigidbody.gameObject, playerTag))
                return true;

            Transform current = other.transform.parent;
            while (current != null)
            {
                if (IsPlayerObject(current.gameObject, playerTag))
                    return true;

                current = current.parent;
            }

            return false;
        }

        private static bool IsPlayerObject(GameObject candidate, string playerTag)
        {
            if (candidate == null)
                return false;

            if (!string.IsNullOrWhiteSpace(playerTag) && HasTag(candidate, playerTag))
                return true;

            if (candidate.name == "Player" || candidate.name == "PlatformPlayer")
                return true;

            return candidate.GetComponent("Platform_PlayerController") != null
                || candidate.GetComponent("Cave_PlayerController") != null;
        }

        private static bool HasTag(GameObject candidate, string tag)
        {
            try
            {
                return candidate.CompareTag(tag);
            }
            catch (UnityException)
            {
                return false;
            }
        }
    }
}
