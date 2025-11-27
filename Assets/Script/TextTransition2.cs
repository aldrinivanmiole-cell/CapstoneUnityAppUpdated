using System.Collections;
using UnityEngine;
using TMPro;

public class TextTransition2 : MonoBehaviour
{
    [Header("Text Settings")]
    public TMP_Text textObject;          
    [TextArea] public string firstSentence = "Welcome back!";
    [TextArea] public string secondSentence = "Let's begin learning!";
    public float delayBetweenSentences = 1.5f;

    [Header("Animation Settings")]
    public float typeSpeed = 0.05f;     
    public float fadeDuration = 1f;      
    public float slideDistance = 100f;   
    public float slideDuration = 0.5f;    

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

        textObject.text = "";
        textObject.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);

        StartCoroutine(PlayTransitionSequence());
    }

    IEnumerator PlayTransitionSequence()
    {
        // Play first sentence
        yield return StartCoroutine(AnimateText(firstSentence));

        // Short pause
        yield return new WaitForSeconds(delayBetweenSentences);

        // Play second sentence
        yield return StartCoroutine(AnimateText(secondSentence));
    }

    IEnumerator AnimateText(string sentence)
    {
        switch (transition)
        {
            case TransitionType.FadeIn:
                yield return StartCoroutine(FadeInText(sentence));
                break;
            case TransitionType.Typewriter:
                yield return StartCoroutine(TypeWriterEffect(sentence));
                break;
            case TransitionType.SlideIn:
                yield return StartCoroutine(SlideInText(sentence));
                break;
        }
    }

    IEnumerator FadeInText(string sentence)
    {
        textObject.text = sentence;
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / fadeDuration);
            textObject.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
    }

    IEnumerator TypeWriterEffect(string sentence)
    {
        textObject.text = "";
        textObject.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1);

        foreach (char c in sentence)
        {
            textObject.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    IEnumerator SlideInText(string sentence)
    {
        textObject.text = sentence;
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
