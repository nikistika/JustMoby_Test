using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Services.Blocks.Interfaces
{
    public interface IElementAnimator
    {
        UniTask PlayAdd(RectTransform element, Vector2 toPos);
        UniTask PlayMiss(RectTransform proxy);
        UniTask PlayRemove(RectTransform element, Vector2 toPos);
        UniTask PlayCollapse(IReadOnlyList<(RectTransform rect, Vector2 toPos)> moves);
    }
}