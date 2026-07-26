using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Data
{
    [CreateAssetMenu(fileName = "DayScheduleConfig", menuName = "Gameplay/Day Schedule Config")]
    public class DayScheduleConfig : ScriptableObject
    {
        [Header("Schedule Settings")]
        public float DelayBeforeFirstClient = 3f;
        public float DelayBetweenClients = 3f;

        [Header("Clients Queue")]
        public List<ClientDataConfig> Clients;
    }
}
