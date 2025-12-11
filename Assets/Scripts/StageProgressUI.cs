using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StageProgressUI : MonoBehaviour
{
    [System.Serializable]
    public class StageIndicator
    {
        public GameObject indicatorObject;
        public Image fillImage;
        public TextMeshProUGUI stageNameText;
        public Color completedColor = Color.green;
        public Color currentColor = Color.yellow;
        public Color uncompletedColor = Color.gray;
    }

    [Header("Stage Indicators")]
    public List<StageIndicator> stageIndicators = new List<StageIndicator>();
    
    [Header("Progress Bar")]
    public Image overallProgressBar;
    public TextMeshProUGUI progressText;

    public void InitializeStages(int stageCount)
    {
        // Ensure we have enough indicators
        while (stageIndicators.Count < stageCount)
        {
            stageIndicators.Add(new StageIndicator());
        }
        
        // Hide excess indicators
        for (int i = stageCount; i < stageIndicators.Count; i++)
        {
            if (stageIndicators[i].indicatorObject != null)
                stageIndicators[i].indicatorObject.SetActive(false);
        }
        
        // Show and reset needed indicators
        for (int i = 0; i < stageCount; i++)
        {
            if (stageIndicators[i].indicatorObject != null)
            {
                stageIndicators[i].indicatorObject.SetActive(true);
                UpdateStageIndicator(i, false, false);
            }
        }
    }

    public void UpdateStageIndicator(int stageIndex, bool isCompleted, bool isCurrent)
    {
        if (stageIndex < 0 || stageIndex >= stageIndicators.Count)
            return;
        
        StageIndicator indicator = stageIndicators[stageIndex];
        
        if (indicator.fillImage != null)
        {
            if (isCompleted)
                indicator.fillImage.color = indicator.completedColor;
            else if (isCurrent)
                indicator.fillImage.color = indicator.currentColor;
            else
                indicator.fillImage.color = indicator.uncompletedColor;
            
            indicator.fillImage.fillAmount = isCompleted ? 1f : (isCurrent ? 0.5f : 0f);
        }
    }

    public void UpdateOverallProgress(int currentQuestion, int totalQuestions)
    {
        float progress = totalQuestions > 0 ? (float)currentQuestion / totalQuestions : 0f;
        
        if (overallProgressBar != null)
            overallProgressBar.fillAmount = progress;
        
        if (progressText != null)
            progressText.text = $"{currentQuestion}/{totalQuestions}";
    }

    public void SetStageComplete(int stageIndex)
    {
        UpdateStageIndicator(stageIndex, true, false);
    }

    public void SetCurrentStage(int stageIndex)
    {
        // Reset all to uncompleted/uncurrent first
        for (int i = 0; i < stageIndicators.Count; i++)
        {
            if (i < stageIndex)
                UpdateStageIndicator(i, true, false); // Previous stages are completed
            else if (i == stageIndex)
                UpdateStageIndicator(i, false, true); // Current stage
            else
                UpdateStageIndicator(i, false, false); // Future stages
        }
    }
}
