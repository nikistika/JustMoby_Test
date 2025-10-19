using System.Collections.Generic;
using _Project.Scripts.Services.Blocks;
using _Project.Scripts.Services.Blocks.Interfaces;
using _Project.Scripts.Services.Tower.Interfaces;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace _Project.Scripts.Services.Tower
{
    public sealed class StackAnimator : IStackAnimator
    {
        private const float CollapseMoveDuration = 0.24f;
        private const float CollapseStagger = 0.04f;
        private const float CollapseLandPop = 0.06f;

        private readonly IElementAnimator _animator;
        public StackAnimator(IElementAnimator animator) => _animator = animator;

        public UniTask PlayAdd(Block block, Vector2 to)
        {
            if (!IsRectValid(block?.Rect))
                return UniTask.CompletedTask;

            return _animator.PlayAdd(block.Rect, to);
        }

        public UniTask PlayRemove(Block block, Vector2 to)
        {
            if (!IsRectValid(block?.Rect)) return UniTask.CompletedTask;

            return _animator.PlayRemove(block.Rect, to);
        }

        public UniTask PlayCollapse(IReadOnlyList<(RectTransform rect, Vector2 to)> moves)
        {
            if (!HasMoves(moves))
                return UniTask.CompletedTask;

            var seq = BuildCollapseSequence(moves);
            FixFinalValuesOnComplete(seq, moves);
            return seq.AsyncWaitForCompletion().AsUniTask();
        }

        private static bool HasMoves(IReadOnlyList<(RectTransform rect, Vector2 to)> moves) =>
            moves is { Count: > 0 };

        private static bool IsRectValid(RectTransform rectTransform) =>
            rectTransform != null && rectTransform.gameObject != null && rectTransform.gameObject.activeInHierarchy;

        private static Sequence BuildCollapseSequence(IReadOnlyList<(RectTransform rect, Vector2 to)> moves)
        {
            var seq = DOTween.Sequence();

            for (int index = 0; index < moves.Count; index++)
            {
                var (rect, to) = moves[index];
                if (!IsRectValid(rect))
                    continue;

                PrepareRectForMove(rect);
                JoinMoveAndPop(seq, rect, to, index);
            }

            return seq;
        }

        private static void PrepareRectForMove(RectTransform rect)
        {
            rect.DOKill();
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void JoinMoveAndPop(Sequence seq, RectTransform rect, Vector2 to, int index)
        {
            float delay = index * CollapseStagger;

            seq.Join(rect
                .DOAnchorPos(to, CollapseMoveDuration)
                .SetEase(Ease.InOutQuad)
                .SetDelay(delay));

            seq.Join(rect
                .DOScale(1.03f, CollapseLandPop)
                .SetEase(Ease.OutQuad)
                .SetDelay(delay + CollapseMoveDuration - CollapseLandPop)
                .SetLoops(2, LoopType.Yoyo));
        }

        private static void FixFinalValuesOnComplete(Sequence seq,
            IReadOnlyList<(RectTransform rect, Vector2 to)> moves)
        {
            seq.OnComplete(() =>
            {
                for (int i = 0; i < moves.Count; i++)
                {
                    var (rect, to) = moves[i];
                    if (!IsRectValid(rect))
                        continue;

                    rect.anchoredPosition = to;
                    rect.localScale = Vector3.one;
                    rect.localRotation = Quaternion.identity;
                }
            });
        }
    }
}