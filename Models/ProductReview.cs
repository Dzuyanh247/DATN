using System.ComponentModel.DataAnnotations;

namespace Datn.PcStore.Models;

public enum ReviewStatus
{
    Pending = 1,
    Approved = 2,
    Hidden = 3,
    Rejected = 4
}

public class ProductReview : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int OrderDetailId { get; set; }
    public OrderDetail? OrderDetail { get; set; }
    [Range(1, 5)] public int Rating { get; set; }
    [Required, StringLength(1000, MinimumLength = 10)] public string Comment { get; set; } = string.Empty;
    public ReviewStatus Status { get; set; } = ReviewStatus.Approved;
    [MaxLength(1000)] public string? AdminReply { get; set; }
    public DateTime? AdminRepliedAt { get; set; }
    public int? HandledByStaffId { get; set; }
    [MaxLength(100)] public string? HandledByStaffName { get; set; }
    public DateTime? HandledAt { get; set; }
    public int HelpfulCount { get; set; }
}
