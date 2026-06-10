#!/usr/bin/env python3
"""Generate the 11-slide DATN PC Store graduation-defense deck.

The presentation is assembled directly as OOXML so the repository does not need
python-pptx. Only vector shapes and text are embedded: no downloaded images,
base64 payloads, PDFs, videos, or raster assets. Functional claims are guarded
by a lightweight source audit before rendering.
"""
from __future__ import annotations

import re
import sys
import zipfile
from dataclasses import dataclass, field
from pathlib import Path
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "output"
PPTX_PATH = OUTPUT / "DATN_PC_Store_Gioi_Thieu.pptx"
SPEECH_PATH = OUTPUT / "presentation_speech.md"
TOTAL = 11
W, H, EMU = 13.333, 7.5, 914400
FONT = "Aptos"

C = {
    "navy": "003845", "blue": "1677FF", "cyan": "14D9FF", "orange": "FF6B00",
    "bg": "F8FAFC", "paper": "FFFFFF", "text": "1E293B", "muted": "64748B",
    "success": "10B981", "danger": "EF4444", "purple": "8B5CF6", "pink": "EC4899",
    "line": "DCE6F1", "ice": "EAF4FF", "mint": "E9FBF5", "warm": "FFF6DF",
    "dark": "002B36", "slate": "0F3D47", "soft": "F1F5F9",
}

SECTIONS = {
    1: ("PHẦN 01", "TỔNG QUAN ĐỀ TÀI", C["cyan"], "◎", "01"),
    2: ("PHẦN 02", "CHỨC NĂNG & CÔNG NGHỆ", C["blue"], "◇", "02"),
    3: ("PHẦN 03", "GIAO DIỆN DEMO", C["orange"], "▣", "03"),
    4: ("PHẦN 04", "ĐÁNH GIÁ & KẾT LUẬN", C["success"], "↗", "04"),
}


def section_for(number: int) -> int:
    if number <= 3: return 1
    if number <= 6: return 2
    if number <= 9: return 3
    return 4


@dataclass
class SourceFacts:
    dbsets: int
    controllers: int
    services: int
    user_groups: int = 4
    stack_cards: int = 8


def audit_source() -> SourceFacts:
    """Fail early if a claim used by the deck is no longer present in source."""
    required = {
        "Program.cs": ["AddSignalR", "UseSqlServer", "MapHub<ChatHub>", "IGhnShippingService", "IEmailSender"],
        "Controllers/BuildPcController.cs": ["BuildPcController"],
        "Controllers/CompareController.cs": ["CompareController", "Add/{productId:int}"],
        "Controllers/OrdersController.cs": ["BankTransfer", "TrackingStatus", "ConfirmTransferred"],
        "Controllers/SupportChatController.cs": ["CreateConversation", "SendMessage"],
        "Controllers/WarrantyController.cs": ["WarrantyController"],
        "Models/Entities.cs": ["OrderStatus", "BuildPcConfig", "WarrantyRequest"],
        "ViewModels/BuildPcVm.cs": ["BuildPcViewModel", "SelectedComponentViewModel"],
        "Services/BuildCompatibilityService.cs": ["BuildCompatibilityService"],
        "Data/ApplicationDbContext.cs": ["DbSet<Product>", "DbSet<ChatConversation>"],
        "Migrations/202606080001_AddSupportChat.cs": ["ChatConversations", "ChatMessages"],
        "Views/Products/Index.cshtml": ["ProductFilterVm"],
        "Views/Orders/BankTransfer.cshtml": ["QR", "PaymentExpireAt"],
        "Views/AdminDashboard/Index.cshtml": ["ProductCount", "OrderCount"],
        "wwwroot/js/chatbox.js": ["signalR"],
        "wwwroot/js/buildpc.js": ["/buildpc/select"],
        "wwwroot/css/admin.css": ["admin"],
    }
    missing: list[str] = []
    for rel, markers in required.items():
        path = ROOT / rel
        content = path.read_text(encoding="utf-8") if path.exists() else ""
        for marker in markers:
            if marker not in content:
                missing.append(f"{rel}: {marker}")
    if missing:
        raise RuntimeError("Source audit failed; missing verified features: " + ", ".join(missing))
    dbsets = len(re.findall(r"DbSet<", (ROOT / "Data/ApplicationDbContext.cs").read_text(encoding="utf-8")))
    controllers = len(list((ROOT / "Controllers").glob("*Controller.cs")))
    service_files = len(list((ROOT / "Services").glob("*.cs")))
    return SourceFacts(dbsets=dbsets, controllers=controllers, services=service_files)


@dataclass
class Element:
    kind: str
    x: float; y: float; w: float; h: float
    text: str = ""
    fill: str = "FFFFFF"
    fill2: str | None = None
    line: str = "FFFFFF"
    color: str = C["text"]
    size: int = 16
    bold: bool = False
    align: str = "l"
    valign: str = "ctr"
    geom: str = "roundRect"
    alpha: int = 100000
    line_alpha: int = 100000
    line_width: int = 12700
    dash: str | None = None
    shadow: bool = False
    rotation: int = 0
    arrow_end: str | None = None
    margin: float = 0.12


@dataclass
class Slide:
    number: int
    title: str
    layout: str
    transition: str | None = None
    elements: list[Element] = field(default_factory=list)
    speaker: list[str] = field(default_factory=list)
    animation: list[str] = field(default_factory=list)
    duration: str = "40–50 giây"
    image_note: str = "Không cần chèn ảnh."


class Canvas:
    def __init__(self, slide: Slide): self.s = slide

    def shape(self, x, y, w, h, *, fill="FFFFFF", line=None, geom="roundRect", alpha=100000,
              line_alpha=100000, line_width=12700, dash=None, shadow=False, rotation=0,
              fill2=None, arrow_end=None) -> None:
        self.s.elements.append(Element("shape", x, y, w, h, fill=fill, fill2=fill2,
            line=line or fill, geom=geom, alpha=alpha, line_alpha=line_alpha,
            line_width=line_width, dash=dash, shadow=shadow, rotation=rotation,
            arrow_end=arrow_end))

    def text(self, x, y, w, h, value, *, size=16, color=None, bold=False, align="l",
             valign="ctr", fill="FFFFFF", fill2=None, line=None, geom="rect", alpha=0,
             line_alpha=0, shadow=False, rotation=0, margin=.08) -> None:
        self.s.elements.append(Element("text", x, y, w, h, text=value, fill=fill, fill2=fill2,
            line=line or fill, color=color or C["text"], size=max(14, size), bold=bold, align=align,
            valign=valign, geom=geom, alpha=alpha, line_alpha=line_alpha, shadow=shadow,
            rotation=rotation, margin=margin))

    def line(self, x1, y1, x2, y2, *, color=None, width=19050, dash=None, alpha=100000,
             arrow=False) -> None:
        self.s.elements.append(Element("line", x1, y1, x2-x1, y2-y1, fill="FFFFFF",
            line=color or C["line"], line_width=width, dash=dash, line_alpha=alpha,
            arrow_end="triangle" if arrow else None, geom="line"))


# ---------- clean IT thesis deck (v2) ----------
def add_safe_background(c: Canvas, section: int, *, dark: bool = False, variant: int = 0) -> None:
    """Render a lightweight, PowerPoint-safe background using only large shapes."""
    accent = SECTIONS[section][2]
    base = C["dark"] if dark else C["bg"]
    c.shape(0, 0, W, H, fill=base, fill2=C["navy"] if dark else "EEF5FC", line=base, geom="rect")
    c.shape(10.55, -.72, 3.45, 3.45, fill=accent, alpha=9000 if dark else 6500,
            line=accent, line_alpha=0, geom="ellipse")
    c.shape(-1.05, 5.45, 2.85, 2.85, fill=C["cyan"], alpha=6500,
            line=C["cyan"], line_alpha=0, geom="ellipse")
    # A very light grid: four lines only, avoiding many small shapes.
    grid = "8EB6D9" if dark else "B8CCE0"
    for x in (3.3, 6.65, 10.0):
        c.line(x, 0, x, H, color=grid, width=7000, alpha=6500)
    for y in (2.5, 5.0):
        c.line(0, y, W, y, color=grid, width=7000, alpha=6500)
    c.text(10.60, .26, 1.95, .85, SECTIONS[section][4], size=48, color=accent,
           bold=True, align="r", fill=base, alpha=0, line_alpha=0, margin=0)


