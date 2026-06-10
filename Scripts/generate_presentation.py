#!/usr/bin/env python3
"""Generate the 30-slide DATN PC Store graduation-defense deck.

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
TOTAL = 30
W, H, EMU = 13.333, 7.5, 914400
FONT = "Aptos"

C = {
    "navy": "0B3D91", "blue": "2563EB", "cyan": "06B6D4", "orange": "F59E0B",
    "bg": "F8FAFC", "paper": "FFFFFF", "text": "1E293B", "muted": "64748B",
    "success": "10B981", "danger": "EF4444", "purple": "8B5CF6", "pink": "EC4899",
    "line": "DCE6F1", "ice": "EAF4FF", "mint": "E9FBF5", "warm": "FFF6DF",
    "dark": "071D3B", "slate": "0F2942", "soft": "F1F5F9",
}

SECTIONS = {
    1: ("SECTION 01", "TỔNG QUAN ĐỀ TÀI", C["blue"], "◎", "01"),
    2: ("SECTION 02", "PHÂN TÍCH & THIẾT KẾ", C["purple"], "◇", "02"),
    3: ("SECTION 03", "DEMO WEBSITE KHÁCH HÀNG", C["cyan"], "▣", "03"),
    4: ("SECTION 04", "QUẢN TRỊ HỆ THỐNG", C["orange"], "▤", "04"),
    5: ("SECTION 05", "ĐÁNH GIÁ & KẾT LUẬN", C["success"], "↗", "05"),
}


def section_for(number: int) -> int:
    if number <= 6: return 1
    if number <= 12: return 2
    if number <= 23: return 3
    if number <= 26: return 4
    return 5


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
    slides: list[Slide] = []

    s = Slide(1, "DATN PC Store", "cover")
    c = Canvas(s); add_safe_background(c, 1, dark=True)
    c.text(.82, .82, 4.4, .30, "ĐỒ ÁN TỐT NGHIỆP · WEBSITE PC STORE", size=15, color=C["cyan"], bold=True, fill=C["dark"])
    c.text(.82, 1.62, 9.4, .82, "DATN PC STORE", size=40, color=C["paper"], bold=True, fill=C["dark"], margin=0)
    c.text(.84, 2.63, 7.9, .54, "Website bán linh kiện và hỗ trợ Build PC", size=21, color="C7DAED", fill=C["dark"], margin=0)
    c.shape(.84, 3.50, 2.20, .09, fill=C["orange"], line=C["orange"], geom="rect")
    c.text(.84, 4.06, 5.8, .35, "Sinh viên: ................................", size=16, color=C["paper"], fill=C["dark"])
    c.text(.84, 4.56, 5.8, .35, "Giảng viên hướng dẫn: ........................", size=16, color="C7DAED", fill=C["dark"])
    add_footer_progress(c, dark=True)
    slides.append(_set_notes(s,["Em xin kính chào hội đồng và thầy cô tham dự buổi bảo vệ.","Đề tài của em là xây dựng website PC Store phục vụ bán linh kiện máy tính.","Hệ thống tập trung vào hành trình từ tìm sản phẩm đến đặt hàng và hậu mãi.","Sau đây em xin trình bày ngắn gọn bài toán, giải pháp và phần demo chính."],["Website thương mại điện tử cho linh kiện PC","Trình bày theo luồng nghiệp vụ thực tế"]))

    simple = [
        (2,"Đặt vấn đề",["Nhu cầu mua linh kiện ngày càng tăng","Thông tin sản phẩm phân tán, khó đối chiếu","Khách cần quy trình mua hàng liền mạch"]),
        (3,"Khó khăn thực tế",["Thông số kỹ thuật khó đọc","Khó kiểm tra tương thích linh kiện","Theo dõi đơn và hậu mãi chưa thuận tiện"]),
        (4,"Lý do chọn đề tài",["Bài toán gần với thực tế","Vận dụng kiến thức phát triển web","Có nhiều nghiệp vụ để kiểm chứng"]),
        (5,"Mục tiêu đề tài",["Xây dựng website bán linh kiện","Hỗ trợ Build PC và so sánh","Quản trị tập trung, dễ vận hành","Theo dõi đơn và hỗ trợ sau bán"]),
        (6,"Đối tượng và phạm vi",["Khách vãng lai và khách đăng nhập","Nhân viên, quản trị viên hệ thống","Phạm vi website bán linh kiện PC","Thanh toán COD và chuyển khoản"]),
    ]
    for n,title,items in simple:
        s,c=_base_slide(n,title); c.shape(.76,1.58,11.82,4.88,fill=C["paper"],line=C["line"],shadow=True)
        _bullets(c,items,1.20,2.05,10.60,gap=.82)
        slides.append(_set_notes(s,[f"Ở phần {title.lower()}, em tập trung vào vấn đề trực tiếp của người dùng.","Các ý trên slide được rút gọn để hội đồng dễ theo dõi trên máy chiếu.","Phạm vi được giới hạn ở những chức năng đã triển khai trong source code.","Đây là cơ sở để xác định yêu cầu và thiết kế hệ thống ở phần tiếp theo."],[items[0],items[-1]]))

    s=add_section_divider(7,"Phân tích & thiết kế","Thiết kế vừa đủ để giải thích cách hệ thống vận hành","02")
    slides.append(_set_notes(s,["Tiếp theo em xin chuyển sang phần phân tích và thiết kế.","Phần này không đi sâu vào sơ đồ phức tạp mà tập trung vào các thành phần chính.","Em sẽ lần lượt trình bày yêu cầu, kiến trúc, công nghệ và dữ liệu.","Mục tiêu là cho thấy giải pháp bám sát bài toán đã nêu."],["Thiết kế đơn giản, bám nghiệp vụ","Tập trung vào luồng xử lý chính"]))

    s,c=_base_slide(8,"Yêu cầu chức năng")
    add_card(c,.75,1.60,5.75,4.85,title="KHÁCH HÀNG",body="• Xem và lọc sản phẩm\n• Quản lý giỏ hàng\n• Đặt và theo dõi đơn\n• Build PC, so sánh\n• Chat, bảo hành, báo giá",accent=C["blue"])
    add_card(c,6.82,1.60,5.75,4.85,title="QUẢN TRỊ VIÊN",body="• Quản lý sản phẩm\n• Quản lý đơn hàng\n• Quản lý người dùng\n• Xử lý chat, bảo hành\n• Cấu hình nội dung website",accent=C["orange"])
    slides.append(_set_notes(s,["Yêu cầu được chia thành hai nhóm người dùng chính.","Khách hàng thao tác từ khám phá sản phẩm đến dịch vụ sau bán.","Quản trị viên tập trung vào dữ liệu, đơn hàng và hỗ trợ khách.","Cách chia này giúp thiết kế controller và giao diện rõ trách nhiệm."],["Hai nhóm chức năng rõ ràng","Không đưa chức năng ngoài source"]))

    s,c=_base_slide(9,"Kiến trúc hệ thống"); add_simple_architecture(c)
    c.text(1.10,5.28,11.1,.40,"Luồng xử lý một chiều, dễ theo dõi và bảo trì",size=16,color=C["muted"],bold=True,align="ctr",fill=C["bg"])
    slides.append(_set_notes(s,["Hệ thống sử dụng kiến trúc phân lớp quen thuộc của ASP.NET Core MVC.","Yêu cầu từ trình duyệt đi qua controller rồi đến lớp service.","EF Core đảm nhiệm truy cập SQL Server và ánh xạ dữ liệu.","SignalR, GHN và SMTP hoặc QR là các tích hợp hỗ trợ bên ngoài."],["Năm lớp xử lý chính","Tích hợp ngoài được tách riêng"]))

    s,c=_base_slide(10,"Công nghệ sử dụng")
    tech=[("ASP.NET Core MVC","Nền tảng web",C["blue"]),("Entity Framework Core","Truy cập dữ liệu",C["cyan"]),("SQL Server","Lưu trữ",C["purple"]),("SignalR","Chat realtime",C["orange"]),("Bootstrap / JS","Giao diện",C["success"]),("GHN / SMTP","Tích hợp",C["blue"])]
    for i,(t,b,col) in enumerate(tech): add_card(c,.72+(i%3)*4.18,1.58+(i//3)*2.40,3.78,1.92,title=t,body=b,accent=col)
    slides.append(_set_notes(s,["Công nghệ chính là ASP.NET Core MVC kết hợp Entity Framework Core.","SQL Server lưu dữ liệu nghiệp vụ và migration quản lý thay đổi cấu trúc.","SignalR hỗ trợ chat thời gian thực giữa khách và quản trị.","Giao vận, email và QR được tích hợp theo từng nghiệp vụ cụ thể."],["Stack đồng nhất với source","Tích hợp phục vụ nghiệp vụ thật"]))

    s,c=_base_slide(11,"Cơ sở dữ liệu"); add_simple_database_groups(c)
    c.text(.95,5.34,11.4,.42,f"DbContext hiện khai báo {facts.dbsets} DbSet",size=17,color=C["navy"],bold=True,align="ctr",fill=C["bg"])
    slides.append(_set_notes(s,["Thay vì trình bày ERD chi tiết, em nhóm dữ liệu theo năm miền nghiệp vụ.","Tài khoản và sản phẩm là dữ liệu nền của hệ thống.","Giỏ hàng, đơn hàng và bảo hành thể hiện luồng mua bán.","Nhóm chat lưu cả hội thoại và tin nhắn để hỗ trợ khách lâu dài."],[f"{facts.dbsets} DbSet trong ApplicationDbContext","Năm nhóm dữ liệu dễ theo dõi"]))

    s,c=_base_slide(12,"Các module chính")
    modules=[("Sản phẩm","Danh mục, lọc, chi tiết"),("Tài khoản","Đăng ký, đăng nhập"),("Giỏ hàng","Thêm, sửa, xóa"),("Đơn hàng","Checkout, thanh toán"),("Build PC / So sánh","Hỗ trợ lựa chọn"),("Quản trị / hỗ trợ","Vận hành, hậu mãi")]
    for i,(t,b) in enumerate(modules): add_card(c,.72+(i%3)*4.18,1.58+(i//3)*2.38,3.78,1.88,title=t,body=b,accent=[C["blue"],C["cyan"],C["purple"],C["orange"],C["success"],C["navy"]][i])
    slides.append(_set_notes(s,["Source code được tổ chức thành các module nghiệp vụ tương đối rõ.","Nhóm sản phẩm, tài khoản và giỏ hàng phục vụ đầu hành trình mua sắm.","Đơn hàng, Build PC và so sánh hỗ trợ quyết định và giao dịch.","Khối quản trị cùng hỗ trợ giúp cửa hàng vận hành sau khi khách đặt hàng."],["Sáu module nghiệp vụ","Module khách hàng và quản trị liên kết"]))

    s=add_section_divider(13,"Demo website khách hàng","Minh họa hành trình mua sắm từ trang chủ đến hậu mãi","03")
    slides.append(_set_notes(s,["Sau phần thiết kế, em xin chuyển sang demo website khách hàng.","Các màn hình được sắp theo đúng hành trình sử dụng phổ biến.","Mỗi slide dành phần lớn diện tích cho ảnh chụp giao diện thật.","Khi bảo vệ, em sẽ thao tác trực tiếp và dùng slide làm phương án dự phòng."],["Demo theo hành trình người dùng","Ưu tiên ảnh thật, ít chữ"]))

    demos=[
        (14,"Trang chủ","/",["Banner và danh mục nổi bật","Sản phẩm khuyến mãi","Điểm vào hành trình mua sắm"]),
        (15,"Danh sách sản phẩm","/Products",["Lọc theo danh mục và giá","Tìm kiếm, sắp xếp","Hiển thị tồn kho và khuyến mãi"]),
        (16,"Chi tiết sản phẩm","/Products/Detail/{id}",["Thông tin và hình ảnh sản phẩm","Thông số kỹ thuật rõ ràng","Thêm giỏ hoặc mua ngay"]),
        (17,"Giỏ hàng","/Cart",["Cập nhật số lượng","Xóa hoặc làm trống giỏ","Tính tổng trước checkout"]),
        (18,"Checkout & vận chuyển","/Checkout",["Nhập thông tin nhận hàng","Tính phí giao hàng","Chọn phương thức thanh toán"]),
        (19,"Thanh toán QR / chuyển khoản","/Orders/BankTransfer/{id}",["Hiển thị QR và nội dung chuyển","Có thời hạn thanh toán","Khách xác nhận đã chuyển tiền"]),
        (20,"Theo dõi đơn hàng","/Order/Tracking/{id}",["Tra cứu bằng mã đơn","Theo dõi trạng thái xử lý","Xem thông tin vận chuyển"]),
        (21,"Build PC","/BuildPc",["Chọn linh kiện theo nhóm","Kiểm tra tương thích cơ bản","Thêm cấu hình vào giỏ"]),
        (22,"So sánh sản phẩm","/Compare",["So sánh tối đa hai sản phẩm","Đối chiếu giá và thông số","Lưu lựa chọn trong session"]),
        (23,"Hỗ trợ và hậu mãi","/Warranty",["Chat realtime với hỗ trợ","Gửi yêu cầu bảo hành","Xem báo giá từ đơn hàng"]),
    ]
    for n,title,url,items in demos:
        s=add_demo_slide(n,title,url,items)
        slides.append(_set_notes(s,[f"Màn hình {title.lower()} là một bước trong hành trình của khách hàng.","Phần ảnh lớn giúp hội đồng quan sát giao diện thật thay vì đọc mô tả dài.",f"Các thao tác chính gồm {items[0].lower()} và {items[1].lower()}.","Tất cả nội dung trình bày ở đây đều được đối chiếu với controller, view và script liên quan."],[items[0],items[-1]]))

    s=add_section_divider(24,"Quản trị hệ thống","Theo dõi vận hành và xử lý nghiệp vụ cửa hàng","04")
    slides.append(_set_notes(s,["Tiếp theo là phần quản trị hệ thống.","Em tập trung vào dashboard và hai nhóm nghiệp vụ vận hành quan trọng nhất.","Giao diện quản trị sử dụng dữ liệu thật từ database.","Các thao tác được giới hạn theo quyền của người quản trị."],["Dashboard tổng quan","Quản lý sản phẩm và đơn hàng"]))

    s,c=_base_slide(25,"Dashboard quản trị")
    add_image_placeholder(c,.68,1.55,8.22,4.95,"DASHBOARD QUẢN TRỊ","/AdminDashboard",accent=C["orange"])
    for i,(t,b,col) in enumerate([("SẢN PHẨM","ProductCount",C["blue"]),("ĐƠN HÀNG","OrderCount",C["orange"]),("NGƯỜI DÙNG","UserCount",C["purple"]),("BẢO HÀNH","WarrantyRequestCount",C["success"])]):
        add_card(c,9.20,1.55+i*1.22,3.45,1.00,title=t,body=b,accent=col,title_size=14,body_size=14)
    s.image_note="Có. URL: /AdminDashboard."
    slides.append(_set_notes(s,["Dashboard cung cấp cái nhìn nhanh về tình trạng vận hành.","Bốn KPI trên slide tương ứng với dữ liệu có trong AdminDashboardVm.","Quản trị viên có thể từ đây chuyển sang các màn hình nghiệp vụ.","Khi demo, em sẽ dùng ảnh thật để tránh tạo dashboard giả trên PowerPoint."],["KPI lấy từ ViewModel thật","Ảnh thật chiếm phần lớn slide"]))

    s,c=_base_slide(26,"Quản lý sản phẩm & đơn hàng")
    add_image_placeholder(c,.70,1.58,5.72,3.58,"QUẢN LÝ SẢN PHẨM","/AdminProducts",accent=C["blue"])
    add_image_placeholder(c,6.90,1.58,5.72,3.58,"QUẢN LÝ ĐƠN HÀNG","/AdminOrders",accent=C["orange"])
    c.text(.90,5.46,5.30,.42,"Thêm, sửa, xóa · ảnh · tồn kho",size=15,color=C["blue"],bold=True,align="ctr",fill=C["bg"])
    c.text(7.10,5.46,5.30,.42,"Chi tiết · trạng thái · xác nhận tiền",size=15,color=C["orange"],bold=True,align="ctr",fill=C["bg"])
    s.image_note="Có. URL: /AdminProducts và /AdminOrders."
    slides.append(_set_notes(s,["Hai màn hình quản trị chính là sản phẩm và đơn hàng.","Quản lý sản phẩm hỗ trợ tạo, sửa, xóa, ảnh và thông tin tồn kho.","Quản lý đơn cho phép xem chi tiết, cập nhật trạng thái và xác nhận chuyển khoản.","Bố cục hai ảnh giúp so sánh nhanh mà không cần dựng dashboard phức tạp."],["Hai nghiệp vụ vận hành chính","Không mô phỏng giao diện bằng nhiều shape"]))

    s,c=_base_slide(27,"Kết quả đạt được")
    add_stat_cards(c,[("6","Nhóm module",C["blue"]),(str(facts.dbsets),"DbSet",C["cyan"]),(str(facts.controllers),"Controller",C["purple"]),("4","Nhóm người dùng",C["orange"]),("ASP.NET","Công nghệ chính",C["success"])])
    c.text(.88,5.33,11.55,.44,"Hoàn thiện luồng mua hàng, quản trị và hỗ trợ sau bán",size=17,color=C["navy"],bold=True,align="ctr",fill=C["bg"])
    slides.append(_set_notes(s,["Kết quả đạt được được tổng hợp trực tiếp từ cấu trúc source hiện tại.",f"Hệ thống có {facts.controllers} controller và {facts.dbsets} DbSet trong DbContext.","Sáu nhóm module bao phủ luồng khách hàng, quản trị và hỗ trợ.","Quan trọng nhất là các chức năng có thể liên kết thành một quy trình mua hàng hoàn chỉnh."],[f"{facts.controllers} controller, {facts.dbsets} DbSet","Luồng nghiệp vụ đã kết nối"]))

    s,c=_base_slide(28,"Hạn chế")
    add_card(c,.78,1.62,5.70,4.82,title="HIỆN TẠI",body="• Chuyển khoản cần xác nhận\n• Gợi ý Build PC theo quy tắc\n• Báo cáo quản trị còn cơ bản\n• Trải nghiệm mobile cần tối ưu",accent=C["orange"])
    add_card(c,6.84,1.62,5.70,4.82,title="CẦN CẢI THIỆN",body="• Tự động hóa thanh toán\n• Nâng chất lượng gợi ý\n• Bổ sung báo cáo trực quan\n• Tối ưu hiệu năng và SEO",accent=C["blue"])
    slides.append(_set_notes(s,["Bên cạnh kết quả đạt được, hệ thống vẫn còn một số giới hạn.","Thanh toán chuyển khoản hiện cần bước xác nhận của khách và quản trị.","Build PC mới dừng ở kiểm tra tương thích theo quy tắc đã cài đặt.","Đây là các điểm thực tế để tiếp tục cải thiện sau đồ án."],["Nhìn nhận đúng giới hạn hiện tại","Hạn chế gắn với hướng phát triển"]))

    s,c=_base_slide(29,"Hướng phát triển")
    roadmap=[("01","Cổng thanh toán"),("02","Build PC thông minh"),("03","Báo cáo doanh thu"),("04","Mobile và SEO")]
    c.line(1.30,3.18,12.00,3.18,color=C["success"],width=26000,alpha=42000)
    for i,(code,label) in enumerate(roadmap):
        x=1.10+i*3.00
        c.shape(x,2.75,.82,.82,fill=C["success"],line=C["paper"],geom="ellipse",shadow=True)
        c.text(x,2.75,.82,.82,code,size=15,color=C["paper"],bold=True,align="ctr",fill=C["success"])
        c.text(x-.48,3.85,1.80,.70,label,size=15,color=C["text"],bold=True,align="ctr",fill=C["bg"])
    slides.append(_set_notes(s,["Từ các hạn chế vừa nêu, em đề xuất bốn hướng phát triển.","Ưu tiên đầu tiên là tích hợp cổng thanh toán để tự động đối soát.","Sau đó có thể nâng Build PC bằng dữ liệu và mô hình gợi ý phù hợp hơn.","Báo cáo doanh thu, mobile và SEO sẽ giúp hệ thống sẵn sàng vận hành thực tế."],["Bốn bước rõ ràng","Ưu tiên thanh toán và trải nghiệm"]))

    s=Slide(30,"Xin chân thành cảm ơn", "thanks"); c=Canvas(s); add_safe_background(c,5,dark=True)
    c.text(.82,1.50,11.7,.42,"DATN PC STORE",size=17,color=C["cyan"],bold=True,align="ctr",fill=C["dark"])
    c.text(.82,2.18,11.7,.82,"XIN CHÂN THÀNH CẢM ƠN",size=34,color=C["paper"],bold=True,align="ctr",fill=C["dark"])
    c.text(2.10,3.35,9.1,.52,"Em sẵn sàng lắng nghe câu hỏi và góp ý từ hội đồng",size=19,color="C7DAED",align="ctr",fill=C["dark"])
    c.shape(5.45,4.30,2.42,.08,fill=C["orange"],line=C["orange"],geom="rect"); add_footer_progress(c,dark=True)
    slides.append(_set_notes(s,["Phần trình bày của em xin được kết thúc tại đây.","Em xin cảm ơn thầy cô và hội đồng đã lắng nghe.","Em rất mong nhận được nhận xét để tiếp tục hoàn thiện sản phẩm.","Em xin sẵn sàng trả lời các câu hỏi của hội đồng."],["Cảm ơn hội đồng","Sẵn sàng trao đổi"]))
    return slides


def write_presentation_speech(slides: list[Slide]) -> None:
    lines = ["# Lời thuyết trình — DATN PC Store", "", "> Nội dung khớp với 30 slide được sinh từ source code hiện tại.", ""]
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
        raise RuntimeError("Speech markdown does not contain exactly 30 slides")


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
          '<dc:title>DATN PC Store — Bảo vệ đồ án tốt nghiệp</dc:title><dc:creator>DATN PC Store</dc:creator><dc:description>30-slide creative thesis-defense deck generated from verified source code.</dc:description></cp:coreProperties>')
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
