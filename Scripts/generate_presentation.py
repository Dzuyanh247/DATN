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
NOTES_PATH = OUTPUT / "presentation_notes.md"
TOTAL = 30
W, H, EMU = 13.333, 7.5, 914400
FONT = "Aptos"

C = {
    "navy": "0B3D91", "blue": "1E88E5", "cyan": "00B8D9", "orange": "F59E0B",
    "bg": "F8FAFC", "paper": "FFFFFF", "text": "16324F", "muted": "64748B",
    "success": "10B981", "danger": "EF4444", "purple": "8B5CF6", "pink": "EC4899",
    "line": "DCE6F1", "ice": "EAF4FF", "mint": "E9FBF5", "warm": "FFF6DF",
    "dark": "071D3B", "slate": "0F2942", "soft": "F1F5F9",
}

SECTIONS = {
    1: ("SECTION 01", "MỞ ĐẦU", C["blue"], "◎", "01"),
    2: ("SECTION 02", "PHÂN TÍCH & THIẾT KẾ", C["purple"], "◇", "02"),
    3: ("SECTION 03", "TRIỂN KHAI WEBSITE", C["cyan"], "▣", "03"),
    4: ("SECTION 04", "ĐÁNH GIÁ & PHÁT TRIỂN", C["orange"], "↗", "04"),
    5: ("SECTION 05", "CẢM ƠN", C["pink"], "✦", "05"),
}


def section_for(number: int) -> int:
    if number <= 6: return 1
    if number <= 13: return 2
    if number <= 24: return 3
    if number <= 29: return 4
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
        "Services/BuildCompatibilityService.cs": ["BuildCompatibilityService"],
        "Views/Products/Index.cshtml": ["ProductFilterVm"],
        "Views/Orders/BankTransfer.cshtml": ["QR", "PaymentExpireAt"],
        "wwwroot/js/chatbox.js": ["signalR"],
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
            line=line or fill, color=color or C["text"], size=size, bold=bold, align=align,
            valign=valign, geom=geom, alpha=alpha, line_alpha=line_alpha, shadow=shadow,
            rotation=rotation, margin=margin))

    def line(self, x1, y1, x2, y2, *, color=None, width=19050, dash=None, alpha=100000,
             arrow=False) -> None:
        self.s.elements.append(Element("line", x1, y1, x2-x1, y2-y1, fill="FFFFFF",
            line=color or C["line"], line_width=width, dash=dash, line_alpha=alpha,
            arrow_end="triangle" if arrow else None, geom="line"))


# ---------- reusable design helpers ----------
def add_background(c: Canvas, section: int, *, dark=False, variant=0) -> None:
    accent = SECTIONS[section][2]
    if dark:
        c.shape(0, 0, W, H, fill=C["dark"], fill2=C["navy"], line=C["dark"], geom="rect")
        c.shape(9.2, -.7, 4.9, 4.9, fill=accent, alpha=16000, line=accent, line_alpha=0, geom="ellipse")
        c.shape(-1.0, 5.4, 4.2, 4.2, fill=C["cyan"], alpha=9000, line=C["cyan"], line_alpha=0, geom="ellipse")
    else:
        c.shape(0, 0, W, H, fill=C["bg"], fill2="EEF5FC", line=C["bg"], geom="rect")
        positions = [(10.6, -.9, 3.5), (-1.2, 4.9, 3.0), (10.8, 5.5, 2.2)]
        x, y, d = positions[variant % len(positions)]
        c.shape(x, y, d, d, fill=accent, alpha=7000, line=accent, line_alpha=0, geom="ellipse")
        c.shape(x+.55, y+.55, d-1.1, d-1.1, fill=C["paper"], alpha=42000,
                line=accent, line_alpha=12000, geom="ellipse")
    # section watermark
    c.text(10.52, .38, 2.1, 1.2, SECTIONS[section][4], size=54,
           color="FFFFFF" if dark else accent, bold=True, align="r",
           fill=C["dark"] if dark else C["bg"], alpha=0, line_alpha=0, margin=0)


def add_header(c: Canvas, title: str, kicker: str | None = None, *, dark=False) -> None:
    section = section_for(c.s.number)
    accent = SECTIONS[section][2]
    text_color = C["paper"] if dark else C["text"]
    muted = "B8D7F4" if dark else C["muted"]
    c.text(.64, .22, 4.8, .24, kicker or SECTIONS[section][0], size=9, color=accent, bold=True,
           fill=C["dark"] if dark else C["bg"])
    c.text(.64, .50, 11.3, .55, title, size=30, color=text_color, bold=True,
           fill=C["dark"] if dark else C["bg"], margin=0)
    c.shape(.64, 1.13, .76, .055, fill=accent, line=accent, geom="rect")
    c.shape(1.46, 1.13, 10.9, .025, fill=muted, line=muted, alpha=26000, line_alpha=0, geom="rect")


def add_footer(c: Canvas, *, dark=False) -> None:
    if c.s.number == 1:
        return
    section = section_for(c.s.number)
    section_name = SECTIONS[section][1]
    accent = SECTIONS[section][2]
    base = "17365E" if dark else "E2E8F0"
    label = "C7DBF2" if dark else C["muted"]
    y = 7.17
    c.text(.62, y-.12, .72, .20, f"{c.s.number:02d}/{TOTAL:02d}", size=9, color=accent, bold=True,
           fill=C["dark"] if dark else C["bg"])
    c.text(1.40, y-.12, 2.55, .20, section_name, size=8, color=label, bold=True,
           fill=C["dark"] if dark else C["bg"])
    c.shape(4.02, y-.035, 8.66, .045, fill=base, line=base, geom="rect")
    c.shape(4.02, y-.035, 8.66 * c.s.number / TOTAL, .045, fill=accent, line=accent, geom="rect")


def add_card(c: Canvas, x, y, w, h, *, title="", body="", accent=None, icon=None,
             dark=False, title_size=15, body_size=11, number=None) -> None:
    accent = accent or SECTIONS[section_for(c.s.number)][2]
    fill = "102E52" if dark else C["paper"]
    line = "2A4C73" if dark else C["line"]
    c.shape(x, y, w, h, fill=fill, line=line, shadow=True, alpha=93000 if dark else 98000)
    c.shape(x, y, .055, h, fill=accent, line=accent, geom="rect")
    if icon:
        c.shape(x+.22, y+.22, .48, .48, fill=accent, alpha=18000, line=accent, line_alpha=0, geom="ellipse")
        c.text(x+.22, y+.20, .48, .48, icon, size=16, color=accent, bold=True, align="ctr", fill=fill)
    if number:
        c.text(x+w-.62, y+.12, .42, .28, number, size=10, color=accent, bold=True, align="r", fill=fill)
    tx = x + (.84 if icon else .28)
    c.text(tx, y+.18, w-(tx-x)-.20, .36, title, size=title_size,
           color=C["paper"] if dark else C["text"], bold=True, fill=fill, margin=0)
    if body:
        c.text(x+.28, y+.70, w-.56, h-.86, body, size=body_size,
               color="C2D5E8" if dark else C["muted"], fill=fill, valign="top", margin=.02)


def add_chip(c: Canvas, x, y, w, label, *, color=None, dark=False) -> None:
    color = color or SECTIONS[section_for(c.s.number)][2]
    bg = "17365E" if dark else C["paper"]
    c.text(x, y, w, .34, label, size=10, color=color, bold=True, align="ctr", fill=bg,
           line=color, geom="roundRect", alpha=90000, line_alpha=52000, margin=.02)


def add_icon(c: Canvas, x, y, symbol, *, color=None, size=.62, dark=False) -> None:
    color = color or SECTIONS[section_for(c.s.number)][2]
    c.shape(x, y, size, size, fill=color, alpha=17000, line=color, line_alpha=28000, geom="ellipse")
    c.text(x, y-.01, size, size, symbol, size=17, color=color, bold=True, align="ctr",
           fill=C["dark"] if dark else C["paper"])


def add_placeholder(c: Canvas, x, y, w, h, label, url, *, accent=None, dark=False) -> None:
    accent = accent or SECTIONS[section_for(c.s.number)][2]
    fill = "102E52" if dark else "F3F8FD"
    c.shape(x, y, w, h, fill=fill, line=accent, line_width=19050, dash="dash", shadow=True)
    d = min(.72, h*.22)
    c.shape(x+w/2-d/2, y+h*.25-d/2, d, d, fill=accent, alpha=15000,
            line=accent, line_alpha=35000, geom="ellipse")
    c.text(x+w/2-d/2, y+h*.25-d/2-.01, d, d, "▣", size=20, color=accent, bold=True,
           align="ctr", fill=fill)
    c.text(x+.25, y+h*.42, w-.5, .36, "THÊM ẢNH", size=17, color=accent, bold=True,
           align="ctr", fill=fill)
    c.text(x+.28, y+h*.56, w-.56, .38, label, size=12,
           color=C["paper"] if dark else C["text"], bold=True, align="ctr", fill=fill)
    c.text(x+.28, y+h-.43, w-.56, .22, f"URL: {url}", size=9,
           color="B8D7F4" if dark else C["muted"], align="ctr", fill=fill)


def add_node(c: Canvas, x, y, w, h, label, *, color=None, sub="", dark=False) -> None:
    color = color or SECTIONS[section_for(c.s.number)][2]
    fill = "102E52" if dark else C["paper"]
    c.shape(x, y, w, h, fill=fill, line=color, line_width=15000, shadow=True)
    c.text(x+.12, y+.12, w-.24, .30, label, size=13, color=color, bold=True, align="ctr", fill=fill)
    if sub:
        c.text(x+.12, y+.48, w-.24, h-.58, sub, size=9,
               color="C2D5E8" if dark else C["muted"], align="ctr", fill=fill)


def add_timeline(c: Canvas, items: list[tuple[str, str]], *, y=3.25, color=None) -> None:
    color = color or SECTIONS[section_for(c.s.number)][2]
    start, end = 1.02, 12.30
    c.line(start, y, end, y, color=color, width=26000, alpha=45000)
    gap = (end-start)/(len(items)-1)
    for i, (num, label) in enumerate(items):
        x = start + i*gap
        c.shape(x-.25, y-.25, .50, .50, fill=C["paper"], line=color, line_width=19050, geom="ellipse", shadow=True)
        c.text(x-.25, y-.25, .50, .50, num, size=11, color=color, bold=True, align="ctr", fill=C["paper"])
        c.text(x-.72, y+.43, 1.44, .58, label, size=11, color=C["text"], bold=True, align="ctr", fill=C["bg"])