def add_title(c: Canvas, title: str, kicker: str | None = None, *, dark: bool = False) -> None:
    section = section_for(c.s.number)
    accent = SECTIONS[section][2]
    bg = C["dark"] if dark else C["bg"]
    c.text(.72, .32, 4.8, .24, kicker or SECTIONS[section][0], size=14, color=accent,
           bold=True, fill=bg, margin=0)
    c.text(.72, .62, 11.45, .52, title, size=27, color=C["paper"] if dark else C["text"],
           bold=True, fill=bg, margin=0)
    c.shape(.72, 1.22, .72, .055, fill=accent, line=accent, geom="rect")


def add_footer_progress(c: Canvas, *, dark: bool = False) -> None:
    section = section_for(c.s.number)
    accent = SECTIONS[section][2]
    bg = C["dark"] if dark else C["bg"]
    muted = "BCD0E5" if dark else C["muted"]
    c.text(.58, 7.04, 1.55, .20, "DATN PC Store", size=14, color=muted, bold=True, fill=bg)
    c.text(2.14, 7.04, 3.20, .20, SECTIONS[section][1], size=14, color=muted, fill=bg)
    c.shape(5.45, 7.14, 6.35, .045, fill="D7E1EB" if not dark else "29496C", line=bg, geom="rect")
    c.shape(5.45, 7.14, 6.35 * c.s.number / TOTAL, .045, fill=accent, line=accent, geom="rect")
    c.text(12.00, 7.04, .70, .20, f"{c.s.number:02d}/{TOTAL:02d}", size=14,
           color=accent, bold=True, align="r", fill=bg)


def add_card(c: Canvas, x, y, w, h, *, title: str = "", body: str = "", accent: str | None = None,
             dark: bool = False, title_size: int = 16, body_size: int = 14) -> None:
    accent = accent or SECTIONS[section_for(c.s.number)][2]
    fill = "102E52" if dark else C["paper"]
    c.shape(x, y, w, h, fill=fill, line="315275" if dark else C["line"], shadow=True)
    c.shape(x, y, .055, h, fill=accent, line=accent, geom="rect")
    c.text(x + .25, y + .20, w - .48, .34, title, size=title_size, color=accent,
           bold=True, fill=fill, margin=0)
    if body:
        c.text(x + .25, y + .66, w - .48, h - .82, body, size=body_size,
               color="D9E8F5" if dark else C["text"], fill=fill, valign="top", margin=0)


def add_section_divider(number: int, title: str, subtitle: str, icon: str = "") -> Slide:
    s = Slide(number, title, f"section-{number}")
    c = Canvas(s)
    section = section_for(number)
    add_safe_background(c, section, dark=True)
    accent = SECTIONS[section][2]
    c.text(.82, 1.42, 3.2, .28, SECTIONS[section][0], size=14, color=accent, bold=True, fill=C["dark"])
    c.text(.82, 2.02, 10.9, .82, title, size=36, color=C["paper"], bold=True, fill=C["dark"], margin=0)
    c.text(.84, 3.04, 8.9, .48, subtitle, size=18, color="C7D9EA", fill=C["dark"], margin=0)
    c.shape(.84, 3.78, 2.1, .08, fill=accent, line=accent, geom="rect")
    c.text(10.1, 4.70, 1.65, 1.25, icon or SECTIONS[section][4], size=46, color=accent,
           bold=True, align="ctr", fill=C["dark"])
    add_footer_progress(c, dark=True)
    return s


def add_image_placeholder(c: Canvas, x: float, y: float, w: float, h: float, label: str,
                          url: str, *, accent: str | None = None, dark: bool = False) -> None:
    accent = accent or SECTIONS[section_for(c.s.number)][2]
    fill = "102E52" if dark else "F4F8FC"
    c.shape(x, y, w, h, fill=fill, line=accent, line_width=19050, dash="dash", shadow=True)
    c.text(x + .35, y + h * .35, w - .70, .40, "CHÈN ẢNH GIAO DIỆN THẬT", size=18,
           color=accent, bold=True, align="ctr", fill=fill)
    c.text(x + .35, y + h * .49, w - .70, .34, label, size=15,
           color=C["paper"] if dark else C["text"], bold=True, align="ctr", fill=fill)
    c.text(x + .35, y + h - .52, w - .70, .25, f"URL: {url}", size=14,
           color="BFD2E4" if dark else C["muted"], align="ctr", fill=fill)


def add_simple_architecture(c: Canvas) -> None:
    labels = ["Browser", "Controller", "Service", "EF Core", "SQL Server"]
    colors = [C["blue"], C["cyan"], C["purple"], C["orange"], C["success"]]
    for i, (label, color) in enumerate(zip(labels, colors)):
        x = .78 + i * 2.50
        c.shape(x, 2.35, 1.95, 1.02, fill=C["paper"], line=color, shadow=True)
        c.text(x + .08, 2.35, 1.79, 1.02, label, size=15, color=color, bold=True,
               align="ctr", fill=C["paper"])
        if i < 4:
            c.text(x + 2.00, 2.65, .42, .34, "→", size=20, color=C["muted"], bold=True,
                   align="ctr", fill=C["bg"])
    for i, label in enumerate(("SignalR", "GHN", "SMTP / QR")):
        x = 3.45 + i * 2.20
        c.shape(x, 4.12, 1.75, .50, fill="EAF4FF", line=C["line"])
        c.text(x, 4.12, 1.75, .50, label, size=14, color=C["navy"], bold=True,
               align="ctr", fill="EAF4FF")


def add_simple_database_groups(c: Canvas) -> None:
    groups = [
        ("Tài khoản", "Role · User\nPasswordResetOtp", C["blue"]),
        ("Sản phẩm", "Category · Product\nProductImage", C["cyan"]),
        ("Giỏ hàng", "Cart · CartItem", C["purple"]),
        ("Đơn hàng", "Order · OrderDetail\nWarrantyRequest", C["orange"]),
        ("Hỗ trợ", "ChatConversation\nChatMessage", C["success"]),
    ]
    for i, (title, body, color) in enumerate(groups):
        x = .65 + i * 2.52
        add_card(c, x, 2.00, 2.18, 2.72, title=title, body=body, accent=color,
                 title_size=15, body_size=14)


def add_stat_cards(c: Canvas, stats: list[tuple[str, str, str]]) -> None:
    for i, (value, label, color) in enumerate(stats[:5]):
        x = .62 + i * 2.52
        c.shape(x, 2.05, 2.18, 2.52, fill=C["paper"], line=C["line"], shadow=True)
        c.text(x + .15, 2.42, 1.88, .68, value, size=27, color=color, bold=True,
               align="ctr", fill=C["paper"])
        c.text(x + .18, 3.34, 1.82, .58, label, size=14, color=C["muted"], bold=True,
               align="ctr", fill=C["paper"])


def _base_slide(number: int, title: str, *, dark: bool = False) -> tuple[Slide, Canvas]:
    s = Slide(number, title, f"clean-{number:02d}")
    c = Canvas(s)
    add_safe_background(c, section_for(number), dark=dark, variant=number)
    add_title(c, title, dark=dark)
    add_footer_progress(c, dark=dark)
    return s, c


def _bullets(c: Canvas, items: list[str], x: float, y: float, w: float, *, dark: bool = False,
             accent: str | None = None, gap: float = .62) -> None:
    accent = accent or SECTIONS[section_for(c.s.number)][2]
    bg = C["dark"] if dark else C["paper"]
    for i, item in enumerate(items[:5]):
        yy = y + i * gap
        c.shape(x, yy + .08, .16, .16, fill=accent, line=accent, geom="ellipse")
        c.text(x + .30, yy, w - .30, .36, item, size=15,
               color=C["paper"] if dark else C["text"], bold=True, fill=bg, margin=0)


