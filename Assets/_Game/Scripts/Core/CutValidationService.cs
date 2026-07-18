using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Сервис для расчета процента соответствия пользовательских вырезов эталонным (целевым).
/// </summary>
public class CutValidationService : MonoBehaviour
{
    [Header("Настройки уровня")]
    [Tooltip("Правильные отрезки, которые нужно было вырезать в этом видео")]
    public List<SkipInterval> targetIntervals = new List<SkipInterval>();

    [Header("Ссылки")]
    [Tooltip("Менеджер, в котором хранятся вырезы игрока")]
    public VideoCutManager playerCutManager;

    [Header("Настройки оценки")]
    [Tooltip("Допустимая погрешность (в секундах). Если игрок поставил маркер близко к идеалу, он засчитывается как идеальный.")]
    public double marginOfErrorSeconds = 0.5;

    [Tooltip("Штраф в процентах за каждую секунду лишнего вырезанного видео (мимо цели).")]
    public double falseCutPenaltyPerSecond = 5.0;

    /// <summary>
    /// Главный метод для получения процента соответствия (от 0 до 100).
    /// Вызовите его, когда уровень завершен.
    /// </summary>
    /// <returns>Процент успеха (0 - 100)</returns>
    public float GetMatchPercentage()
    {
        if (playerCutManager == null)
        {
            Debug.LogError("CutValidationService: Не назначен playerCutManager!");
            return 0f;
        }

        List<SkipInterval> rawPlayerIntervals = playerCutManager.intervalsToSkip;

        double totalTargetDuration = 0;
        double totalPlayerDuration = 0;
        double totalOverlapDuration = 0;

        // 1. Считаем общую длительность эталонных вырезов
        foreach (var target in targetIntervals)
        {
            totalTargetDuration += (target.endTime - target.startTime);
        }

        // Если вырезать вообще ничего не надо было
        if (totalTargetDuration <= 0)
        {
            // Если игрок тоже ничего не вырезал - 100%, иначе штрафуем за каждый вырез
            if (rawPlayerIntervals.Count == 0) return 100f;
            
            double extraDuration = 0;
            foreach (var p in rawPlayerIntervals) extraDuration += (p.endTime - p.startTime);
            return (float)Math.Clamp(100.0 - (extraDuration * falseCutPenaltyPerSecond), 0, 100);
        }

        // Обрабатываем вырезы игрока с учетом погрешности (Snap)
        List<SkipInterval> snappedPlayerIntervals = ApplyMarginOfErrorToIntervals(rawPlayerIntervals, targetIntervals);

        // 2. Считаем длительность того, что ВЫРЕЗАЛ игрок (уже с учетом погрешности)
        foreach (var playerCut in snappedPlayerIntervals)
        {
            totalPlayerDuration += (playerCut.endTime - playerCut.startTime);
        }

        // 3. Считаем чистое ПЕРЕСЕЧЕНИЕ (попадания)
        foreach (var target in targetIntervals)
        {
            foreach (var playerCut in snappedPlayerIntervals)
            {
                double overlapStart = Math.Max(target.startTime, playerCut.startTime);
                double overlapEnd = Math.Min(target.endTime, playerCut.endTime);

                // Если есть пересечение
                if (overlapEnd > overlapStart)
                {
                    totalOverlapDuration += (overlapEnd - overlapStart);
                }
            }
        }

        // Ограничиваем overlap сверху (чтобы из-за наложений не было > 100%)
        totalOverlapDuration = Math.Min(totalOverlapDuration, totalTargetDuration);

        // 4. Полнота: какую долю от нужного мы перекрыли (от 0 до 1)
        double recall = totalOverlapDuration / totalTargetDuration;

        // 5. Штраф за лишнее: сколько секунд игрок отрезал мимо цели
        double falseCutsDuration = totalPlayerDuration - totalOverlapDuration;
        if (falseCutsDuration < 0) falseCutsDuration = 0; // Защита от отрицательных значений
        
        double penalty = falseCutsDuration * falseCutPenaltyPerSecond; 

        // Итоговый счет: (Процент попадания) - (Штрафные проценты)
        double finalScorePercent = (recall * 100.0) - penalty;

        // Ограничиваем от 0 до 100
        return (float)Math.Clamp(finalScorePercent, 0, 100);
    }

    /// <summary>
    /// "Примагничивает" края отрезков игрока к эталонным, если они находятся в пределах погрешности.
    /// </summary>
    private List<SkipInterval> ApplyMarginOfErrorToIntervals(List<SkipInterval> playerIntervals, List<SkipInterval> targets)
    {
        List<SkipInterval> snappedIntervals = new List<SkipInterval>();

        foreach (var pInterval in playerIntervals)
        {
            double newStart = pInterval.startTime;
            double newEnd = pInterval.endTime;

            foreach (var tInterval in targets)
            {
                // Проверяем старт
                if (Math.Abs(pInterval.startTime - tInterval.startTime) <= marginOfErrorSeconds)
                {
                    newStart = tInterval.startTime;
                }
                
                // Проверяем конец
                if (Math.Abs(pInterval.endTime - tInterval.endTime) <= marginOfErrorSeconds)
                {
                    newEnd = tInterval.endTime;
                }
            }

            // Убеждаемся, что старт не стал больше конца из-за примагничивания
            if (newStart < newEnd)
            {
                snappedIntervals.Add(new SkipInterval { startTime = newStart, endTime = newEnd });
            }
            else
            {
                // Если после примагничивания отрезок вывернулся наизнанку, оставляем как есть
                snappedIntervals.Add(pInterval);
            }
        }

        return snappedIntervals;
    }

    /// <summary>
    /// Метод для удобного вызова из кнопок UI (выводит результат в консоль)
    /// </summary>
    public void DebugPrintMatchPercentage()
    {
        float score = GetMatchPercentage();
        Debug.Log($"<color=green>Итоговый счет соответствия: {score:F1}%</color>");
    }
}
