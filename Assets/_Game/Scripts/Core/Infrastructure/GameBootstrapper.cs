using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Cinemachine;
using Core.Services;
using GamePlay.View;
using GamePlay.Data;
using GamePlay.Controllers;

public class GameBootstrapper : MonoBehaviour
{
    [Header("Менеджер игры")]
    [SerializeField] private GameManager _gameManager;

    [Header("Расписание дня")]
    public GamePlay.Data.DayScheduleConfig todaySchedule;

    [Header("Сцена: Клиент")]
    public GamePlay.View.ClientView clientView;
    public Transform clientRoot;
    public Transform spawnPoint;
    public Transform intermediatePoint;
    public Transform deskPoint;
    public Transform exitPoint;
    public GamePlay.Data.ClientMovementConfig clientMovementConfig;

    [Header("Сцена: Плееры")]
    [UnityEngine.Serialization.FormerlySerializedAs("videoPlayer")]
    public VideoPlayer forwardPlayer;
    public VideoPlayer reversePlayer;

    [Header("Сцена: Отображение")]
    public GamePlay.View.TV tv;
    public Material tvOnMaterial;
    public Material tvReverseOnMaterial;
    public Renderer displayRenderer;
    public string materialTextureProperty = "_MainTex";

    [Header("Сцена: UI диалогов")]
    public GameObject dialogueWindow;
    public TMPro.TMP_Text dialogueNameText;
    public TMPro.TMP_Text dialogueMessageText;

    [Header("Сцена: UI управления видеоплеером")]
    public GamePlay.View.VideoPlayerControlsUIView videoPlayerControlsView;

    [Header("Сцена: Настройки управления")]
    public GamePlay.Data.GameControlsConfig controlsConfig;

    [Header("Сцена: Камеры (Cinemachine)")]
    public CinemachineCamera mainCamera;
    public CinemachineCamera tvCamera;
    public CinemachineCamera clientCamera;
    public CinemachineCamera cassetteCamera;
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
        var playerManager = new VideoPlayerService(forwardPlayer, reversePlayer, materialTextureProperty, tvService);
        var cutManager = new VideoCutService();
        var validationService = new CutValidationService();
        var cutVisualizer = new VideoCutVisualizer(videoPlayerControlsView);
        var levelMediator = new CutLevelMediator();
        var timelineLogic = new VideoTimelineUILogic(videoPlayerControlsView, playerManager, cutManager);
        var cameraControlService = new CameraControlService(mainCamera, tvCamera, clientCamera, cassetteCamera, activeCameraPriority, inactiveCameraPriority);
        var playerViewController = new GamePlay.Controllers.PlayerViewController(cameraControlService, controlsConfig);
        var clientBehaviorController = new GamePlay.Controllers.ClientBehaviorController(clientView, clientRoot, spawnPoint, intermediatePoint, deskPoint, exitPoint, clientMovementConfig);
        var clientsController = new GamePlay.Controllers.ClientsController(clientBehaviorController, clientView);
        var gameStateManager = new GameStateManager();
        gameStateManager.RegisterState(new RoomGameState(playerViewController));
        gameStateManager.RegisterState(new MontageGameState(videoPlayerControlsView, playerViewController));
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
        ServiceLocator.Register(gameStateManager);
        if (videoPlayerControlsView != null)
        {
            ServiceLocator.Register(videoPlayerControlsView);
        }
        if (tvService != null)
        {
            ServiceLocator.Register(tvService);
        }

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
        AddService(gameStateManager);

        GamePlay.Data.DayScheduleConfig schedule = todaySchedule;
        VideotapeConfig debugTape = null;
        TV tvComp = tv;
        Material tvOnMat = null;
        Material tvRevMat = null;
        GameControlsConfig ctrlCfg = controlsConfig;
        bool isDebugMode = false;

        if (_gameManager == null)
        {
            _gameManager = GetComponent<GameManager>();
        }

        if (_gameManager != null)
        {
            _gameManager.enabled = true;
            if (schedule == null) schedule = _gameManager.Schedule;
            debugTape = _gameManager.DebugVideotapeConfig;
            if (tvComp == null) tvComp = _gameManager.Tv;
            tvOnMat = _gameManager.TvOnMaterial;
            tvRevMat = _gameManager.TvReverseOnMaterial;
            if (ctrlCfg == null) ctrlCfg = _gameManager.ControlsConfig;
            isDebugMode = _gameManager.IsDebugMode;
        }

        var gameLoopController = new GamePlay.Controllers.GameLoopController(
            schedule,
            debugTape,
            tvComp,
            tvOnMat,
            tvRevMat,
            ctrlCfg,
            clientView,
            videoPlayerControlsView,
            isDebugMode
        );

        ServiceLocator.Register(gameLoopController);
        AddService(gameLoopController);

        if (clientView != null)
        {
            clientView.Initialize();
        }

        // 3. Вызов Initialize() для каждого сервиса
        foreach (var init in _initializables)
        {
            init.Initialize();
        }

        // 4. Установка начального состояния при стандартном запуске
        if (!isDebugMode)
        {
            gameStateManager.SwitchState<MontageGameState>();
        }
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
