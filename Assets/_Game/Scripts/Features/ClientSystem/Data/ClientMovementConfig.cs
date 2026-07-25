using UnityEngine;

namespace GamePlay.Data
{
    [CreateAssetMenu(fileName = "ClientMovementConfig", menuName = "Gameplay/Client Movement Config")]
    public class ClientMovementConfig : ScriptableObject
    {
        [Header("Скорость перемещения")]
        [Tooltip("Скорость ходьбы клиента в юнитах/сек")]
        public float moveSpeed = 2.0f;

        [Header("Поворот")]
        [Tooltip("Скорость разворота клиента")]
        public float rotationSpeed = 10.0f;

        [Header("Дистанция остановки")]
        [Tooltip("Порог расстояния для определения достижения целевой точки")]
        public float arrivalThreshold = 0.05f;
    }
}
