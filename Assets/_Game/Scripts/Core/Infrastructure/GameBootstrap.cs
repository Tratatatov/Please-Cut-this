using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameBootstrap : MonoBehaviour
{
    [Header("Настройки уровня (по умолчанию)")]
    public CutLevelData defaultLevel;

    [Header("Сцена: Плееры")]
    [UnityEngine.Serialization.FormerlySerializedAs("videoPlayer")]
    public VideoPlayer forwardPlayer;
    public VideoPlayer reversePlayer;

    [Header("Сцена: Отображение")]
    public RawImage displayImage;
    public Renderer displayRenderer;
    public string materialTextureProperty = "_MainTex";

    [Header("Сцена: UI таймлайна")]
    public VideoTimelineUIView timelineView;
    public RectTransform markerContainer;
    public RectTransform markerPrefab;
    public Button deleteSelectedCutButton;

    private List<IInitializable> _initializables = new List<IInitializable>();
    private List<IUpdatable> _updatables = new List<IUpdatable>();
    private List<IDisposableService> _disposables = new List<IDisposableService>();

    private void Awake()
    {
        ServiceLocator.Clear();

        // 1. Создание сервисов (обычные классы C#)
        var playerManager = new VideoPlayerManager(forwardPlayer, reversePlayer, displayImage, displayRenderer, materialTextureProperty);
        var cutManager = new VideoCutManager();
        var validationService = new CutValidationService();
        var cutVisualizer = new VideoCutVisualizer(markerContainer, markerPrefab, deleteSelectedCutButton);
        var levelMediator = new CutLevelMediator();
        var timelineLogic = new VideoTimelineUILogic(timelineView, playerManager, cutManager);

        var gameStateManager = new GameStateManager();
        gameStateManager.RegisterState(new MontageGameState());
        gameStateManager.RegisterState(new ClientDialogueGameState());

        // 2. Регистрация в Service Locator
        ServiceLocator.Register(playerManager);
        ServiceLocator.Register(cutManager);
        ServiceLocator.Register(validationService);
        ServiceLocator.Register(cutVisualizer);
        ServiceLocator.Register(levelMediator);
        ServiceLocator.Register(timelineLogic);
        ServiceLocator.Register(gameStateManager);

        // Добавляем в списки для вызова жизненного цикла
        AddService(playerManager);
        AddService(cutManager);
        AddService(validationService);
        AddService(cutVisualizer);
        AddService(levelMediator);
        AddService(timelineLogic);
        AddService(gameStateManager);

        // 3. Вызов Initialize() для каждого сервиса
        foreach (var init in _initializables)
        {
            init.Initialize();
        }

        // 4. Запуск дефолтного уровня
        if (defaultLevel != null)
        {
            levelMediator.LoadLevel(defaultLevel);
        }
        else
        {
            Debug.LogWarning("GameBootstrap: Дефолтный уровень (CutLevelData) не назначен в инспекторе!");
        }

        // 5. Установка начального состояния
        gameStateManager.SwitchState<MontageGameState>();
    }

    private void AddService(object service)
    {
        if (service is IInitializable init) _initializables.Add(init);
        if (service is IUpdatable update) _updatables.Add(update);
        if (service is IDisposableService disp) _disposables.Add(disp);
    }

    private void Update()
    {
        foreach (var update in _updatables)
        {
            update.Update();
        }
    }

    private void OnDestroy()
    {
        foreach (var disp in _disposables)
        {
            disp.Dispose();
        }
        ServiceLocator.Clear();
    }
}
