namespace Scripts.Enemies.Core
{
    /// <summary>
    /// A marker interface for any component that provides an enemy's attack logic.
    /// This allows other scripts to find and interact with attack components generically.
    /// </summary>
    public interface IEnemyAttack
    {
        // This interface can be empty. It's just used for type identification.
        // Or, we can add the common methods to enforce the contract.
        
        bool CanInitiateAttack(UnityEngine.Transform target);
        void TryAttack(UnityEngine.Transform target);
    }
}