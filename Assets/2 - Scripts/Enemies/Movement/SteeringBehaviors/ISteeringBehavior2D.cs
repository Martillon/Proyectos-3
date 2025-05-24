using UnityEngine; // Para Vector2

namespace Scripts.Enemies.Movement.SteeringBehaviors
{
    public interface ISteeringBehavior2D
    {
        /// <summary>
        /// Calcula la salida de steering deseada.
        /// </summary>
        /// <param name="context">Referencia a EnemyMovementComponent para acceder a su estado y propiedades.</param>
        /// <returns>La salida de steering con la velocidad deseada y si debe orientarse.</returns>
        SteeringOutput2D GetSteering(EnemyMovementComponent context);
    }
}