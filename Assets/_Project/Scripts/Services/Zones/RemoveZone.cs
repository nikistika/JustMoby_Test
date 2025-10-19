using _Project.Scripts.Installers.Structures;
using _Project.Scripts.Services.Blocks;
using _Project.Scripts.Services.Blocks.Interfaces;
using _Project.Scripts.Services.DragAndDrop.Interfaces;
using _Project.Scripts.Services.Input;
using _Project.Scripts.Services.Tower;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Services.Zones
{
    public sealed class RemoveZone : MonoBehaviour, IDropTarget, ICanvasRaycastFilter
    {
        [Header("Raycast")] [SerializeField] private RectTransform _raycastRect;
        [SerializeField] private PolygonCollider2D _polygon;

        private StackZone _tower;
        private IElementAnimator _animator;
        private IInputService _input;
        private SignalBus _signalBus;

        [Inject]
        public void Construct(StackZone tower, IElementAnimator animator, IInputService input, SignalBus signalBus)
        {
            _tower = tower;
            _animator = animator;
            _input = input;
            _signalBus = signalBus;
        }

        public bool CanDrop(IDragTarget drag) => drag is Block e && IsFromTower(e);

        public async void OnDrop(IDragTarget drag)
        {
            if (drag is not Block element)
                return;

            await HandleRemoveAsync(element);
        }

        public bool IsRaycastLocationValid(Vector2 screenPos, Camera eventCamera)
        {
            return IsInsidePolygon(screenPos, eventCamera);
        }

        private static bool IsFromTower(Block e) => e.Owner is StackZone || e.SlotIndex < 0;

        private async UniTask HandleRemoveAsync(Block block)
        {
            var localInTower = ScreenToLocalInTower(_input.CursorPosition.Value);
            await _animator.PlayRemove(block.Rect, localInTower);
            await _tower.RemoveElementAsync(block);
            _signalBus.Fire(new BlockRemoved());
        }

        private Vector2 ScreenToLocalInTower(Vector2 screenPos)
        {
            var area = _tower.Area;
            var cam = GetCanvasCamera(area);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPos, cam, out var local))
                return Vector2.zero;

            return local;
        }

        private bool IsInsidePolygon(Vector2 screenPos, Camera eventCamera)
        {
            if (_polygon == null)
                return true;

            var rect = _raycastRect ? _raycastRect : (RectTransform)transform;

            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, screenPos, eventCamera, out var world))
                return false;

            var point2D = new Vector2(world.x, world.y);
            return _polygon.OverlapPoint(point2D);
        }

        private static Camera GetCanvasCamera(RectTransform rect)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            if (!canvas) return null;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
    }
}