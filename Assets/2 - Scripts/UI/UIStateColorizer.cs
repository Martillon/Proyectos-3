// --- File: UIStateColorizer.cs ---

using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class UIStateColorizer : MonoBehaviour
{
    [Header("Target Text Elements")]
    // This is the key part that answers your question.
    // This single script holds a LIST of all the text objects it will control.
    // So, you add one UIStateColorizer to your prefab, and then you drag
    // all three of your text objects (Title, Reward, Description) into this list.
    [Tooltip("A list of all TextMeshPro components whose color will be changed by this script.")]
    [SerializeField] private List<TMP_Text> targetTexts;

    [Header("State Colors")]
    [SerializeField] private Color normalColor = Color.magenta;
    [SerializeField] private Color highlightedColor = Color.magenta;
    [SerializeField] private Color lockedColor = new Color(128,118,241);

    // This private helper method loops through the 'targetTexts' list
    // and applies the same color to every text object in it.
    private void ApplyColor(Color colorToApply)
    {
        if (targetTexts == null) return;
        
        foreach (var text in targetTexts)
        {
            if (text != null)
            {
                text.color = colorToApply;
            }
        }
    }

    // --- PUBLIC METHODS ---
    // When one of these methods is called, it triggers the ApplyColor
    // method, which updates all the texts in the list at once.
    public void SetNormalState() => ApplyColor(normalColor);
    public void SetHighlightedState() => ApplyColor(highlightedColor);
    public void SetLockedState() => ApplyColor(lockedColor);
    // The "Completed" state reuses the normal color.
    public void SetCompletedState() => ApplyColor(normalColor);
}