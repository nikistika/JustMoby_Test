using System.Collections.Generic;
using _Project.Scripts.Services.Blocks.Interfaces;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace _Project.Scripts.Services.Blocks
{
    public sealed class BlockAnimator : IElementAnimator
    {
        private const float _jumpPower = 100f;
        private const int _jumpCount = 1;
        private const float _jumpDuration = 0.36f;
        private const float _addPulseDuration = 0.06f;

        private const float _missScaleUpDuration = 0.06f;
        private const float _missScaleDownDuration = 0.16f;

        private const float _removeBounceInDuration = 0.12f;
        private const float _removeBounceOutDuration = 0.08f;
        private const float _removeFlyDuration = 0.35f;
        private const float _removeScaleTarget = 0f;
        private const float _collapseMoveDuration = 0.15f;

        public UniTask PlayAdd(RectTransform element, Vector2 toPos)
        {
            if (!IsTransformValid(element))
                return UniTask.CompletedTask;

            element.DOKill();
            RestoreTransform(element);

            var sequence = DOTween.Sequence()
                .Append(element.DOJumpAnchorPos(toPos, _jumpPower, _jumpCount, _jumpDuration).SetEase(Ease.OutQuad))
                .Append(element.DOScale(1.03f, _addPulseDuration).SetLoops(2, LoopType.Yoyo));

            sequence.OnComplete(() =>
            {
                element.anchoredPosition = toPos;
                element.localScale = Vector3.one;
                element.rotation = Quaternion.identity;
            });

            return sequence.AsyncWaitForCompletion().AsUniTask();
        }

        public UniTask PlayMiss(RectTransform proxy)
        {
            if (!IsTransformValid(proxy))
                return UniTask.CompletedTask;

            proxy.DOComplete();
            var sequence = DOTween.Sequence()
                .Append(proxy.DOScale(1.1f, _missScaleUpDuration))
                .Append(proxy.DOScale(0f, _missScaleDownDuration).SetEase(Ease.InBack));

            return sequence.AsyncWaitForCompletion().AsUniTask();
        }

        public UniTask PlayRemove(RectTransform element, Vector2 toPos)
        {
            if (!IsTransformValid(element)) return UniTask.CompletedTask;

            element.DOKill();
            element.localScale = Vector3.one;

            float spinAngle = Random.Range(90f, 140f) * (Random.value < 0.5f ? -1f : 1f);

            var sequence = DOTween.Sequence()
                .Append(element.DOScale(0.92f, _removeBounceInDuration).SetEase(Ease.OutQuad))
                .Append(element.DOScale(1f, _removeBounceOutDuration).SetEase(Ease.OutBack))
                .Append(element.DOAnchorPos(toPos, _removeFlyDuration).SetEase(Ease.InQuad))
                .Join(element.DOScale(_removeScaleTarget, _removeFlyDuration).SetEase(Ease.InCubic))
                .Join(element.DORotate(new Vector3(0f, 0f, spinAngle), _removeFlyDuration, RotateMode.FastBeyond360));

            return sequence.AsyncWaitForCompletion().AsUniTask();
        }

        public UniTask PlayCollapse(IReadOnlyList<(RectTransform rect, Vector2 toPos)> moves)
        {
            if (moves == null || moves.Count == 0)
                return UniTask.CompletedTask;

            var sequence = DOTween.Sequence();

            for (int i = 0; i < moves.Count; i++)
            {
                var (rect, dest) = moves[i];
                if (!IsTransformValid(rect)) continue;

                rect.DOComplete();
                sequence.Join(rect.DOAnchorPos(dest, _collapseMoveDuration));
            }

            return sequence.AsyncWaitForCompletion().AsUniTask();
        }

        private static bool IsTransformValid(RectTransform rect)
        {
            return rect != null && rect.gameObject != null && rect.gameObject.activeInHierarchy;
        }

        private static void RestoreTransform(RectTransform rect)
        {
            rect.localScale = Vector3.one;
            rect.rotation = Quaternion.identity;
        }
    }
}