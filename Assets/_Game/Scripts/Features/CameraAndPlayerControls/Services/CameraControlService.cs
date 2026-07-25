using UnityEngine;
using Unity.Cinemachine;

namespace Core.Services
{
    public class CameraControlService : IInitializable
    {
        private readonly CinemachineCamera _mainCamera;
        private readonly CinemachineCamera _tvCamera;
        private readonly CinemachineCamera _clientCamera;
        private readonly CinemachineCamera _cassetteCamera;

        private readonly int _activePriority;
        private readonly int _inactivePriority;

        public CinemachineCamera MainCamera => _mainCamera;
        public CinemachineCamera TVCamera => _tvCamera;
        public CinemachineCamera ClientCamera => _clientCamera;
        public CinemachineCamera CassetteCamera => _cassetteCamera;
        public CinemachineCamera ActiveCamera { get; private set; }

        public bool IsLocked { get; private set; }

        public CameraControlService(
            CinemachineCamera mainCamera, 
            CinemachineCamera tvCamera, 
            CinemachineCamera clientCamera, 
            CinemachineCamera cassetteCamera, 
            int activePriority = 10, 
            int inactivePriority = 0)
        {
            _mainCamera = mainCamera;
            _tvCamera = tvCamera;
            _clientCamera = clientCamera;
            _cassetteCamera = cassetteCamera;
            _activePriority = activePriority;
            _inactivePriority = inactivePriority;
        }

        public void Initialize()
        {
            IsLocked = false;
            if (_mainCamera != null)
            {
                SwitchToMainCamera();
            }
        }

        /// <summary>
        /// Переключает на главную камеру (MainCamera), если переключение не заблокировано.
        /// </summary>
        /// <param name="lockCamera">Если true, блокирует возможность переключения на другие камеры до вызова UnlockCamera</param>
        public void SwitchToMainCamera(bool lockCamera = false)
        {
            IsLocked = false;
            SwitchToCamera(_mainCamera);
            IsLocked = lockCamera;
        }

        /// <summary>
        /// Переключает на телевизионную камеру/камеру монтажа (TVCamera), если переключение не заблокировано.
        /// </summary>
        /// <param name="lockCamera">Если true, блокирует возможность переключения на другие камеры до вызова UnlockCamera</param>
        public void SwitchToTVCamera(bool lockCamera = false)
        {
            IsLocked = false;
            SwitchToCamera(_tvCamera);
            IsLocked = lockCamera;
            if (lockCamera)
            {
                Debug.Log("<color=lightblue>[CameraControlService]</color> Камера заблокирована на TVCamera.");
            }
        }

        /// <summary>
        /// Переключает на камеру диалога с клиентом (ClientCamera).
        /// </summary>
        /// <param name="lockCamera">Если true, блокирует возможность переключения на другие камеры до вызова UnlockCamera</param>
        public void SwitchToClientCamera(bool lockCamera = true)
        {
            bool wasLocked = IsLocked;
            IsLocked = false;
            SwitchToCamera(_clientCamera);
            IsLocked = lockCamera;
            if (lockCamera)
            {
                Debug.Log("<color=lightblue>[CameraControlService]</color> Камера заблокирована на ClientCamera на время диалога.");
            }
        }

        /// <summary>
        /// Переключает на камеру анимации кассеты (CassetteCamera).
        /// </summary>
        /// <param name="lockCamera">Если true, блокирует возможность переключения на другие камеры до вызова UnlockCamera</param>
        public void SwitchToCassetteCamera(bool lockCamera = true)
        {
            IsLocked = false;
            SwitchToCamera(_cassetteCamera);
            IsLocked = lockCamera;
            if (lockCamera)
            {
                Debug.Log("<color=lightblue>[CameraControlService]</color> Камера заблокирована на CassetteCamera.");
            }
        }

        /// <summary>
        /// Снимает блокировку переключения камер (вызывается по завершении диалога).
        /// </summary>
        public void UnlockCamera()
        {
            IsLocked = false;
            Debug.Log("<color=lightblue>[CameraControlService]</color> Блокировка переключения камер снята.");
        }

        // Алиасы для обратной совместимости
        public void SwitchToCamera1() => SwitchToMainCamera();
        public void SwitchToCamera2() => SwitchToTVCamera();
        public void SwitchToCamera3() => SwitchToClientCamera(false);

        public void ActivateCamera1() => SwitchToMainCamera();
        public void ActivateCamera2() => SwitchToTVCamera();
        public void ActivateCamera3() => SwitchToClientCamera(false);

        public void SwitchToCamera(int index)
        {
            switch (index)
            {
                case 0:
                    SwitchToMainCamera();
                    break;
                case 1:
                    SwitchToTVCamera();
                    break;
                case 2:
                    SwitchToClientCamera(false);
                    break;
                case 3:
                    SwitchToCassetteCamera(false);
                    break;
                default:
                    Debug.LogWarning($"<color=lightblue>[CameraControlService]</color> Неверный индекс камеры: {index}. Ожидается 0, 1, 2 или 3.");
                    break;
            }
        }

        /// <summary>
        /// Переключает активную камеру на переданную CinemachineCamera (если переключение не заблокировано).
        /// </summary>
        public void SwitchToCamera(CinemachineCamera targetCamera)
        {
            if (IsLocked)
            {
                Debug.LogWarning($"<color=lightblue>[CameraControlService]</color> Нельзя переключить камеру: заблокировано на время диалога (активна {ActiveCamera?.name}).");
                return;
            }

            if (targetCamera == null)
            {
                Debug.LogWarning("<color=lightblue>[CameraControlService]</color> Целевая камера не задана (null)!");
                return;
            }

            SetCameraPriority(_mainCamera, targetCamera == _mainCamera);
            SetCameraPriority(_tvCamera, targetCamera == _tvCamera);
            SetCameraPriority(_clientCamera, targetCamera == _clientCamera);
            SetCameraPriority(_cassetteCamera, targetCamera == _cassetteCamera);

            ActiveCamera = targetCamera;
        }

        private void SetCameraPriority(CinemachineCamera cam, bool isActive)
        {
            if (cam != null)
            {
                cam.Priority = isActive ? _activePriority : _inactivePriority;
            }
        }
    }
}
