using UnityEngine;

namespace GamePlay.Data
{
    [CreateAssetMenu(fileName = "GameControlsConfig", menuName = "Gameplay/Controls Config")]
    public class GameControlsConfigSO : ScriptableObject
    {
        [Header("Tape Editing & Controls")]
        [Tooltip("Клавиша для завершения работы с кассетой и передачи ее клиенту.")]
        [SerializeField] private KeyCode finishEditingKey = KeyCode.E;

        [Tooltip("Клавиша для переключения видов камеры.")]
        [SerializeField] private KeyCode toggleCameraKey = KeyCode.Q;

        [Tooltip("Клавиша для вызова следующего клиента.")]
        [SerializeField] private KeyCode nextClientKey = KeyCode.N;

        [Tooltip("Клавиша для ухода клиента.")]
        [SerializeField] private KeyCode dismissClientKey = KeyCode.X;

        public KeyCode FinishEditingKey
        {
            get => finishEditingKey;
            set => finishEditingKey = value;
        }

        public KeyCode ToggleCameraKey
        {
            get => toggleCameraKey;
            set => toggleCameraKey = value;
        }

        public KeyCode NextClientKey
        {
            get => nextClientKey;
            set => nextClientKey = value;
        }

        public KeyCode DismissClientKey
        {
            get => dismissClientKey;
            set => dismissClientKey = value;
        }
    }
}