def add_demo_slide(number: int, title: str, url: str, bullets: list[str], label: str | None = None) -> Slide:
    s, c = _base_slide(number, title)
    add_image_placeholder(c, .68, 1.55, 8.05, 4.95, label or title.upper(), url, accent=C["cyan"])
    c.shape(9.05, 1.55, 3.60, 4.95, fill=C["paper"], line=C["line"], shadow=True)
    c.text(9.38, 1.88, 2.95, .36, "ĐIỂM TRÌNH BÀY", size=15, color=C["cyan"], bold=True, fill=C["paper"])
    _bullets(c, bullets, 9.38, 2.55, 2.88, gap=.82)
    s.image_note = f"Có. URL: {url}."
    return s


def _set_notes(slide: Slide, speech: list[str], emphasis: list[str]) -> Slide:
    slide.speaker = speech
    slide.animation = emphasis  # reused as concise emphasis metadata
    return slide


def build_slides(facts: SourceFacts) -> list[Slide]:
    """Build the focused 11-slide graduation-defense presentation."""
    slides: list[Slide] = []

    def notes(slide: Slide, speech: list[str], emphasis: list[str], image: str | None = None) -> None:
        slide.speaker = speech
        slide.animation = emphasis
        if image:
            slide.image_note = f"Có. URL: {image}."
        slides.append(slide)

    def base(number: int, title: str, kicker: str) -> tuple[Slide, Canvas]:
        slide = Slide(number, title, f"defense-{number:02d}", transition="fade")
        canvas = Canvas(slide)
        add_safe_background(canvas, section_for(number), variant=number)
        add_title(canvas, title, kicker)
        add_footer_progress(canvas)
        return slide, canvas

    def browser_frame(c: Canvas, x: float, y: float, w: float, h: float, label: str, url: str) -> None:
        c.shape(x, y, w, h, fill=C["paper"], line="CBD5E1", shadow=True)
        c.shape(x, y, w, .42, fill=C["dark"], line=C["dark"], geom="roundRect")
        for i, color in enumerate((C["danger"], C["orange"], C["success"])):
            c.shape(x + .20 + i * .22, y + .15, .10, .10, fill=color, line=color, geom="ellipse")
        c.shape(x + .95, y + .10, w - 1.18, .22, fill="163E49", line="163E49", geom="roundRect")
        c.text(x + 1.08, y + .10, w - 1.45, .22, url, size=14, color="D9F7FC", fill="163E49", margin=0)
        c.shape(x + .22, y + .64, w - .44, h - .88, fill="EDF4F7", line=C["cyan"],
                line_width=18000, dash="dash")
        c.text(x + .55, y + h * .40, w - 1.10, .42, "VỊ TRÍ ẢNH CHỤP WEBSITE", size=20,
               color=C["dark"], bold=True, align="ctr", fill="EDF4F7", margin=0)
        c.text(x + .70, y + h * .51, w - 1.40, .58, label, size=14,
               color=C["muted"], bold=True, align="ctr", fill="EDF4F7", margin=0)

    # 01 — Cover
    s = Slide(1, "Website bán máy tính & linh kiện", "cover", transition="fade")
    c = Canvas(s)
    add_safe_background(c, 1, dark=True)
    c.text(.80, .70, 5.6, .28, "ĐỒ ÁN TỐT NGHIỆP · CÔNG NGHỆ THÔNG TIN", size=14,
           color=C["cyan"], bold=True, fill=C["dark"], margin=0)
    c.text(.80, 1.48, 9.7, 1.18, "WEBSITE BÁN MÁY TÍNH\n& LINH KIỆN PC", size=36,
           color=C["paper"], bold=True, fill=C["dark"], valign="top", margin=0)
    c.text(.82, 2.92, 7.8, .44, "Nền tảng mua sắm, đặt hàng và quản trị tập trung", size=19,
           color="C7E7EC", fill=C["dark"], margin=0)
    c.shape(.82, 3.58, 2.20, .08, fill=C["orange"], line=C["orange"], geom="rect")
    c.shape(.80, 4.05, 7.05, 1.75, fill="0B3540", line="1C5662", shadow=True)
    c.text(1.08, 4.30, 3.15, .34, "SINH VIÊN", size=13, color=C["cyan"], bold=True, fill="0B3540", margin=0)
    c.text(1.08, 4.68, 3.15, .42, "........................................", size=17, color=C["paper"], bold=True, fill="0B3540", margin=0)
    c.text(4.55, 4.30, 2.95, .34, "LỚP · KHOA", size=13, color=C["orange"], bold=True, fill="0B3540", margin=0)
    c.text(4.55, 4.68, 2.95, .58, ".........................\n.........................", size=15, color=C["paper"], fill="0B3540", valign="top", margin=0)
    c.text(.82, 6.28, 5.0, .30, "NĂM THỰC HIỆN  ·  2026", size=14, color="B6D4DB", bold=True, fill=C["dark"], margin=0)
    c.text(9.35, 2.05, 2.75, 2.75, "PC\nSTORE", size=29, color=C["cyan"], bold=True,
           align="ctr", fill="0A323D", line=C["cyan"], geom="ellipse", alpha=100000, line_alpha=100000)
    add_footer_progress(c, dark=True)
    notes(s, [
        "Em xin kính chào hội đồng và thầy cô tham dự buổi bảo vệ đồ án tốt nghiệp.",
        "Đề tài của em là xây dựng website bán máy tính và linh kiện PC theo mô hình thương mại điện tử.",
        "Sản phẩm tập trung vào trải nghiệm mua sắm của khách hàng và khả năng vận hành tập trung của quản trị viên.",
        "Trong phần trình bày, em sẽ giới thiệu mục tiêu, công nghệ, chức năng chính, giao diện và định hướng phát triển."
    ], ["Bài toán thương mại điện tử thực tế", "Hai nhóm người dùng: khách hàng và quản trị"])

    # 02 — Introduction
    s, c = base(2, "Giới thiệu đề tài", "01 · BỐI CẢNH & MỤC TIÊU")
    add_card(c, .72, 1.55, 3.72, 4.75, title="VÌ SAO CHỌN ĐỀ TÀI?",
             body="Thị trường PC có nhiều nhóm sản phẩm, thông số kỹ thuật và mức giá. Một website chuyên biệt giúp chuẩn hóa thông tin, giảm thời gian tư vấn và tạo môi trường để vận dụng kiến thức phát triển web vào bài toán thực tế.", accent=C["orange"], body_size=15)
    add_card(c, 4.66, 1.55, 3.72, 4.75, title="NHU CẦU NGƯỜI DÙNG",
             body="Khách hàng cần tìm kiếm nhanh, lọc đúng nhu cầu, so sánh giá và kiểm tra tình trạng đơn hàng. Với linh kiện máy tính, nội dung rõ ràng và quy trình thanh toán minh bạch là yếu tố quyết định niềm tin.", accent=C["cyan"], body_size=15)
    add_card(c, 8.60, 1.55, 3.98, 4.75, title="MỤC TIÊU XÂY DỰNG",
             body="Xây dựng hệ thống mua sắm liền mạch từ xem sản phẩm đến đặt hàng; đồng thời cung cấp trang quản trị để cập nhật danh mục, xử lý đơn, theo dõi người dùng và đánh giá hoạt động kinh doanh.", accent=C["success"], body_size=15)
    notes(s, [
        "Đề tài xuất phát từ thực tế sản phẩm công nghệ có nhiều thông số và khách hàng thường mất thời gian đối chiếu.",
        "Nhu cầu không chỉ dừng ở xem giá mà còn gồm tìm kiếm, lọc, đặt hàng và theo dõi sau mua.",
        "Vì vậy hệ thống được định hướng như một quy trình mua sắm hoàn chỉnh thay vì chỉ là trang trưng bày sản phẩm.",
        "Mục tiêu cuối cùng là cân bằng giữa trải nghiệm khách hàng và hiệu quả quản trị của cửa hàng."
    ], ["Bài toán có nhu cầu thực tế", "Mục tiêu là một quy trình mua sắm hoàn chỉnh"])

    # 03 — System overview
    s, c = base(3, "Tổng quan hệ thống", "01 · PHẠM VI GIẢI PHÁP")
    add_card(c, .72, 1.52, 2.78, 2.08, title="ĐỐI TƯỢNG SỬ DỤNG",
             body="Khách vãng lai, khách đã đăng nhập và quản trị viên. Mỗi nhóm có quyền truy cập và hành trình sử dụng riêng.", accent=C["blue"], body_size=14)
    add_card(c, 3.72, 1.52, 2.78, 2.08, title="CHỨC NĂNG CỐT LÕI",
             body="Tra cứu sản phẩm, giỏ hàng, checkout, theo dõi đơn và quản lý dữ liệu bán hàng tại khu vực admin.", accent=C["cyan"], body_size=14)
    add_card(c, 6.72, 1.52, 2.78, 2.08, title="NỀN TẢNG CÔNG NGHỆ",
             body="ASP.NET Core MVC, Entity Framework Core, SQL Server và giao diện responsive bằng CSS/JavaScript.", accent=C["purple"], body_size=14)
    add_card(c, 9.72, 1.52, 2.86, 2.08, title="ĐIỂM NỔI BẬT",
             body="Phân tách nghiệp vụ rõ ràng, hỗ trợ khách chưa đăng nhập và có khả năng mở rộng tích hợp dịch vụ.", accent=C["orange"], body_size=14)
    c.shape(.72, 3.92, 11.86, 2.15, fill=C["dark"], line=C["dark"], shadow=True)
    c.text(1.04, 4.18, 2.0, .30, "HÀNH TRÌNH CHÍNH", size=14, color=C["cyan"], bold=True, fill=C["dark"], margin=0)
    journey = [("01", "Khám phá", C["cyan"]), ("02", "Chọn sản phẩm", C["blue"]), ("03", "Đặt hàng", C["orange"]), ("04", "Theo dõi", C["success"])]
    for i, (num, label, color) in enumerate(journey):
        x = 1.05 + i * 2.87
        c.text(x, 4.73, .54, .54, num, size=16, color=C["dark"], bold=True, align="ctr", fill=color, line=color, geom="ellipse", alpha=100000, line_alpha=100000)
        c.text(x + .72, 4.72, 1.85, .56, label, size=15, color=C["paper"], bold=True, fill=C["dark"], margin=0)
        if i < 3:
            c.text(x + 2.48, 4.80, .28, .30, "→", size=19, color="80A8B2", bold=True, align="ctr", fill=C["dark"], margin=0)
    notes(s, [
        "Hệ thống phục vụ ba nhóm chính gồm khách vãng lai, khách có tài khoản và quản trị viên.",
        "Phần khách hàng bao phủ hành trình từ khám phá sản phẩm, thêm giỏ, đặt hàng đến theo dõi trạng thái.",
        "Phần quản trị tập trung dữ liệu sản phẩm, đơn hàng, người dùng và báo cáo để hỗ trợ vận hành.",
        "Kiến trúc MVC và lớp dịch vụ giúp mã nguồn có tổ chức, dễ bảo trì và mở rộng thêm tích hợp trong tương lai."
    ], ["Ba nhóm người dùng", "Một hành trình xuyên suốt từ khám phá đến theo dõi"])

    # 04 — Technology
    s, c = base(4, "Công nghệ sử dụng", "02 · NỀN TẢNG TRIỂN KHAI")
    tech = [
        ("ASP.NET CORE MVC", "Tổ chức ứng dụng theo Model–View–Controller, tách giao diện khỏi xử lý nghiệp vụ. Razor View hỗ trợ render phía máy chủ và kiểm soát luồng điều hướng rõ ràng.", C["blue"]),
        ("ENTITY FRAMEWORK CORE", "Đảm nhiệm ánh xạ đối tượng và truy vấn dữ liệu bằng LINQ. Migration hỗ trợ quản lý thay đổi cấu trúc cơ sở dữ liệu theo từng phiên bản.", C["purple"]),
        ("SQL SERVER", "Lưu trữ tập trung dữ liệu người dùng, sản phẩm, giỏ hàng và đơn hàng. Quan hệ và ràng buộc giúp duy trì tính nhất quán trong giao dịch.", C["orange"]),
        ("BOOTSTRAP · CSS · JAVASCRIPT", "Xây dựng giao diện responsive, card sản phẩm và các tương tác phía trình duyệt. CSS tùy biến đồng bộ nhận diện xanh đậm, cyan và cam của website.", C["cyan"]),
    ]
    for i, (title, body, color) in enumerate(tech):
        x = .72 + (i % 2) * 6.02
        y = 1.52 + (i // 2) * 2.46
        add_card(c, x, y, 5.80, 2.18, title=title, body=body, accent=color, body_size=14)
    c.text(.82, 6.52, 11.65, .28, f"Mã nguồn hiện tại: {facts.controllers} controller · {facts.services} tệp service · {facts.dbsets} DbSet được định nghĩa", size=14,
           color=C["muted"], bold=True, align="ctr", fill=C["bg"], margin=0)
    notes(s, [
        "ASP.NET Core MVC là nền tảng chính, giúp phân tách trách nhiệm giữa dữ liệu, xử lý và giao diện.",
        "Entity Framework Core kết nối ứng dụng với SQL Server, giảm mã truy vấn lặp lại và hỗ trợ migration.",
        "SQL Server lưu trữ dữ liệu nghiệp vụ có quan hệ như khách hàng, sản phẩm, giỏ hàng và đơn hàng.",
        "Ở phía giao diện, Bootstrap, CSS và JavaScript được kết hợp để bảo đảm khả năng hiển thị responsive và tương tác thuận tiện."
    ], ["Mỗi công nghệ có một vai trò rõ ràng", "Stack phù hợp ứng dụng thương mại điện tử MVC"])

    # 05 — Customer features
    s, c = base(5, "Chức năng dành cho khách hàng", "02 · TRẢI NGHIỆM MUA SẮM")
    customer = [
        ("01", "Đăng ký & đăng nhập", "Tạo tài khoản, xác thực và duy trì phiên mua sắm cá nhân.", C["blue"]),
        ("02", "Tìm kiếm & lọc", "Thu hẹp sản phẩm theo từ khóa, danh mục, giá và thuộc tính.", C["cyan"]),
        ("03", "Giỏ hàng", "Thay đổi số lượng, kiểm tra tạm tính và lưu lựa chọn trước checkout.", C["purple"]),
        ("04", "Đặt hàng", "Nhập thông tin nhận hàng, kiểm tra tồn kho và tạo mã đơn.", C["orange"]),
        ("05", "Theo dõi đơn hàng", "Xem tiến trình xử lý và tra cứu bằng thông tin đơn hàng.", C["success"]),
        ("06", "Thanh toán", "Hỗ trợ COD hoặc chuyển khoản với nội dung thanh toán rõ ràng.", C["danger"]),
    ]
    for i, (num, title, body, color) in enumerate(customer):
        x = .72 + (i % 3) * 4.02
        y = 1.52 + (i // 3) * 2.48
        c.shape(x, y, 3.80, 2.20, fill=C["paper"], line=C["line"], shadow=True)
        c.text(x + .22, y + .20, .48, .48, num, size=15, color=C["paper"], bold=True, align="ctr", fill=color, line=color, geom="ellipse", alpha=100000, line_alpha=100000)
        c.text(x + .84, y + .22, 2.70, .36, title, size=15, color=C["text"], bold=True, fill=C["paper"], margin=0)
        c.text(x + .24, y + .90, 3.30, .86, body, size=14, color=C["muted"], fill=C["paper"], valign="top", margin=0)
    notes(s, [
        "Các chức năng khách hàng được thiết kế theo đúng thứ tự của một hành trình mua sắm trực tuyến.",
        "Người dùng có thể tìm và lọc sản phẩm trước khi đăng nhập, sau đó quản lý lựa chọn trong giỏ hàng.",
        "Tại checkout, hệ thống thu thập thông tin giao nhận, kiểm tra dữ liệu và tạo đơn với mã theo dõi.",
        "Sau khi đặt hàng, khách có thể kiểm tra trạng thái và lựa chọn COD hoặc chuyển khoản tùy nhu cầu."
    ], ["Sáu chức năng theo một hành trình", "Giảm số bước và giữ thông tin minh bạch"])

    # 06 — Admin features
    s, c = base(6, "Chức năng quản trị", "02 · VẬN HÀNH HỆ THỐNG")
    admin = [
        ("SẢN PHẨM", "Thêm, sửa, ẩn/hiện sản phẩm; quản lý giá, tồn kho, hình ảnh và danh mục.", C["blue"]),
        ("ĐƠN HÀNG", "Duyệt đơn, cập nhật trạng thái xử lý và theo dõi thông tin giao nhận, thanh toán.", C["orange"]),
        ("NGƯỜI DÙNG", "Tra cứu tài khoản, phân quyền và kiểm soát trạng thái sử dụng hệ thống.", C["purple"]),
        ("KHUYẾN MÃI", "Thiết lập nội dung ưu đãi, giá khuyến mãi và thời gian áp dụng trên sản phẩm.", C["danger"]),
        ("BÁO CÁO THỐNG KÊ", "Tổng hợp chỉ số sản phẩm, đơn hàng và doanh thu để hỗ trợ ra quyết định.", C["success"]),
    ]
    for i, (title, body, color) in enumerate(admin):
        if i < 3:
            x, y, w = .72 + i * 4.02, 1.52, 3.80
        else:
            x, y, w = 2.72 + (i - 3) * 4.02, 4.00, 3.80
        add_card(c, x, y, w, 2.18, title=title, body=body, accent=color, body_size=14)
    notes(s, [
        "Khu vực quản trị được tổ chức theo các nhóm nghiệp vụ mà cửa hàng phải thực hiện hằng ngày.",
        "Quản trị viên có thể cập nhật sản phẩm, tồn kho và hình ảnh mà không cần sửa trực tiếp cơ sở dữ liệu.",
        "Đơn hàng được theo dõi theo trạng thái, kết hợp thông tin giao nhận và phương thức thanh toán.",
        "Các nhóm người dùng, khuyến mãi và thống kê giúp hệ thống không chỉ bán hàng mà còn hỗ trợ vận hành và đánh giá."
    ], ["Quản trị theo nghiệp vụ thực tế", "Dữ liệu tập trung, dễ theo dõi và cập nhật"])

    # 07 — Home UI
    s, c = base(7, "Giao diện trang chủ", "03 · DEMO WEBSITE")
    browser_frame(c, .65, 1.45, 7.65, 5.18, "Trang chủ toàn màn hình — ưu tiên banner và khu vực sản phẩm", "/")
    c.shape(8.62, 1.45, 4.05, 5.18, fill=C["paper"], line=C["line"], shadow=True)
    c.text(8.94, 1.76, 3.35, .30, "CÁC KHU VỰC CHÍNH", size=14, color=C["cyan"], bold=True, fill=C["paper"], margin=0)
    home_blocks = [
        ("01", "Header & tìm kiếm", "Điều hướng nhanh, tài khoản và giỏ hàng."),
        ("02", "Banner khuyến mãi", "Tạo điểm nhấn và dẫn đến chiến dịch bán hàng."),
        ("03", "Danh mục sản phẩm", "Nhóm PC, laptop, màn hình và linh kiện."),
        ("04", "Sản phẩm nổi bật", "Card hiển thị giá, ưu đãi và thao tác mua."),
    ]
    for i, (num, title, body) in enumerate(home_blocks):
        y = 2.30 + i * .93
        c.text(8.94, y, .44, .44, num, size=14, color=C["dark"], bold=True, align="ctr", fill=C["cyan"], line=C["cyan"], geom="ellipse", alpha=100000, line_alpha=100000)
        c.text(9.56, y - .01, 2.72, .28, title, size=14, color=C["text"], bold=True, fill=C["paper"], margin=0)
        c.text(9.56, y + .29, 2.72, .54, body, size=13, color=C["muted"], fill=C["paper"], valign="top", margin=0)
    notes(s, [
        "Trang chủ là điểm bắt đầu của hành trình nên phần ảnh chụp được đặt lớn để hội đồng quan sát tổng thể.",
        "Header tập trung tìm kiếm, danh mục, tài khoản và giỏ hàng để giảm thời gian điều hướng.",
        "Banner và các khu vực khuyến mãi tạo điểm nhấn nhưng vẫn giữ cấu trúc rõ ràng, không che nội dung sản phẩm.",
        "Các nhóm sản phẩm được trình bày theo card đồng nhất, giúp người dùng quét nhanh tên, giá và ưu đãi."
    ], ["Ảnh demo chiếm khoảng 60%", "Bốn khu vực chính có vai trò khác nhau"], "/")

    # 08 — Product detail UI
    s, c = base(8, "Giao diện chi tiết sản phẩm", "03 · DEMO WEBSITE")
    browser_frame(c, .65, 1.45, 7.35, 5.18, "Chi tiết sản phẩm — hình ảnh, giá và thông số kỹ thuật", "/Products/Details/{id}")
    c.shape(8.30, 1.45, 4.37, 2.28, fill=C["dark"], line=C["dark"], shadow=True)
    c.text(8.62, 1.76, 3.60, .28, "MỤC TIÊU GIAO DIỆN", size=14, color=C["cyan"], bold=True, fill=C["dark"], margin=0)
    c.text(8.62, 2.18, 3.55, 1.22, "Cung cấp đủ dữ liệu để khách hàng hiểu sản phẩm và đưa ra quyết định ngay trên một màn hình.", size=16, color=C["paper"], fill=C["dark"], valign="top", margin=0)
    utilities = [
        ("Thông tin rõ", "Tên, giá, tình trạng và khuyến mãi."),
        ("Thông số dễ đọc", "Nhóm cấu hình theo từng đặc điểm."),
        ("Thao tác mua", "Thêm giỏ, mua ngay hoặc so sánh."),
    ]
    for i, (title, body) in enumerate(utilities):
        y = 3.97 + i * .89
        c.shape(8.30, y, 4.37, .72, fill=C["paper"], line=C["line"], shadow=True)
        c.shape(8.30, y, .07, .72, fill=(C["cyan"], C["orange"], C["success"])[i], line=(C["cyan"], C["orange"], C["success"])[i], geom="rect")
        c.text(8.58, y + .08, 1.58, .42, title, size=13, color=C["text"], bold=True, fill=C["paper"], margin=0)
        c.text(10.14, y + .09, 2.15, .46, body, size=14, color=C["muted"], fill=C["paper"], valign="top", margin=0)
    notes(s, [
        "Trang chi tiết sản phẩm cần trả lời ba câu hỏi: đây là sản phẩm gì, có phù hợp không và mua bằng cách nào.",
        "Hình ảnh, giá, tồn kho và khuyến mãi được ưu tiên ở vùng nhìn đầu tiên để hỗ trợ quyết định nhanh.",
        "Thông số được nhóm theo cấu trúc dễ đọc thay vì đưa thành một đoạn văn dài.",
        "Các thao tác thêm giỏ, mua ngay hoặc so sánh được đặt gần thông tin chính để giảm số lần chuyển trang."
    ], ["Một màn hình hỗ trợ quyết định mua", "Thông tin và hành động được đặt gần nhau"], "/Products/Details/{id}")

    # 09 — Cart and payment
    s, c = base(9, "Giỏ hàng & thanh toán", "03 · DEMO WEBSITE")
    browser_frame(c, .65, 1.45, 6.95, 5.18, "Giỏ hàng / checkout / màn hình chuyển khoản", "/Cart  →  /Cart/Checkout")
    c.shape(7.90, 1.45, 4.77, 5.18, fill=C["paper"], line=C["line"], shadow=True)
    c.text(8.22, 1.76, 3.95, .30, "QUY TRÌNH ĐẶT HÀNG", size=14, color=C["orange"], bold=True, fill=C["paper"], margin=0)
    steps = [
        ("1", "Kiểm tra giỏ", "Số lượng, giá và tạm tính."),
        ("2", "Nhập giao nhận", "Thông tin người nhận và địa chỉ."),
        ("3", "Chọn thanh toán", "COD hoặc chuyển khoản ngân hàng."),
        ("4", "Theo dõi đơn", "Mã đơn và tiến trình xử lý."),
    ]
    for i, (num, title, body) in enumerate(steps):
        y = 2.26 + i * .86
        color = (C["cyan"], C["blue"], C["orange"], C["success"])[i]
        c.text(8.22, y, .46, .46, num, size=14, color=C["paper"], bold=True, align="ctr", fill=color, line=color, geom="ellipse", alpha=100000, line_alpha=100000)
        c.text(8.86, y - .01, 1.70, .44, title, size=13, color=C["text"], bold=True, fill=C["paper"], margin=0)
        c.text(10.58, y - .01, 1.62, .48, body, size=14, color=C["muted"], fill=C["paper"], valign="top", margin=0)
    c.shape(8.22, 5.83, 4.05, .50, fill="FFF2E8", line="FFD2B3")
    c.text(8.40, 5.86, 3.70, .40, "Chuyển khoản: nội dung & QR", size=13, color="B54708", bold=True, align="ctr", fill="FFF2E8", margin=0)
    notes(s, [
        "Quy trình checkout được chia thành bốn bước ngắn để người dùng luôn biết mình đang ở giai đoạn nào.",
        "Trước khi tạo đơn, khách kiểm tra lại sản phẩm, số lượng, giá và nhập thông tin giao nhận.",
        "Với chuyển khoản, hệ thống hiển thị số tiền, nội dung và mã QR để hạn chế sai sót khi thanh toán.",
        "Sau khi hoàn tất, mã đơn và trạng thái giúp khách chủ động theo dõi thay vì phải liên hệ cửa hàng nhiều lần."
    ], ["Quy trình bốn bước", "Thông tin chuyển khoản rõ ràng và có thể đối chiếu"], "/Cart/Checkout")

    # 10 — Advantages
    s, c = base(10, "Ưu điểm của hệ thống", "04 · ĐÁNH GIÁ GIẢI PHÁP")
    advantages = [
        ("01", "GIAO DIỆN THÂN THIỆN", "Nhận diện nhất quán, độ tương phản tốt và ưu tiên nội dung quan trọng.", C["cyan"]),
        ("02", "DỄ SỬ DỤNG", "Luồng mua hàng quen thuộc, thao tác rõ và phản hồi trực tiếp cho người dùng.", C["blue"]),
        ("03", "QUẢN LÝ THUẬN TIỆN", "Dữ liệu sản phẩm, đơn hàng và người dùng tập trung trong khu vực admin.", C["purple"]),
        ("04", "HIỆU NĂNG ỔN ĐỊNH", "Render phía máy chủ, truy vấn có cấu trúc và kiểm soát dữ liệu trong giao dịch.", C["orange"]),
        ("05", "HỖ TRỢ MỞ RỘNG", "Kiến trúc phân lớp thuận lợi bổ sung thanh toán, vận chuyển và báo cáo.", C["success"]),
    ]
    for i, (num, title, body, color) in enumerate(advantages):
        if i < 3:
            x, y, w = .72 + i * 4.02, 1.52, 3.80
        else:
            x, y, w = 2.72 + (i - 3) * 4.02, 4.04, 3.80
        c.shape(x, y, w, 2.18, fill=C["paper"], line=C["line"], shadow=True)
        c.text(x + .22, y + .20, .48, .48, num, size=14, color=C["paper"], bold=True, align="ctr", fill=color, line=color, geom="ellipse", alpha=100000, line_alpha=100000)
        c.text(x + .86, y + .24, w - 1.10, .30, title, size=14, color=color, bold=True, fill=C["paper"], margin=0)
        c.text(x + .24, y + .89, w - .50, .88, body, size=14, color=C["muted"], fill=C["paper"], valign="top", margin=0)
    notes(s, [
        "Ưu điểm đầu tiên là giao diện đồng bộ với website, dễ đọc và làm nổi bật thông tin mua hàng quan trọng.",
        "Luồng sử dụng quen thuộc giúp khách mới có thể tìm sản phẩm và đặt hàng mà không cần hướng dẫn dài.",
        "Đối với cửa hàng, dữ liệu quản trị được tập trung nên việc cập nhật và theo dõi thuận tiện hơn.",
        "Kiến trúc hiện tại cũng tạo nền tảng để tối ưu hiệu năng và tích hợp thêm dịch vụ khi quy mô tăng."
    ], ["Ưu điểm trải đều ở UX, quản trị và kỹ thuật", "Kiến trúc tạo nền tảng mở rộng"])

    # 11 — Conclusion
    s, c = base(11, "Kết luận & hướng phát triển", "04 · TỔNG KẾT ĐỒ ÁN")
    c.shape(.72, 1.52, 5.72, 4.90, fill=C["dark"], line=C["dark"], shadow=True)
    c.text(1.05, 1.88, 4.92, .34, "KẾT QUẢ ĐẠT ĐƯỢC", size=16, color=C["cyan"], bold=True, fill=C["dark"], margin=0)
    results = [
        "Hoàn thiện website MVC với luồng mua hàng cốt lõi.",
        "Xây dựng khu vực quản trị dữ liệu và xử lý đơn.",
        "Áp dụng EF Core, SQL Server và giao diện responsive.",
        "Rèn luyện phân tích nghiệp vụ, thiết kế và triển khai.",
    ]
    for i, text in enumerate(results):
        y = 2.55 + i * .78
        c.text(1.05, y, .38, .38, "✓", size=15, color=C["dark"], bold=True, align="ctr", fill=C["cyan"], line=C["cyan"], geom="ellipse", alpha=100000, line_alpha=100000)
        c.text(1.62, y - .04, 4.25, .58, text, size=14, color=C["paper"], fill=C["dark"], valign="top", margin=0)
    c.shape(6.68, 1.52, 5.90, 4.90, fill=C["paper"], line=C["line"], shadow=True)
    c.text(7.02, 1.88, 4.90, .34, "HƯỚNG PHÁT TRIỂN", size=16, color=C["orange"], bold=True, fill=C["paper"], margin=0)
    future = [
        ("01", "Tích hợp cổng thanh toán và đối soát tự động."),
        ("02", "Nâng cấp bảo mật tài khoản và kiểm thử tự động."),
        ("03", "Bổ sung gợi ý sản phẩm, báo cáo và SEO."),
        ("04", "Tối ưu mobile, hiệu năng và triển khai cloud."),
    ]
    for i, (num, text) in enumerate(future):
        y = 2.48 + i * .81
        c.text(7.02, y, .48, .48, num, size=14, color=C["paper"], bold=True, align="ctr", fill=C["orange"], line=C["orange"], geom="ellipse", alpha=100000, line_alpha=100000)
        c.text(7.72, y - .01, 4.22, .52, text, size=15, color=C["text"], fill=C["paper"], valign="top", margin=0)
    notes(s, [
        "Đồ án đã hoàn thiện các chức năng cốt lõi của một website bán máy tính, từ giao diện khách hàng đến quản trị.",
        "Quá trình thực hiện giúp em vận dụng kiến thức MVC, cơ sở dữ liệu, thiết kế giao diện và phân tích nghiệp vụ.",
        "Tuy nhiên hệ thống vẫn cần tiếp tục nâng cấp bảo mật, kiểm thử và tự động hóa thanh toán để phù hợp vận hành thực tế.",
        "Trong tương lai, em định hướng bổ sung gợi ý sản phẩm, báo cáo chuyên sâu, tối ưu mobile và triển khai trên hạ tầng cloud.",
        "Em xin chân thành cảm ơn hội đồng và sẵn sàng tiếp nhận câu hỏi, góp ý."
    ], ["Đã đạt mục tiêu cốt lõi", "Hướng phát triển ưu tiên tính thực tế và khả năng vận hành"])

    return slides


def write_presentation_speech(slides: list[Slide]) -> None:
    lines = ["# Lời thuyết trình — DATN PC Store", "", "> Nội dung khớp với 11 slide bảo vệ đồ án được sinh từ source code hiện tại.", ""]
    for slide in slides:
        lines += [f"## Slide {slide.number:02d} — {slide.title}", "", "### Lời thuyết trình", ""]
        lines += [f"{sentence}" for sentence in slide.speaker]
        lines += ["", "### Ý cần nhấn mạnh"]
        lines += [f"- {item}" for item in slide.animation[:3]]
        lines += ["", "### Ảnh cần chèn"]
        if slide.image_note.startswith("Có"):
            url = slide.image_note.removeprefix("Có. URL: ").rstrip(".")
            lines += ["- Có.", f"- URL: {url}"]
        else:
            lines += ["- Không."]
        lines += [""]
    SPEECH_PATH.write_text("\n".join(lines), encoding="utf-8")


def write_speech_markdown(slides: list[Slide]) -> None:
    write_presentation_speech(slides)


def validate_pptx(slides: list[Slide]) -> None:
    if len(slides) != TOTAL:
        raise RuntimeError(f"Expected {TOTAL} slides, got {len(slides)}")
    if [s.number for s in slides] != list(range(1, TOTAL + 1)):
        raise RuntimeError("Slide numbering is not continuous")
    if any(len(s.speaker) < 4 or len(s.speaker) > 6 for s in slides):
        raise RuntimeError("Each slide must have 4–6 speech sentences")
    for slide in slides:
        bullet_lines = [line.strip("•- ") for e in slide.elements if e.kind == "text"
                        for line in e.text.splitlines() if line.lstrip().startswith(("•", "-"))]
        if slide.number in (8, 28) and len(bullet_lines) <= (10 if slide.number == 8 else 8):
            pass  # two columns, each intentionally capped at five bullets
        elif len(bullet_lines) > 5:
            raise RuntimeError(f"Slide {slide.number:02d} has more than five bullets")
        if any(len(line.split()) > 12 for line in bullet_lines):
            raise RuntimeError(f"Slide {slide.number:02d} has a bullet above 12 words")
        for e in slide.elements:
            if e.x < 0 or e.y < 0 or e.x + e.w > W + .01 or e.y + e.h > H + .01:
                # Large decorative background circles may intentionally bleed.
                if not (e.kind == "shape" and e.geom == "ellipse"):
                    raise RuntimeError(f"Slide {slide.number:02d} has content outside canvas")
    if not zipfile.is_zipfile(PPTX_PATH):
        raise RuntimeError("Generated PPTX is not a valid ZIP package")
    with zipfile.ZipFile(PPTX_PATH) as archive:
        bad = archive.testzip()
        if bad:
            raise RuntimeError(f"Corrupt ZIP member: {bad}")
        names = [n for n in archive.namelist() if re.fullmatch(r"ppt/slides/slide\d+\.xml", n)]
        if len(names) != TOTAL:
            raise RuntimeError(f"PPTX contains {len(names)} slides")
        for name in names:
            import xml.etree.ElementTree as ET
            ET.fromstring(archive.read(name))
    speech = SPEECH_PATH.read_text(encoding="utf-8")
    if speech.count("## Slide ") != TOTAL:
        raise RuntimeError(f"Speech markdown does not contain exactly {TOTAL} slides")


def validate_deck(slides: list[Slide]) -> None:
    validate_pptx(slides)


def create_deck(facts: SourceFacts) -> list[Slide]:
    return build_slides(facts)


def write_notes(slides: list[Slide]) -> None:
    write_presentation_speech(slides)


def validate(slides: list[Slide]) -> None:
    validate_pptx(slides)


def emu(v: float) -> int: return round(v*EMU)


def color_xml(value: str, alpha: int = 100000) -> str:
    alpha_xml = f'<a:alpha val="{alpha}"/>' if alpha != 100000 else ""
    return f'<a:srgbClr val="{value}">{alpha_xml}</a:srgbClr>'


def fill_xml(e: Element) -> str:
    if e.alpha == 0:
        return "<a:noFill/>"
    if e.fill2:
        return (f'<a:gradFill rotWithShape="1"><a:gsLst>'
                f'<a:gs pos="0">{color_xml(e.fill,e.alpha)}</a:gs>'
                f'<a:gs pos="100000">{color_xml(e.fill2,e.alpha)}</a:gs>'
                f'</a:gsLst><a:lin ang="5400000" scaled="1"/></a:gradFill>')
    return f'<a:solidFill>{color_xml(e.fill,e.alpha)}</a:solidFill>'


def line_xml(e: Element) -> str:
    if e.line_alpha == 0:
        return '<a:ln><a:noFill/></a:ln>'
    dash = f'<a:prstDash val="{e.dash}"/>' if e.dash else ""
    end = f'<a:tailEnd type="{e.arrow_end}"/>' if e.arrow_end else ""
    return f'<a:ln w="{e.line_width}"><a:solidFill>{color_xml(e.line,e.line_alpha)}</a:solidFill>{dash}{end}</a:ln>'


def effect_xml(e: Element) -> str:
    if not e.shadow: return ""
    return ('<a:effectLst><a:outerShdw blurRad="50000" dist="24000" dir="2700000" rotWithShape="0">'
            '<a:srgbClr val="16324F"><a:alpha val="13000"/></a:srgbClr></a:outerShdw></a:effectLst>')


def tx_body(e: Element) -> str:
    anchor={"top":"t","ctr":"ctr","bottom":"b"}.get(e.valign,"ctr")
    align={"l":"l","ctr":"ctr","r":"r"}.get(e.align,"l")
    margin=emu(e.margin)
    paragraphs=[]
    lines=e.text.split("\n") if e.text else [""]
    for line in lines:
        value=escape(line)
        paragraphs.append(
            f'<a:p><a:pPr algn="{align}"/><a:r><a:rPr lang="vi-VN" sz="{e.size*100}" b="{1 if e.bold else 0}" '
            f'dirty="0"><a:solidFill><a:srgbClr val="{e.color}"/></a:solidFill><a:latin typeface="{FONT}"/>'
            f'</a:rPr><a:t>{value}</a:t></a:r><a:endParaRPr lang="vi-VN" sz="{e.size*100}"/></a:p>')
    return (f'<p:txBody><a:bodyPr wrap="square" lIns="{margin}" tIns="{margin}" rIns="{margin}" bIns="{margin}" '
            f'anchor="{anchor}" anchorCtr="0"/><a:lstStyle/>{"".join(paragraphs)}</p:txBody>')


def element_xml(e: Element, shape_id: int) -> str:
    name=escape(f"Element {shape_id}")
    rot=f' rot="{e.rotation*60000}"' if e.rotation else ""
    if e.kind == "line":
        x,y,cx,cy=e.x,e.y,e.w,e.h
        flip_h=' flipH="1"' if cx<0 else ""; flip_v=' flipV="1"' if cy<0 else ""
        x0=min(x,x+cx); y0=min(y,y+cy)
        return (f'<p:sp><p:nvSpPr><p:cNvPr id="{shape_id}" name="{name}"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>'
                f'<p:spPr><a:xfrm{flip_h}{flip_v}><a:off x="{emu(x0)}" y="{emu(y0)}"/><a:ext cx="{emu(abs(cx))}" cy="{emu(abs(cy))}"/></a:xfrm>'
                f'<a:prstGeom prst="line"><a:avLst/></a:prstGeom><a:noFill/>{line_xml(e)}</p:spPr></p:sp>')
    text_body=tx_body(e) if e.kind=="text" else ""
    return (f'<p:sp><p:nvSpPr><p:cNvPr id="{shape_id}" name="{name}"/><p:cNvSpPr txBox="{1 if e.kind=="text" else 0}"/><p:nvPr/></p:nvSpPr>'
            f'<p:spPr><a:xfrm{rot}><a:off x="{emu(e.x)}" y="{emu(e.y)}"/><a:ext cx="{emu(e.w)}" cy="{emu(e.h)}"/></a:xfrm>'
            f'<a:prstGeom prst="{e.geom}"><a:avLst/></a:prstGeom>{fill_xml(e)}{line_xml(e)}{effect_xml(e)}</p:spPr>{text_body}</p:sp>')


def transition_xml(kind: str | None) -> str:
    if not kind: return ""
    tags={"fade":"<p:fade/>","push":"<p:push dir=\"l\"/>","wipe":"<p:wipe dir=\"l\"/>"}
    return f'<p:transition spd="med" advClick="1">{tags.get(kind,"<p:fade/>")}</p:transition>'


def slide_xml(slide: Slide) -> str:
    shapes=''.join(element_xml(e,i+2) for i,e in enumerate(slide.elements))
    return (f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
            f'<p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" '
            f'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" '
            f'xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">'
            f'<p:cSld name="{escape(slide.title)}"><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/>'
            f'</p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>{shapes}</p:spTree></p:cSld>'
            f'<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>{transition_xml(slide.transition)}</p:sld>')


def package_xml(slides: list[Slide]) -> dict[str,str]:
    count=len(slides)
    overrides=''.join(f'<Override PartName="/ppt/slides/slide{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slide+xml"/>' for i in range(1,count+1))
    content=(f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">'
             f'<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/>'
             f'<Override PartName="/ppt/presentation.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml"/>'
             f'<Override PartName="/ppt/slideMasters/slideMaster1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml"/>'
             f'<Override PartName="/ppt/slideLayouts/slideLayout1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>'
             f'<Override PartName="/ppt/theme/theme1.xml" ContentType="application/vnd.openxmlformats-officedocument.theme+xml"/>'
             f'<Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>'
             f'<Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>{overrides}</Types>')
    rootrels=('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
              '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="ppt/presentation.xml"/>'
              '<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>'
              '<Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/></Relationships>')
    sldids=''.join(f'<p:sldId id="{255+i}" r:id="rId{i+1}"/>' for i in range(1,count+1))
    presentation=(f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?><p:presentation xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">'
                  f'<p:sldMasterIdLst><p:sldMasterId id="2147483648" r:id="rId1"/></p:sldMasterIdLst><p:sldIdLst>{sldids}</p:sldIdLst>'
                  f'<p:sldSz cx="{emu(W)}" cy="{emu(H)}" type="screen16x9"/><p:notesSz cx="6858000" cy="9144000"/><p:defaultTextStyle/></p:presentation>')
    presrels=['<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster" Target="slideMasters/slideMaster1.xml"/>']
    presrels += [f'<Relationship Id="rId{i+1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide" Target="slides/slide{i}.xml"/>' for i in range(1,count+1)]
    presrels_xml='<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'+''.join(presrels)+'</Relationships>'
    master=('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><p:sldMaster xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">'
            '<p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr></p:spTree></p:cSld>'
            '<p:clrMap accent1="accent1" accent2="accent2" accent3="accent3" accent4="accent4" accent5="accent5" accent6="accent6" bg1="lt1" bg2="lt2" folHlink="folHlink" hlink="hlink" tx1="dk1" tx2="dk2"/>'
            '<p:sldLayoutIdLst><p:sldLayoutId id="1" r:id="rId1"/></p:sldLayoutIdLst><p:txStyles><p:titleStyle/><p:bodyStyle/><p:otherStyle/></p:txStyles></p:sldMaster>')
    masterrels=('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
                '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout1.xml"/>'
                '<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme" Target="../theme/theme1.xml"/></Relationships>')
    layout=('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><p:sldLayout xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main" type="blank">'
            '<p:cSld name="Blank"><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr></p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sldLayout>')
    layoutrels=('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
                '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster" Target="../slideMasters/slideMaster1.xml"/></Relationships>')
    theme=(f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?><a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="DATN PC Store Premium"><a:themeElements>'
           f'<a:clrScheme name="DATN"><a:dk1><a:srgbClr val="{C["dark"]}"/></a:dk1><a:lt1><a:srgbClr val="{C["paper"]}"/></a:lt1><a:dk2><a:srgbClr val="{C["text"]}"/></a:dk2><a:lt2><a:srgbClr val="{C["bg"]}"/></a:lt2>'
           f'<a:accent1><a:srgbClr val="{C["blue"]}"/></a:accent1><a:accent2><a:srgbClr val="{C["cyan"]}"/></a:accent2><a:accent3><a:srgbClr val="{C["orange"]}"/></a:accent3><a:accent4><a:srgbClr val="{C["purple"]}"/></a:accent4><a:accent5><a:srgbClr val="{C["success"]}"/></a:accent5><a:accent6><a:srgbClr val="{C["pink"]}"/></a:accent6><a:hlink><a:srgbClr val="{C["blue"]}"/></a:hlink><a:folHlink><a:srgbClr val="{C["purple"]}"/></a:folHlink></a:clrScheme>'
           f'<a:fontScheme name="Aptos"><a:majorFont><a:latin typeface="{FONT} Display"/></a:majorFont><a:minorFont><a:latin typeface="{FONT}"/></a:minorFont></a:fontScheme>'
           '<a:fmtScheme name="DATN"><a:fillStyleLst><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:fillStyleLst><a:lnStyleLst><a:ln w="12700"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln></a:lnStyleLst><a:effectStyleLst><a:effectStyle><a:effectLst/></a:effectStyle></a:effectStyleLst><a:bgFillStyleLst><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:bgFillStyleLst></a:fmtScheme></a:themeElements></a:theme>')
    core=('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/">'
          '<dc:title>DATN PC Store — Bảo vệ đồ án tốt nghiệp</dc:title><dc:creator>DATN PC Store</dc:creator><dc:description>11-slide graduation-defense deck generated from verified source code.</dc:description></cp:coreProperties>')
    app=f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties"><Application>Microsoft Office PowerPoint</Application><PresentationFormat>Widescreen</PresentationFormat><Slides>{count}</Slides><Notes>0</Notes></Properties>'
    sliderel=('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
              '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout1.xml"/></Relationships>')
    return {"[Content_Types].xml":content,"_rels/.rels":rootrels,"ppt/presentation.xml":presentation,
            "ppt/_rels/presentation.xml.rels":presrels_xml,"ppt/slideMasters/slideMaster1.xml":master,
            "ppt/slideMasters/_rels/slideMaster1.xml.rels":masterrels,"ppt/slideLayouts/slideLayout1.xml":layout,
            "ppt/slideLayouts/_rels/slideLayout1.xml.rels":layoutrels,"ppt/theme/theme1.xml":theme,
            "docProps/core.xml":core,"docProps/app.xml":app,"__slide_rel__":sliderel}


def render(slides: list[Slide]) -> None:
    OUTPUT.mkdir(exist_ok=True)
    files=package_xml(slides); sliderel=files.pop("__slide_rel__")
    with zipfile.ZipFile(PPTX_PATH,"w",zipfile.ZIP_DEFLATED) as z:
        for path,value in files.items():z.writestr(path,value)
        for slide in slides:
            z.writestr(f"ppt/slides/slide{slide.number}.xml",slide_xml(slide))
            z.writestr(f"ppt/slides/_rels/slide{slide.number}.xml.rels",sliderel)




def main() -> int:
    facts = audit_source()
    slides = create_deck(facts)
    write_presentation_speech(slides)
    render(slides)
    validate_pptx(slides)
    image_slides = [s.number for s in slides if s.image_note.startswith("Có")]
    print(f"Created {len(slides)} clean IT thesis slides.")
    print(f"Source facts: {facts.dbsets} DbSet, {facts.controllers} controllers, {facts.services} service files.")
    print("Image placeholders: " + ", ".join(f"{n:02d}" for n in image_slides))
    print(f"PowerPoint: {PPTX_PATH}")
    print(f"Speech: {SPEECH_PATH}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
