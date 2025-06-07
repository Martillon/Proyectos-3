namespace Scripts.Enemies.Movement.SteeringBehaviors.Implementations
{
    /// <summary>
    /// A simple steering behavior that results in no movement.
    /// </summary>
    public class StayStillBehavior : ISteeringBehavior
    {
        public SteeringOutput GetSteering(EnemyMovementComponent context)
        {
            return SteeringOutput.Zero;
        }
    }
}
