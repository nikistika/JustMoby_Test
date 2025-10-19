using _Project.Scripts.Services.Blocks.Interfaces;
using _Project.Scripts.Services.DragAndDrop.Interfaces;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Services.Blocks
{
    public class Block : MonoBehaviour, IDragTarget
    {
        private const float DragOpacity = 0.6f;
        private const float FullOpacity = 1f;

        [SerializeField] private RectTransform _rectTf;
        [SerializeField] private Image _imageComponent;

        private readonly Subject<Unit> _dragBegin = new();
        private readonly Subject<Unit> _dragEnd = new();

        private BlockConfig _config;
        private IElementContainer _owner;
        private int _slotIndex = -1;
        public IElementContainer Owner => _owner;
        public int SlotIndex => _slotIndex;
        public RectTransform Rect => _rectTf;
        public BlockConfig Config => _config;

        public void Initialize(BlockConfig config, IElementContainer owner, int slotIndex = -1)
        {
            if (config == null)
            {
                Debug.LogError($"Block.Initialize: config is null on '{name}'. Deactivating object.");
                gameObject.SetActive(false);
                return;
            }

            _config = config;
            _owner = owner;
            _slotIndex = slotIndex;

            if (_imageComponent == null)
                _imageComponent = GetComponent<Image>();
            if (_rectTf == null)
                _rectTf = GetComponent<RectTransform>();

            if (_imageComponent == null)
            {
                Debug.LogError($"Block.Initialize: Image component missing on '{name}'. Deactivating object.");
                gameObject.SetActive(false);
                return;
            }

            ApplyVisuals(config);

            gameObject.SetActive(true);
        }

        private void ApplyVisuals(BlockConfig config)
        {
            if (config == null || _imageComponent == null)
                return;

            if (config.Sprite != null)
                _imageComponent.sprite = config.Sprite;

            _imageComponent.raycastTarget = true;

            var baseColor = _imageComponent.color;
            _imageComponent.color = new Color(baseColor.r, baseColor.g, baseColor.b, FullOpacity);

            if (_rectTf != null)
                _rectTf.localScale = Vector3.one;
        }

        public void OnBeginDrag()
        {
            if (_imageComponent != null)
            {
                var color = _imageComponent.color;
                _imageComponent.color = new Color(color.r, color.g, color.b, DragOpacity);
            }

            _owner?.OnBlockPicked(this, _slotIndex);
            _dragBegin.OnNext(Unit.Default);
        }

        public void OnEndDrag(bool droppedSuccessfully)
        {
            if (_imageComponent != null)
            {
                var color = _imageComponent.color;
                _imageComponent.color = new Color(color.r, color.g, color.b, FullOpacity);
            }

            _owner?.OnBlockReleased(this, _slotIndex, droppedSuccessfully);
            _dragEnd.OnNext(Unit.Default);
        }

        private void OnDestroy()
        {
            _dragBegin.OnCompleted();
            _dragEnd.OnCompleted();
        }
    }
}