using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Data
{
    [CreateAssetMenu(fileName = "DaySchedule", menuName = "Gameplay/Day Schedule")]
    public class DayScheduleSO : ScriptableObject
    {
        [Header("Schedule Settings")]
        public float DelayBetweenClients = 3f;

        [Header("Clients Queue")]
        public List<ClientDataSO> Clients;
    }
}
