using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Data
{
    [CreateAssetMenu(fileName = "PhoneCallConfig", menuName = "Gameplay/Phone Call Config")]
    public class PhoneCallConfig : ScriptableObject
    {
        [Header("Phone Call Dialogue")]
        [Tooltip("Список фраз для стартового телефонного звонка.")]
        public List<string> Phrases;

        [Header("Settings")]
        [Tooltip("Задержка после окончания разговора перед приходом первого клиента (в секундах).")]
        public float DelayAfterCall = 2.0f;
    }
}
