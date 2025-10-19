using System.Collections.Generic;
using _Project.Scripts.Services.Blocks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Services.Tower.Interfaces
{
    public interface IStackAnimator
    {
        UniTask PlayAdd(Block block, Vector2 to);
        UniTask PlayRemove(Block block, Vector2 to);
        UniTask PlayCollapse(IReadOnlyList<(RectTransform rect, Vector2 to)> moves);
    }
}