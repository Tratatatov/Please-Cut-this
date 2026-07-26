using UnityEngine;
using TMPro;

namespace GamePlay.View
{
    public class EndDayStatsUIView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _statsPanel;
        [SerializeField] private TMP_Text _accuracyText;

        public void Initialize()
        {
            if (_statsPanel != null)
            {
                _statsPanel.SetActive(false);
            }
        }

        public void ShowStats(float totalAccuracy)
        {
            if (_statsPanel != null)
            {
                _statsPanel.SetActive(true);
            }
            
            if (_accuracyText != null)
            {
                _accuracyText.text = $"Точность всех отрезков: {totalAccuracy:F1}%";
            }
            
            Debug.Log($"<color=green>[EndDayStatsUIView]</color> Показ финальной статистики: {totalAccuracy:F1}%");
        }
    }
}
