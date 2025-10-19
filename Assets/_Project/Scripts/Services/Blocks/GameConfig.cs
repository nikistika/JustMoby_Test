using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Services.Blocks
{
    [CreateAssetMenu(fileName = "GameConfiguration", menuName = "Game/Game Configuration")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField] private List<BlockConfig> _configurations = new();
        public IReadOnlyList<BlockConfig> Configurations => _configurations;
    }
}