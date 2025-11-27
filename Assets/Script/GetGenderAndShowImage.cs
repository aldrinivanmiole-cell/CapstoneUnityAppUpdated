using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GetGenderAndShowImage : MonoBehaviour
{
    public Image maleImage;
    public Image femaleImage;

    void Start()
    {   
        StartCoroutine(ShowGenderImageWhenReady());
    }

    IEnumerator ShowGenderImageWhenReady()
    {
        // Wait until SessionManager is ready and has gender data
        yield return new WaitUntil(() => SessionManager.Instance != null && 
                                        !string.IsNullOrEmpty(SessionManager.Instance.gender));

        string gender = SessionManager.Instance.gender.ToLower();
        Debug.Log("🧠 Gender from session: " + gender);

        maleImage.gameObject.SetActive(gender == "male");
        femaleImage.gameObject.SetActive(gender == "female");
    }
}
