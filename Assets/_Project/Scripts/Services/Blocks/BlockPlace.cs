using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace _Project.Scripts.Services.Blocks
{
    public class BlockPlace : BlockBox
    {
        [Header("UI")] [SerializeField] private RectTransform _slotsRoot;
        [SerializeField] private ScrollRect _scroll;

        [Header("Data")] [SerializeField] private GameConfig _gameConfig;
        private readonly Dictionary<int, Image> _slotShadows = new();
        private readonly Dictionary<int, Block> _slotElements = new();
        private BlockPool _pool;

        [Inject]
        public void Construct(BlockPool pool) => _pool = pool;

        private void Start()
        {
            ValidateSerializedFields();
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            var configs = _gameConfig.Configurations;
            for (int index = 0; index < configs.Count; index++)
            {
                CreateSlotElement(index, configs[index]);
            }
        }

        public void RegisterShadow(int slotIndex, Image shadow)
        {
            if (shadow == null)
                return;
            _slotShadows[slotIndex] = shadow;
            shadow.gameObject.SetActive(false);
        }

        public override void OnBlockPicked(Block block, int slotIndex)
        {
            ToggleScroll(false);
            if (slotIndex < 0)
                return;
            ShowShadow(slotIndex);
        }

        public override void OnBlockReleased(Block block, int slotIndex, bool success)
        {
            ToggleScroll(true);
            if (slotIndex < 0)
                return;

            HideShadow(slotIndex);

            if (success)
            {
                _pool.Despawn(block);
                _slotElements.Remove(slotIndex);

                var config = block.Config;
                CreateSlotElement(slotIndex, config);
            }
        }

        private void CreateSlotElement(int slotIndex, BlockConfig config)
        {
            var element = _pool.Spawn();
            AttachUnderSlotsRoot(element.transform);
            element.Initialize(config, this, slotIndex);
            _slotElements[slotIndex] = element;
        }

        private void AttachUnderSlotsRoot(Transform t)
        {
            if (_slotsRoot == null)
                throw new InvalidOperationException("SlotsRoot is not assigned.");
            t.SetParent(_slotsRoot, false);
        }

        private void ToggleScroll(bool enabled)
        {
            if (_scroll != null)
                _scroll.enabled = enabled;
        }

        private void ShowShadow(int slotIndex)
        {
            if (_slotShadows.TryGetValue(slotIndex, out var shadow))
                shadow.gameObject.SetActive(true);
        }

        private void HideShadow(int slotIndex)
        {
            if (_slotShadows.TryGetValue(slotIndex, out var shadow))
                shadow.gameObject.SetActive(false);
        }

        private void ValidateSerializedFields()
        {
            if (_gameConfig == null) throw new InvalidOperationException("GameConfiguration is not assigned.");
            if (_slotsRoot == null) throw new InvalidOperationException("SlotsRoot is not assigned.");
            if (_pool == null) throw new InvalidOperationException("ElementsPool is not injected.");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_slotsRoot == null || _gameConfig == null)
            {
                EditorApplication.delayCall += () =>
                {
                    if (this == null) return;
                    Debug.LogWarning("[ElementPlace] Missing serialized references (GameConfiguration/SlotsRoot).");
                };
            }
        }
#endif
    }
}