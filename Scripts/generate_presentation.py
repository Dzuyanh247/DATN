#!/usr/bin/env python3
"""Generate the DATN PC Store graduation-project presentation.

Run locally from the repository root:
    pip install python-pptx
    python Scripts/generate_presentation.py

The generated file is written to output/DATN_PC_Store_Gioi_Thieu.pptx.
The output is intentionally ignored by Git; only this editable source belongs in the
repository.
"""

from pathlib import Path
from typing import Iterable

from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_CONNECTOR, MSO_SHAPE
from pptx.enum.text import MSO_ANCHOR, PP_ALIGN
from pptx.util import Inches, Pt


# -----------------------------------------------------------------------------
# Presentation settings: edit these values to update the look of every slide.
# Aptos and Arial both support Vietnamese characters on common office systems.
# -----------------------------------------------------------------------------
SLIDE_WIDTH = Inches(13.333)
SLIDE_HEIGHT = Inches(7.5)
FONT_HEAD = "Aptos Display"
FONT_BODY = "Arial"

NAVY = RGBColor(9, 28, 62)
BLUE = RGBColor(17, 94, 210)
CYAN = RGBColor(29, 188, 230)
PALE_BLUE = RGBColor(232, 244, 255)
WHITE = RGBColor(255, 255, 255)
INK = RGBColor(25, 39, 65)
MUTED = RGBColor(91, 108, 133)
BORDER = RGBColor(210, 224, 240)
GREEN = RGBColor(37, 176, 126)
ORANGE = RGBColor(245, 158, 11)
LIGHT = RGBColor(247, 250, 253)

REPO_ROOT = Path(__file__).resolve().parents[1]
OUTPUT_FILE = REPO_ROOT / "output" / "DATN_PC_Store_Gioi_Thieu.pptx"


def set_background(slide, color: RGBColor = WHITE) -> None:
    """Apply a solid background color to a slide."""
    fill = slide.background.fill
    fill.solid()
    fill.fore_color.rgb = color


def add_text(
    slide,
    text: str,
    left: float,
    top: float,
    width: float,
    height: float,
    *,
    size: int = 18,
    color: RGBColor = INK,
    bold: bool = False,
    font: str = FONT_BODY,
    align: PP_ALIGN = PP_ALIGN.LEFT,
    valign: MSO_ANCHOR = MSO_ANCHOR.TOP,
    margin: float = 0.04,
):
    """Add consistently formatted text and return its shape for later editing."""
    box = slide.shapes.add_textbox(Inches(left), Inches(top), Inches(width), Inches(height))
    frame = box.text_frame
    frame.clear()
    frame.word_wrap = True
    frame.margin_left = Inches(margin)
    frame.margin_right = Inches(margin)
    frame.margin_top = Inches(margin)
    frame.margin_bottom = Inches(margin)
    frame.vertical_anchor = valign
    paragraph = frame.paragraphs[0]
    paragraph.alignment = align
    run = paragraph.add_run()
    run.text = text
    run.font.name = font
    run.font.size = Pt(size)
    run.font.bold = bold
    run.font.color.rgb = color
    return box


def add_round_rect(
    slide,
    left: float,
    top: float,
    width: float,
    height: float,
    *,
    fill: RGBColor = WHITE,
    line: RGBColor = BORDER,
    radius_shape=MSO_SHAPE.ROUNDED_RECTANGLE,
):
    """Add a reusable rounded card shape."""
    shape = slide.shapes.add_shape(
        radius_shape, Inches(left), Inches(top), Inches(width), Inches(height)
    )
    shape.fill.solid()
    shape.fill.fore_color.rgb = fill
    shape.line.color.rgb = line
    shape.line.width = Pt(1)
    return shape


def add_accent_bar(slide, left: float, top: float, width: float = 0.65) -> None:
    """Add the blue-to-cyan visual accent used near headings."""
    bar = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE,
        Inches(left),
        Inches(top),
        Inches(width),
        Inches(0.09),
    )
    bar.fill.solid()
    bar.fill.fore_color.rgb = CYAN
    bar.line.fill.background()


def add_header(slide, number: str, title: str, subtitle: str = "") -> None:
    """Add the standard header used by content slides."""
    add_text(slide, number, 0.7, 0.47, 0.55, 0.35, size=12, color=BLUE, bold=True)
    add_text(slide, title, 1.25, 0.32, 8.9, 0.62, size=28, color=NAVY, bold=True, font=FONT_HEAD)
    add_accent_bar(slide, 0.72, 1.04, 1.15)
    if subtitle:
        add_text(slide, subtitle, 8.85, 0.45, 3.75, 0.4, size=11, color=MUTED, align=PP_ALIGN.RIGHT)