def add_stats_cards(c: Canvas, stats: list[tuple[str, str, str]], y=2.15) -> None:
    gap, x0 = .22, .68
    w = (12.0-gap*(len(stats)-1))/len(stats)
    for i, (value, label, color) in enumerate(stats):
        x = x0+i*(w+gap)
        c.shape(x, y, w, 2.45, fill=C["paper"], line=C["line"], shadow=True)
        c.shape(x+.22, y+.22, .54, .54, fill=color, alpha=16000, line=color, line_alpha=0, geom="ellipse")
        c.text(x+.22, y+.20, .54, .54, "✦", size=15, color=color, bold=True, align="ctr", fill=C["paper"])
        c.text(x+.24, y+.84, w-.48, .72, value, size=32, color=color, bold=True, align="ctr", fill=C["paper"])
        c.text(x+.24, y+1.65, w-.48, .42, label, size=12, color=C["muted"], bold=True, align="ctr", fill=C["paper"])


def new_slide(number: int, title: str, layout: str, *, dark=False, variant=0,
              header=True, transition=None) -> tuple[Slide, Canvas]:
    s = Slide(number, title, layout, transition=transition)
    c = Canvas(s)
    add_background(c, section_for(number), dark=dark, variant=variant)
    if header: add_header(c, title, dark=dark)
    add_footer(c, dark=dark)
    return s, c


# ---------- 30 individually composed slides ----------
def slide01() -> Slide:
    s, c = new_slide(1, "DATN PC STORE", "01 Cover full-screen", dark=True, header=False, transition="fade")
    # abstract technology board
    for x, y, w, h, a in [(8.85,.72,3.75,1.15,22000),(9.35,2.08,3.00,1.05,15000),(8.35,3.40,4.00,1.12,18000),(9.10,4.76,3.55,1.05,12000)]:
        c.shape(x,y,w,h,fill=C["paper"],alpha=a,line=C["cyan"],line_alpha=26000,shadow=True)
    c.line(10.7,1.87,10.7,2.08,color=C["cyan"],width=18000,alpha=50000)
    c.line(10.7,3.13,10.2,3.40,color=C["cyan"],width=18000,alpha=50000)
    c.line(10.2,4.52,10.8,4.76,color=C["cyan"],width=18000,alpha=50000)
    c.text(.78,.64,5.9,.28,"BÁO CÁO BẢO VỆ ĐỒ ÁN TỐT NGHIỆP",size=11,color="9CD8F5",bold=True,fill=C["dark"])
    c.text(.78,1.22,7.55,1.48,"WEBSITE BÁN\nLINH KIỆN MÁY TÍNH",size=38,color=C["paper"],bold=True,fill=C["dark"],valign="top",margin=0)
    c.shape(.78,2.92,1.12,.065,fill=C["orange"],line=C["orange"],geom="rect")
    c.text(.78,3.20,6.8,.40,"DATN PC STORE",size=22,color=C["cyan"],bold=True,fill=C["dark"])
    add_chip(c,.78,3.82,1.72,"ASP.NET CORE",color=C["blue"],dark=True)
    add_chip(c,2.66,3.82,1.50,"SQL SERVER",color=C["cyan"],dark=True)
    add_chip(c,4.32,3.82,1.28,"SIGNALR",color=C["orange"],dark=True)
    c.shape(.78,4.62,6.78,1.34,fill="102E52",line="2B507B",alpha=94000,shadow=True)
    c.text(1.05,4.84,2.85,.23,"SINH VIÊN THỰC HIỆN",size=9,color="92BBDD",bold=True,fill="102E52")
    c.text(1.05,5.18,2.85,.34,"[HỌ VÀ TÊN] · [MSSV]",size=14,color=C["paper"],bold=True,fill="102E52")
    c.line(4.10,4.88,4.10,5.68,color="3A5D83",width=12000)
    c.text(4.38,4.84,2.80,.23,"GIẢNG VIÊN HƯỚNG DẪN",size=9,color="92BBDD",bold=True,fill="102E52")
    c.text(4.38,5.18,2.80,.34,"[HỌ VÀ TÊN GIẢNG VIÊN]",size=14,color=C["paper"],bold=True,fill="102E52")
    c.text(.78,6.48,3.8,.22,"HÀ NỘI · 2026",size=10,color="9CD8F5",bold=True,fill=C["dark"])
    c.text(9.08,.98,3.30,.42,"01  DISCOVER",size=14,color=C["paper"],bold=True,align="ctr",fill="102E52")
    c.text(9.58,2.34,2.50,.42,"02  DESIGN",size=14,color=C["paper"],bold=True,align="ctr",fill="102E52")
    c.text(8.60,3.72,3.50,.42,"03  BUILD",size=14,color=C["paper"],bold=True,align="ctr",fill="102E52")
    c.text(9.36,5.06,3.00,.42,"04  DELIVER",size=14,color=C["paper"],bold=True,align="ctr",fill="102E52")
    s.speaker = ["Kính thưa thầy cô và hội đồng, em xin trình bày đề tài DATN PC Store.", "Đề tài xây dựng website bán PC và linh kiện máy tính trên ASP.NET Core MVC.", "Hệ thống kết nối trải nghiệm mua hàng, vận chuyển, hỗ trợ và quản trị.", "Bài trình bày đi từ vấn đề thực tế đến thiết kế, triển khai và đánh giá."]
    s.animation = ["Tiêu đề Fade trước trong 0,6 giây.", "Các chip công nghệ xuất hiện lần lượt sau tiêu đề.", "Khối quy trình bên phải Wipe từ trên xuống trong 0,8 giây."]
    s.duration = "35–45 giây"
    return s


def slide02() -> Slide:
    s, c = new_slide(2, "Đặt vấn đề", "02 Quote problem statement", dark=True, variant=1, transition="fade")
    add_icon(c,.78,1.50,"!",color=C["danger"],size=.82,dark=True)
    c.text(1.88,1.42,9.75,1.35,"“Mua linh kiện PC không chỉ cần nhanh —\nmà còn phải đúng, dễ hiểu và dễ so sánh.”",size=26,color=C["paper"],bold=True,fill=C["dark"],margin=0)
    c.shape(.80,3.35,11.75,.02,fill="31577E",line="31577E",geom="rect")
    for i,(num,label,sub,color) in enumerate([
        ("01","NHANH","Tìm đúng sản phẩm",C["cyan"]),("02","CHÍNH XÁC","Đọc đúng thông số",C["orange"]),("03","MINH BẠCH","Theo dõi rõ đơn hàng",C["success"]) ]):
        x=.82+i*4.05
        c.text(x,3.70,.52,.34,num,size=10,color=color,bold=True,align="ctr",fill="17365E",line=color,geom="roundRect",alpha=85000,line_alpha=50000)
        c.text(x,4.18,3.45,.42,label,size=18,color=C["paper"],bold=True,fill=C["dark"])
        c.text(x,4.72,3.45,.34,sub,size=12,color="B8D7F4",fill=C["dark"])
    s.speaker = ["Thị trường linh kiện có nhiều lựa chọn và thông số kỹ thuật phức tạp.", "Người mua cần tìm nhanh nhưng vẫn phải chọn đúng thành phần phù hợp nhu cầu.", "Khả năng so sánh và theo dõi đơn giúp giảm sự không chắc chắn trong quyết định mua.", "Đây là vấn đề trung tâm mà hệ thống PC Store hướng tới giải quyết."]
    s.animation = ["Biểu tượng cảnh báo Zoom nhẹ trong 0,3 giây.", "Câu statement Fade trong 0,6 giây.", "Ba từ khóa xuất hiện nối tiếp bằng Wipe, mỗi mục 0,25 giây."]
    return s


def slide03() -> Slide:
    s, c = new_slide(3, "Những khó khăn thực tế", "03 Three asymmetric problem cards", variant=2)
    cards=[(.72,1.55,3.58,4.65,"⌕","KHÓ TÌM ĐÚNG\nLINH KIỆN","Danh mục lớn\nNhiều chuẩn kỹ thuật",C["danger"]),
           (4.52,2.05,3.58,4.10,"⇄","KHÓ SO SÁNH\nTHÔNG SỐ","Dữ liệu khác cấu trúc\nKhó đối chiếu trực tiếp",C["orange"]),
           (8.32,1.55,4.25,4.65,"⌁","KHÓ QUẢN LÝ\nĐƠN HÀNG","Thanh toán · giao hàng\nTrạng thái cần minh bạch",C["blue"])]
    for i,(x,y,w,h,icon,title,body,color) in enumerate(cards):
        c.shape(x,y,w,h,fill=C["paper"],line=C["line"],shadow=True)
        c.shape(x,y,w,.10,fill=color,line=color,geom="rect")
        add_icon(c,x+.30,y+.38,icon,color=color,size=.72)
        c.text(x+.30,y+1.30,w-.60,.78,title,size=18,color=C["text"],bold=True,fill=C["paper"],valign="top",margin=0)
        c.text(x+.30,y+2.42,w-.60,.82,body,size=13,color=C["muted"],fill=C["paper"],valign="top",margin=0)
        c.text(x+w-.78,y+h-.62,.46,.28,f"0{i+1}",size=10,color=color,bold=True,align="r",fill=C["paper"])
    s.speaker = ["Khó khăn đầu tiên là tìm đúng sản phẩm trong một danh mục linh kiện lớn.", "Khó khăn thứ hai là so sánh các thông số được trình bày theo nhiều cách khác nhau.", "Khó khăn thứ ba là theo dõi xuyên suốt thanh toán, xử lý và giao hàng.", "Ba điểm đau này định hướng trực tiếp cho các module cốt lõi của đề tài."]
    s.animation = ["Card 01 Float In từ trái trong 0,4 giây.", "Card 02 Fade sau 0,2 giây.", "Card 03 Float In từ phải trong 0,4 giây."]
    return s


