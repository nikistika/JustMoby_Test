using System.Collections.Generic;
using _Project.Scripts.Services.Blocks;
using _Project.Scripts.Services.Tower.Interfaces;
using UnityEngine;

namespace _Project.Scripts.Services.Tower
{
    public sealed class StackDropValidatorDefault : IStackDropValidator
    {
        private const string ReasonHeightLimit = "height_limit";
        private const string ReasonInvalidInput = "invalid_input";

        public bool CanAdd(Block block, IReadOnlyList<Block> stack, RectTransform towerArea, out string reason)
        {
            if (!ValidateInputs(block, towerArea, out reason))
                return false;

            float elementSize = GetElementHeight(block);
            float halfAreaHeight = towerArea.rect.height * 0.5f;

            float nextCenterY = ComputeNextCenterY(stack, elementSize, halfAreaHeight);
            float nextTopEdge = nextCenterY + elementSize * 0.5f;

            if (ExceedsTopBound(nextTopEdge, halfAreaHeight))
            {
                reason = ReasonHeightLimit;
                return false;
            }

            reason = null;
            return true;
        }

        private static bool ValidateInputs(Block block, RectTransform area, out string reason)
        {
            if (block == null || block.Rect == null || area == null)
            {
                reason = ReasonInvalidInput;
                return false;
            }

            reason = null;
            return true;
        }

        private static float GetElementHeight(Block block) => block.Rect.rect.height;

        private static float ComputeNextCenterY(IReadOnlyList<Block> stack, float newSize, float halfAreaHeight)
        {
            if (stack == null || stack.Count == 0)
            {
                return -halfAreaHeight + newSize * 0.5f;
            }

            var topRect = stack[^1].Rect;
            float topHeight = topRect.rect.height;

            return topRect.anchoredPosition.y + (topHeight + newSize) * 0.5f;
        }

        private static bool ExceedsTopBound(float nextTopEdge, float halfAreaHeight) =>
            nextTopEdge > halfAreaHeight + Mathf.Epsilon;
    }
}