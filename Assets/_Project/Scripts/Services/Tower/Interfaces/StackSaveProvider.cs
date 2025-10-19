using UnityEngine;

namespace _Project.Scripts.Services.Tower.Interfaces
{
    public interface IStackSaveProvider
    {
        Vector2 CalculateAddPosition(RectTransform towerArea, RectTransform topRect, float elementSize);

    }
}