def slide04() -> Slide:
    s, c = new_slide(4, "Lý do chọn đề tài", "04 Editorial two-column rationale", variant=0)
    c.shape(.68,1.50,5.72,4.90,fill=C["paper"],line=C["line"],shadow=True)
    c.shape(6.66,1.50,5.98,4.90,fill=C["dark"],fill2=C["navy"],line=C["navy"],shadow=True)
    c.text(.98,1.80,4.9,.28,"GÓC NHÌN THỰC TẾ",size=10,color=C["blue"],bold=True,fill=C["paper"])
    c.text(.98,2.30,4.8,.72,"Một hành trình mua hàng\ncần ít ma sát hơn",size=22,color=C["text"],bold=True,fill=C["paper"],valign="top",margin=0)
    for i,t in enumerate(["Tìm và lọc sản phẩm nhanh","So sánh trước khi quyết định","Theo dõi đơn sau checkout"]):
        add_icon(c,1.00,3.42+i*.68,"✓",color=C["success"],size=.36)
        c.text(1.52,3.35+i*.68,4.35,.44,t,size=13,color=C["muted"],bold=True,fill=C["paper"])
    c.text(6.98,1.80,4.9,.28,"GÓC NHÌN KỸ THUẬT",size=10,color=C["cyan"],bold=True,fill=C["dark"])
    c.text(6.98,2.30,4.9,.72,"Một bài toán đủ sâu\nđể vận dụng full-stack",size=22,color=C["paper"],bold=True,fill=C["dark"],valign="top",margin=0)
    for i,(a,b) in enumerate([("MVC","Tách lớp rõ ràng"),("EF","Dữ liệu quan hệ"),("RT","SignalR thời gian thực")]):
        add_icon(c,7.00,3.42+i*.68,a,color=[C["blue"],C["purple"],C["orange"]][i],size=.38,dark=True)
        c.text(7.54,3.35+i*.68,4.45,.44,b,size=13,color="C2D5E8",bold=True,fill=C["dark"])
    add_chip(c,9.25,5.68,2.72,"ASP.NET CORE MVC",color=C["cyan"],dark=True)
    s.speaker = ["Về thực tế, đề tài giải quyết một hành trình mua linh kiện có nhiều điểm ra quyết định.", "Về kỹ thuật, bài toán đủ rộng để áp dụng MVC, dữ liệu quan hệ và tích hợp dịch vụ.", "Source code hiện có cả luồng khách hàng, quản trị và hỗ trợ thời gian thực.", "Vì vậy đề tài vừa phù hợp nhu cầu thực tiễn vừa thể hiện năng lực phát triển web."]
    s.animation = ["Cột thực tế Wipe từ trái trong 0,5 giây.", "Cột kỹ thuật Wipe từ phải trong 0,5 giây.", "Badge ASP.NET Core MVC Pulse nhẹ ở cuối."]
    return s


def slide05() -> Slide:
    s, c = new_slide(5, "Mục tiêu hệ thống", "05 Circular target goal", variant=1)
    cx, cy = 6.66, 3.90
    c.shape(cx-1.38,cy-1.38,2.76,2.76,fill=C["blue"],alpha=10000,line=C["blue"],line_alpha=30000,geom="ellipse")
    c.shape(cx-.98,cy-.98,1.96,1.96,fill=C["paper"],line=C["blue"],line_width=24000,geom="ellipse",shadow=True)
    c.text(cx-1.05,cy-.42,2.10,.84,"XÂY DỰNG\nPC STORE",size=18,color=C["navy"],bold=True,align="ctr",fill=C["paper"],margin=0)
    orbit=[(6.05,1.48,"▦","SẢN PHẨM",C["blue"]),(9.20,2.22,"▤","GIỎ HÀNG",C["cyan"]),(9.42,4.76,"✓","CHECKOUT",C["success"]),(2.70,4.78,"◆","QUẢN TRỊ",C["purple"]),(2.86,2.22,"↔","HỖ TRỢ",C["orange"])]
    for x,y,icon,label,color in orbit:
        c.line(cx,cy,x+.67,y+.45,color=color,width=15000,alpha=35000)
        c.shape(x,y,1.34,.90,fill=C["paper"],line=color,shadow=True)
        c.text(x+.08,y+.08,.36,.32,icon,size=13,color=color,bold=True,align="ctr",fill=C["paper"])
        c.text(x+.12,y+.47,1.10,.22,label,size=9,color=C["text"],bold=True,align="ctr",fill=C["paper"])
    c.text(.74,6.38,11.9,.30,"MỘT LUỒNG THỐNG NHẤT · TỪ KHÁM PHÁ ĐẾN HẬU MÃI",size=11,color=C["muted"],bold=True,align="ctr",fill=C["bg"])
    s.speaker = ["Mục tiêu là xây dựng một website PC Store có luồng nghiệp vụ thống nhất.", "Phần sản phẩm hỗ trợ khám phá, lọc, xem chi tiết và so sánh.", "Phần giao dịch bao gồm giỏ hàng, checkout, thanh toán và theo dõi đơn.", "Phần vận hành gồm quản trị dữ liệu, chat hỗ trợ và tiếp nhận bảo hành."]
    s.animation = ["Vòng tròn trung tâm Zoom trong 0,45 giây.", "Năm mục tiêu xuất hiện theo chiều kim đồng hồ.", "Thông điệp cuối Fade sau cùng trong 0,35 giây."]
    return s


def slide06() -> Slide:
    s, c = new_slide(6, "Đối tượng & phạm vi", "06 Persona cards plus scope boundary", variant=2)
    personas=[("◉","KHÁCH VÃNG LAI","Xem · lọc · so sánh",C["blue"]),("♙","KHÁCH HÀNG","Đặt hàng · bảo hành",C["success"]),("◆","ADMIN","Quản lý vận hành",C["purple"]),("↔","NHÂN VIÊN HỖ TRỢ","Chat tại Admin",C["orange"])]
    for i,(icon,title,body,color) in enumerate(personas):
        x=.68+i*3.05
        c.shape(x,1.45,2.82,1.62,fill=C["paper"],line=C["line"],shadow=True)
        add_icon(c,x+.18,1.68,icon,color=color,size=.50)
        c.text(x+.82,1.62,1.78,.44,title,size=11,color=C["text"],bold=True,fill=C["paper"])
        c.text(x+.82,2.15,1.78,.35,body,size=9,color=C["muted"],fill=C["paper"])
    c.shape(.68,3.42,7.52,2.72,fill=C["paper"],line=C["success"],line_width=17000,shadow=True)
    c.text(.98,3.70,3.0,.34,"TRONG PHẠM VI",size=14,color=C["success"],bold=True,fill=C["paper"])
    inside=["Sản phẩm · giỏ · đơn hàng","Build PC · so sánh","GHN · QR · SMTP · SignalR"]
    for i,t in enumerate(inside):
        add_icon(c,1.00,4.34+i*.48,"✓",color=C["success"],size=.30)
        c.text(1.44,4.28+i*.48,5.90,.34,t,size=12,color=C["text"],bold=True,fill=C["paper"])
    c.shape(8.46,3.42,4.18,2.72,fill="FFF8F7",line=C["danger"],line_width=15000,shadow=True)
    c.text(8.76,3.70,3.55,.34,"NGOÀI PHẠM VI HIỆN TẠI",size=12,color=C["danger"],bold=True,fill="FFF8F7")
    c.text(8.76,4.32,3.25,1.20,"Ứng dụng mobile native\nAI tư vấn tự động\nCổng thanh toán production",size=12,color=C["muted"],fill="FFF8F7",valign="top",margin=0)
    s.speaker = ["Hệ thống phục vụ bốn nhóm tương tác: khách vãng lai, khách hàng, admin và nhân viên hỗ trợ.", "Nhân viên hỗ trợ là vai trò Staff hoặc Admin truy cập màn hình Admin Chat.", "Phạm vi hiện tại tập trung vào website thương mại điện tử và các tích hợp có trong source.", "Mobile native, AI và cổng thanh toán production được xác định là ngoài phạm vi hiện tại."]
    s.animation = ["Bốn persona Fade đồng thời trong 0,5 giây.", "Khung trong phạm vi Wipe từ trái.", "Khung ngoài phạm vi Wipe từ phải, trễ 0,2 giây."]
    s.duration = "45–55 giây"
    return s


def divider(number, title, subtitle, icon) -> Slide:
    s, c = new_slide(number, title, f"{number:02d} Section divider", dark=True, header=False, transition="fade")
    section = section_for(number); accent=SECTIONS[section][2]; idx=SECTIONS[section][4]
    c.text(.78,.74,2.2,.28,SECTIONS[section][0],size=11,color=accent,bold=True,fill=C["dark"])
    c.text(.74,1.30,5.0,2.15,idx,size=102,color=accent,bold=True,fill=C["dark"],margin=0)
    c.shape(5.38,1.34,.03,4.72,fill=accent,line=accent,alpha=60000,geom="rect")
    c.text(6.02,1.66,5.95,1.25,title.upper(),size=34,color=C["paper"],bold=True,fill=C["dark"],valign="top",margin=0)
    c.text(6.04,3.25,5.30,.74,subtitle,size=17,color="B8D7F4",fill=C["dark"],valign="top",margin=0)
    c.shape(10.62,5.10,1.10,1.10,fill=accent,alpha=18000,line=accent,line_alpha=45000,geom="ellipse")
    c.text(10.62,5.08,1.10,1.10,icon,size=31,color=accent,bold=True,align="ctr",fill=C["dark"])
    s.speaker = [f"Tiếp theo là phần {title.lower()}.", subtitle + ".", "Các sơ đồ được rút gọn để làm rõ cấu trúc thay vì trình bày chi tiết mã nguồn.", "Mọi chức năng đề cập trong phần này đều đã được đối chiếu với repository."]
    s.animation = ["Số section Fade trong 0,5 giây.", "Tiêu đề Wipe từ trái trong 0,6 giây.", "Biểu tượng section Zoom nhẹ sau cùng."]
    s.duration = "15–20 giây"
    return s


def slide07(): return divider(7,"Phân tích & thiết kế","Từ yêu cầu nghiệp vụ đến kiến trúc và dữ liệu","◇")


def slide08() -> Slide:
    s,c=new_slide(8,"Yêu cầu chức năng","08 Split requirements matrix",variant=0)
    cols=[(.68,C["blue"],"KHÁCH HÀNG","♙",["Tìm kiếm, lọc sản phẩm","So sánh tối đa 2 sản phẩm","Build PC, thêm vào giỏ","Checkout và theo dõi đơn","Chat, gửi yêu cầu bảo hành"]),
          (6.78,C["purple"],"QUẢN TRỊ VIÊN","◆",["Quản lý sản phẩm, danh mục","Quản lý đơn và thanh toán","Quản lý người dùng, banner","Xử lý bảo hành, hội thoại","Cấu hình website, vận chuyển"])]
    for x,color,head,icon,items in cols:
        c.shape(x,1.48,5.86,4.96,fill=C["paper"],line=C["line"],shadow=True)
        c.shape(x,1.48,5.86,.76,fill=color,line=color)
        c.text(x+.26,1.65,.42,.34,icon,size=16,color=C["paper"],bold=True,align="ctr",fill=color)
        c.text(x+.82,1.61,4.55,.38,head,size=16,color=C["paper"],bold=True,fill=color)
        for i,item in enumerate(items):
            y=2.55+i*.68
            c.text(x+.30,y,.38,.34,f"{i+1:02d}",size=9,color=color,bold=True,align="ctr",fill=color,line=color,geom="ellipse",alpha=13000,line_alpha=0)
            c.text(x+.84,y-.02,4.54,.38,item,size=12,color=C["text"],bold=True,fill=C["paper"])
            if i<4:c.shape(x+.84,y+.48,4.54,.012,fill=C["line"],line=C["line"],geom="rect")
    s.speaker=["Yêu cầu được chia theo hai phía chính là khách hàng và quản trị viên.","Khách hàng đi qua chuỗi khám phá, lựa chọn, giao dịch và hậu mãi.","Quản trị viên chịu trách nhiệm dữ liệu, đơn hàng, người dùng và cấu hình vận hành.","Các yêu cầu này ánh xạ trực tiếp tới controller, view và service trong source."]
    s.animation=["Hai tiêu đề cột xuất hiện đồng thời.","Các yêu cầu khách hàng Wipe theo nhóm trong 0,6 giây.","Các yêu cầu quản trị xuất hiện sau, trễ 0,2 giây."]
    return s


