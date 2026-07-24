using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// ScriptableObject для хранения настроек и эталонных данных уровня.
/// </summary>
[CreateAssetMenu(fileName = "NewCutLevelData", menuName = "Please Cut This/Cut Level Data", order = 1)]
public class CutLevelData : ScriptableObject
{
    [Header("Ресурсы")]
    [Tooltip("Видео-клип для этого уровня")]
    public VideoClip videoClip;

    [Tooltip("Reverse версия видео-клипа для перемотки назад")]
    public VideoClip reverseVideoClip;

    [Header("Эталонные интервалы")]
    [Tooltip("Отрезки видео, которые игрок должен вырезать для идеального прохождения")]
    public List<SkipInterval> targetIntervals = new List<SkipInterval>();
}
