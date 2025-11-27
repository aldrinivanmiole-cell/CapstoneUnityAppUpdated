using System.Collections;
using UnityEngine;
using TMPro;

public class TextTransition : MonoBehaviour
{
    [Header("Text Settings")]
    public TMP_Text textObject;          // Assign your TMP_Text here
    [TextArea] public string fullText;   // The text to display
    public float delay = 0.05f;          // Delay between letters (for typewriter)
    public float fadeDuration = 1f;      // Duration for fade
    public float slideDistance = 100f;   // How far to slide from
    public float slideDuration = 0.5f;   // Duration of slide-in animation

    [Header("Transition Type")]
    public TransitionType transition = TransitionType.Typewriter;

    public enum TransitionType
    {
        FadeIn,
        Typewriter,
        SlideIn
    }

    private Vector3 originalPosition;
    private Color originalColor;

    void Start()
    {
        if (textObject == null)
            textObject = GetComponent<TMP_Text>();

        originalPosition = textObject.rectTransform.anchoredPosition;
        originalColor = textObject.color;

        // Reset before showing
        textObject.text = "";
        textObject.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);

        // Start the animation
        switch (transition)
        {
            case TransitionType.FadeIn:
                StartCoroutine(FadeInText());
                break;
            case TransitionType.Typewriter:
                StartCoroutine(TypeWriterEffect());
                break;
            case TransitionType.SlideIn:
                StartCoroutine(SlideInText());
                break;
        }
    }

    IEnumerator FadeInText()
    {
        textObject.text = fullText;
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / fadeDuration);
            textObject.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
    }

    IEnumerator TypeWriterEffect()
    {
        textObject.text = "";
        textObject.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1);

        foreach (char c in fullText)
        {
            textObject.text += c;
            yield return new WaitForSeconds(delay);
        }
    }

    IEnumerator SlideInText()
    {
        textObject.text = fullText;
        textObject.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1);

        Vector3 startPos = originalPosition - new Vector3(slideDistance, 0, 0);
        textObject.rectTransform.anchoredPosition = startPos;

        float t = 0;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / slideDuration);
            textObject.rectTransform.anchoredPosition = Vector3.Lerp(startPos, originalPosition, normalized);
            yield return null;
        }
    }
}
