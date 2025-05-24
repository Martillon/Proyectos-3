// En una carpeta, ej: Scripts/Enemies/Movement/SteeringBehaviors/Implementations
using UnityEngine;

namespace Scripts.Enemies.Movement.SteeringBehaviors.Implementations
{
    public class StayStillBehavior2D : ISteeringBehavior2D
    {
        public SteeringOutput2D GetSteering(EnemyMovementComponent context)
        {
            return SteeringOutput2D.Zero; // Devuelve velocidad cero y no necesita orientarse
        }
    }
}
