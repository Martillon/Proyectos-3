using System.Collections.Generic;

namespace Scripts.Core.Progression
{
    /// <summary>
    /// Represents the unlock and completion status of a single game level.
    /// This is a data-only class, designed to be part of a larger serializable structure.
    /// </summary>
    [System.Serializable]
    public class LevelStatus
    {
        // Using a scene name as the identifier is simple and effective.
        public string levelIdentifier;
        public bool isUnlocked;
        public bool isCompleted;
        // You could add more data here later, e.g., bestTime, collectiblesFound, etc.
        // public float bestTime = -1f;

        public LevelStatus(string id, bool unlocked = false, bool completed = false)
        {
            levelIdentifier = id;
            isUnlocked = unlocked;
            isCompleted = completed;
        }
    }

    /// <summary>
    /// A container for the progression data of all levels.
    /// This is the top-level object that gets serialized to and from JSON.
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