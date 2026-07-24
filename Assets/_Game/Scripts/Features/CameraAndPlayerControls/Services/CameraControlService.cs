using UnityEngine;
using Unity.Cinemachine;

namespace Core.Services
{
    public class CameraControlService : IInitializable
    {
        private readonly CinemachineCamera _mainCamera;
        private readonly CinemachineCamera _tvCamera;
        private readonly CinemachineCamera _clientCamera;

        private readonly int _activePriority;
        private readonly int _inactivePriority;

        public CinemachineCamera MainCamera => _mainCamera;
        public CinemachineCamera TVCamera => _tvCamera;
        public CinemachineCamera ClientCamera => _clientCamera;
        public CinemachineCamera ActiveCamera { get; private set; }

        public bool IsLocked { get; private set; }

        public CameraControlService(
            CinemachineCamera mainCamera, 
            CinemachineCamera tvCamera, 
            CinemachineCamera clientCamera, 
            int activePriority = 10, 
            int inactivePriority = 0)
        {
            _mainCamera = mainCamera;
            _tvCamera = tvCamera;
            _clientCamera = clientCamera;
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
        public void SwitchToMainCamera()
        {
            SwitchToCamera(_mainCamera);
        }

        /// <summary>
        /// Переключает на телевизионную камеру/камеру монтажа (TVCamera), если переключение не заблокировано.
        /// </summary>
        public void SwitchToTVCamera()
        {
            SwitchToCamera(_tvCamera);
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
                Debug.Log("[CameraControlService] Камера заблокирована на ClientCamera на время диалога.");
            }
        }

        /// <summary>
        /// Снимает блокировку переключения камер (вызывается по завершении диалога).
        /// </summary>
        public void UnlockCamera()
        {
            IsLocked = false;
            Debug.Log("[CameraControlService] Блокировка переключения камер снята.");
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
                default:
                    Debug.LogWarning($"[CameraControlService] Неверный индекс камеры: {index}. Ожидается 0, 1 или 2.");
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
                Debug.LogWarning($"[CameraControlService] Нельзя переключить камеру: заблокировано на время диалога (активна {ActiveCamera?.name}).");
                return;
            }

            if (targetCamera == null)
            {
                Debug.LogWarning("[CameraControlService] Целевая камера не задана (null)!");
                return;
            }

            SetCameraPriority(_mainCamera, targetCamera == _mainCamera);
            SetCameraPriority(_tvCamera, targetCamera == _tvCamera);
            SetCameraPriority(_clientCamera, targetCamera == _clientCamera);

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
