namespace _Project.Scripts.Services.Blocks.Interfaces
{
    public interface IElementContainer
    {
        void OnBlockPicked(Block block, int slotIndex);
        void OnBlockReleased(Block block, int slotIndex, bool success);
    }
}