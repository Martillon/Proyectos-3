// --- File: AnimatedMenuButton.cs ---
using UnityEngine;
using UnityEngine.EventSystems; // Required for all the UI event interfaces
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// A "smart" UI button component that handles visual state changes for selection,
/// deselection, and presses. It controls a selection indicator animation and text color.
/// It unifies behavior for both mouse and gamepad/keyboard input.
/// </summary>
[RequireComponent(typeof(Button))]
public class AnimatedMenuButton : MonoBehaviour,
    ISelectHandler, IDeselectHandler,           // For keyboard/gamepad navigation
    IPointerEnterHandler, IPointerExitHandler,  // For mouse hover
    IPointerDownHandler, IPointerUpHandler,     // For mouse clicks
    ISubmitHandler                              // For gamepad/keyboard confirm button
{
    [Header("UI References")]
    [Tooltip("The RectTransform of the selection arrow child object that will be animated.")]
    [SerializeField] private RectTransform selectionArrow;
    [Tooltip("The TextMeshPro UGUI component for this button's label.")]
    [SerializeField] private TMP_Text labelText;

    [Header("State Colors")]
    [Tooltip("The color of the text when this button is selected or hovered over.")]
    [SerializeField] private Color selectedColor = Color.cyan;
    [Tooltip("The color of the text when this button is not selected.")]
    [SerializeField] private Color deselectedColor = new Color(1, 0.5f, 0); // Default orange-red
    [Tooltip("The color of the text when the button is being actively pressed down.")]
    [SerializeField] private Color pressedColor = Color.yellow;
    [Tooltip("The color of the text when the button is disabled (not interactable).")]
    [SerializeField] private Color disabledColor = Color.grey;
    
    [Header("Arrow Animation (Script-based)")]
    [Tooltip("How far the arrow moves from its starting point, in canvas units.")]
    [SerializeField] private float arrowMoveDistance = 10f;
    [Tooltip("How fast the arrow moves back and forth in its animation loop.")]
    [SerializeField] private float arrowMoveSpeed = 5f;

    private Button _button;
    private Coroutine _arrowAnimationCoroutine;
    private Vector2 _arrowInitialPosition;
    private bool _isCurrentlySelected = false;

    private void Awake()
    {
        _button = GetComponent<Button>();
        
        // Find components automatically if not assigned, for convenience.
        if (labelText == null) labelText = GetComponentInChildren<TMP_Text>();
        if (selectionArrow != null)
        {
            _arrowInitialPosition = selectionArrow.anchoredPosition;
        }

        // Validate that all necessary parts are present.
        if (selectionArrow == null) Debug.LogError($"Button '{name}' is missing its Selection Arrow reference!", this);
        if (labelText == null) Debug.LogError($"Button '{name}' is missing its Label Text reference!", this);
    }
    
    private void OnEnable()
    {
        // When this button becomes active, immediately update its visuals to reflect
        // the current state of the EventSystem.
        _isCurrentlySelected = EventSystem.current.currentSelectedGameObject == this.gameObject;
        UpdateVisuals();
    }

    // --- EventSystem Interface Implementations ---

    // Called when this button is selected via keyboard/gamepad OR when the mouse enters.
    public void OnSelect(BaseEventData eventData)
    {
        _isCurrentlySelected = true;
        UpdateVisuals();
    }
    
    // Called when this button is deselected.
    public void OnDeselect(BaseEventData eventData) 
    {
        _isCurrentlySelected = false;
        UpdateVisuals();
    }
    
    // Called when the mouse cursor starts hovering over the button.
    public void OnPointerEnter(PointerEventData eventData)
    {
        // To unify controls, we tell the EventSystem to officially "select" this button
        // when the mouse hovers over it. This will automatically trigger OnSelect.
        if (_button.interactable)
        {
            EventSystem.current.SetSelectedGameObject(this.gameObject);
        }
    }

    // Called when the mouse cursor stops hovering over the button.
    public void OnPointerExit(PointerEventData eventData)
    {
        // We only deselect the button if the mouse leaves it. This prevents the gamepad
        // selection from being cleared just because the mouse moved.
        if (EventSystem.current.currentSelectedGameObject == this.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // Called when the mouse button is pressed down over the button.
    public void OnPointerDown(PointerEventData eventData) => SetPressedVisuals(true);

    // Called when the mouse button is released over the button.
    public void OnPointerUp(PointerEventData eventData) => SetPressedVisuals(false);

    // Called when the "Submit" action is used on this button (e.g., Enter key, 'A' on gamepad).
    public void OnSubmit(BaseEventData eventData) => SetPressedVisuals(true);
    

    /// <summary>
    /// The main method for updating the button's appearance based on its current state.
    /// </summary>
    private void UpdateVisuals()
    {
        if (labelText == null || selectionArrow == null) return;
        
        // Handle Text Color based on selection state
        labelText.color = _isCurrentlySelected ? selectedColor : deselectedColor;
        
        // Handle Arrow State and Animation
        if (_isCurrentlySelected)
        {
            // If selected, show the arrow and start the animation if it's not already running.
            selectionArrow.gameObject.SetActive(true);
            if (_arrowAnimationCoroutine == null)
            {
                _arrowAnimationCoroutine = StartCoroutine(AnimateArrowRoutine());
            }
        }
        else
        {
            // If deselected, stop the animation and hide the arrow.
            selectionArrow.gameObject.SetActive(false);
            if (_arrowAnimationCoroutine != null)
            {
                StopCoroutine(_arrowAnimationCoroutine);
                _arrowAnimationCoroutine = null;
            }
            // Reset position when hidden
            selectionArrow.anchoredPosition = _arrowInitialPosition;
        }
        
        // Disabled state overrides all others.
        if (!_button.interactable)
        {
            labelText.color = disabledColor;
            selectionArrow.gameObject.SetActive(false);
            if (_arrowAnimationCoroutine != null)
            {
                StopCoroutine(_arrowAnimationCoroutine);
                _arrowAnimationCoroutine = null;
            }
        }
    }
    
    private void SetPressedVisuals(bool isPressed)
    {
        if (!_button.interactable || labelText == null) return;
        labelText.color = isPressed ? pressedColor : selectedColor;
    }

    private void StartArrowAnimation()
    {
        if (_arrowAnimationCoroutine != null) StopCoroutine(_arrowAnimationCoroutine);
        _arrowAnimationCoroutine = StartCoroutine(AnimateArrowRoutine());
    }

    private void StopArrowAnimation()
    {
        if (_arrowAnimationCoroutine != null)
        {
            StopCoroutine(_arrowAnimationCoroutine);
            _arrowAnimationCoroutine = null;
        }
        selectionArrow.anchoredPosition = _arrowInitialPosition; // Reset position
    }

    private IEnumerator AnimateArrowRoutine()
    {
        float time = 0;
        
        // The loop now has a condition: it only runs while this button is selected.
        while (_isCurrentlySelected)
        {
            time += Time.unscaledDeltaTime * arrowMoveSpeed;
            float xOffset = Mathf.PingPong(time, arrowMoveDistance);
            selectionArrow.anchoredPosition = new Vector2(_arrowInitialPosition.x + xOffset, _arrowInitialPosition.y);
            
            // yield return null tells the coroutine to pause here and continue on the next frame.
            yield return null;
        }

        // When the loop finishes (because _isCurrentlySelected became false),
        // the coroutine will naturally end. We clean up the reference.
        _arrowAnimationCoroutine = null;
    }
}