def add_footer(slide, page_number: int, dark: bool = False) -> None:
    """Add a restrained footer and page number."""
    color = RGBColor(174, 198, 226) if dark else MUTED
    line = slide.shapes.add_connector(
        MSO_CONNECTOR.STRAIGHT,
        Inches(0.72),
        Inches(7.05),
        Inches(12.62),
        Inches(7.05),
    )
    line.line.color.rgb = RGBColor(59, 91, 133) if dark else BORDER
    line.line.width = Pt(0.75)
    add_text(slide, "DATN • PC STORE", 0.72, 7.1, 2.8, 0.22, size=9, color=color, bold=True)
    add_text(slide, f"{page_number:02d}", 11.95, 7.09, 0.65, 0.22, size=9, color=color, bold=True, align=PP_ALIGN.RIGHT)


def add_bullet_list(
    slide,
    items: Iterable[str],
    left: float,
    top: float,
    width: float,
    height: float,
    *,
    size: int = 17,
    color: RGBColor = INK,
    bullet_color: RGBColor = BLUE,
    spacing: int = 10,
) -> None:
    """Add a manual bullet list for predictable styling across PowerPoint versions."""
    items = list(items)
    row_height = height / max(len(items), 1)
    for index, item in enumerate(items):
        y = top + index * row_height
        dot = slide.shapes.add_shape(
            MSO_SHAPE.OVAL, Inches(left), Inches(y + 0.13), Inches(0.11), Inches(0.11)
        )
        dot.fill.solid()
        dot.fill.fore_color.rgb = bullet_color
        dot.line.fill.background()
        text = add_text(slide, item, left + 0.25, y, width - 0.25, row_height, size=size, color=color)
        text.text_frame.paragraphs[0].space_after = Pt(spacing)


def add_icon_badge(slide, label: str, left: float, top: float, color: RGBColor = BLUE) -> None:
    """Add a text-based icon badge; avoids external images and binary assets."""
    circle = slide.shapes.add_shape(MSO_SHAPE.OVAL, Inches(left), Inches(top), Inches(0.56), Inches(0.56))
    circle.fill.solid()
    circle.fill.fore_color.rgb = color
    circle.line.fill.background()
    add_text(
        slide,
        label,
        left,
        top + 0.01,
        0.56,
        0.51,
        size=16,
        color=WHITE,
        bold=True,
        align=PP_ALIGN.CENTER,
        valign=MSO_ANCHOR.MIDDLE,
        margin=0,
    )


def add_metric_card(slide, value: str, label: str, left: float, top: float, color: RGBColor) -> None:
    """Add a compact project metric card."""
    add_round_rect(slide, left, top, 2.32, 1.28, fill=WHITE, line=BORDER)
    add_text(slide, value, left + 0.18, top + 0.16, 1.96, 0.48, size=24, color=color, bold=True, font=FONT_HEAD)
    add_text(slide, label, left + 0.18, top + 0.72, 1.96, 0.35, size=11, color=MUTED)


def add_feature_card(
    slide,
    title: str,
    description: str,
    badge: str,
    left: float,
    top: float,
    width: float,
    accent: RGBColor = BLUE,
) -> None:
    """Add a feature card. Edit title/description where slides are assembled below."""
    add_round_rect(slide, left, top, width, 1.45, fill=WHITE, line=BORDER)
    add_icon_badge(slide, badge, left + 0.22, top + 0.25, accent)
    add_text(slide, title, left + 0.92, top + 0.19, width - 1.12, 0.36, size=16, color=NAVY, bold=True)
    add_text(slide, description, left + 0.92, top + 0.61, width - 1.12, 0.63, size=11, color=MUTED)


def add_section_label(slide, text: str, left: float, top: float, color: RGBColor = BLUE) -> None:
    """Add a small uppercase section label."""
    add_text(slide, text.upper(), left, top, 3.2, 0.3, size=10, color=color, bold=True)