def slide09() -> Slide:
    s,c=new_slide(9,"Use case tổng quan","09 Use case orbit diagram",variant=1)
    # actors
    for x,label,color in [(.62,"KHÁCH HÀNG",C["blue"]),(11.20,"ADMIN",C["purple"])]:
        c.shape(x,2.62,1.45,1.45,fill=C["paper"],line=color,line_width=19050,geom="ellipse",shadow=True)
        c.text(x,2.82,1.45,.42,"♙" if x<1 else "◆",size=24,color=color,bold=True,align="ctr",fill=C["paper"])
        c.text(x-.15,4.25,1.75,.34,label,size=10,color=color,bold=True,align="ctr",fill=C["bg"])
    center=(4.58,2.52,4.15,1.56)
    add_node(c,*center,"PC STORE",color=C["navy"],sub="Hệ thống thương mại điện tử")
    uses=[(2.52,1.34,2.0,.72,"Tìm & lọc",C["blue"]),(2.38,4.78,2.25,.72,"Giỏ & checkout",C["success"]),
          (5.52,5.34,2.25,.72,"Build PC",C["orange"]),(8.78,4.78,2.15,.72,"Quản lý đơn",C["purple"]),
          (8.88,1.34,2.05,.72,"Quản lý dữ liệu",C["pink"]),(5.46,1.10,2.35,.72,"Chat & bảo hành",C["cyan"])]
    for x,y,w,h,label,color in uses:
        c.shape(x,y,w,h,fill=C["paper"],line=color,shadow=True,geom="ellipse")
        c.text(x+.12,y+.15,w-.24,.34,label,size=11,color=color,bold=True,align="ctr",fill=C["paper"])
        c.line(6.65,3.30,x+w/2,y+h/2,color=color,width=12000,alpha=33000)
    c.line(2.07,3.35,4.58,3.30,color=C["blue"],width=18000,arrow=True)
    c.line(8.73,3.30,11.20,3.35,color=C["purple"],width=18000,arrow=True)
    s.speaker=["Sơ đồ đặt PC Store ở trung tâm với hai tác nhân chính.","Khách hàng tương tác với tìm kiếm, giỏ hàng, checkout, Build PC và hỗ trợ.","Admin tương tác với quản lý dữ liệu, đơn hàng, chat và bảo hành.","Một số use case dùng chung nhưng quyền truy cập được kiểm soát theo vai trò."]
    s.animation=["Node PC Store Zoom trước.","Các use case bung ra theo quỹ đạo bằng Fade lần lượt.","Hai actor và đường kết nối xuất hiện sau cùng."]
    return s


def slide10() -> Slide:
    s,c=new_slide(10,"Kiến trúc hệ thống","10 Layered architecture flow",dark=True,variant=2)
    nodes=[(.52,2.24,1.72,1.15,"BROWSER","Razor · JS",C["cyan"]),(2.68,2.24,2.05,1.15,"MVC CONTROLLER","Routing · Auth",C["blue"]),
           (5.16,2.24,1.86,1.15,"SERVICE","Nghiệp vụ",C["orange"]),(7.45,2.24,1.72,1.15,"EF CORE","ORM",C["purple"]),(9.60,2.24,2.05,1.15,"SQL SERVER","22 DbSet",C["success"])]
    for x,y,w,h,label,sub,color in nodes:add_node(c,x,y,w,h,label,color=color,sub=sub,dark=True)
    for i in range(len(nodes)-1):
        x1=nodes[i][0]+nodes[i][2]; x2=nodes[i+1][0]
        c.line(x1,2.82,x2,2.82,color="76BCEB",width=18000,arrow=True)
    c.text(.66,1.46,3.8,.28,"LUỒNG XỬ LÝ CHÍNH",size=10,color="91BFE3",bold=True,fill=C["dark"])
    branches=[(1.08,4.58,"SIGNALR","Chat realtime",C["cyan"]),(3.56,4.58,"GHN","Địa chỉ · phí ship",C["orange"]),(6.04,4.58,"SMTP","OTP · email",C["pink"]),(8.52,4.58,"QR BANK","Chuyển khoản",C["success"]),(10.72,4.58,"ROUTE","Khoảng cách",C["blue"])]
    for x,y,label,sub,color in branches:
        c.shape(x,y,1.52,1.03,fill="102E52",line=color,shadow=True)
        c.text(x+.12,y+.16,1.28,.30,label,size=12,color=color,bold=True,align="ctr",fill="102E52")
        c.text(x+.10,y+.56,1.32,.24,sub,size=8,color="B8D7F4",align="ctr",fill="102E52")
        c.line(x+.76,4.58,6.05,3.42,color=color,width=10000,dash="dash",alpha=26000)
    s.speaker=["Kiến trúc chính đi từ trình duyệt qua Controller, Service, EF Core tới SQL Server.","MVC đảm nhiệm routing, xác thực và phối hợp dữ liệu cho Razor View.","Service chứa các nghiệp vụ như giỏ hàng, tương thích Build PC, vận chuyển và email.","Các nhánh SignalR, GHN, SMTP, QR và định tuyến đều có đăng ký hoặc luồng xử lý thật trong source."]
    s.animation=["Năm lớp chính Wipe từ trái sang phải trong 1 giây.","Mũi tên luồng chính xuất hiện cùng từng node.","Năm tích hợp Fade theo nhóm ở cuối."]
    return s


def slide11() -> Slide:
    s,c=new_slide(11,"Công nghệ sử dụng","11 Technology bento grid",variant=0)
    tech=[(".NET 8","ASP.NET Core MVC","#",C["navy"]),("RAZOR","Server-side View","R",C["blue"]),("EF CORE","ORM dữ liệu","E",C["purple"]),("SQL SERVER","CSDL quan hệ","S",C["cyan"]),
          ("BOOTSTRAP","UI responsive","B",C["purple"]),("JAVASCRIPT","Tương tác phía client","JS",C["orange"]),("SIGNALR","Realtime chat","↔",C["success"]),("GHN · SMTP","Shipping · Email","+",C["pink"])]
    sizes=[(.68,1.48,3.54,2.18),(4.44,1.48,2.54,2.18),(7.20,1.48,2.54,2.18),(9.96,1.48,2.68,2.18),
           (.68,3.90,2.54,2.18),(3.44,3.90,3.54,2.18),(7.20,3.90,2.54,2.18),(9.96,3.90,2.68,2.18)]
    for (name,sub,icon,color),(x,y,w,h) in zip(tech,sizes):
        c.shape(x,y,w,h,fill=C["paper"],line=C["line"],shadow=True)
        c.shape(x+.22,y+.22,.62,.62,fill=color,alpha=15000,line=color,line_alpha=0,geom="ellipse")
        c.text(x+.22,y+.20,.62,.62,icon,size=14,color=color,bold=True,align="ctr",fill=C["paper"])
        c.text(x+.24,y+1.08,w-.48,.34,name,size=14,color=C["text"],bold=True,fill=C["paper"])
        c.text(x+.24,y+1.53,w-.48,.30,sub,size=10,color=C["muted"],fill=C["paper"])
    s.speaker=["Nền tảng chính là .NET 8 với ASP.NET Core MVC và Razor View.","EF Core làm việc với SQL Server để quản lý dữ liệu quan hệ.","Bootstrap, CSS và JavaScript tạo giao diện responsive và tương tác phía client.","SignalR, GHN và SMTP mở rộng hệ thống cho chat, vận chuyển và email."]
    s.animation=["Bento grid xuất hiện theo hai hàng.","Bốn card hàng trên Fade trong 0,5 giây.","Bốn card hàng dưới Fade sau 0,2 giây."]
    return s


def slide12() -> Slide:
    s,c=new_slide(12,"Cơ sở dữ liệu","12 Database constellation map",dark=True,variant=1)
    # central database and eight real groups
    c.shape(5.36,2.42,2.62,1.72,fill=C["blue"],fill2=C["cyan"],line=C["cyan"],shadow=True)
    c.text(5.58,2.70,2.18,.34,"SQL SERVER",size=18,color=C["paper"],bold=True,align="ctr",fill=C["blue"])
    c.text(5.58,3.25,2.18,.30,"22 DbSet",size=12,color="D9F4FF",bold=True,align="ctr",fill=C["blue"])
    groups=[(.52,1.42,"USER / AUTH","Role · User · OTP",C["blue"]),(3.04,1.12,"PRODUCT","Category · Product · Image",C["cyan"]),
            (8.20,1.12,"CART","Cart · CartItem",C["success"]),(10.68,1.42,"ORDER","Order · Detail",C["orange"]),
            (.72,4.92,"SHIPPING","Config · ShopLocation",C["orange"]),(3.26,5.20,"CHAT","Conversation · Message",C["pink"]),
            (8.02,5.20,"WARRANTY","Warranty · Request",C["purple"]),(10.54,4.92,"BUILD PC","Config · Item",C["success"])]
    for x,y,title,sub,color in groups:
        add_node(c,x,y,2.12,.88,title,color=color,sub=sub,dark=True)
        c.line(6.67,3.28,x+1.06,y+.44,color=color,width=10000,dash="dash",alpha=30000)
    c.text(.66,6.50,12.0,.24,"NHÓM DỮ LIỆU THẬT TRONG APPLICATIONDBCONTEXT",size=9,color="91BFE3",bold=True,align="ctr",fill=C["dark"])
    s.speaker=["ApplicationDbContext hiện khai báo 22 DbSet.","Sơ đồ gom các bảng thành tám cụm nghiệp vụ để dễ quan sát.","Các cụm chính gồm tài khoản, sản phẩm, giỏ, đơn, vận chuyển, chat, bảo hành và Build PC.","Ngoài ra source còn có banner, bài viết, feedback và cấu hình website."]
    s.animation=["Khối SQL Server Zoom trước trong 0,4 giây.","Tám cụm dữ liệu xuất hiện theo cặp đối xứng.","Các đường liên kết Fade cùng từng cụm."]
    return s


