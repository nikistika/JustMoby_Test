using UnityEngine;
using Zenject;

namespace _Project.Scripts.Services.Blocks
{
    public class BlockPool : MonoMemoryPool<Block>
    {
        protected override void OnSpawned(Block item)
        {
            base.OnSpawned(item);

            if (item == null)
                return;

            item.gameObject.SetActive(true);

            var rect = item.Rect;
            if (rect != null)
            {
                ResetRect(rect);
            }
        }

        protected override void OnDespawned(Block item)
        {
            if (item != null)
            {
                if (item.Rect != null)
                    item.Rect.localScale = Vector3.one;

                item.gameObject.SetActive(false);
            }

            base.OnDespawned(item);
        }

        private static void ResetRect(RectTransform rectTransform)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }
    }
}