def add_cover(prs: Presentation) -> None:
    """Slide 1 — Cover. Edit author/school details in this function."""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    set_background(slide, NAVY)

    # Decorative technology grid and circuit nodes.
    for x in (8.8, 9.65, 10.5, 11.35, 12.2):
        line = slide.shapes.add_connector(
            MSO_CONNECTOR.STRAIGHT, Inches(x), Inches(0), Inches(x), Inches(7.5)
        )
        line.line.color.rgb = RGBColor(24, 58, 104)
        line.line.width = Pt(0.6)
    for y in (0.85, 1.7, 2.55, 3.4, 4.25, 5.1, 5.95, 6.8):
        line = slide.shapes.add_connector(
            MSO_CONNECTOR.STRAIGHT, Inches(8.25), Inches(y), Inches(13.333), Inches(y)
        )
        line.line.color.rgb = RGBColor(24, 58, 104)
        line.line.width = Pt(0.6)
    for x, y in ((9.65, 1.7), (11.35, 2.55), (10.5, 4.25), (12.2, 5.95)):
        node = slide.shapes.add_shape(MSO_SHAPE.OVAL, Inches(x - 0.07), Inches(y - 0.07), Inches(0.14), Inches(0.14))
        node.fill.solid()
        node.fill.fore_color.rgb = CYAN
        node.line.fill.background()

    add_section_label(slide, "Đồ án tốt nghiệp • 2026", 0.8, 0.72, CYAN)
    add_text(slide, "DATN", 0.76, 1.45, 5.4, 0.85, size=48, color=WHITE, bold=True, font=FONT_HEAD)
    add_text(slide, "PC STORE", 0.76, 2.22, 6.2, 1.0, size=54, color=CYAN, bold=True, font=FONT_HEAD)
    add_text(
        slide,
        "XÂY DỰNG WEBSITE THƯƠNG MẠI ĐIỆN TỬ\nKINH DOANH MÁY TÍNH & LINH KIỆN",
        0.82,
        3.42,
        6.8,
        1.05,
        size=20,
        color=WHITE,
        bold=True,
        font=FONT_HEAD,
    )
    add_text(
        slide,
        "Nền tảng mua sắm, cấu hình PC và quản trị bán hàng tập trung",
        0.82,
        4.72,
        6.45,
        0.6,
        size=15,
        color=RGBColor(179, 205, 235),
    )
    add_round_rect(slide, 0.82, 5.68, 5.62, 0.72, fill=RGBColor(14, 45, 88), line=RGBColor(44, 83, 132))
    add_text(slide, "Sinh viên: ................................    •    GVHD: ................................", 1.05, 5.89, 5.2, 0.28, size=11, color=WHITE)

    # Abstract monitor illustration made entirely from editable PowerPoint shapes.
    monitor = add_round_rect(slide, 8.55, 1.3, 3.85, 3.2, fill=RGBColor(13, 42, 81), line=CYAN)
    monitor.line.width = Pt(2)
    add_round_rect(slide, 8.83, 1.58, 3.29, 2.55, fill=RGBColor(232, 246, 255), line=RGBColor(77, 127, 183))
    add_text(slide, "PC", 9.25, 2.05, 2.45, 0.72, size=40, color=BLUE, bold=True, font=FONT_HEAD, align=PP_ALIGN.CENTER)
    add_text(slide, "STORE", 9.25, 2.78, 2.45, 0.4, size=17, color=NAVY, bold=True, align=PP_ALIGN.CENTER)
    stand = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(10.15), Inches(4.48), Inches(0.68), Inches(0.62))
    stand.fill.solid(); stand.fill.fore_color.rgb = CYAN; stand.line.fill.background()
    base = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(9.55), Inches(5.02), Inches(1.88), Inches(0.18))
    base.fill.solid(); base.fill.fore_color.rgb = CYAN; base.line.fill.background()
    add_footer(slide, 1, dark=True)


def add_introduction(prs: Presentation) -> None:
    """Slide 2 — Project context and overview."""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    set_background(slide, LIGHT)
    add_header(slide, "01", "GIỚI THIỆU ĐỀ TÀI", "TỔNG QUAN DỰ ÁN")

    add_round_rect(slide, 0.72, 1.42, 5.2, 4.88, fill=NAVY, line=NAVY)
    add_section_label(slide, "Bối cảnh", 1.05, 1.78, CYAN)
    add_text(slide, "Mua sắm công nghệ\nđang dịch chuyển mạnh\nsang trực tuyến.", 1.02, 2.18, 4.45, 1.65, size=27, color=WHITE, bold=True, font=FONT_HEAD)
    add_text(slide, "Khách hàng cần thông tin rõ ràng, cấu hình phù hợp và quy trình đặt hàng thuận tiện.", 1.04, 4.2, 4.25, 0.95, size=15, color=RGBColor(191, 213, 238))
    add_text(slide, "PC STORE", 1.04, 5.48, 2.0, 0.34, size=12, color=CYAN, bold=True)
    add_text(slide, "Kết nối nhu cầu • sản phẩm • vận hành", 1.04, 5.82, 4.0, 0.35, size=11, color=WHITE)

    add_section_label(slide, "Giải pháp", 6.42, 1.55)
    add_text(slide, "Một hệ thống thương mại điện tử chuyên biệt cho máy tính và linh kiện.", 6.42, 1.93, 5.65, 0.8, size=23, color=NAVY, bold=True, font=FONT_HEAD)
    add_bullet_list(
        slide,
        [
            "Trải nghiệm mua sắm trực quan cho khách hàng.",
            "Hỗ trợ xây dựng và kiểm tra cấu hình PC.",
            "Quản trị sản phẩm, đơn hàng và hậu mãi tập trung.",
            "Số hóa quy trình bán hàng từ tiếp cận đến bảo hành.",
        ],
        6.45,
        3.05,
        5.55,
        2.65,
        size=15,
    )
    add_footer(slide, 2)


