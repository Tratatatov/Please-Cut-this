using System;
using Core.Services;
using GamePlay.Data;
using GamePlay.View;
using UnityEngine;

namespace GamePlay.Controllers
{
    /// <summary>
    /// Сервис управления клиентами. Отвечает за перезапуск состояния единственного клиента
    /// на сцене при вызове StartNewClient и управление его поведением.
    /// </summary>
    public class ClientsController : IInitializable, IUpdatable
    {
        private readonly ClientBehaviorController _behaviorController;
        private readonly ClientView _clientView;

        public ClientBehaviorController BehaviorController => _behaviorController;
        public ClientView ClientView => _clientView;

        public ClientsController(ClientBehaviorController behaviorController, ClientView clientView)
        {
            _behaviorController = behaviorController;
            _clientView = clientView;
        }

        public void Initialize()
        {
            _clientView?.Initialize();
            _behaviorController?.Initialize();
        }

        public void Update()
        {
            _behaviorController?.Update();
        }

        /// <summary>
        /// Запускает нового клиента: сбрасывает позицию к точке спавна, выбирает подходящую 3D-модель
        /// из пула и отправляет клиента к стойке.
        /// </summary>
        /// <param name="clientData">Данные о клиенте (ScriptableObject)</param>
        /// <param name="onArrived">Коллбек, вызываемый по прибытию клиента к стойке</param>
        public void StartNewClient(ClientDataConfig clientData, Action onArrived = null)
        {
            if (clientData == null)
            {
                Debug.LogWarning("<color=cyan>[ClientsController]</color> Попытка запустить нового клиента с null ClientDataConfig!");
                return;
            }

            Debug.Log($"<color=cyan>[ClientsController]</color> Запуск нового клиента: {clientData.ClientName} (Тип: {clientData.ModelType})");

            _behaviorController?.MoveToDesk(clientData, onArrived);
        }

        /// <summary>
        /// Отправляет текущего клиента к выходу.
        /// </summary>
        /// <param name="onExited">Коллбек по завершении ухода клиента из комнаты</param>
        public void DismissCurrentClient(Action onExited = null)
        {
            Debug.Log("<color=cyan>[ClientsController]</color> Отправка клиента к выходу...");
            _behaviorController?.LeaveRoom(onExited);
        }
    }
}
