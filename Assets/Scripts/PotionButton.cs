using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PotionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Settings")]
    public Image potionImage;
    public Transform potionTransform;
    public float hoverScale = 1.1f;
    public float animationSpeed = 5f;
    
    private Vector3 originalScale;
    private bool isHovered = false;

    void Start()
    {
        if (potionTransform == null)
            potionTransform = transform;
        
        originalScale = potionTransform.localScale;
    }

    void Update()
    {
        // Smooth scale animation
        Vector3 targetScale = isHovered ? originalScale * hoverScale : originalScale;
        potionTransform.localScale = Vector3.Lerp(potionTransform.localScale, targetScale, Time.deltaTime * animationSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    public void DisableInteraction()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.interactable = false;
        
        isHovered = false;
    }

    public void EnableInteraction()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.interactable = true;
    }
}
