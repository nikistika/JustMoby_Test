using System.Collections.Generic;
using _Project.Scripts.Services.Blocks;
using UnityEngine;

namespace _Project.Scripts.Services.Tower.Interfaces
{
    public interface IStackDropValidator
    {
        bool CanAdd(Block block, IReadOnlyList<Block> currentStack, RectTransform towerArea, out string reason);

    }
}