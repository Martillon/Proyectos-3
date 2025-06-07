using UnityEngine;
using System.Collections.Generic;
using Scripts.Core;
using Scripts.Player.Core;
using Scripts.Player.Movement.Motor;
using Scripts.Player.Weapons;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

/// <summary>
/// A persistent singleton for handling debug commands and cheats.
/// This script and its GameObject will only be included in Editor and Development builds.
/// </summary>
public class DebugController : MonoBehaviour
{
    private static DebugController Instance { get; set; }

    [Header("UI Feedback")]
    [Tooltip("The UI GameObject with a red border to show when cheats are active. Should be on a persistent canvas.")]
    [SerializeField] private GameObject cheatActiveIndicator;

    [Header("Noclip Settings")]
    [Tooltip("Movement speed when in Noclip mode.")]
    [SerializeField] private float noclipSpeed = 10f;
    
    [Header("Weapon Cheats")]
    [Tooltip("A list of all weapon upgrade prefabs to cycle through.")]
    [SerializeField] private List<GameObject> weaponPrefabs;

    private bool _cheatsEnabled = false;
    private bool _isNoclipActive = false;
    private bool _isSlowMoActive = false;
    private int _currentWeaponIndex = 0;

    // Cached references to player components
    private PlayerHealthSystem _playerHealth;
    private Rigidbody2D _playerRb;
    private Collider2D _playerStandingCollider;
    private Collider2D _playerCrouchingCollider;
    private PlayerMotor _playerMotor;
    private WeaponBase _playerWeaponBase;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Start with cheats disabled
        if (cheatActiveIndicator)
        {
            cheatActiveIndicator.SetActive(false);
        }
    }

    private void Update()
    {
        // --- MASTER TOGGLE ---
        if (Input.GetKeyDown(KeyCode.F5))
        {
            _cheatsEnabled = !_cheatsEnabled;
            if (cheatActiveIndicator)
            {
                cheatActiveIndicator.SetActive(_cheatsEnabled);
            }
            // If we disable cheats, also disable any active cheat states.
            if (!_cheatsEnabled)
            {
                SetNoclip(false);
                SetSlowMo(false);
            }
        }

        // Do not process any other cheats if the system is disabled.
        if (!_cheatsEnabled) return;

        // --- CHEAT INPUTS ---
        HandlePlayerCheats();
        HandleGameStateCheats();
        HandleWeaponCheats();
        
        // Noclip requires its own logic in Update
        HandleNoclipMovement();
    }
    
    private void FindPlayerComponents()
    {
        // Find player components on-demand. In a debug script, this is acceptable.
        if (!_playerHealth) _playerHealth = FindAnyObjectByType<PlayerHealthSystem>();
        if (!_playerMotor) _playerMotor = FindAnyObjectByType<PlayerMotor>();
        if (!_playerWeaponBase) _playerWeaponBase = FindAnyObjectByType<WeaponBase>();

        if (_playerMotor && !_playerRb)
        {
            _playerRb = _playerMotor.GetComponentInParent<Rigidbody2D>();
            // Find colliders via PlayerCrouchHandler or other means if needed.
        }
    }

    private void HandlePlayerCheats()
    {
        // Toggle Invincibility
        if (Input.GetKeyDown(KeyCode.I))
        {
            FindPlayerComponents();
            if (_playerHealth)
            {
                // We need to add this public property to PlayerHealthSystem
                _playerHealth.IsDebugInvincible = !_playerHealth.IsDebugInvincible;
                Debug.Log($"Player Invincibility: {_playerHealth.IsDebugInvincible}");
            }
        }
        
        // Noclip Toggle
        if (Input.GetKeyDown(KeyCode.N))
        {
            SetNoclip(!_isNoclipActive);
        }

        // Teleport to Mouse
        if (Input.GetKeyDown(KeyCode.T))
        {
            FindPlayerComponents();
            if (_playerMotor && Camera.main)
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                _playerMotor.transform.root.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0);
                Debug.Log($"Player teleported to {mouseWorldPos}");
            }
        }
        
        // Give/Remove Life
        if (Input.GetKeyDown(KeyCode.L))
        {
            FindPlayerComponents();
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                _playerHealth?.TakeDamage(9999); // A bit hacky way to remove a life
                Debug.Log("Removed 1 Life.");
            }
            else
            {
                _playerHealth?.HealLife(1);
                Debug.Log("Added 1 Life.");
            }
        }

        // Give/Remove Armor
        if (Input.GetKeyDown(KeyCode.K))
        {
            FindPlayerComponents();
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                _playerHealth?.TakeDamage(1);
                Debug.Log("Removed 1 Armor.");
            }
            else
            {
                _playerHealth?.HealArmor(1);
                Debug.Log("Added 1 Armor.");
            }
        }
    }

    private void HandleGameStateCheats()
    {
        // Complete Level
        if (Input.GetKeyDown(KeyCode.F10))
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            PlayerEvents.RaiseLevelCompleted(currentScene);
            Debug.Log($"Level '{currentScene}' marked as complete via debug command.");
        }

        // Unlock All Levels
        if (Input.GetKeyDown(KeyCode.U))
        {
            // We need to add this method to LevelProgressionManager
            LevelProgressionManager.Instance?.UnlockAllLevels();
            Debug.Log("All levels unlocked.");
        }

        // Reset Progress
        if (Input.GetKeyDown(KeyCode.R))
        {
            LevelProgressionManager.Instance?.ResetAllProgression();
            Debug.Log("All level progression has been reset.");
        }

        // Slow-Motion
        if (Input.GetKeyDown(KeyCode.F11))
        {
            SetSlowMo(!_isSlowMoActive);
        }
    }

    private void HandleWeaponCheats()
    {
        if (weaponPrefabs == null || weaponPrefabs.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            _currentWeaponIndex++;
            if (_currentWeaponIndex >= weaponPrefabs.Count)
            {
                _currentWeaponIndex = 0;
            }
            EquipWeaponByIndex(_currentWeaponIndex);
        }
        
        if (Input.GetKeyDown(KeyCode.PageDown))
        {
            _currentWeaponIndex--;
            if (_currentWeaponIndex < 0)
            {
                _currentWeaponIndex = weaponPrefabs.Count - 1;
            }
            EquipWeaponByIndex(_currentWeaponIndex);
        }
    }

    private void EquipWeaponByIndex(int index)
    {
        FindPlayerComponents();
        if (_playerWeaponBase)
        {
            _playerWeaponBase.EquipUpgradeFromPrefab(weaponPrefabs[index]);
            Debug.Log($"Equipped weapon: {weaponPrefabs[index].name}");
        }
    }

    private void SetNoclip(bool active)
    {
        FindPlayerComponents();
        if (!_playerMotor) return;

        _isNoclipActive = active;
        _playerMotor.enabled = !active;

        if (_playerRb)
        {
            _playerRb.gravityScale = active ? 0f : 1f; // Use your default gravity scale
        }
        
        // Getting player colliders might be tricky. This is a simplified example.
        // A better way would be for PlayerStateManager to hold references to them.
        foreach (var col in _playerMotor.GetComponentsInParent<Collider2D>())
        {
            col.enabled = !active;
        }

        Debug.Log($"Noclip mode: {(_isNoclipActive ? "ON" : "OFF")}");
    }

    private void HandleNoclipMovement()
    {
        if (!_isNoclipActive || !_playerMotor) return;

        float horizontal = Input.GetKey(KeyCode.D) ? 1f : (Input.GetKey(KeyCode.A) ? -1f : 0f);
        float vertical = Input.GetKey(KeyCode.W) ? 1f : (Input.GetKey(KeyCode.S) ? -1f : 0f);

        Vector2 moveDirection = new Vector2(horizontal, vertical).normalized;
        _playerRb.linearVelocity = moveDirection * noclipSpeed;
    }

    private void SetSlowMo(bool active)
    {
        _isSlowMoActive = active;
        Time.timeScale = active ? 0.3f : 1.0f;
        Debug.Log($"Slow-motion: {(_isSlowMoActive ? "ON" : "OFF")}");
    }
}

#endif