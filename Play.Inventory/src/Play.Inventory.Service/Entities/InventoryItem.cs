using Play.Common;

namespace Play.Inventory.Service.Entities
{
    public class InventoryItem : IEntity
    {
        public Guid Id { get; set; }
        public Guid Userid { get; set; }
        public Guid CatalogitemId { get; set; }
        public int Quantity { get; set; }
        public DateTimeOffset AccquiredDate { get; set; }
    }
}