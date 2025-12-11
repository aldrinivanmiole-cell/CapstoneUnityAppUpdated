using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TreasureHuntersEnding : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI DialogText;
    public GameObject nextButton;
    public GameObject exitButton;
    
    [Header("Character Display")]
    public Image characterImage;
    public Sprite teacherCharacter;
    
    [Header("Trophy Display")]
    public Image trophyImage;
    public Sprite goldTrophy;
    public Sprite silverTrophy;
    public Sprite bronzeTrophy;
    
    [Header("Animation")]
    public Animator trophyAnimator;
    
    [Header("Particle Effects")]
    public ParticleSystem sparkleEffect;
    public ParticleSystem glowEffect;
    
    [Header("Additional Visual Elements")]
    public Image flashOverlay;
    public GameObject[] floatingStars;
    
    private string[] perfectScoreDialog = {
        "AS THE FINAL TREASURE GLOWS IN YOUR HANDS, THE TEMPLE BEGINS TO TREMBLE!",
        "THE ANCIENT GUARDIANS EMERGE FROM THE SHADOWS, BOWING BEFORE YOU.",
        "NEVER IN A THOUSAND YEARS HAS AN EXPLORER CLAIMED EVERY TREASURE FLAWLESSLY!",
        "THE TEMPLE'S HEART OPENS, REVEALING THE LEGENDARY CRYSTAL OF INFINITE WISDOM.",
        "YOU ARE NOW A MASTER EXPLORER, YOUR NAME CARVED IN THE HALL OF LEGENDS.",
        "THE WORLD AWAITS YOUR NEXT ADVENTURE, CHAMPION OF KNOWLEDGE!"
    };
    
    private string[] excellentScoreDialog = {
        "THE TEMPLE DOORS SWING OPEN AS YOU CLUTCH YOUR TREASURES TIGHTLY.",
        "THE GUARDIANS NOD WITH APPROVAL - YOU HAVE PASSED THEIR TRIALS ADMIRABLY.",
        "THOUGH SOME ILLUSIONS DECEIVED YOU, YOUR WISDOM PROVED STRONG.",
        "A GOLDEN PATH APPEARS BENEATH YOUR FEET, LEADING TO GREATER MYSTERIES.",
        "THE SPIRITS WHISPER: 'RETURN WHEN YOU SEEK PERFECTION, BRAVE ONE.'",
        "YOUR JOURNEY AS A TREASURE HUNTER HAS ONLY JUST BEGUN!"
    };
    
    private string[] goodScoreDialog = {
        "YOU EMERGE FROM THE TEMPLE, TREASURES IN HAND, BUT THE PATH WAS TREACHEROUS.",
        "THE GUARDIANS WATCHED AS YOU STRUGGLED AGAINST THE ILLUSIONS AND RIDDLES.",
        "SOME TREASURES SLIPPED THROUGH YOUR FINGERS INTO THE DARKNESS BELOW.",
        "BUT YOU DID NOT FLEE! YOU STOOD YOUR GROUND UNTIL THE END.",
        "THE TEMPLE REMAINS OPEN FOR YOU, WAITING FOR YOUR TRIUMPHANT RETURN.",
        "SHARPEN YOUR MIND, YOUNG EXPLORER, AND CLAIM YOUR DESTINY!"
    };
    
    private string[] encouragementDialog = {
        "THE TEMPLE'S ILLUSIONS OVERWHELMED YOU, AND MANY TREASURES WERE LOST.",
        "YOU STUMBLE OUT INTO THE SUNLIGHT, EXHAUSTED BUT ALIVE.",
        "THE GUARDIANS DO NOT MOCK YOU - THEY SEE THE FIRE IN YOUR EYES.",
        "EVERY GREAT EXPLORER HAS FAILED BEFORE RISING TO GLORY.",
        "THE ANCIENT SCROLLS AWAIT IN THE LIBRARY - STUDY THEM WELL.",
        "WHEN YOU RETURN, YOU WILL BE STRONGER, WISER, AND UNSTOPPABLE!"
    };
    
    private string[] currentDialog;
    private int currentDialogIndex = 0;
    private string fulltext;
    private float letterPause = 0.05f;
    private bool isTyping = false;
    
    public static int FinalScore = 0;
    public static int TotalQuestions = 0;
    
    void Awake()
    {
        // Set teacher character
        if (characterImage != null && teacherCharacter != null)
        {
            characterImage.sprite = teacherCharacter;
        }
        
        // Select dialog based on score
        SelectDialogBasedOnScore();
        
        // Setup button listeners
        if (nextButton != null)
        {
            Button btnNext = nextButton.GetComponent<Button>();
            if (btnNext != null)
            {
                btnNext.onClick.AddListener(Next);
            }
        }
        
        if (exitButton != null)
        {
            exitButton.SetActive(false);
            Button btnExit = exitButton.GetComponent<Button>();
            if (btnExit != null)
            {
                btnExit.onClick.AddListener(ExitToMap);
            }
        }
        
        // Start first dialog
        StartCoroutine(Type());
        
        // Start trophy animation
        if (trophyAnimator != null)
        {
            StartCoroutine(AnimateTrophy());
        }
        
        // Start additional animations
        StartCoroutine(EntranceAnimation());
        StartCoroutine(AnimateFloatingStars());
    }
    
    void SelectDialogBasedOnScore()
    {
        float percentage = TotalQuestions > 0 ? (float)FinalScore / TotalQuestions * 100f : 0f;
        
        // Set trophy sprite based on score
        if (percentage == 100)
        {
            currentDialog = perfectScoreDialog;
            if (trophyImage != null && goldTrophy != null)
                trophyImage.sprite = goldTrophy;
        }
        else if (percentage >= 80)
        {
            currentDialog = excellentScoreDialog;
            if (trophyImage != null && goldTrophy != null)
                trophyImage.sprite = goldTrophy;
        }
        else if (percentage >= 60)
        {
            currentDialog = goodScoreDialog;
            if (trophyImage != null && silverTrophy != null)
                trophyImage.sprite = silverTrophy;
        }
        else
        {
            currentDialog = encouragementDialog;
            if (trophyImage != null && bronzeTrophy != null)
                trophyImage.sprite = bronzeTrophy;
        }
    }
    
    public void Next()
    {
        if (isTyping)
        {
            // Skip typing animation
            StopAllCoroutines();
            DialogText.text = fulltext;
            isTyping = false;
        }
        else
        {
            currentDialogIndex++;
            if (currentDialogIndex < currentDialog.Length)
            {
                StartCoroutine(Type());
            }
            else
            {
                // Show exit button instead of next
                if (nextButton != null)
                    nextButton.SetActive(false);
                if (exitButton != null)
                    exitButton.SetActive(true);
            }
        }
    }
    
    IEnumerator Type()
    {
        isTyping = true;
        fulltext = currentDialog[currentDialogIndex];
        DialogText.text = "";
        
        foreach (char c in fulltext.ToCharArray())
        {
            DialogText.text += c;
            yield return new WaitForSeconds(letterPause);
        }
        
        isTyping = false;
    }
    
    IEnumerator AnimateTrophy()
    {
        if (trophyImage == null) yield break;
        
        // Start with small scale
        trophyImage.transform.localScale = Vector3.zero;
        
        // Bounce in animation
        float duration = 0.8f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Bounce effect using sine wave
            float scale = Mathf.Sin(progress * Mathf.PI);
            if (progress > 0.5f)
                scale = 1f + (scale - 1f) * 0.2f; // Overshoot then settle
            
            trophyImage.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        trophyImage.transform.localScale = Vector3.one;
        
        // Continuous floating animation
        StartCoroutine(FloatTrophy());
    }
    
    IEnumerator FloatTrophy()
    {
        if (trophyImage == null) yield break;
        
        Vector3 originalPos = trophyImage.transform.localPosition;
        float floatSpeed = 1.5f;
        float floatAmount = 15f;
        
        while (true)
        {
            float newY = originalPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            trophyImage.transform.localPosition = new Vector3(originalPos.x, newY, originalPos.z);
            yield return null;
        }
    }
    
    void ExitToMap()
    {
        StartCoroutine(ExitAnimation());
    }
    
    IEnumerator ExitAnimation()
    {
        // Flash effect
        if (flashOverlay != null)
        {
            flashOverlay.gameObject.SetActive(true);
            Color flashColor = flashOverlay.color;
            
            // Fade to white
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                flashColor.a = Mathf.Lerp(0f, 1f, elapsed / duration);
                flashOverlay.color = flashColor;
                yield return null;
            }
        }
        
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("NEWMAP");
    }
    
    IEnumerator EntranceAnimation()
    {
        // Flash entrance effect
        if (flashOverlay != null)
        {
            flashOverlay.gameObject.SetActive(true);
            Color flashColor = Color.white;
            flashOverlay.color = flashColor;
            
            // Fade from white
            float duration = 1f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                flashColor.a = Mathf.Lerp(1f, 0f, elapsed / duration);
                flashOverlay.color = flashColor;
                yield return null;
            }
            
            flashOverlay.gameObject.SetActive(false);
        }
        
        // Trigger particles based on score
        float percentage = TotalQuestions > 0 ? (float)FinalScore / TotalQuestions * 100f : 0f;
        
        if (percentage >= 80 && sparkleEffect != null)
        {
            sparkleEffect.Play();
        }
        
        if (percentage == 100 && glowEffect != null)
        {
            glowEffect.Play();
        }
        
        // Character entrance slide
        if (characterImage != null)
        {
            Vector3 originalPos = characterImage.transform.localPosition;
            Vector3 startPos = originalPos - new Vector3(500f, 0f, 0f);
            characterImage.transform.localPosition = startPos;
            
            float duration = 0.8f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                characterImage.transform.localPosition = Vector3.Lerp(startPos, originalPos, progress);
                yield return null;
            }
            
            characterImage.transform.localPosition = originalPos;
        }
    }
    
    IEnumerator AnimateFloatingStars()
    {
        if (floatingStars == null || floatingStars.Length == 0) yield break;
        
        // Start each star invisible
        foreach (GameObject star in floatingStars)
        {
            if (star != null)
            {
                star.SetActive(false);
            }
        }
        
        yield return new WaitForSeconds(1.5f);
        
        // Animate each star appearing one by one
        foreach (GameObject star in floatingStars)
        {
            if (star != null)
            {
                star.SetActive(true);
                StartCoroutine(FloatStar(star));
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
    
    IEnumerator FloatStar(GameObject star)
    {
        if (star == null) yield break;
        
        Vector3 originalPos = star.transform.localPosition;
        float floatSpeed = Random.Range(1f, 2f);
        float floatAmount = Random.Range(20f, 40f);
        float rotateSpeed = Random.Range(20f, 50f);
        
        // Pop in animation
        star.transform.localScale = Vector3.zero;
        float popDuration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Sin((elapsed / popDuration) * Mathf.PI);
            star.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        star.transform.localScale = Vector3.one;
        
        // Continuous float and rotate
        float timeOffset = Random.Range(0f, 100f);
        while (true)
        {
            float newY = originalPos.y + Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatAmount;
            star.transform.localPosition = new Vector3(originalPos.x, newY, originalPos.z);
            star.transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
            yield return null;
        }
    }
}
