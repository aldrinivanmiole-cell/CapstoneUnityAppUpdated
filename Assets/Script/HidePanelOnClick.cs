using UnityEngine;
using UnityEngine.EventSystems;

public class HidePanelOnClick : MonoBehaviour, IPointerClickHandler
{
    [Header("Assign the panel to show/hide")]
    public GameObject panelToHide;

    [Header("Show mode")]
    public bool toggle = true; // if true, clicking again will hide the panel

    // ✅ This triggers when the image is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        if (panelToHide == null) return;

        if (toggle)
            panelToHide.SetActive(!panelToHide.activeSelf);
        else
            panelToHide.SetActive(false);

        Debug.Log("🟦 Image clicked! Panel visibility: " + panelToHide.activeSelf);
    }
}