def slide13() -> Slide:
    s,c=new_slide(13,"Quan hệ dữ liệu chính","13 Simplified ERD relationship",variant=2)
    entities=[(.48,1.54,1.52,.82,"USER",C["blue"]),(2.44,1.54,1.58,.82,"ORDER",C["orange"]),(4.52,1.54,1.88,.82,"ORDER DETAIL",C["orange"]),(6.94,1.54,1.72,.82,"PRODUCT",C["cyan"]),(9.20,1.54,1.62,.82,"CATEGORY",C["purple"]),(11.18,1.54,1.62,.82,"IMAGE",C["pink"]),
              (.48,4.34,1.52,.82,"CART",C["success"]),(2.44,4.34,1.58,.82,"CART ITEM",C["success"]),(4.52,4.34,1.88,.82,"BUILD CONFIG",C["orange"]),(6.94,4.34,1.72,.82,"BUILD ITEM",C["orange"]),(9.20,4.34,1.62,.82,"CHAT",C["pink"]),(11.18,4.34,1.62,.82,"WARRANTY",C["purple"])]
    for x,y,w,h,label,color in entities:
        c.shape(x,y,w,h,fill=C["paper"],line=color,line_width=15000,shadow=True)
        c.shape(x,y,w,.12,fill=color,line=color,geom="rect")
        c.text(x+.10,y+.28,w-.20,.30,label,size=10,color=C["text"],bold=True,align="ctr",fill=C["paper"])
    # top relationships
    for a,b in [(0,1),(1,2),(2,3),(3,4),(3,5)]:
        A=entities[a];B=entities[b];c.line(A[0]+A[2],A[1]+.42,B[0],B[1]+.42,color=C["muted"],width=15000,arrow=True)
    for a,b in [(6,7),(7,3),(8,9),(9,3),(10,0),(11,3)]:
        A=entities[a];B=entities[b];c.line(A[0]+A[2]/2,A[1]+.42,B[0]+B[2]/2,B[1]+.42,color=C["muted"],width=12000,dash="dash",arrow=True)
    c.text(.70,3.05,5.15,.28,"LUỒNG GIAO DỊCH",size=10,color=C["orange"],bold=True,fill=C["bg"])
    c.text(7.50,3.05,4.70,.28,"DỮ LIỆU BỔ TRỢ",size=10,color=C["purple"],bold=True,align="r",fill=C["bg"])
    c.text(.70,6.18,11.90,.32,"1 — N biểu diễn quan hệ chính; sơ đồ đã lược giản khóa và thuộc tính.",size=10,color=C["muted"],align="ctr",fill=C["bg"])
    s.speaker=["Luồng dữ liệu giao dịch đi từ User đến Order, OrderDetail và Product.","Product thuộc Category và có thể có nhiều ProductImage.","CartItem và BuildPcItem đều tham chiếu về sản phẩm.","Chat và Warranty liên kết với người dùng hoặc sản phẩm theo đúng cấu hình EF Core."]
    s.animation=["Hàng entity giao dịch xuất hiện trước.","Các quan hệ chính Wipe từ trái sang phải.","Hàng dữ liệu bổ trợ và đường nét đứt Fade sau cùng."]
    return s


def slide14(): return divider(14,"Triển khai website","Trải nghiệm người dùng, giao dịch và hỗ trợ realtime","▣")


def slide15() -> Slide:
    s,c=new_slide(15,"Trang chủ","15 Hero browser showcase",variant=0,transition="push")
    # browser frame occupying ~70%
    c.shape(.58,1.42,8.70,5.25,fill=C["paper"],line=C["line"],shadow=True)
    c.shape(.58,1.42,8.70,.46,fill="E8EEF5",line="E8EEF5")
    for i,color in enumerate([C["danger"],C["orange"],C["success"]]):c.shape(.82+i*.25,1.57,.11,.11,fill=color,line=color,geom="ellipse")
    c.text(2.14,1.52,4.92,.20,"/Home/Index",size=8,color=C["muted"],align="ctr",fill=C["paper"],line=C["line"],geom="roundRect",alpha=90000,line_alpha=50000)
    add_placeholder(c,.80,2.08,8.24,4.30,"ẢNH TRANG CHỦ","/Home/Index",accent=C["cyan"])
    c.text(9.68,1.58,2.66,.28,"FIRST IMPRESSION",size=10,color=C["cyan"],bold=True,fill=C["bg"])
    c.text(9.68,2.12,2.80,1.06,"Điểm chạm đầu tiên\ncủa hành trình mua",size=21,color=C["text"],bold=True,fill=C["bg"],valign="top",margin=0)
    for i,(icon,t) in enumerate([("▣","Banner động"),("⌕","Nhóm sản phẩm"),("↗","Khuyến mãi nổi bật")]):
        add_icon(c,9.70,3.58+i*.72,icon,color=[C["cyan"],C["blue"],C["orange"]][i],size=.40)
        c.text(10.26,3.51+i*.72,2.18,.38,t,size=11,color=C["muted"],bold=True,fill=C["bg"])
    s.image_note="Chèn ảnh thật trang chủ tại /Home/Index, ưu tiên khung nhìn desktop đầy đủ banner và nhóm sản phẩm."
    s.speaker=["Trang chủ là điểm chạm đầu tiên của hành trình mua hàng.","HomeIndexVm cung cấp banner, danh mục và nhiều nhóm sản phẩm nổi bật.","Giao diện hướng người dùng tới khuyến mãi và các nhóm sản phẩm chính.","Ảnh thực tế nên thể hiện rõ banner và ít nhất hai section sản phẩm."]
    s.animation=["Khung trình duyệt Push từ trái trong 0,6 giây.","Placeholder ảnh Fade ngay sau khung.","Ba điểm nhấn bên phải xuất hiện lần lượt."]
    return s


def slide16() -> Slide:
    s,c=new_slide(16,"Danh sách sản phẩm","16 Product grid showcase",variant=1,transition="wipe")
    c.shape(.68,1.44,12.00,4.72,fill=C["paper"],line=C["line"],shadow=True)
    # filter rail
    c.shape(.90,1.72,2.08,4.10,fill=C["soft"],line=C["line"])
    c.text(1.12,1.98,1.62,.28,"BỘ LỌC",size=12,color=C["text"],bold=True,fill=C["soft"])
    for i,label in enumerate(["Danh mục","Khoảng giá","Hãng","CPU","RAM / GPU"]):
        c.shape(1.12,2.52+i*.54,1.56,.34,fill=C["paper"],line=C["line"])
        c.text(1.24,2.55+i*.54,1.30,.20,label,size=8,color=C["muted"],bold=True,fill=C["paper"])
    # grid placeholder stylized
    add_placeholder(c,3.25,1.72,9.08,4.10,"ẢNH DANH SÁCH SẢN PHẨM","/Products",accent=C["blue"])
    chips=[("TÌM KIẾM",C["blue"]),("LỌC",C["cyan"]),("DANH MỤC",C["purple"]),("GIÁ",C["orange"]),("HÃNG",C["success"])]
    for i,(label,color) in enumerate(chips):add_chip(c,.86+i*1.52,6.35,1.34,label,color=color)
    c.text(9.10,6.38,3.22,.22,"CPU · RAM · GPU facets",size=9,color=C["muted"],bold=True,align="r",fill=C["bg"])
    s.image_note="Chèn ảnh /Products với sidebar bộ lọc và lưới sản phẩm; chọn dữ liệu có nhiều hãng và mức giá."
    s.speaker=["Trang sản phẩm hỗ trợ tìm kiếm theo từ khóa và lọc theo nhiều tiêu chí.","ProductFilterVm có danh mục, hãng, khoảng giá, CPU, RAM và GPU.","Kết quả được trình bày dạng lưới để người dùng quét nhanh thông tin chính.","Ảnh chụp nên mở sidebar bộ lọc để thể hiện rõ khả năng thu hẹp lựa chọn."]
    s.animation=["Sidebar Wipe từ trái trong 0,4 giây.","Lưới sản phẩm Wipe từ phải trong 0,6 giây.","Các chip tiêu chí Fade đồng thời."]
    return s


def slide17() -> Slide:
    s,c=new_slide(17,"Chi tiết sản phẩm","17 Product detail split layout",variant=2,transition="push")
    add_chip(c,.70,1.38,1.70,"PRODUCT DETAIL",color=C["purple"])
    add_placeholder(c,.70,1.88,5.25,4.62,"ẢNH CHI TIẾT SẢN PHẨM","/Products/Detail/{id}",accent=C["purple"])
    c.shape(6.24,1.88,6.40,4.62,fill=C["paper"],line=C["line"],shadow=True)
    c.text(6.58,2.15,5.60,.36,"TÊN SẢN PHẨM THỰC TẾ",size=18,color=C["text"],bold=True,fill=C["paper"])
    c.text(6.58,2.72,2.36,.48,"GIÁ BÁN",size=11,color=C["muted"],bold=True,fill=C["paper"])
    c.text(9.28,2.62,2.84,.58,"— đ",size=24,color=C["danger"],bold=True,align="r",fill=C["paper"])
    rows=[("THÔNG SỐ","Specifications"),("KHUYẾN MÃI","PromotionText"),("TỒN KHO","StockQuantity"),("BẢO HÀNH","WarrantyDuration")]
    for i,(a,b) in enumerate(rows):
        y=3.40+i*.53
        c.text(6.58,y,1.35,.28,a,size=9,color=C["muted"],bold=True,fill=C["paper"])
        c.text(8.05,y,3.92,.28,b,size=10,color=C["text"],bold=True,fill=C["paper"])
        c.shape(6.58,y+.36,5.55,.012,fill=C["line"],line=C["line"],geom="rect")
    c.text(6.58,5.82,2.42,.48,"−    1    +",size=12,color=C["text"],bold=True,align="ctr",fill=C["soft"],line=C["line"],geom="roundRect",alpha=100000,line_alpha=70000)
    c.text(9.18,5.82,2.96,.48,"THÊM VÀO GIỎ",size=12,color=C["paper"],bold=True,align="ctr",fill=C["blue"],line=C["blue"],geom="roundRect",alpha=100000,line_alpha=100000)
    s.image_note="Chèn ảnh trang /Products/Detail/{id}; chọn sản phẩm có giá, khuyến mãi, thông số và bảo hành."
    s.speaker=["Trang chi tiết tập trung các thông tin ra quyết định của một sản phẩm.","Model Product có giá, giá giảm, tồn kho, thông số, khuyến mãi và bảo hành.","Người dùng có thể chọn số lượng và thêm sản phẩm vào giỏ.","Ảnh thực tế nên chọn sản phẩm có đủ khuyến mãi và thông số để slide giàu thông tin."]
    s.animation=["Ảnh sản phẩm Push từ trái.","Khối thông tin Push từ phải.","Nút thêm vào giỏ Pulse nhẹ sau cùng."]
    return s


