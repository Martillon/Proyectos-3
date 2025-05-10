// --- START OF FILE LevelData.cs ---
using System.Collections.Generic; // For List

namespace Scripts.Core.Progression // Nuevo namespace
{
    /// <summary>
    /// Represents the unlock and completion status of a single game level.
    /// </summary>
    [System.Serializable]
    public class LevelStatus
    {
        public string levelIdentifier; // Can be scene name, build index as string, or a custom ID
        public bool isUnlocked;
        public bool isCompleted;

        public LevelStatus(string id, bool unlocked = false, bool completed = false)
        {
            levelIdentifier = id;
            isUnlocked = unlocked;
            isCompleted = completed;
        }
    }

    /// <summary>
    /// Container for the progression data of all levels. This class will be serialized to JSON.
    /// </summary>
    [System.Serializable]
    public class AllLevelsProgressionData
    {
        public List<LevelStatus> levelStatuses;

        public AllLevelsProgressionData()
        {
            levelStatuses = new List<LevelStatus>();
        }
    }
}
// --- END OF FILE LevelData.cs ---
