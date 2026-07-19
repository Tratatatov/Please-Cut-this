using System;
using System.Collections.Generic;
using UnityEngine;

public class CutValidationService : IInitializable
{
    public List<SkipInterval> targetIntervals = new List<SkipInterval>();

    private VideoCutManager _playerCutManager;

    public double marginOfErrorSeconds = 0.5;
    public double falseCutPenaltyPerSecond = 5.0;

    public void Initialize()
    {
        _playerCutManager = ServiceLocator.Get<VideoCutManager>();
        if (_playerCutManager == null)
        {
            Debug.LogError("CutValidationService: VideoCutManager не найден в ServiceLocator!");
        }
    }

    public float GetMatchPercentage()
    {
        if (_playerCutManager == null) return 0f;

        List<SkipInterval> rawPlayerIntervals = _playerCutManager.intervalsToSkip;

        double totalTargetDuration = 0;
        double totalPlayerDuration = 0;
        double totalOverlapDuration = 0;

        foreach (var target in targetIntervals)
        {
            totalTargetDuration += (target.endTime - target.startTime);
        }

        if (totalTargetDuration <= 0)
        {
            if (rawPlayerIntervals.Count == 0) return 100f;

            double extraDuration = 0;
            foreach (var p in rawPlayerIntervals) extraDuration += (p.endTime - p.startTime);
            return (float)Math.Clamp(100.0 - (extraDuration * falseCutPenaltyPerSecond), 0, 100);
        }

        List<SkipInterval> snappedPlayerIntervals = ApplyMarginOfErrorToIntervals(rawPlayerIntervals, targetIntervals);

        foreach (var playerCut in snappedPlayerIntervals)
        {
            totalPlayerDuration += (playerCut.endTime - playerCut.startTime);
        }

        foreach (var target in targetIntervals)
        {
            foreach (var playerCut in snappedPlayerIntervals)
            {
                double overlapStart = Math.Max(target.startTime, playerCut.startTime);
                double overlapEnd = Math.Min(target.endTime, playerCut.endTime);

                if (overlapEnd > overlapStart)
                {
                    totalOverlapDuration += (overlapEnd - overlapStart);
                }
            }
        }

        totalOverlapDuration = Math.Min(totalOverlapDuration, totalTargetDuration);

        double recall = totalOverlapDuration / totalTargetDuration;

        double falseCutsDuration = totalPlayerDuration - totalOverlapDuration;
        if (falseCutsDuration < 0) falseCutsDuration = 0;

        double penalty = falseCutsDuration * falseCutPenaltyPerSecond;

        double finalScorePercent = (recall * 100.0) - penalty;

        return (float)Math.Clamp(finalScorePercent, 0, 100);
    }

    private List<SkipInterval> ApplyMarginOfErrorToIntervals(List<SkipInterval> playerIntervals, List<SkipInterval> targets)
    {
        List<SkipInterval> snappedIntervals = new List<SkipInterval>();

        foreach (var pInterval in playerIntervals)
        {
            double newStart = pInterval.startTime;
            double newEnd = pInterval.endTime;

            foreach (var tInterval in targets)
            {
                if (Math.Abs(pInterval.startTime - tInterval.startTime) <= marginOfErrorSeconds)
                {
                    newStart = tInterval.startTime;
                }

                if (Math.Abs(pInterval.endTime - tInterval.endTime) <= marginOfErrorSeconds)
                {
                    newEnd = tInterval.endTime;
                }
            }

            if (newStart < newEnd)
            {
                snappedIntervals.Add(new SkipInterval { startTime = newStart, endTime = newEnd });
            }
            else
            {
                snappedIntervals.Add(pInterval);
            }
        }

        return snappedIntervals;
    }

    public void DebugPrintMatchPercentage()
    {
        float score = GetMatchPercentage();
        Debug.Log($"<color=green>Итоговый счет соответствия: {score:F1}%</color>");
    }
}
