using UnityEngine;
using UnityEngine.UI;

public class ClosePanel : MonoBehaviour
{
    [Header("Assign the Panel you want to close")]
    public GameObject panelToClose;

    [Header("Assign the Close Button")]
    public Button closeButton;

    void Start()
    {
        // Make sure both references are set
        if (closeButton != null && panelToClose != null)
        {
            closeButton.onClick.AddListener(CloseThisPanel);
        }
        else
        {
            Debug.LogWarning("⚠️ ClosePanel: Missing references in the Inspector!");
        }
    }

    void CloseThisPanel()
    {
        if (panelToClose != null)
        {
            panelToClose.SetActive(false);
        }
    }
}
