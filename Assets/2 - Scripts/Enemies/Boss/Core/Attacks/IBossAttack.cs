using System.Collections;
using Scripts.Enemies.Boss.Core;

namespace Scripts.Enemies.Boss.Attacks
{
    /// <summary>
    /// An interface that defines the contract for any modular boss attack.
    /// The BossController will use this interface to execute attacks without needing to
    /// know the specific details of each one.
    /// </summary>
    public interface IBossAttack
    {
        /// <summary>
        /// Executes the entire logic for this attack as a coroutine.
        /// The BossController will wait for this coroutine to finish before
        /// proceeding to the next action in its pattern.
        /// </summary>
        /// <returns>An IEnumerator to be run as a coroutine.</returns>
        IEnumerator Execute();

        /// <summary>
        /// A method for the BossController to pass necessary references (like itself)
        /// to the attack script upon initialization.
        /// </summary>
        void Initialize(BossController controller);
    }
}
