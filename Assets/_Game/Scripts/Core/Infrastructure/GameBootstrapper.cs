using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Cinemachine;
using Core.Services;

public class GameBootstrapper : MonoBehaviour
{
    [Header("Расписание дня")]
    public GamePlay.Data.DayScheduleSO todaySchedule;

    [Header("Сцена: Клиент")]
    public GamePlay.View.ClientView clientView;
    public Transform clientRoot;
    public Transform spawnPoint;
    public Transform intermediatePoint;
    public Transform deskPoint;
    public Transform exitPoint;
    public GamePlay.Data.ClientMovementConfigSO clientMovementConfig;

    [Header("Сцена: Плееры")]
    [UnityEngine.Serialization.FormerlySerializedAs("videoPlayer")]
    public VideoPlayer forwardPlayer;
    public VideoPlayer reversePlayer;

    [Header("Сцена: Отображение")]
    public GamePlay.View.TV tv;
    public Renderer displayRenderer;
    public string materialTextureProperty = "_MainTex";

    [Header("Сцена: UI диалогов")]
    public GameObject dialogueWindow;
    public TMPro.TMP_Text dialogueNameText;
    public TMPro.TMP_Text dialogueMessageText;

    [Header("Сцена: UI таймлайна")]
    public VideoTimelineUIView timelineView;
    public RectTransform markerContainer;
    public RectTransform markerPrefab;
    public Button deleteSelectedCutButton;

    [Header("Сцена: Настройки управления")]
    public GamePlay.Data.GameControlsConfigSO controlsConfig;

    [Header("Сцена: Камеры (Cinemachine)")]
    public CinemachineCamera mainCamera;
    public CinemachineCamera tvCamera;
    public CinemachineCamera clientCamera;
    public int activeCameraPriority = 10;
    public int inactiveCameraPriority = 0;

    private List<IInitializable> _initializables = new List<IInitializable>();
    private List<IUpdatable> _updatables = new List<IUpdatable>();
    private List<IDisposableService> _disposables = new List<IDisposableService>();

    private void Awake()
    {
        ServiceLocator.Clear();

        if (tv != null)
        {
            tv.Initialize();
        }
        TVRendererService tvService = tv != null ? tv.TVRendererService : null;

        // 1. Создание сервисов (обычные классы C#)
        var dialogueService = new DialogueService(dialogueNameText, dialogueMessageText, dialogueWindow);
        var playerManager = new VideoPlayerService(forwardPlayer, reversePlayer, displayRenderer, materialTextureProperty, tvService);
        var cutManager = new VideoCutService();
        var validationService = new CutValidationService();
        var cutVisualizer = new VideoCutVisualizer(markerContainer, markerPrefab, deleteSelectedCutButton);
        var levelMediator = new CutLevelMediator();
        var timelineLogic = new VideoTimelineUILogic(timelineView, playerManager, cutManager);
        var cameraControlService = new CameraControlService(mainCamera, tvCamera, clientCamera, activeCameraPriority, inactiveCameraPriority);
        var playerViewController = new GamePlay.Controllers.PlayerViewController(cameraControlService, controlsConfig);
        var clientBehaviorController = new GamePlay.Controllers.ClientBehaviorController(clientView, clientRoot, spawnPoint, intermediatePoint, deskPoint, exitPoint, clientMovementConfig);
        var clientsController = new GamePlay.Controllers.ClientsController(clientBehaviorController, clientView);
        var gameLoopController = new GamePlay.Controllers.GameLoopController(todaySchedule, clientView);
        var testGameManager = new GamePlay.Controllers.TestGameManager(todaySchedule, clientsController, playerViewController, controlsConfig);

        var gameStateManager = new GameStateManager();
        gameStateManager.RegisterState(new MontageGameState());
        gameStateManager.RegisterState(new ClientDialogueGameState());

        // 2. Регистрация в Service Locator
        ServiceLocator.Register(dialogueService);
        ServiceLocator.Register(playerManager);
        ServiceLocator.Register(cutManager);
        ServiceLocator.Register(validationService);
        ServiceLocator.Register(cutVisualizer);
        ServiceLocator.Register(levelMediator);
        ServiceLocator.Register(timelineLogic);
        ServiceLocator.Register(cameraControlService);
        ServiceLocator.Register(playerViewController);
        ServiceLocator.Register(clientBehaviorController);
        ServiceLocator.Register(clientsController);
        ServiceLocator.Register(gameLoopController);
        ServiceLocator.Register(testGameManager);
        ServiceLocator.Register(gameStateManager);

        // Добавляем в списки для вызова жизненного цикла
        AddService(dialogueService);
        AddService(playerManager);
        AddService(cutManager);
        AddService(validationService);
        AddService(cutVisualizer);
        AddService(levelMediator);
        AddService(timelineLogic);
        AddService(cameraControlService);
        AddService(playerViewController);
        AddService(clientBehaviorController);
        AddService(clientsController);
        AddService(gameLoopController);
        AddService(testGameManager);
        AddService(gameStateManager);

        if (clientView != null)
        {
            clientView.Initialize();
        }

        // 3. Вызов Initialize() для каждого сервиса
        foreach (var init in _initializables)
        {
            init.Initialize();
        }

        // 4. Установка начального состояния
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