def add_objectives(prs: Presentation) -> None:
    """Slide 3 — Project objectives."""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    set_background(slide, WHITE)
    add_header(slide, "02", "MỤC TIÊU DỰ ÁN", "PROJECT OBJECTIVES")
    add_text(slide, "Xây dựng một nền tảng ổn định, dễ sử dụng và có khả năng mở rộng.", 0.76, 1.35, 8.9, 0.55, size=18, color=MUTED)

    objectives = [
        ("01", "Trải nghiệm", "Tối ưu hành trình tìm kiếm, so sánh, chọn cấu hình và mua hàng.", BLUE),
        ("02", "Nghiệp vụ", "Đảm bảo quy trình giỏ hàng, thanh toán, đơn hàng và tồn kho nhất quán.", CYAN),
        ("03", "Quản trị", "Cung cấp công cụ vận hành tập trung, trực quan và tiết kiệm thời gian.", GREEN),
        ("04", "Kiến trúc", "Ứng dụng mô hình MVC, phân lớp rõ ràng và dữ liệu có cấu trúc.", ORANGE),
    ]
    for index, (num, title, description, color) in enumerate(objectives):
        left = 0.74 + index * 3.12
        add_round_rect(slide, left, 2.18, 2.82, 3.48, fill=LIGHT, line=BORDER)
        add_text(slide, num, left + 0.24, 2.43, 0.65, 0.45, size=15, color=color, bold=True)
        accent = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(left + 0.24), Inches(3.05), Inches(0.1), Inches(1.75))
        accent.fill.solid(); accent.fill.fore_color.rgb = color; accent.line.fill.background()
        add_text(slide, title, left + 0.55, 3.0, 1.95, 0.5, size=20, color=NAVY, bold=True, font=FONT_HEAD)
        add_text(slide, description, left + 0.55, 3.64, 1.95, 1.35, size=13, color=MUTED)
        add_text(slide, "KẾT QUẢ", left + 0.55, 5.12, 1.4, 0.25, size=9, color=color, bold=True)
    add_footer(slide, 3)


def add_technologies(prs: Presentation) -> None:
    """Slide 4 — Technologies used by the project."""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    set_background(slide, LIGHT)
    add_header(slide, "03", "CÔNG NGHỆ SỬ DỤNG", "TECH STACK")

    stack = [
        (".NET", "ASP.NET Core MVC", "Nền tảng backend & điều phối nghiệp vụ", BLUE),
        ("RZ", "Razor Views", "Giao diện render phía máy chủ", CYAN),
        ("EF", "Entity Framework Core", "ORM, truy vấn và quản lý dữ liệu", GREEN),
        ("SQL", "SQL Server", "Lưu trữ dữ liệu nghiệp vụ", ORANGE),
        ("UI", "HTML • CSS • JavaScript", "Trình bày và tương tác người dùng", BLUE),
        ("RT", "SignalR", "Hỗ trợ trao đổi theo thời gian thực", CYAN),
    ]
    for index, (badge, title, description, color) in enumerate(stack):
        row, col = divmod(index, 3)
        add_feature_card(slide, title, description, badge, 0.74 + col * 4.17, 1.55 + row * 1.75, 3.8, color)

    add_round_rect(slide, 0.74, 5.28, 12.0, 0.82, fill=NAVY, line=NAVY)
    add_text(slide, "KIẾN TRÚC", 1.0, 5.52, 1.25, 0.25, size=10, color=CYAN, bold=True)
    add_text(slide, "Client  →  MVC Controller  →  Service Layer  →  Entity Framework Core  →  SQL Server", 2.3, 5.45, 9.85, 0.35, size=15, color=WHITE, bold=True, align=PP_ALIGN.CENTER)
    add_footer(slide, 4)


