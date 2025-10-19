using UnityEngine;

namespace _Project.Scripts.Services.Blocks
{
    [CreateAssetMenu(fileName = "BlockConfig", menuName = "Game/New BlockConfig")]
    public sealed class BlockConfig : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private ElementColor _color;

        public string Id => _id;
        public Sprite Sprite => _sprite;
    }
}