def slide18() -> Slide:
    s,c=new_slide(18,"Giỏ hàng","18 Cart process ribbon",variant=0,transition="fade")
    add_placeholder(c,.72,1.50,4.52,4.90,"ẢNH GIỎ HÀNG","/Cart",accent=C["success"])
    steps=[("01","THÊM SẢN PHẨM","CartController.Add",C["blue"]),("02","CẬP NHẬT SỐ LƯỢNG","CartController.Update",C["cyan"]),("03","TÍNH TỔNG","CartService",C["orange"]),("04","CHECKOUT","/Checkout",C["success"])]
    for i,(num,title,sub,color) in enumerate(steps):
        y=1.62+i*1.12
        c.shape(5.72,y,6.56,.84,fill=C["paper"],line=C["line"],shadow=True)
        c.shape(5.94,y+.16,.52,.52,fill=color,line=color,geom="ellipse")
        c.text(5.94,y+.15,.52,.52,num,size=10,color=C["paper"],bold=True,align="ctr",fill=color)
        c.text(6.76,y+.13,3.10,.30,title,size=13,color=C["text"],bold=True,fill=C["paper"])
        c.text(6.76,y+.47,3.72,.22,sub,size=9,color=C["muted"],fill=C["paper"])
        if i<3:c.line(6.20,y+.84,6.20,y+1.12,color=color,width=17000,arrow=True)
    c.shape(9.98,5.18,2.30,1.02,fill=C["success"],line=C["success"],shadow=True)
    c.text(10.18,5.36,1.90,.25,"TỔNG ĐƠN",size=9,color="D8FFF1",bold=True,align="ctr",fill=C["success"])
    c.text(10.18,5.72,1.90,.26,"TỰ ĐỘNG",size=13,color=C["paper"],bold=True,align="ctr",fill=C["success"])
    s.image_note="Chèn ảnh /Cart có ít nhất hai sản phẩm, số lượng và tổng tiền."
    s.speaker=["Giỏ hàng tiếp nhận sản phẩm từ trang chi tiết hoặc thao tác mua ngay.","Người dùng có thể cập nhật số lượng, xóa từng dòng hoặc làm trống giỏ.","CartService tính lại thành tiền dựa trên sản phẩm và số lượng hiện tại.","Từ giỏ hàng, người dùng chuyển sang checkout để nhập thông tin nhận hàng."]
    s.animation=["Placeholder giỏ Fade trước.","Bốn bước xuất hiện từ trên xuống, mỗi bước 0,25 giây.","Card tổng đơn Zoom nhẹ sau bước cuối."]
    return s


def slide19() -> Slide:
    s,c=new_slide(19,"Checkout & vận chuyển","19 Five-step checkout timeline",variant=1,transition="fade")
    c.text(.72,1.52,5.72,.72,"Từ thông tin người nhận\nđến một đơn hàng có thể theo dõi",size=21,color=C["text"],bold=True,fill=C["bg"],valign="top",margin=0)
    add_chip(c,10.30,1.58,2.10,"GHN SHIPPING",color=C["orange"])
    add_timeline(c,[("01","THÔNG TIN"),("02","VẬN CHUYỂN"),("03","THANH TOÁN"),("04","TẠO ĐƠN"),("05","THEO DÕI")],y=3.42,color=C["cyan"])
    c.shape(.74,5.12,11.86,1.08,fill=C["paper"],line=C["line"],shadow=True)
    facts=[("ĐỊA CHỈ","Tỉnh · huyện · xã"),("PHÍ SHIP","GHN hoặc công thức"),("PHƯƠNG THỨC","COD · chuyển khoản"),("KẾT QUẢ","Order + OrderDetail")]
    for i,(a,b) in enumerate(facts):
        x=1.02+i*2.88
        c.text(x,5.35,2.34,.22,a,size=9,color=C["cyan"],bold=True,align="ctr",fill=C["paper"])
        c.text(x,5.72,2.34,.22,b,size=10,color=C["text"],bold=True,align="ctr",fill=C["paper"])
    s.speaker=["Checkout được tổ chức thành năm bước logic từ thông tin đến theo dõi.","Địa chỉ được chọn theo tỉnh, huyện và xã qua dịch vụ địa chỉ GHN.","Phí vận chuyển dùng GHN hoặc công thức nội bộ tùy chính sách cấu hình.","Khi xác nhận, hệ thống tạo Order, OrderDetail và chuyển tới trạng thái phù hợp."]
    s.animation=["Thông điệp mở đầu Fade trong 0,4 giây.","Timeline chạy Wipe từ trái sang phải trong 1 giây.","Khối thông tin kỹ thuật xuất hiện sau timeline."]
    return s


def slide20() -> Slide:
    s,c=new_slide(20,"Thanh toán QR / chuyển khoản","20 Payment two-column console",dark=True,variant=2,transition="push")
    c.shape(.68,1.48,5.20,4.90,fill="102E52",line="2A4C73",shadow=True)
    c.text(.98,1.78,4.40,.30,"THÔNG TIN ĐƠN",size=12,color=C["cyan"],bold=True,fill="102E52")
    fields=[("MÃ ĐƠN","DHxxxxxx"),("SỐ TIỀN","TotalAmount"),("NỘI DUNG","TransferContent"),("NGÂN HÀNG","MBBank"),("TRẠNG THÁI","PENDING")]
    for i,(a,b) in enumerate(fields):
        y=2.40+i*.66
        c.text(.98,y,1.58,.28,a,size=9,color="91BFE3",bold=True,fill="102E52")
        c.text(2.74,y,2.56,.28,b,size=11,color=C["paper"],bold=True,align="r",fill="102E52")
        c.shape(.98,y+.41,4.32,.012,fill="2A4C73",line="2A4C73",geom="rect")
    add_placeholder(c,6.22,1.48,6.42,3.78,"ẢNH QR / CHUYỂN KHOẢN","/Orders/BankTransfer?id={id}",accent=C["cyan"],dark=True)
    c.shape(6.22,5.52,6.42,.86,fill="3A2B14",line=C["orange"],line_alpha=60000)
    c.text(6.48,5.70,.40,.38,"!",size=16,color=C["orange"],bold=True,align="ctr",fill="3A2B14")
    c.text(7.05,5.66,5.10,.42,"Thanh toán trước PaymentExpireAt",size=11,color="FFE4A3",bold=True,fill="3A2B14")
    s.image_note="Chèn ảnh /Orders/BankTransfer?id={id}, hiển thị QR, thông tin chuyển khoản và đồng hồ thời hạn."
    s.speaker=["Với chuyển khoản, hệ thống tạo trang hướng dẫn thanh toán và mã QR ngân hàng.","Thông tin gồm mã đơn, số tiền và nội dung chuyển khoản duy nhất.","Order lưu PaymentExpireAt để giới hạn thời gian thanh toán.","Khách có thể xác nhận đã chuyển khoản, sau đó admin kiểm tra và xử lý."]
    s.animation=["Cột thông tin đơn Wipe từ trái.","Khung QR Push từ phải trong 0,6 giây.","Cảnh báo thời hạn Fade và Pulse nhẹ."]
    return s


def slide21() -> Slide:
    s,c=new_slide(21,"Theo dõi đơn hàng","21 Order status metro line",variant=0,transition="fade")
    add_placeholder(c,.72,1.42,11.90,2.05,"ẢNH THEO DÕI ĐƠN HÀNG","/Order/Tracking/{id}",accent=C["blue"])
    statuses=[("PENDING PAYMENT","Chờ thanh toán",C["orange"]),("PENDING","Chờ xác nhận",C["blue"]),("PROCESSING","Đã xác nhận",C["purple"]),("DELIVERING","Đang giao",C["cyan"]),("COMPLETED","Hoàn thành",C["success"])]
    y=4.70
    c.line(1.18,y,12.10,y,color=C["line"],width=33000)
    for i,(code,label,color) in enumerate(statuses):
        x=1.18+i*2.73
        c.shape(x-.24,y-.24,.48,.48,fill=color,line=C["paper"],line_width=18000,geom="ellipse",shadow=True)
        c.text(x-.70,y+.44,1.40,.28,label,size=10,color=C["text"],bold=True,align="ctr",fill=C["bg"])
        c.text(x-.78,y+.88,1.56,.22,code,size=7,color=color,bold=True,align="ctr",fill=C["bg"])
    add_chip(c,4.86,6.24,1.64,"CANCELLED",color=C["danger"])
    add_chip(c,6.70,6.24,1.64,"EXPIRED",color=C["muted"])
    s.image_note="Chèn ảnh /Order/Tracking/{id} hoặc /Order/Lookup với timeline trạng thái đơn thực tế."
    s.speaker=["Theo dõi đơn sử dụng các trạng thái được khai báo trong enum OrderStatus.","Luồng chính đi từ chờ thanh toán hoặc chờ xác nhận đến xử lý, giao hàng và hoàn thành.","Cancelled và Expired là hai nhánh kết thúc ngoài luồng thành công.","Trang tracking có endpoint cập nhật trạng thái để giao diện phản ánh tiến trình hiện tại."]
    s.animation=["Ảnh tracking Fade trước.","Đường trạng thái Wipe từ trái sang phải trong 1 giây.","Cancelled và Expired xuất hiện cuối như hai nhánh phụ."]
    return s


def slide22() -> Slide:
    s,c=new_slide(22,"Build PC","22 Component board configurator",dark=True,variant=1,transition="push")
    parts=[("CPU","Cpu",C["blue"]),("MAINBOARD","Mainboard",C["purple"]),("RAM","Ram",C["cyan"]),("SSD","Storage",C["success"]),("VGA","Gpu",C["pink"]),("PSU","Psu",C["orange"]),("CASE","Case",C["muted"])]
    for i,(label,code,color) in enumerate(parts):
        col=i%4; row=i//4; x=.62+col*2.72; y=1.52+row*1.46
        c.shape(x,y,2.46,1.16,fill="102E52",line=color,shadow=True)
        c.text(x+.18,y+.18,.42,.32,"+",size=15,color=color,bold=True,align="ctr",fill="102E52")
        c.text(x+.72,y+.15,1.50,.30,label,size=12,color=C["paper"],bold=True,fill="102E52")
        c.text(x+.72,y+.55,1.50,.25,code,size=9,color="91BFE3",fill="102E52")
    c.shape(8.78,4.46,3.92,1.62,fill=C["orange"],fill2="F7B733",line=C["orange"],shadow=True)
    c.text(9.08,4.72,3.30,.25,"TỔNG GIÁ CẤU HÌNH",size=10,color="5A3800",bold=True,align="ctr",fill=C["orange"])
    c.text(9.08,5.18,3.30,.46,"— đ",size=25,color=C["dark"],bold=True,align="ctr",fill=C["orange"])
    add_placeholder(c,.62,4.46,7.78,1.62,"ẢNH BUILD PC","/BuildPc",accent=C["orange"],dark=True)
    c.text(.66,6.42,11.86,.24,"BuildCompatibilityService kiểm tra socket CPU/Mainboard và loại RAM.",size=9,color="91BFE3",bold=True,align="ctr",fill=C["dark"])
    s.image_note="Chèn ảnh /BuildPc với một cấu hình đã chọn CPU, mainboard, RAM, SSD, VGA, PSU và case."
    s.speaker=["Build PC trình bày linh kiện theo từng vị trí cấu hình.","Source hỗ trợ CPU, mainboard, RAM, lưu trữ, GPU, nguồn và case.","BuildCompatibilityService kiểm tra socket CPU với mainboard và loại RAM.","Tổng giá được cập nhật từ các sản phẩm đã chọn và cấu hình có thể đưa vào giỏ."]
    s.animation=["Các ô linh kiện xuất hiện theo hàng bằng Fade.","Placeholder Build PC Push từ trái.","Card tổng giá Zoom và Pulse nhẹ sau cùng."]
    return s


