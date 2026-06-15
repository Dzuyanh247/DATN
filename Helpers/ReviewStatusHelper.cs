using Datn.PcStore.Models;

namespace Datn.PcStore.Helpers;

public static class ReviewStatusHelper
{
    public static string Label(ReviewStatus status) => status switch
    {
        ReviewStatus.Pending => "Chờ duyệt",
        ReviewStatus.Approved => "Đã duyệt / Đang hiển thị",
        ReviewStatus.Hidden => "Đã ẩn",
        ReviewStatus.Rejected => "Từ chối",
        _ => "Không xác định"
    };

    public static string BadgeClass(ReviewStatus status) => status switch
    {
        ReviewStatus.Pending => "text-bg-warning",
        ReviewStatus.Approved => "text-bg-success",
        ReviewStatus.Hidden => "text-bg-secondary",
        ReviewStatus.Rejected => "text-bg-danger",
        _ => "text-bg-light"
    };
}