def add_customer_features(prs: Presentation) -> None:
    """Slide 5 — Customer-facing capabilities."""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    set_background(slide, WHITE)
    add_header(slide, "04", "CHỨC NĂNG KHÁCH HÀNG", "CUSTOMER EXPERIENCE")

    features = [
        ("Tìm kiếm & lọc", "Khám phá sản phẩm theo danh mục, giá và nhu cầu.", "01", BLUE),
        ("Chi tiết & so sánh", "Xem thông số, hình ảnh và đối chiếu lựa chọn.", "02", CYAN),
        ("Build PC", "Lựa chọn linh kiện và hỗ trợ kiểm tra tương thích.", "03", GREEN),
        ("Giỏ hàng", "Quản lý sản phẩm cho cả khách và tài khoản đăng nhập.", "04", ORANGE),
        ("Đặt hàng", "Nhập địa chỉ, phương thức thanh toán và xác nhận đơn.", "05", BLUE),
        ("Hậu mãi", "Tra cứu đơn, gửi yêu cầu bảo hành và nhận hỗ trợ.", "06", CYAN),
    ]
    for index, item in enumerate(features):
        row, col = divmod(index, 2)
        add_feature_card(slide, item[0], item[1], item[2], 0.76 + col * 6.12, 1.48 + row * 1.62, 5.72, item[3])
    add_text(slide, "MỤC TIÊU TRẢI NGHIỆM", 0.78, 6.5, 2.2, 0.25, size=9, color=BLUE, bold=True)
    add_text(slide, "Nhanh chóng  •  Minh bạch  •  Thuận tiện  •  Tin cậy", 3.05, 6.43, 8.9, 0.32, size=14, color=NAVY, bold=True)
    add_footer(slide, 5)


def add_admin_features(prs: Presentation) -> None:
    """Slide 6 — Administrative capabilities."""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    set_background(slide, NAVY)
    add_header(slide, "05", "CHỨC NĂNG QUẢN TRỊ", "ADMIN OPERATIONS")
    # Header colors are adjusted for the dark slide.
    for shape in slide.shapes:
        if shape.has_text_frame:
            for paragraph in shape.text_frame.paragraphs:
                for run in paragraph.runs:
                    if run.font.color.type is not None and run.font.color.rgb == NAVY:
                        run.font.color.rgb = WHITE

    add_text(slide, "Một bảng điều khiển cho toàn bộ hoạt động kinh doanh.", 0.76, 1.3, 7.2, 0.48, size=18, color=RGBColor(185, 208, 235))
    admin_items = [
        ("Sản phẩm & danh mục", "Giá bán • tồn kho • hình ảnh • khuyến mãi"),
        ("Đơn hàng", "Theo dõi • cập nhật trạng thái • xử lý thanh toán"),
        ("Khách hàng", "Tài khoản • phân quyền • lịch sử giao dịch"),
        ("Nội dung", "Banner • bài viết • thiết lập website"),
        ("Bảo hành", "Tiếp nhận • xử lý • phản hồi yêu cầu"),
        ("Hỗ trợ", "Trao đổi trực tuyến và quản lý hội thoại"),
    ]
    for index, (title, description) in enumerate(admin_items):
        row, col = divmod(index, 3)
        left = 0.76 + col * 4.13
        top = 2.03 + row * 1.72
        add_round_rect(slide, left, top, 3.77, 1.42, fill=RGBColor(14, 43, 82), line=RGBColor(42, 76, 118))
        add_text(slide, f"{index + 1:02d}", left + 0.22, top + 0.22, 0.56, 0.34, size=12, color=CYAN, bold=True)
        add_text(slide, title, left + 0.86, top + 0.17, 2.55, 0.4, size=16, color=WHITE, bold=True)
        add_text(slide, description, left + 0.86, top + 0.66, 2.55, 0.48, size=11, color=RGBColor(177, 203, 232))
    add_round_rect(slide, 0.76, 5.63, 12.0, 0.65, fill=CYAN, line=CYAN)
    add_text(slide, "TẬP TRUNG DỮ LIỆU   •   CHUẨN HÓA QUY TRÌNH   •   NÂNG CAO HIỆU QUẢ VẬN HÀNH", 1.0, 5.84, 11.5, 0.25, size=12, color=NAVY, bold=True, align=PP_ALIGN.CENTER)
    add_footer(slide, 6, dark=True)


