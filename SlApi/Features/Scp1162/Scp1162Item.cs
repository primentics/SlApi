using System.Collections.Generic;

namespace SlApi.Features.Scp1162
{
    public class Scp1162Item
    {
        public ItemType AcceptedItem { get; set; }

        public Dictionary<ItemType, int> OutputItems { get; set; }
    }
}