def slide23() -> Slide:
    s,c=new_slide(23,"So sánh sản phẩm","23 Editorial comparison table",variant=2,transition="wipe")
    c.shape(.62,1.46,12.06,4.82,fill=C["paper"],line=C["line"],shadow=True)
    c.text(.88,1.70,2.16,.38,"TIÊU CHÍ",size=11,color=C["muted"],bold=True,fill=C["paper"])
    c.text(3.50,1.64,3.80,.50,"SẢN PHẨM A",size=16,color=C["blue"],bold=True,align="ctr",fill=C["ice"],line=C["blue"],geom="roundRect",alpha=100000,line_alpha=40000)
    c.text(8.08,1.64,3.80,.50,"SẢN PHẨM B",size=16,color=C["purple"],bold=True,align="ctr",fill="F4F0FF",line=C["purple"],geom="roundRect",alpha=100000,line_alpha=40000)
    rows=[("GIÁ","Giá bán hiện tại"),("THÔNG SỐ","CPU · RAM · GPU"),("KHUYẾN MÃI","PromotionText"),("TỒN KHO","StockQuantity")]
    for i,(label,value) in enumerate(rows):
        y=2.48+i*.72
        bg=C["soft"] if i%2==0 else C["paper"]
        c.shape(.84,y,11.60,.60,fill=bg,line=bg,geom="rect")
        c.text(1.02,y+.13,1.80,.28,label,size=9,color=C["muted"],bold=True,fill=bg)
        c.text(3.48,y+.12,3.84,.28,value,size=10,color=C["text"],bold=True,align="ctr",fill=bg)
        c.text(8.06,y+.12,3.84,.28,value,size=10,color=C["text"],bold=True,align="ctr",fill=bg)
    add_placeholder(c,3.48,5.46,8.42,.56,"ẢNH SO SÁNH","/Compare",accent=C["purple"])
    c.text(.86,6.48,4.80,.22,"Tối đa 2 sản phẩm trong session",size=9,color=C["muted"],bold=True,fill=C["bg"])
    s.image_note="Chèn ảnh /Compare với hai sản phẩm cùng nhóm để các hàng thông số có ý nghĩa."
    s.speaker=["Module Compare lưu tối đa hai sản phẩm trong session.","Màn hình đối chiếu giá, thông số, khuyến mãi và tồn kho theo cùng hàng.","Compare ViewModel chuẩn hóa các dòng thông số để dễ đọc giữa hai sản phẩm.","Ảnh chụp nên chọn hai sản phẩm cùng loại để thể hiện giá trị của phép so sánh."]
    s.animation=["Hai tiêu đề sản phẩm Wipe từ hai phía.","Các hàng tiêu chí xuất hiện từ trên xuống.","Placeholder ảnh Fade ở cuối."]
    return s


def slide24() -> Slide:
    s,c=new_slide(24,"Chat hỗ trợ SignalR","24 Realtime chat bridge plus service cards",dark=True,variant=0,transition="fade")
    add_chip(c,10.84,1.30,1.54,"REALTIME",color=C["success"],dark=True)
    add_node(c,.72,2.02,2.62,1.32,"CUSTOMER WIDGET",color=C["cyan"],sub="_SupportChatBox",dark=True)
    add_node(c,5.34,2.02,2.62,1.32,"SIGNALR HUB",color=C["success"],sub="/hubs/support-chat",dark=True)
    add_node(c,9.96,2.02,2.62,1.32,"ADMIN CHAT",color=C["purple"],sub="Admin / Staff",dark=True)
    c.line(3.34,2.68,5.34,2.68,color=C["cyan"],width=22000,arrow=True)
    c.line(5.34,2.94,3.34,2.94,color=C["cyan"],width=14000,arrow=True)
    c.line(7.96,2.68,9.96,2.68,color=C["purple"],width=22000,arrow=True)
    c.line(9.96,2.94,7.96,2.94,color=C["purple"],width=14000,arrow=True)
    add_placeholder(c,.72,3.80,5.05,2.16,"ẢNH HỘP CHAT","Mở widget trên mọi trang",accent=C["cyan"],dark=True)
    services=[("CHAT","Hội thoại & tin nhắn",C["cyan"]),("BẢO HÀNH","Yêu cầu sau bán",C["purple"]),("LIÊN HỆ","Feedback khách hàng",C["orange"])]
    for i,(title,body,color) in enumerate(services):
        x=6.08+i*2.14
        c.shape(x,3.80,1.94,2.16,fill="102E52",line=color,shadow=True)
        add_icon(c,x+.62,4.08,"↔" if i==0 else "✓",color=color,size=.68,dark=True)
        c.text(x+.14,4.92,1.66,.26,title,size=10,color=color,bold=True,align="ctr",fill="102E52")
        c.text(x+.14,5.34,1.66,.34,body,size=8,color="B8D7F4",align="ctr",fill="102E52")
    s.image_note="Chèn ảnh widget chat phía khách và, nếu đủ chỗ, ảnh màn hình /AdminChat ở cùng hội thoại."
    s.speaker=["Chat hỗ trợ kết nối widget khách hàng với màn hình Admin Chat qua SignalR Hub.","Hệ thống lưu Conversation và Message trong cơ sở dữ liệu.","Khách vãng lai dùng access token, còn người dùng đăng nhập được liên kết bằng UserId.","Cùng với bảo hành và feedback, chat tạo thành nhóm dịch vụ sau bán hàng."]
    s.animation=["Ba node hệ thống Fade đồng thời.","Hai chiều mũi tên Wipe trong 0,5 giây.","Ảnh chat và ba card hậu mãi xuất hiện sau."]
    return s


def slide25(): return divider(25,"Đánh giá & phát triển","Nhìn lại kết quả, giới hạn và lộ trình tiếp theo","↗")


def slide26() -> Slide:
    s,c=new_slide(26,"Trang quản trị","26 Admin dashboard and management split",variant=1,transition="push")
    # dashboard screenshot large left
    c.shape(.62,1.42,8.18,5.18,fill=C["paper"],line=C["line"],shadow=True)
    c.shape(.62,1.42,1.32,5.18,fill=C["dark"],line=C["dark"])
    c.text(.80,1.72,.96,.28,"ADMIN",size=11,color=C["cyan"],bold=True,align="ctr",fill=C["dark"])
    for i,t in enumerate(["Dashboard","Products","Orders","Users","Warranty"]):
        c.text(.78,2.32+i*.52,1.00,.25,t,size=8,color="B8D7F4",bold=i==0,fill=C["dark"])
    add_placeholder(c,2.18,1.72,6.30,4.58,"ẢNH ADMIN DASHBOARD","/Admin",accent=C["orange"])
    # management cards right
    c.text(9.18,1.52,3.12,.28,"VẬN HÀNH TẬP TRUNG",size=10,color=C["orange"],bold=True,fill=C["bg"])
    modules=[("SẢN PHẨM","CRUD · ảnh",C["blue"]),("ĐƠN HÀNG","Trạng thái · CSV",C["orange"]),("NGƯỜI DÙNG","Role · active",C["purple"]),("BẢO HÀNH","Xử lý yêu cầu",C["success"]),("BANNER / SETTING","Giao diện web",C["cyan"])]
    for i,(title,body,color) in enumerate(modules):
        y=2.02+i*.86
        c.shape(9.14,y,3.36,.68,fill=C["paper"],line=C["line"],shadow=True)
        c.shape(9.14,y,.10,.68,fill=color,line=color,geom="rect")
        c.text(9.48,y+.10,1.72,.22,title,size=9,color=C["text"],bold=True,fill=C["paper"])
        c.text(10.92,y+.36,1.28,.18,body,size=7,color=C["muted"],align="r",fill=C["paper"])
    s.image_note="Chèn ảnh /Admin (AdminDashboard/Index) có các KPI hoặc bảng đơn hàng gần đây."
    s.speaker=["Khu vực quản trị tập trung hoạt động vận hành của cửa hàng.","Dashboard tổng hợp số liệu sản phẩm, đơn hàng, người dùng và doanh thu theo ViewModel.","Các controller riêng quản lý sản phẩm, danh mục, đơn, người dùng, bảo hành, banner và cài đặt.","Quyền truy cập được giới hạn cho vai trò Admin hoặc Staff tùy màn hình."]
    s.animation=["Khung dashboard Push từ trái trong 0,6 giây.","Sidebar mô phỏng xuất hiện cùng khung.","Năm card quản lý Wipe từ trên xuống."]
    return s


def slide27(facts: SourceFacts) -> Slide:
    s,c=new_slide(27,"Kết quả đạt được","27 Source-derived result stats",dark=True,variant=2,transition="fade")
    c.text(.72,1.40,7.60,.82,"Một hệ thống đủ rộng để chứng minh\nkhả năng phân tích và triển khai",size=22,color=C["paper"],bold=True,fill=C["dark"],valign="top",margin=0)
    add_stats_cards(c,[(str(facts.dbsets),"DBSET",C["cyan"]),(str(facts.controllers),"CONTROLLER",C["orange"]),(str(facts.stack_cards),"NHÓM CÔNG NGHỆ",C["purple"]),(str(facts.user_groups),"NHÓM NGƯỜI DÙNG",C["success"])],y=2.62)
    # repaint helper cards for dark bg by overlay labels not needed, white cards contrast
    c.shape(.72,5.54,11.90,.72,fill="102E52",line="2A4C73",shadow=True)
    c.text(1.02,5.72,11.30,.28,"✓ Mua hàng  ·  ✓ Vận chuyển  ·  ✓ Thanh toán  ·  ✓ Hỗ trợ  ·  ✓ Quản trị",size=12,color="D8EDF9",bold=True,align="ctr",fill="102E52")
    s.speaker=[f"Kết quả source hiện tại có {facts.dbsets} DbSet và {facts.controllers} controller.","Deck nhóm công nghệ thành tám khối đại diện cho nền tảng, dữ liệu, UI và tích hợp.","Bốn nhóm người dùng được thể hiện gồm khách vãng lai, khách hàng, admin và nhân viên hỗ trợ.","Quan trọng hơn số lượng là hệ thống đã nối được hành trình mua hàng với vận hành sau bán."]
    s.animation=["Thông điệp kết quả Fade trước.","Bốn stats card Count Up theo thứ tự trái sang phải.","Thanh kết quả nghiệp vụ Wipe sau cùng."]
    return s