def add_ordering_process(prs: Presentation) -> None:
    """Slide 7 — Main ordering workflow."""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    set_background(slide, LIGHT)
    add_header(slide, "06", "QUY TRÌNH ĐẶT HÀNG", "ORDERING PROCESS")
    add_text(slide, "Từ nhu cầu đến đơn hàng hoàn tất trong một luồng xuyên suốt.", 0.76, 1.3, 8.0, 0.45, size=17, color=MUTED)

    steps = [
        ("01", "Khám phá", "Tìm kiếm\n& lọc"),
        ("02", "Lựa chọn", "Xem chi tiết\n& so sánh"),
        ("03", "Giỏ hàng", "Kiểm tra\nsản phẩm"),
        ("04", "Thanh toán", "Địa chỉ\n& phương thức"),
        ("05", "Xác nhận", "Tạo đơn\n& trừ tồn kho"),
        ("06", "Theo dõi", "Trạng thái\n& hậu mãi"),
    ]
    for index, (number, title, detail) in enumerate(steps):
        left = 0.75 + index * 2.08
        if index < len(steps) - 1:
            connector = slide.shapes.add_connector(
                MSO_CONNECTOR.STRAIGHT,
                Inches(left + 1.26),
                Inches(3.02),
                Inches(left + 2.0),
                Inches(3.02),
            )
            connector.line.color.rgb = RGBColor(150, 183, 219)
            connector.line.width = Pt(2)
        circle = slide.shapes.add_shape(MSO_SHAPE.OVAL, Inches(left + 0.28), Inches(2.51), Inches(1.02), Inches(1.02))
        circle.fill.solid(); circle.fill.fore_color.rgb = BLUE if index < 5 else GREEN; circle.line.fill.background()
        add_text(slide, number, left + 0.28, 2.74, 1.02, 0.38, size=17, color=WHITE, bold=True, align=PP_ALIGN.CENTER)
        add_text(slide, title, left, 3.83, 1.58, 0.38, size=16, color=NAVY, bold=True, align=PP_ALIGN.CENTER)
        add_text(slide, detail, left, 4.35, 1.58, 0.72, size=12, color=MUTED, align=PP_ALIGN.CENTER)

    add_round_rect(slide, 1.42, 5.55, 10.45, 0.73, fill=WHITE, line=BORDER)
    add_text(slide, "NGUYÊN TẮC", 1.7, 5.79, 1.25, 0.24, size=9, color=BLUE, bold=True)
    add_text(slide, "Kiểm tra dữ liệu  →  Transaction  →  Cập nhật tồn kho  →  Xóa giỏ hàng  →  Thông báo kết quả", 3.0, 5.7, 8.45, 0.37, size=13, color=INK, bold=True, align=PP_ALIGN.CENTER)
    add_footer(slide, 7)


def add_website_interface(prs: Presentation) -> None:
    """Slide 8 — Editable website-interface mockups; replace with screenshots if desired."""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    set_background(slide, WHITE)
    add_header(slide, "07", "GIAO DIỆN WEBSITE", "WEBSITE INTERFACE")
    add_text(slide, "Thiết kế nhất quán, ưu tiên sản phẩm và khả năng thao tác.", 0.76, 1.3, 8.5, 0.42, size=17, color=MUTED)

    # Browser mockup — can be replaced by an actual project screenshot later.
    add_round_rect(slide, 0.76, 1.92, 7.72, 4.35, fill=LIGHT, line=RGBColor(172, 192, 215))
    toolbar = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.77), Inches(1.93), Inches(7.7), Inches(0.45))
    toolbar.fill.solid(); toolbar.fill.fore_color.rgb = RGBColor(226, 235, 244); toolbar.line.fill.background()
    for index, color in enumerate((RGBColor(239, 93, 93), ORANGE, GREEN)):
        dot = slide.shapes.add_shape(MSO_SHAPE.OVAL, Inches(0.97 + index * 0.24), Inches(2.07), Inches(0.1), Inches(0.1))
        dot.fill.solid(); dot.fill.fore_color.rgb = color; dot.line.fill.background()
    add_round_rect(slide, 1.85, 2.02, 4.8, 0.22, fill=WHITE, line=BORDER)
    add_text(slide, "pcstore.local", 3.48, 2.01, 1.55, 0.19, size=7, color=MUTED, align=PP_ALIGN.CENTER, margin=0)

    hero = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.98), Inches(2.62), Inches(7.27), Inches(1.25))
    hero.fill.solid(); hero.fill.fore_color.rgb = NAVY; hero.line.fill.background()
    add_text(slide, "BUILD YOUR\nDREAM PC", 1.28, 2.84, 2.25, 0.7, size=20, color=WHITE, bold=True, font=FONT_HEAD)
    add_round_rect(slide, 3.88, 2.83, 1.05, 0.42, fill=CYAN, line=CYAN)
    add_text(slide, "KHÁM PHÁ", 3.89, 2.94, 1.03, 0.16, size=7, color=NAVY, bold=True, align=PP_ALIGN.CENTER, margin=0)
    for index in range(3):
        left = 1.0 + index * 2.42
        add_round_rect(slide, left, 4.15, 2.05, 1.67, fill=WHITE, line=BORDER)
        product = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(left + 0.17), Inches(4.34), Inches(0.72), Inches(0.72))
        product.fill.solid(); product.fill.fore_color.rgb = PALE_BLUE; product.line.fill.background()
        add_text(slide, "PC", left + 0.17, 4.53, 0.72, 0.24, size=12, color=BLUE, bold=True, align=PP_ALIGN.CENTER, margin=0)
        add_text(slide, ("PC Gaming", "Laptop", "Linh kiện")[index], left + 1.0, 4.35, 0.88, 0.36, size=10, color=NAVY, bold=True)
        add_text(slide, ("Hiệu năng", "Di động", "Nâng cấp")[index], left + 1.0, 4.79, 0.88, 0.25, size=8, color=MUTED)
        add_text(slide, "Xem ngay  →", left + 1.0, 5.31, 0.88, 0.22, size=8, color=BLUE, bold=True)

    add_section_label(slide, "Nguyên tắc thiết kế", 9.05, 1.98)
    design_points = [
        ("01", "Điều hướng rõ ràng", "Danh mục và tác vụ chính dễ tiếp cận."),
        ("02", "Thông tin trực quan", "Nhấn mạnh giá, cấu hình và trạng thái."),
        ("03", "Responsive", "Thích ứng với máy tính và thiết bị di động."),
        ("04", "Nhất quán", "Màu sắc, khoảng cách và thành phần đồng bộ."),
    ]
    for index, (num, title, desc) in enumerate(design_points):
        top = 2.47 + index * 0.93
        add_text(slide, num, 9.05, top, 0.45, 0.3, size=10, color=CYAN, bold=True)
        add_text(slide, title, 9.57, top - 0.03, 2.55, 0.31, size=13, color=NAVY, bold=True)
        add_text(slide, desc, 9.57, top + 0.32, 2.65, 0.38, size=10, color=MUTED)
    add_footer(slide, 8)


