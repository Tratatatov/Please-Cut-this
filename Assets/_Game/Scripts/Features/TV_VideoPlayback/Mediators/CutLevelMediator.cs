using UnityEngine;

public class CutLevelMediator : IInitializable
{
    private VideoPlayerService _videoPlayerManager;
    private VideoCutService _videoCutManager;
    private CutValidationService _cutValidationService;
    private VideoCutVisualizer _videoCutVisualizer;

    public VideotapeConfig CurrentLevelData { get; private set; }

    public void Initialize()
    {
        _videoPlayerManager = ServiceLocator.Get<VideoPlayerService>();
        _videoCutManager = ServiceLocator.Get<VideoCutService>();
        _cutValidationService = ServiceLocator.Get<CutValidationService>();
        _videoCutVisualizer = ServiceLocator.Get<VideoCutVisualizer>();
    }

    public void LoadLevel(VideotapeConfig levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("CutLevelMediator: Попытка загрузить пустые данные уровня (null)!");
            return;
        }

        CurrentLevelData = levelData;

        if (_videoPlayerManager != null)
        {
            _videoPlayerManager.LoadClips(levelData.videoClip, levelData.reverseVideoClip);
        }
        else
        {
            Debug.LogWarning("CutLevelMediator: VideoPlayerService или его VideoPlayer не найдены!");
        }

        if (_videoCutManager != null)
        {
            _videoCutManager.ClearAllCuts();
        }

        if (_cutValidationService != null)
        {
            _cutValidationService.targetIntervals = new System.Collections.Generic.List<SkipInterval>(levelData.targetIntervals);
        }

        if (_videoCutVisualizer != null)
        {
            _videoCutVisualizer.UpdateVisuals();
        }

        Debug.Log($"CutLevelMediator: Уровень '{levelData.name}' успешно загружен.");
    }
}
