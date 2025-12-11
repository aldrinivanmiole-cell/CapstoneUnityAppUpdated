using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CauldronController : MonoBehaviour
{
    [Header("Visual Settings")]
    public Image cauldronImage;
    public Transform cauldronTransform;
    
    [Header("Idle Animation")]
    public bool enableIdleAnimation = true;
    public float idleBobSpeed = 1f;
    public float idleBobAmount = 0.05f;
    
    private Vector3 originalPosition;
    private float idleTimer = 0f;

    void Start()
    {
        if (cauldronTransform == null)
            cauldronTransform = transform;
        
        originalPosition = cauldronTransform.localPosition;
    }

    void Update()
    {
        if (enableIdleAnimation)
        {
            IdleAnimation();
        }
    }

    void IdleAnimation()
    {
        idleTimer += Time.deltaTime * idleBobSpeed;
        
        float yOffset = Mathf.Sin(idleTimer) * idleBobAmount;
        cauldronTransform.localPosition = originalPosition + new Vector3(0, yOffset, 0);
    }

    public void PlaySuccessAnimation()
    {
        StartCoroutine(SuccessAnimationRoutine());
    }

    public void PlayFailureAnimation()
    {
        StartCoroutine(FailureAnimationRoutine());
    }

    IEnumerator SuccessAnimationRoutine()
    {
        // Simple scale up animation
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = cauldronTransform.localScale;
        Vector3 targetScale = startScale * 1.2f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cauldronTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        // Scale back
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cauldronTransform.localScale = Vector3.Lerp(targetScale, startScale, t);
            yield return null;
        }
        
        cauldronTransform.localScale = startScale;
    }

    IEnumerator FailureAnimationRoutine()
    {
        // Simple shake animation
        float duration = 0.5f;
        float elapsed = 0f;
        float shakeAmount = 10f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            float xOffset = Random.Range(-shakeAmount, shakeAmount);
            cauldronTransform.localPosition = originalPosition + new Vector3(xOffset, 0, 0);
            
            yield return null;
        }
        
        cauldronTransform.localPosition = originalPosition;
    }
}
