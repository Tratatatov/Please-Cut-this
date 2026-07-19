using UnityEngine;

public class CutLevelMediator : IInitializable
{
    private VideoPlayerManager _videoPlayerManager;
    private VideoCutManager _videoCutManager;
    private CutValidationService _cutValidationService;
    private VideoCutVisualizer _videoCutVisualizer;

    public CutLevelData CurrentLevelData { get; private set; }

    public void Initialize()
    {
        _videoPlayerManager = ServiceLocator.Get<VideoPlayerManager>();
        _videoCutManager = ServiceLocator.Get<VideoCutManager>();
        _cutValidationService = ServiceLocator.Get<CutValidationService>();
        _videoCutVisualizer = ServiceLocator.Get<VideoCutVisualizer>();
    }

    public void LoadLevel(CutLevelData levelData)
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
            Debug.LogWarning("CutLevelMediator: VideoPlayerManager или его VideoPlayer не найдены!");
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
