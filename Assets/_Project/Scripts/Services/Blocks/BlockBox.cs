using _Project.Scripts.Services.Blocks.Interfaces;
using UnityEngine;

namespace _Project.Scripts.Services.Blocks
{
    public abstract class BlockBox : MonoBehaviour, IElementContainer
    {
        public abstract void OnBlockPicked(Block block, int slotIndex);
        public abstract void OnBlockReleased(Block block, int slotIndex, bool success);
    }
}