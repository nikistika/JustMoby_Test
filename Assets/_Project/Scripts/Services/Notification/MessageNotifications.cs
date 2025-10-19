using System.Collections.Generic;

namespace _Project.Scripts.Services.Notification
{
    public static class MessageNotifications
    {
        public const string Placed = "element_add";
        public const string Missed = "element_miss";
        public const string Deleted = "element_destroy";
        public const string HeightLimit = "element_drop_max_tower";

        private static readonly Dictionary<string, string> _ruTexts = new()
        {
            { Placed, "Куб размещён" },
            { Missed, "Промах" },
            { Deleted, "Куб удалён" },
            { HeightLimit, "Достигнут предел высоты" },
        };

        public static string Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return string.Empty;

            return _ruTexts.GetValueOrDefault(id, id);
        }
    }
}