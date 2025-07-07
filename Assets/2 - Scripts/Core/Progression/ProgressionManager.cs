using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scripts.Core.Progression;

namespace Scripts.Core
{
    // Data structure for the save file
    [System.Serializable]
    public class BountySaveData { public string bountyID; public bool isUnlocked; public bool isCompleted; }

    [System.Serializable]
    public class GameProgressionData { public List<BountySaveData> bountyStatuses = new List<BountySaveData>(); }

    public class ProgressionManager : MonoBehaviour
    {
        public static ProgressionManager Instance { get; private set; }

        [SerializeField] private BountyBoard bountyBoard;
        
        private GameProgressionData _progressionData;
        private Dictionary<string, BountySaveData> _bountyStatusMap = new Dictionary<string, BountySaveData>();
        private string _saveFilePath;
        private const string SAVE_FILE_NAME = "game_progress.json";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _saveFilePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        }

        private void Start()
        {
            LoadProgression();
        }

        public void LoadProgression()
        {
            if (File.Exists(_saveFilePath))
            {
                string json = File.ReadAllText(_saveFilePath);
                _progressionData = JsonUtility.FromJson<GameProgressionData>(json);
            }
            else
            {
                _progressionData = new GameProgressionData();
            }
            SyncWithBountyBoard();
        }

        private void SyncWithBountyBoard()
        {
            if (bountyBoard == null) return;
            bool wasModified = false;

            // Create a lookup of existing saved data
            var savedDataLookup = _progressionData.bountyStatuses.ToDictionary(b => b.bountyID);
            var newStatuses = new List<BountySaveData>();

            // Ensure all bounties from the board exist in our data, and in the correct order
            foreach (var bountyAsset in bountyBoard.allBounties)
            {
                if (savedDataLookup.TryGetValue(bountyAsset.bountyID, out var savedData))
                {
                    newStatuses.Add(savedData);
                }
                else
                {
                    // Add new bounty that wasn't in the save file
                    newStatuses.Add(new BountySaveData { bountyID = bountyAsset.bountyID });
                    wasModified = true;
                }
            }
            _progressionData.bountyStatuses = newStatuses;
            
            // Ensure first bounty is always unlocked
            if (_progressionData.bountyStatuses.Count > 0 && !_progressionData.bountyStatuses[0].isUnlocked)
            {
                _progressionData.bountyStatuses[0].isUnlocked = true;
                wasModified = true;
            }
            
            // Re-populate the fast-lookup dictionary
            _bountyStatusMap = _progressionData.bountyStatuses.ToDictionary(b => b.bountyID);
            if (wasModified) SaveProgression();
        }

        public void SaveProgression()
        {
            string json = JsonUtility.ToJson(_progressionData, true);
            File.WriteAllText(_saveFilePath, json);
        }

        public BountySaveData GetBountyStatus(string bountyID)
        {
            _bountyStatusMap.TryGetValue(bountyID, out var status);
            return status;
        }

        public void CompleteBounty(string bountyID)
        {
            var status = GetBountyStatus(bountyID);
            if (status != null && !status.isCompleted)
            {
                status.isCompleted = true;
                Debug.Log($"Bounty '{bountyID}' marked as complete.");
                UnlockNextBounty(bountyID);
                SaveProgression();
            }
        }
        
        private void UnlockNextBounty(string completedBountyID)
        {
            int index = bountyBoard.allBounties.FindIndex(b => b.bountyID == completedBountyID);
            if (index > -1 && index + 1 < bountyBoard.allBounties.Count)
            {
                string nextBountyID = bountyBoard.allBounties[index + 1].bountyID;
                var nextBountyStatus = GetBountyStatus(nextBountyID);
                if (nextBountyStatus != null && !nextBountyStatus.isUnlocked)
                {
                    nextBountyStatus.isUnlocked = true;
                    Debug.Log($"Bounty '{nextBountyID}' unlocked.");
                }
            }
        }
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void Debug_UnlockAllBounties()
        {
            if (_progressionData == null) LoadProgression();
            foreach (var status in _progressionData.bountyStatuses)
            {
                status.isUnlocked = true;
            }
            SaveProgression();
            Debug.LogWarning("DEBUG: All bounties have been unlocked via debug command.");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void Debug_ResetAllProgression()
        {
            // This is a destructive action. Wiping the save file is the cleanest way.
            if (File.Exists(_saveFilePath))
            {
                File.Delete(_saveFilePath);
            }
            // Reset the in-memory data and re-initialize from the board.
            _progressionData = null;
            _bountyStatusMap.Clear();
            LoadProgression(); // This will create a fresh save file with only the first bounty unlocked.
            Debug.LogWarning("DEBUG: All bounty progression has been reset by deleting the save file.");
        }
    }
}