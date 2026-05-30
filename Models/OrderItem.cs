using System.ComponentModel.DataAnnotations;

namespace Webbanhang.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        public int ProductId { get; set; }

        [Required, StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }
}
