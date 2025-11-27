using UnityEngine;
using UnityEngine.EventSystems;

public class ShowPanelOnClick : MonoBehaviour, IPointerClickHandler
{
    [Header("Assign the panel to show/hide")]
    public GameObject panelToShow;

    [Header("Show mode")]
    public bool toggle = true; // if true, clicking again will hide the panel

    void Start()
    {
        if (panelToShow != null)
        {
            panelToShow.SetActive(false); // hide on start
        }
    }

    // ✅ This triggers when the image is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        if (panelToShow == null) return;

        if (toggle)
            panelToShow.SetActive(!panelToShow.activeSelf);
        else
            panelToShow.SetActive(true);

        Debug.Log("🟦 Image clicked! Panel visibility: " + panelToShow.activeSelf);
    }
}
