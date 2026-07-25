using System;
using UnityEngine;

// Структура для хранения вырезанного интервала
[Serializable]
public struct SkipInterval
{
    [Tooltip("Время начала вырезанного куска (в секундах)")]
    public double startTime;

    [Tooltip("Время конца вырезанного куска (в секундах)")]
    public double endTime;
}
