namespace Scripts.Enemies.Movement.SteeringBehaviors
{
    /// <summary>
    /// Interface for all steering behaviors. Defines the contract for calculating a steering output.
    /// </summary>
    public interface ISteeringBehavior
    {
        /// <summary>
        /// Calculates the desired steering output based on the agent's context.
        /// </summary>
        /// <param name="context">Provides information about the agent's current state (e.g., position, environment checks).</param>
        /// <returns>A SteeringOutput struct containing the desired velocity.</returns>
        SteeringOutput GetSteering(EnemyMovementComponent context);
    }
}