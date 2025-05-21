namespace Scripts.Environment.Interfaces // O un namespace adecuado
{
    /// <summary>
    /// Interface for platforms that can be temporarily made non-collidable
    /// to allow entities (like the player) to pass through them.
    /// </summary>
    public interface ITraversablePlatform
    {
        /// <summary>
        /// Makes the platform non-collidable for a specified duration.
        /// </summary>
        /// <param name="duration">How long the platform should remain non-collidable.</param>
        void BecomeTemporarilyNonCollidable(float duration);
    }
}
