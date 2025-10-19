using System.Collections.Generic;
using _Project.Scripts.Installers.Structures;
using _Project.Scripts.Services.Blocks;
using _Project.Scripts.Services.Blocks.Interfaces;
using _Project.Scripts.Services.DragAndDrop.Interfaces;
using _Project.Scripts.Services.Input;
using _Project.Scripts.Services.Tower.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Services.Tower
{
    public sealed class StackZone : MonoBehaviour, IDropTarget, IElementContainer
    {
        [Header("Area")] [SerializeField] private RectTransform _towerArea;

        [Header("Drop corridor (over-the-top)")] [SerializeField, Range(0f, 1f)]
        private float _topSnapPercent = 0.7f;

        [SerializeField, Min(0f)] private float _sideSlackPx = 8f;

        private readonly List<Block> _stack = new();

        private IStackDropValidator _dropValidator;
        private IStackSaveProvider _placement;
        private IStackAnimator _animation;
        private BlockPool _pool;
        private IInputService _input;
        private SignalBus _signalBus;

        private bool _isCollapsing = false;

        public IReadOnlyList<Block> Stack => _stack;
        public RectTransform Area => _towerArea;

        [Inject]
        public void Construct(
            IStackDropValidator dropValidator,
            IStackSaveProvider placement,
            IStackAnimator animation,
            BlockPool pool,
            IInputService input,
            SignalBus bus)
        {
            _dropValidator = dropValidator;
            _placement = placement;
            _animation = animation;
            _pool = pool;
            _input = input;
            _signalBus = bus;
        }

        public void ClearInstant()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
                _pool.Despawn(_stack[i]);
            _stack.Clear();
        }

        public void SpawnAtInstant(BlockConfig config, Vector2 anchoredPos)
        {
            if (_isCollapsing) return;
            var element = _pool.Spawn();
            element.transform.SetParent(_towerArea, false);
            element.Initialize(config, this, -1);
            element.Rect.anchoredPosition = anchoredPos;
            _stack.Add(element);
        }

        public bool CanDrop(IDragTarget drag)
        {
            if (_isCollapsing) return false;
            if (drag is not Block element) return false;
            if (IsElementFromTower(element)) return false;
            if (!PassesHeightLimit(element)) return false;
            return _stack.Count == 0 || IsPointerInTopCorridor();
        }

        public void OnDrop(IDragTarget drag)
        {
            if (_isCollapsing) return;
            if (drag is not Block source) return;

            var target = CalculateTargetPosition(source);
            var copy = SpawnCopyUnderTower(source.Config);

            if (_stack.Count == 0)
                HandleDropOnEmptyStack(copy, target);
            else
                HandleDropOnExistingStack(source, copy, target);
        }

        public void OnBlockPicked(Block block, int slotIndex)
        {
        }

        public void OnBlockReleased(Block block, int slotIndex, bool success)
        {
        }

        public async UniTask RemoveElementAsync(Block target)
        {
            if (_isCollapsing) return;
            int idx = _stack.IndexOf(target);
            if (idx < 0) return;

            _stack.RemoveAt(idx);
            var moves = BuildCollapseMoves();

            _isCollapsing = true;
            try
            {
                await _animation.PlayCollapse(moves);
            }
            finally
            {
                _isCollapsing = false;
            }

            _pool.Despawn(target);
            _signalBus.TryFire(new StackUpdated());
        }

        private bool IsElementFromTower(Block block) => block.Owner is StackZone || block.SlotIndex < 0;

        private bool PassesHeightLimit(Block block)
        {
            if (_dropValidator.CanAdd(block, _stack, _towerArea, out var reason))
                return true;
            if (reason == "height_limit")
                _signalBus.Fire(new MaxStackReached());
            return false;
        }

        private bool IsPointerInTopCorridor()
        {
            var topRect = _stack[^1].Rect;
            var camera = GetCanvasCamera(_towerArea);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                topRect, _input.CursorPosition.Value, camera, out var localPoint);

            var topRectRect = topRect.rect;
            float height = topRectRect.height;

            var expanded = new Rect(
                topRectRect.xMin - _sideSlackPx,
                topRectRect.yMin,
                topRectRect.width + _sideSlackPx * 2f,
                topRectRect.height + height * _topSnapPercent
            );

            return expanded.Contains(localPoint);
        }

        private Vector2 CalculateTargetPosition(Block source)
        {
            float size = source.Rect.rect.height;
            if (_stack.Count == 0)
                return CalculateBottomPlacement(size);
            var topRect = _stack[^1].Rect;
            return _placement.CalculateAddPosition(_towerArea, topRect, size);
        }

        private Vector2 CalculateBottomPlacement(float size)
        {
            var camera = GetCanvasCamera(_towerArea);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _towerArea,
                _input.CursorPosition.Value,
                camera,
                out var local);

            float half = size * 0.5f;
            float areaHalfW = _towerArea.rect.width * 0.5f;
            float x = Mathf.Clamp(local.x, -areaHalfW + half, areaHalfW - half);
            float bottomY = -_towerArea.rect.height * 0.5f + half;
            return new Vector2(x, bottomY);
        }

        private Block SpawnCopyUnderTower(BlockConfig config)
        {
            var copy = _pool.Spawn();
            copy.transform.SetParent(_towerArea, false);
            copy.Initialize(config, this, -1);
            return copy;
        }

        private void HandleDropOnEmptyStack(Block copy, Vector2 target)
        {
            copy.Rect.anchoredPosition = target;
            _stack.Add(copy);
            _signalBus.Fire(new BlockSet());
            _signalBus.TryFire(new StackUpdated());
        }

        private void HandleDropOnExistingStack(Block source, Block copy, Vector2 target)
        {
            float rectHeight = copy.Rect.rect.height;
            var start = new Vector2(target.x, target.y - rectHeight * 0.6f);
            copy.Rect.anchoredPosition = start;
            _stack.Add(copy);
            _signalBus.Fire(new BlockSet());
            PlayAddAndNotifyAsync(copy, target).Forget();
        }

        private async UniTask PlayAddAndNotifyAsync(Block copy, Vector2 target)
        {
            await _animation.PlayAdd(copy, target);
            _signalBus.TryFire(new StackUpdated());
        }

        private List<(RectTransform rect, Vector2 to)> BuildCollapseMoves()
        {
            var moves = new List<(RectTransform rect, Vector2 to)>(_stack.Count);
            float areaHalfH = _towerArea.rect.height * 0.5f;
            float prevCenterY = 0f;
            float prevH = 0f;

            for (int i = 0; i < _stack.Count; i++)
            {
                var rect = _stack[i].Rect;
                float h = rect.rect.height;
                float x = rect.anchoredPosition.x;
                float y = i == 0
                    ? -areaHalfH + h * 0.5f
                    : prevCenterY + (prevH + h) * 0.5f;
                moves.Add((rect, new Vector2(x, y)));
                prevCenterY = y;
                prevH = h;
            }

            return moves;
        }

        private static Camera GetCanvasCamera(RectTransform rect)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            if (!canvas) return null;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
    }
}