def add_advantages(prs: Presentation) -> None:
    """Slide 9 — Project strengths and delivered value."""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    set_background(slide, LIGHT)
    add_header(slide, "08", "ƯU ĐIỂM NỔI BẬT", "KEY ADVANTAGES")

    add_text(slide, "Giá trị cho khách hàng", 0.76, 1.48, 4.4, 0.45, size=22, color=NAVY, bold=True, font=FONT_HEAD)
    add_text(slide, "Giá trị cho vận hành", 6.78, 1.48, 4.4, 0.45, size=22, color=NAVY, bold=True, font=FONT_HEAD)
    customer = [
        "Danh mục sản phẩm chuyên biệt cho PC và linh kiện.",
        "Quy trình mua hàng liền mạch, thông tin minh bạch.",
        "Tính năng so sánh và build PC hỗ trợ ra quyết định.",
        "Tra cứu đơn hàng, bảo hành và hỗ trợ thuận tiện.",
    ]
    operations = [
        "Quản lý dữ liệu tập trung trên một hệ thống.",
        "Phân tách controller, service và tầng truy cập dữ liệu.",
        "Kiểm soát tồn kho gắn với quy trình tạo đơn.",
        "Nền tảng sẵn sàng mở rộng chức năng và tích hợp.",
    ]
    add_round_rect(slide, 0.76, 2.08, 5.55, 3.72, fill=WHITE, line=BORDER)
    add_round_rect(slide, 6.78, 2.08, 5.55, 3.72, fill=NAVY, line=NAVY)
    add_bullet_list(slide, customer, 1.08, 2.45, 4.75, 2.8, size=14, bullet_color=BLUE)
    add_bullet_list(slide, operations, 7.1, 2.45, 4.75, 2.8, size=14, color=WHITE, bullet_color=CYAN)

    add_metric_card(slide, "MVC", "Kiến trúc rõ ràng", 1.18, 5.5, BLUE)
    add_metric_card(slide, "E2E", "Luồng bán hàng", 3.88, 5.5, CYAN)
    add_metric_card(slide, "01", "Hệ thống tập trung", 6.58, 5.5, GREEN)
    add_metric_card(slide, "∞", "Khả năng mở rộng", 9.28, 5.5, ORANGE)
    add_footer(slide, 9)


def add_future_development(prs: Presentation) -> None:
    """Slide 10 — Suggested development roadmap."""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    set_background(slide, WHITE)
    add_header(slide, "09", "ĐỊNH HƯỚNG PHÁT TRIỂN", "FUTURE ROADMAP")
    add_text(slide, "Từ sản phẩm đồ án đến nền tảng thương mại có khả năng vận hành thực tế.", 0.76, 1.3, 9.8, 0.48, size=17, color=MUTED)

    roadmap = [
        ("GIAI ĐOẠN 01", "Hoàn thiện nền tảng", ["Kiểm thử tự động", "Tối ưu bảo mật", "Chuẩn hóa migration"], BLUE),
        ("GIAI ĐOẠN 02", "Mở rộng trải nghiệm", ["Gợi ý sản phẩm", "Đánh giá & xếp hạng", "PWA / mobile"], CYAN),
        ("GIAI ĐOẠN 03", "Tích hợp hệ sinh thái", ["Cổng thanh toán", "Đơn vị vận chuyển", "Thông báo đa kênh"], GREEN),
        ("GIAI ĐOẠN 04", "Phân tích & tăng trưởng", ["Dashboard nâng cao", "Dự báo tồn kho", "Cá nhân hóa bằng AI"], ORANGE),
    ]
    for index, (phase, title, bullets, color) in enumerate(roadmap):
        left = 0.74 + index * 3.12
        add_round_rect(slide, left, 2.05, 2.82, 3.9, fill=LIGHT, line=BORDER)
        top_bar = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(left), Inches(2.05), Inches(2.82), Inches(0.12))
        top_bar.fill.solid(); top_bar.fill.fore_color.rgb = color; top_bar.line.fill.background()
        add_text(slide, phase, left + 0.24, 2.4, 2.25, 0.28, size=9, color=color, bold=True)
        add_text(slide, title, left + 0.24, 2.86, 2.25, 0.78, size=19, color=NAVY, bold=True, font=FONT_HEAD)
        add_bullet_list(slide, bullets, left + 0.26, 3.9, 2.18, 1.5, size=12, bullet_color=color)
        add_text(slide, f"0{index + 1}", left + 2.08, 5.38, 0.45, 0.3, size=11, color=color, bold=True, align=PP_ALIGN.RIGHT)
    add_footer(slide, 10)


