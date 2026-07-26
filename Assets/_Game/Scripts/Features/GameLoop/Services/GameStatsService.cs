using System.Collections.Generic;
using Core.Services;

namespace GamePlay.Services
{
    public class GameStatsService : IInitializable
    {
        private List<float> _tapeScores = new List<float>();

        public void Initialize()
        {
            _tapeScores.Clear();
        }

        public void AddTapeScore(float score)
        {
            _tapeScores.Add(score);
        }

        public float GetAverageScore()
        {
            if (_tapeScores.Count == 0) return 0f;
            
            float total = 0f;
            foreach (var score in _tapeScores)
            {
                total += score;
            }
            
            return total / _tapeScores.Count;
        }

        public int GetProcessedTapesCount()
        {
            return _tapeScores.Count;
        }
    }
}
