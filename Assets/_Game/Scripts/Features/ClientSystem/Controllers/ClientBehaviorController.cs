using System;
using System.Collections.Generic;
using Core.Services;
using GamePlay.Data;
using GamePlay.View;
using UnityEngine;

namespace GamePlay.Controllers
{
    public class ClientBehaviorController : IInitializable, IUpdatable
    {
        private readonly ClientView _clientView;
        private readonly Transform _clientRoot;
        private readonly Transform _spawnPoint;
        private readonly Transform _intermediatePoint;
        private readonly Transform _deskPoint;
        private readonly Transform _exitPoint;
        private readonly ClientMovementConfig _config;

        private readonly Queue<Vector3> _pathQueue;
        private Vector3 _currentWaypoint;
        private bool _isMoving;
        private Action _onPathFinishedCallback;

        public bool IsMoving => _isMoving;

        public ClientBehaviorController(
            ClientView clientView,
            Transform clientRoot,
            Transform spawnPoint,
            Transform intermediatePoint,
            Transform deskPoint,
            Transform exitPoint,
            ClientMovementConfig config = null)
        {
            _clientView = clientView;
            _clientRoot = clientRoot;
            _spawnPoint = spawnPoint;
            _intermediatePoint = intermediatePoint;
            _deskPoint = deskPoint;
            _exitPoint = exitPoint;
            _config = config;
            _pathQueue = new Queue<Vector3>();
        }

        public ClientBehaviorController(
            ClientView clientView,
            Transform clientRoot,
            Transform spawnPoint,
            Transform deskPoint,
            Transform exitPoint,
            ClientMovementConfig config = null)
            : this(clientView, clientRoot, spawnPoint, null, deskPoint, exitPoint, config)
        {
        }

        public void Initialize()
        {
            _isMoving = false;
            _onPathFinishedCallback = null;
            _pathQueue.Clear();
            ResetPositionToSpawn();
        }

        public void ResetPositionToSpawn()
        {
            if (_clientRoot != null && _spawnPoint != null)
            {
                _clientRoot.position = _spawnPoint.position;
                _clientRoot.rotation = _spawnPoint.rotation;
            }
        }

        public void MoveToDesk(ClientDataConfig clientData, Action onArrived = null)
        {
            if (_clientView != null)
            {
                _clientView.SetClientData(clientData);
            }

            ResetPositionToSpawn();

            List<Vector3> waypoints = new List<Vector3>();
            if (_intermediatePoint != null)
            {
                waypoints.Add(_intermediatePoint.position);
            }
            if (_deskPoint != null)
            {
                waypoints.Add(_deskPoint.position);
            }

            StartPathMovement(waypoints, () =>
            {
                _clientView?.PlayIdle();
                if (_deskPoint != null && _clientRoot != null)
                {
                    _clientRoot.rotation = _deskPoint.rotation;
                }
                onArrived?.Invoke();
            });
        }

        public void LeaveRoom(Action onExited = null)
        {
            List<Vector3> waypoints = new List<Vector3>();
            if (_intermediatePoint != null)
            {
                waypoints.Add(_intermediatePoint.position);
            }
            Vector3 exitPos = _exitPoint != null ? _exitPoint.position : (_spawnPoint != null ? _spawnPoint.position : Vector3.zero);
            waypoints.Add(exitPos);

            StartPathMovement(waypoints, () =>
            {
                _clientView?.SetClientData(null);
                ResetPositionToSpawn();
                onExited?.Invoke();
            });
        }

        private void StartPathMovement(List<Vector3> waypoints, Action onFinished)
        {
            _pathQueue.Clear();
            if (waypoints != null)
            {
                foreach (Vector3 pos in waypoints)
                {
                    _pathQueue.Enqueue(pos);
                }
            }

            _onPathFinishedCallback = onFinished;

            if (_pathQueue.Count > 0)
            {
                _currentWaypoint = _pathQueue.Dequeue();
                _isMoving = true;
                _clientView?.PlayWalk();
            }
            else
            {
                _isMoving = false;
                _onPathFinishedCallback?.Invoke();
                _onPathFinishedCallback = null;
            }
        }

        public void Update()
        {
            if (!_isMoving || _clientRoot == null) return;

            float moveSpeed = _config != null ? _config.moveSpeed : 2.0f;
            float rotationSpeed = _config != null ? _config.rotationSpeed : 10.0f;
            float arrivalThreshold = _config != null ? _config.arrivalThreshold : 0.05f;

            Vector3 direction = (_currentWaypoint - _clientRoot.position);
            direction.y = 0; // Движение только в плоскости пола

            float distance = direction.magnitude;

            if (distance <= arrivalThreshold)
            {
                _clientRoot.position = new Vector3(_currentWaypoint.x, _clientRoot.position.y, _currentWaypoint.z);

                if (_pathQueue.Count > 0)
                {
                    _currentWaypoint = _pathQueue.Dequeue();
                }
                else
                {
                    _isMoving = false;
                    Action callback = _onPathFinishedCallback;
                    _onPathFinishedCallback = null;
                    callback?.Invoke();
                }
                return;
            }

            // Поворот в направлении текущей точки
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _clientRoot.rotation = Quaternion.Slerp(_clientRoot.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            // Перемещение к текущей точке
            _clientRoot.position = Vector3.MoveTowards(_clientRoot.position, _currentWaypoint, moveSpeed * Time.deltaTime);
        }
    }
}