def add_thank_you(prs: Presentation) -> None:
    """Slide 11 — Closing and Q&A."""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    set_background(slide, NAVY)

    # Large subtle circles provide depth while keeping the slide professional.
    for left, top, size, color in (
        (8.6, -1.0, 5.5, RGBColor(13, 48, 91)),
        (10.3, 3.8, 3.9, RGBColor(14, 55, 101)),
        (-1.6, 5.2, 3.6, RGBColor(12, 43, 83)),
    ):
        circle = slide.shapes.add_shape(MSO_SHAPE.OVAL, Inches(left), Inches(top), Inches(size), Inches(size))
        circle.fill.solid(); circle.fill.fore_color.rgb = color; circle.line.fill.background()

    add_section_label(slide, "DATN • PC STORE", 0.84, 0.82, CYAN)
    add_text(slide, "XIN CẢM ƠN", 0.82, 1.62, 8.3, 0.95, size=48, color=WHITE, bold=True, font=FONT_HEAD)
    add_text(slide, "Q & A", 0.82, 2.55, 4.0, 0.88, size=40, color=CYAN, bold=True, font=FONT_HEAD)
    add_text(slide, "Rất mong nhận được ý kiến đóng góp\ntừ Hội đồng và Quý Thầy/Cô.", 0.88, 3.8, 6.8, 1.0, size=20, color=RGBColor(193, 214, 238), font=FONT_HEAD)
    add_round_rect(slide, 0.88, 5.36, 5.4, 0.82, fill=RGBColor(14, 45, 88), line=RGBColor(44, 83, 132))
    add_text(slide, "PC STORE  •  Graduation Project Presentation", 1.12, 5.62, 4.9, 0.28, size=11, color=WHITE, bold=True)

    # Closing monogram.
    ring = slide.shapes.add_shape(MSO_SHAPE.OVAL, Inches(9.2), Inches(1.48), Inches(2.78), Inches(2.78))
    ring.fill.background(); ring.line.color.rgb = CYAN; ring.line.width = Pt(3)
    add_text(slide, "PC", 9.2, 2.01, 2.78, 0.75, size=42, color=WHITE, bold=True, font=FONT_HEAD, align=PP_ALIGN.CENTER)
    add_text(slide, "STORE", 9.2, 2.82, 2.78, 0.42, size=16, color=CYAN, bold=True, align=PP_ALIGN.CENTER)
    add_footer(slide, 11, dark=True)


def build_presentation() -> Presentation:
    """Create all slides. Add, remove, or reorder slide functions here."""
    prs = Presentation()
    prs.slide_width = SLIDE_WIDTH
    prs.slide_height = SLIDE_HEIGHT

    # Each function below owns one editable slide in the final deck.
    add_cover(prs)
    add_introduction(prs)
    add_objectives(prs)
    add_technologies(prs)
    add_customer_features(prs)
    add_admin_features(prs)
    add_ordering_process(prs)
    add_website_interface(prs)
    add_advantages(prs)
    add_future_development(prs)
    add_thank_you(prs)

    # Core metadata appears in PowerPoint's File > Info panel.
    prs.core_properties.title = "DATN PC Store — Giới thiệu đồ án tốt nghiệp"
    prs.core_properties.subject = "Website thương mại điện tử kinh doanh máy tính và linh kiện"
    prs.core_properties.author = "DATN PC Store"
    prs.core_properties.keywords = "DATN, PC Store, ASP.NET Core MVC, thương mại điện tử"
    return prs


def main() -> None:
    """Create the output folder and save the generated PowerPoint locally."""
    OUTPUT_FILE.parent.mkdir(parents=True, exist_ok=True)
    presentation = build_presentation()
    presentation.save(OUTPUT_FILE)
    print(f"Đã tạo bài thuyết trình: {OUTPUT_FILE}")
    print(f"Tổng số slide: {len(presentation.slides)}")


if __name__ == "__main__":
    main()