def slide28() -> Slide:
    s,c=new_slide(28,"Hạn chế","28 Balanced now-next comparison",variant=0)
    c.shape(.68,1.48,5.70,4.98,fill=C["paper"],line=C["line"],shadow=True)
    c.shape(6.94,1.48,5.70,4.98,fill=C["dark"],fill2=C["navy"],line=C["navy"],shadow=True)
    c.text(.98,1.82,4.90,.36,"HIỆN TẠI",size=16,color=C["orange"],bold=True,fill=C["paper"])
    c.text(7.24,1.82,4.90,.36,"CẦN CẢI THIỆN",size=16,color=C["cyan"],bold=True,fill=C["dark"])
    current=["QR chuyển khoản cần admin xác nhận","Tích hợp ngoài phụ thuộc cấu hình","Báo cáo quản trị còn cơ bản","UI cần kiểm thử thêm thiết bị"]
    improve=["Tự động đối soát giao dịch","Tăng retry và quan sát lỗi","Mở rộng biểu đồ phân tích","Tối ưu mobile và accessibility"]
    for col,(x,items,dark,color) in enumerate([(.98,current,False,C["orange"]),(7.24,improve,True,C["cyan"])]):
        for i,item in enumerate(items):
            y=2.62+i*.78
            add_icon(c,x,y,"·" if not dark else "→",color=color,size=.34,dark=dark)
            c.text(x+.52,y-.05,4.44,.46,item,size=11,color="C2D5E8" if dark else C["text"],bold=True,fill=C["dark"] if dark else C["paper"])
    c.text(.82,6.70,11.68,.20,"Hạn chế được trình bày như cơ hội nâng chất lượng, không phủ nhận kết quả hiện tại.",size=9,color=C["muted"],align="ctr",fill=C["bg"])
    s.speaker=["Các hạn chế được nhìn nhận ở mức phù hợp với phạm vi đồ án.","Luồng QR hiện là chuyển khoản và xác nhận, chưa phải đối soát gateway tự động.","Các tích hợp ngoài như GHN, SMTP và định tuyến phụ thuộc khóa cấu hình và môi trường.","Báo cáo, mobile UX và khả năng quan sát lỗi là những điểm có thể tiếp tục hoàn thiện."]
    s.animation=["Cột hiện tại Wipe từ trái.","Cột cần cải thiện Wipe từ phải sau 0,2 giây.","Các cặp nội dung xuất hiện theo từng hàng."]
    return s


def slide29() -> Slide:
    s,c=new_slide(29,"Hướng phát triển","29 Curved roadmap",dark=True,variant=1,transition="fade")
    # curved-looking polyline path
    pts=[(.92,5.42),(3.02,3.18),(5.48,4.78),(7.78,2.42),(10.14,4.18),(12.20,1.78)]
    for a,b in zip(pts,pts[1:]):c.line(a[0],a[1],b[0],b[1],color=C["orange"],width=26000,alpha=70000,arrow=True)
    milestones=[(1.34,4.50,"01","PAYMENT\nGATEWAY",C["orange"]),(3.62,2.25,"02","AI BUILD PC",C["cyan"]),(5.92,4.02,"03","BÁO CÁO\nNÂNG CAO",C["purple"]),(8.26,1.55,"04","SEO",C["success"]),(10.46,3.36,"05","MOBILE UX",C["pink"])]
    for x,y,num,label,color in milestones:
        c.shape(x,y,.68,.68,fill=color,line=C["paper"],line_width=16000,geom="ellipse",shadow=True)
        c.text(x,y-.01,.68,.68,num,size=11,color=C["paper"],bold=True,align="ctr",fill=color)
        c.text(x-.48,y+.84,1.64,.60,label,size=10,color=C["paper"],bold=True,align="ctr",fill=C["dark"],margin=0)
    c.text(.72,1.40,4.90,.54,"ROADMAP 01 → 05",size=22,color=C["paper"],bold=True,fill=C["dark"])
    c.text(.72,2.02,4.55,.44,"Từ giao dịch tự động đến trải nghiệm đa thiết bị",size=12,color="B8D7F4",fill=C["dark"])
    s.speaker=["Lộ trình phát triển bắt đầu bằng payment gateway và đối soát tự động.","AI Build PC có thể tư vấn cấu hình theo ngân sách và nhu cầu.","Báo cáo nâng cao giúp admin hiểu doanh thu, tồn kho và hành vi mua.","SEO và mobile UX hoàn thiện khả năng tiếp cận và trải nghiệm đa thiết bị."]
    s.animation=["Đường roadmap Wipe theo hướng 01 đến 05 trong 1,2 giây.","Mỗi mốc Zoom khi đường đi tới vị trí tương ứng.","Tên roadmap Fade ngay từ đầu."]
    return s


def slide30() -> Slide:
    s,c=new_slide(30,"Xin chân thành cảm ơn","30 Thank-you full-screen",dark=True,header=False,transition="fade")
    accent=SECTIONS[5][2]
    # layered circles and central glass panel
    for x,y,d,a,color in [(9.80,-.70,4.30,15000,accent),(-1.20,4.80,4.00,10000,C["cyan"]),(10.90,5.35,2.70,9000,C["orange"]),(.90,.72,1.30,10000,C["purple"])]:
        c.shape(x,y,d,d,fill=color,alpha=a,line=color,line_alpha=0,geom="ellipse")
    c.shape(1.12,1.28,11.08,4.78,fill=C["paper"],alpha=8500,line=C["paper"],line_alpha=22000,shadow=True)
    c.text(2.00,2.02,9.34,.28,"DATN PC STORE · GRADUATION THESIS",size=11,color="A9CBE6",bold=True,align="ctr",fill=C["dark"])
    c.text(1.82,2.64,9.70,1.05,"XIN CHÂN THÀNH CẢM ƠN",size=37,color=C["paper"],bold=True,align="ctr",fill=C["dark"],margin=0)
    c.shape(5.54,3.92,2.24,.055,fill=accent,line=accent,geom="rect")
    c.text(2.10,4.34,9.12,.72,"Em xin chân thành cảm ơn thầy/cô và hội đồng\nđã lắng nghe.",size=18,color="C7DBF2",align="ctr",fill=C["dark"],margin=0)
    c.text(4.64,5.52,4.02,.34,"Q & A",size=15,color=accent,bold=True,align="ctr",fill=C["dark"],line=accent,geom="roundRect",alpha=12000,line_alpha=50000)
    s.speaker=["Em xin chân thành cảm ơn thầy cô và hội đồng đã lắng nghe phần trình bày.","Đề tài đã hoàn thành các luồng chính của một website PC Store trong phạm vi đồ án.","Em mong nhận được nhận xét để tiếp tục cải thiện cả kỹ thuật và trải nghiệm người dùng.","Em xin sẵn sàng trả lời các câu hỏi của hội đồng."]
    s.animation=["Khung kính Fade trong 0,5 giây.","Tiêu đề Zoom rất nhẹ trong 0,6 giây.","Dòng Q&A Fade sau cùng; giữ slide tĩnh khi trả lời."]
    s.duration="20–30 giây trước phần hỏi đáp"
    return s


def build_slides(facts: SourceFacts) -> list[Slide]:
    slides=[slide01(),slide02(),slide03(),slide04(),slide05(),slide06(),slide07(),slide08(),slide09(),slide10(),
            slide11(),slide12(),slide13(),slide14(),slide15(),slide16(),slide17(),slide18(),slide19(),slide20(),
            slide21(),slide22(),slide23(),slide24(),slide25(),slide26(),slide27(facts),slide28(),slide29(),slide30()]
    return slides


# ---------- OOXML renderer ----------
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


def write_notes(slides: list[Slide]) -> None:
    lines=["# Ghi chú thuyết trình — DATN PC Store","",
           "> Nội dung được đối chiếu trực tiếp với source code. Các gợi ý animation dùng hiệu ứng PowerPoint an toàn, ngắn và tiết chế.",""]
    for s in slides:
        lines += [f"## Slide {s.number:02d} — {s.title}","","**Lời thuyết trình:**"]
        lines += [f"{i+1}. {sentence}" for i,sentence in enumerate(s.speaker)]
        lines += ["","**Animation suggestion:**"]
        lines += [f"- {item}" for item in s.animation]
        lines += ["",f"**Thời lượng gợi ý:** {s.duration}","",f"**Ghi chú ảnh:** {s.image_note}",""]
    NOTES_PATH.write_text("\n".join(lines),encoding="utf-8")


def validate(slides: list[Slide]) -> None:
    if len(slides)!=TOTAL:raise RuntimeError(f"Expected {TOTAL} slides, got {len(slides)}")
    if len({s.layout for s in slides})!=TOTAL:raise RuntimeError("All 30 slides must have distinct layout identities")
    if any(len(s.speaker)<4 or len(s.speaker)>6 for s in slides):raise RuntimeError("Each slide needs 4–6 speaker sentences")
    if any(not s.animation for s in slides):raise RuntimeError("Each slide needs animation suggestions")
    with zipfile.ZipFile(PPTX_PATH) as z:
        bad=z.testzip()
        if bad:raise RuntimeError(f"Corrupt ZIP member: {bad}")
        names=z.namelist()
        slide_names=[n for n in names if re.fullmatch(r"ppt/slides/slide\d+\.xml",n)]
        if len(slide_names)!=TOTAL:raise RuntimeError(f"PPTX contains {len(slide_names)} slides")
        all_xml="\n".join(z.read(n).decode("utf-8") for n in slide_names)
        for token in ["Đặt vấn đề","Build PC","SIGNALR HUB","XIN CHÂN THÀNH CẢM ƠN","PaymentExpireAt"]:
            if escape(token) not in all_xml:raise RuntimeError(f"Missing required content: {token}")
        if sum("<p:transition" in z.read(n).decode("utf-8") for n in slide_names)<12:
            raise RuntimeError("Expected transitions on section/showcase/process slides")
    if PPTX_PATH.stat().st_size<70000:raise RuntimeError("Generated PPTX is unexpectedly small")
    if not NOTES_PATH.exists() or NOTES_PATH.stat().st_size<10000:raise RuntimeError("Presentation notes are incomplete")


def main() -> int:
    facts=audit_source()
    slides=build_slides(facts)
    write_notes(slides)
    render(slides)
    validate(slides)
    image_slides=[s.number for s in slides if s.image_note!="Không cần chèn ảnh."]
    print(f"Created {len(slides)} slides with {len({s.layout for s in slides})} distinct layout identities.")
    print(f"Source facts: {facts.dbsets} DbSet, {facts.controllers} controllers, {facts.services} service files.")
    print("Sections: 01 Blue, 02 Purple, 03 Cyan, 04 Orange, 05 Pink.")
    print("Progress footer: slides 02–30.")
    print("Image placeholders: " + ", ".join(f"{n:02d}" for n in image_slides))
    print(f"PowerPoint: {PPTX_PATH}")
    print(f"Notes: {NOTES_PATH}")
    return 0


if __name__=="__main__":
    sys.exit(main())
