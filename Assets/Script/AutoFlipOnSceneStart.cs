using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AutoFlipOnSceneStart : MonoBehaviour
{
    public RectTransform targetImage;   // assign your UI Image here
    public float flipDelay = 0.5f;      // time before flip starts
    public float flipDuration = 0.5f;   // speed of animation

    void Start()
    {
        StartCoroutine(FlipAnimation());
    }

    IEnumerator FlipAnimation()
    {
        // small delay before flip
        yield return new WaitForSeconds(flipDelay);

        float time = 0f;
        float startAngle = 0f;
        float endAngle = 180f;

        while (time < flipDuration)
        {
            time += Time.deltaTime;
            float angle = Mathf.Lerp(startAngle, endAngle, time / flipDuration);

            // Flip animation on Y axis
            targetImage.localRotation = Quaternion.Euler(0, angle, 0);

            yield return null;
        }

        // ensure final angle
        targetImage.localRotation = Quaternion.Euler(0, 180, 0);
    }
}
