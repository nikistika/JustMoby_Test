using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace _Project.Scripts.Services.Tower
{
    [Serializable]
    public class TowerState
    {
        public List<BlockState> Blocks = new();
    }
}