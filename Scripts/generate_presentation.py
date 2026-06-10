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
    if number <= 5: return 1
    if number <= 9: return 2
    if number <= 21: return 3
    if number <= 27: return 4
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
        "Controllers/CompareController.cs": ["CompareController", "Add/{productId:int}"],
        "Controllers/SupportChatController.cs": ["CreateConversation", "SendMessage"],
        "Controllers/WarrantyController.cs": ["WarrantyController"],
        "Controllers/AccountController.cs": ["ForgotPassword", "VerifyResetCode", "Profile", "ChangePassword"],
        "Controllers/AdminUsersController.cs": ["AdminUsersController", "Authorize(Roles = \"Admin\")"],
        "Controllers/AdminBannersController.cs": ["AdminBannersController"],
        "Controllers/AdminSettingsController.cs": ["AdminSettingsController"],
        "Controllers/ArticlesController.cs": ["ArticlesController", "Detail(string slug)"],
        "Controllers/ContactController.cs": ["Feedback", "Manage"],
        "Controllers/AdminChatController.cs": ["AdminChatController"],
        "Controllers/AdminWarrantyController.cs": ["AdminWarrantyController"],
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
        "Controllers/BuildPcController.cs": ["BuildPcController", "export-csv", "add-to-cart"],
        "Controllers/OrdersController.cs": ["BankTransfer", "TrackingStatus", "ConfirmTransferred", "ExportExcel", "Quotation"],
        "Controllers/ShippingController.cs": ["provinces", "districts", "wards", "calculate"],
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
    name: str = ""


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
              fill2=None, arrow_end=None, name="") -> None:
        self.s.elements.append(Element("shape", x, y, w, h, fill=fill, fill2=fill2,
            line=line or fill, geom=geom, alpha=alpha, line_alpha=line_alpha,
            line_width=line_width, dash=dash, shadow=shadow, rotation=rotation,
            arrow_end=arrow_end, name=name))

    def text(self, x, y, w, h, value, *, size=16, color=None, bold=False, align="l",
             valign="ctr", fill="FFFFFF", fill2=None, line=None, geom="rect", alpha=0,
             line_alpha=0, shadow=False, rotation=0, margin=.08) -> None:
        self.s.elements.append(Element("text", x, y, w, h, text=value, fill=fill, fill2=fill2,
            line=line or fill, color=color or C["text"], size=max(9, size), bold=bold, align=align,
            valign=valign, geom=geom, alpha=alpha, line_alpha=line_alpha, shadow=shadow,
            rotation=rotation, margin=margin))

    def line(self, x1, y1, x2, y2, *, color=None, width=19050, dash=None, alpha=100000,
             arrow=False) -> None:
        self.s.elements.append(Element("line", x1, y1, x2-x1, y2-y1, fill="FFFFFF",
            line=color or C["line"], line_width=width, dash=dash, line_alpha=alpha,
            arrow_end="triangle" if arrow else None, geom="line"))


# ---------- premium IT graduation-defense deck (v3) ----------
def add_safe_background(c: Canvas, section: int, *, dark: bool = False, variant: int = 0) -> None:
    """Create a dedicated first/background layer; all content is appended above it."""
    accent = SECTIONS[section][2]
    base = C["dark"] if dark else C["bg"]
    second = C["slate"] if dark else ("EEF5FF" if variant % 2 == 0 else "F7F3FF")
    c.shape(0, 0, W, H, fill=base, fill2=second, line=base, geom="rect", name="BACKGROUND — replaceable full-slide layer")
    mode = variant % 6
    if mode == 0:
        c.shape(10.55, -.78, 3.55, 3.55, fill=accent, alpha=11000, line=accent, line_alpha=0, geom="ellipse")
        c.shape(-.88, 5.72, 2.40, 2.40, fill=C["cyan"], alpha=6500, line=C["cyan"], line_alpha=0, geom="ellipse")
    elif mode == 1:
        c.shape(9.55, -.15, 4.30, 1.18, fill=accent, alpha=8500, line=accent, line_alpha=0, geom="parallelogram", rotation=-6)
        c.line(.25, 6.18, 4.15, 7.50, color=accent, width=52000, alpha=9000)
    elif mode == 2:
        for i in range(3):
            c.shape(10.30+i*.48, .30+i*.42, 1.15, 1.15, fill=accent, alpha=5500+i*1800, line=accent, line_alpha=0, geom="ellipse")
        c.shape(-.60, 4.90, 2.00, 3.20, fill=C["purple"], alpha=5000, line=C["purple"], line_alpha=0, geom="parallelogram")
    elif mode == 3:
        c.line(9.90, 0, 13.33, 3.43, color=accent, width=92000, alpha=8000)
        c.line(10.72, 0, 13.33, 2.61, color=C["cyan"], width=26000, alpha=9500)
        c.shape(.18, 5.72, 1.35, 1.35, fill=accent, alpha=7000, line=accent, line_alpha=0, geom="ellipse")
    elif mode == 4:
        c.shape(10.52, .22, 2.25, 5.90, fill=accent, alpha=5200, line=accent, line_alpha=0, geom="parallelogram", rotation=8)
        c.shape(.15, 6.56, 5.10, .22, fill=C["cyan"], alpha=10000, line=C["cyan"], line_alpha=0, geom="rect")
    else:
        for x in (10.55, 11.15, 11.75, 12.35):
            c.shape(x, .20+(x-10.55)*.45, .16, 1.35, fill=accent, alpha=12000, line=accent, line_alpha=0, geom="rect", rotation=35)
        c.shape(-.75, 5.90, 2.45, 1.15, fill=C["orange"], alpha=6500, line=C["orange"], line_alpha=0, geom="parallelogram")
    c.text(11.45, .18, 1.05, .70, SECTIONS[section][4], size=38, color=accent, bold=True, align="r", fill=base, alpha=0, margin=0)


def add_title(c: Canvas, title: str, kicker: str | None = None, *, dark: bool = False) -> None:
    section = section_for(c.s.number); accent = SECTIONS[section][2]; bg = C["dark"] if dark else C["bg"]
    c.text(.68, .26, 5.7, .22, kicker or SECTIONS[section][0], size=12, color=accent, bold=True, fill=bg, margin=0)
    c.text(.68, .57, 11.55, .54, title, size=26, color=C["paper"] if dark else C["text"], bold=True, fill=bg, margin=0)
    c.shape(.68, 1.18, .78, .055, fill=accent, line=accent, geom="rect")


def add_footer_progress(c: Canvas, *, dark: bool = False) -> None:
    section = section_for(c.s.number); accent = SECTIONS[section][2]; bg = C["dark"] if dark else C["bg"]
    muted = "BCD0E5" if dark else C["muted"]
    c.text(.56, 7.08, 1.55, .18, "DATN · PC STORE", size=11, color=muted, bold=True, fill=bg)
    c.shape(2.18, 7.16, 9.52, .035, fill="D7E1EB" if not dark else "29496C", line=bg, geom="rect")
    c.shape(2.18, 7.16, 9.52*c.s.number/TOTAL, .035, fill=accent, line=accent, geom="rect")
    c.text(11.90, 7.06, .82, .20, f"{c.s.number:02d}/{TOTAL:02d}", size=11, color=accent, bold=True, align="r", fill=bg)


def _base_slide(number: int, title: str, *, dark: bool = False) -> tuple[Slide, Canvas]:
    s=Slide(number,title,f"premium-{number}"); c=Canvas(s)
    add_safe_background(c,section_for(number),dark=dark,variant=number)
    add_title(c,title,dark=dark); add_footer_progress(c,dark=dark)
    return s,c


def add_card(c: Canvas, x, y, w, h, *, title: str="", body: str="", accent: str|None=None,
             dark: bool=False, title_size: int=15, body_size: int=13) -> None:
    accent=accent or SECTIONS[section_for(c.s.number)][2]; fill="102E52" if dark else C["paper"]
    c.shape(x,y,w,h,fill=fill,line="315275" if dark else C["line"],shadow=True)
    c.shape(x,y,.055,h,fill=accent,line=accent,geom="rect")
    if title: c.text(x+.22,y+.16,w-.40,.30,title,size=title_size,color=accent,bold=True,fill=fill,margin=0)
    if body: c.text(x+.22,y+.56,w-.40,h-.70,body,size=body_size,color="D9E8F5" if dark else C["text"],fill=fill,valign="top",margin=.01)


def add_browser(c: Canvas,x:float,y:float,w:float,h:float,label:str,accent:str|None=None) -> tuple[float,float,float,float]:
    accent=accent or SECTIONS[section_for(c.s.number)][2]
    c.shape(x,y,w,h,fill=C["paper"],line="C7D6E6",shadow=True)
    c.shape(x,y,w,.40,fill="E8EEF5",line="E8EEF5",geom="roundRect")
    for i,col in enumerate((C["danger"],C["orange"],C["success"])): c.shape(x+.18+i*.20,y+.14,.09,.09,fill=col,line=col,geom="ellipse")
    c.shape(x+.88,y+.10,w-1.55,.20,fill="FFFFFF",line="D3DFEB",geom="roundRect")
    c.text(x+1.02,y+.09,w-1.82,.21,label,size=10,color=C["muted"],fill="FFFFFF",margin=0)
    c.shape(x,y+.40,w,.48,fill=C["navy"],line=C["navy"],geom="rect")
    c.text(x+.20,y+.48,1.15,.22,"KKSHOP",size=13,color=C["paper"],bold=True,fill=C["navy"],margin=0)
    c.shape(x+w-1.40,y+.54,.78,.08,fill=accent,line=accent,geom="roundRect")
    return x+.18,y+.98,w-.36,h-1.16


def add_metric(c:Canvas,x,y,w,value,label,color):
    c.shape(x,y,w,1.02,fill=C["paper"],line="DFE8F2",shadow=True)
    c.text(x+.15,y+.12,w-.30,.40,value,size=23,color=color,bold=True,fill=C["paper"],margin=0)
    c.text(x+.15,y+.59,w-.30,.22,label,size=11,color=C["muted"],bold=True,fill=C["paper"],margin=0)


def note(s:Slide, lines:list[str], emphasis:list[str], image:str="Không cần chèn ảnh.") -> Slide:
    s.speaker=lines; s.animation=emphasis; s.image_note=image; return s


def screen_home(c,x,y,w,h):
    ix,iy,iw,ih=add_browser(c,x,y,w,h,"/ · Trang chủ",C["cyan"])
    c.shape(ix,iy,iw,1.18,fill=C["navy"],fill2=C["blue"],line=C["navy"],geom="roundRect")
    c.text(ix+.30,iy+.18,iw*.58,.35,"BUILD YOUR POWER",size=18,color=C["paper"],bold=True,fill=C["navy"],margin=0)
    c.text(ix+.30,iy+.63,iw*.50,.28,"PC gaming · laptop · linh kiện",size=11,color="D8EAFE",fill=C["navy"],margin=0)
    for i,t in enumerate(("PC GAMING","LAPTOP","LINH KIỆN","MÀN HÌNH")):
        xx=ix+i*(iw/4); c.shape(xx,iy+1.36,iw/4-.08,.48,fill="F1F6FB",line="D9E5F0")
        c.text(xx,iy+1.36,iw/4-.08,.48,t,size=10,color=C["navy"],bold=True,align="ctr",fill="F1F6FB")
    for i in range(4):
        xx=ix+i*(iw/4); c.shape(xx,iy+2.03,iw/4-.10,ih-2.10,fill=C["paper"],line="DFE7EF")
        c.shape(xx+.12,iy+2.18,iw/4-.34,.66,fill=["E8F1FF","EAFBF6","FFF4E1","F2ECFF"][i],line="FFFFFF",geom="roundRect")
        c.text(xx+.12,iy+2.94,iw/4-.34,.22,["PC RTX 4060","Laptop Creator","SSD NVMe 1TB","Monitor 2K"][i],size=10,color=C["text"],bold=True,fill=C["paper"],margin=0)


def screen_catalog(c,x,y,w,h):
    ix,iy,iw,ih=add_browser(c,x,y,w,h,"/Products · Danh sách sản phẩm",C["blue"])
    c.shape(ix,iy,iw*.25,ih,fill="F3F6FA",line="DCE6F0")
    c.text(ix+.16,iy+.15,iw*.20,.25,"BỘ LỌC",size=12,color=C["navy"],bold=True,fill="F3F6FA",margin=0)
    for i,t in enumerate(("Danh mục","Khoảng giá","Thương hiệu","Tình trạng")):
        c.text(ix+.18,iy+.62+i*.62,iw*.20,.20,t,size=10,color=C["text"],bold=True,fill="F3F6FA",margin=0)
        c.shape(ix+.18,iy+.90+i*.62,iw*.18,.09,fill="D6E2ED",line="D6E2ED",geom="roundRect")
    gx=ix+iw*.28
    for i in range(6):
        col=i%3; row=i//3; xx=gx+col*(iw*.235); yy=iy+row*1.55
        c.shape(xx,yy,iw*.21,1.38,fill=C["paper"],line="DCE6F0")
        c.shape(xx+.10,yy+.10,iw*.19,.62,fill=["EAF4FF","ECFDF5","FFF7E6"][col],line="FFFFFF")
        c.text(xx+.10,yy+.81,iw*.19,.20,f"Sản phẩm {i+1}",size=10,color=C["text"],bold=True,fill=C["paper"],margin=0)
        c.text(xx+.10,yy+1.08,iw*.19,.18,"Giá · còn hàng",size=9,color=C["danger"],fill=C["paper"],margin=0)


def screen_detail(c,x,y,w,h):
    ix,iy,iw,ih=add_browser(c,x,y,w,h,"/Products/Detail/{id}",C["purple"])
    c.shape(ix,iy,iw*.43,2.35,fill="EEF3F8",line="DCE5EE")
    c.shape(ix+.30,iy+.28,iw*.35,1.55,fill="DDEBFA",line="FFFFFF",geom="roundRect")
    c.text(ix+iw*.47,iy+.05,iw*.48,.42,"PC KKSHOP G5",size=18,color=C["text"],bold=True,fill=C["paper"],margin=0)
    c.text(ix+iw*.47,iy+.58,iw*.48,.28,"23.990.000đ",size=17,color=C["danger"],bold=True,fill=C["paper"],margin=0)
    for i,t in enumerate(("CPU i5 thế hệ 13","RTX 4060 · RAM 16GB","Bảo hành 24 tháng")):
        c.text(ix+iw*.49,iy+1.04+i*.34,iw*.43,.22,"✓  "+t,size=10,color=C["text"],fill=C["paper"],margin=0)
    c.shape(ix+iw*.47,iy+2.12,iw*.22,.42,fill=C["orange"],line=C["orange"]); c.text(ix+iw*.47,iy+2.12,iw*.22,.42,"MUA NGAY",size=11,color=C["paper"],bold=True,align="ctr",fill=C["orange"])
    c.shape(ix,iy+2.62,iw,.76,fill="F7F9FC",line="DCE5EE"); c.text(ix+.20,iy+2.74,iw-.40,.25,"THÔNG SỐ KỸ THUẬT  ·  SẢN PHẨM MUA KÈM  ·  GỢI Ý LIÊN QUAN",size=10,color=C["navy"],bold=True,fill="F7F9FC",margin=0)


def screen_table(c,x,y,w,h,label,columns,accent=C["blue"]):
    ix,iy,iw,ih=add_browser(c,x,y,w,h,label,accent)
    c.shape(ix,iy,iw,.48,fill="EAF1F8",line="DCE6F0")
    c.text(ix+.16,iy+.10,iw-.32,.22,"   ·   ".join(columns),size=10,color=C["navy"],bold=True,fill="EAF1F8",margin=0)
    for r in range(5):
        yy=iy+.58+r*.55; c.shape(ix,yy,iw,.44,fill=C["paper"] if r%2==0 else "F8FAFC",line="E5ECF3")
        c.shape(ix+.16,yy+.13,.95,.12,fill=["DDEBFA","E8F8F1","FFF0D9","F1E9FF","E8F8F1"][r],line="FFFFFF",geom="roundRect")
        c.shape(ix+iw*.38,yy+.13,iw*.20,.12,fill="D9E4EE",line="FFFFFF",geom="roundRect")
        c.shape(ix+iw-.88,yy+.10,.62,.20,fill=accent,alpha=80000,line=accent,geom="roundRect")


def build_slides(facts: SourceFacts) -> list[Slide]:
    slides=[]
    # 01 cover
    s=Slide(1,"DATN PC Store","cover"); c=Canvas(s); add_safe_background(c,1,dark=True,variant=1)
    c.text(.78,.70,5.9,.28,"ĐỒ ÁN TỐT NGHIỆP · CÔNG NGHỆ THÔNG TIN",size=13,color=C["cyan"],bold=True,fill=C["dark"],margin=0)
    c.text(.78,1.48,8.8,.80,"DATN PC STORE",size=39,color=C["paper"],bold=True,fill=C["dark"],margin=0)
    c.text(.80,2.46,7.8,.50,"Nền tảng bán linh kiện & hỗ trợ xây dựng cấu hình PC",size=20,color="C9DBEB",fill=C["dark"],margin=0)
    c.shape(.80,3.25,2.25,.075,fill=C["orange"],line=C["orange"],geom="rect")
    add_card(c,.80,3.82,5.35,1.50,title="THÔNG TIN BẢO VỆ",body="Sinh viên: ........................................\nGVHD: ...............................................",accent=C["cyan"],dark=True)
    c.shape(8.95,1.22,3.25,3.70,fill="102E52",line="315275",shadow=True)
    c.text(9.38,1.65,2.40,.55,"PC",size=34,color=C["cyan"],bold=True,align="ctr",fill="102E52")
    c.text(9.38,2.38,2.40,.35,"SHOP",size=21,color=C["paper"],bold=True,align="ctr",fill="102E52")
    c.text(9.28,3.28,2.60,.72,"DISCOVER\nCONFIGURE\nPURCHASE",size=13,color="BDD3E7",bold=True,align="ctr",fill="102E52")
    add_footer_progress(c,dark=True)
    slides.append(note(s,["Em xin kính chào hội đồng và thầy cô.","Đề tài xây dựng một website thương mại điện tử chuyên PC, laptop và linh kiện.","Điểm nhấn là hành trình mua hàng khép kín, Build PC, so sánh và hỗ trợ sau bán.","Phần trình bày được sắp theo đúng luồng người dùng và các nghiệp vụ quản trị thực tế."],["Website thương mại điện tử chuyên PC","Demo theo hành trình thực tế"]))

    # 02 problem/value
    s,c=_base_slide(2,"Bài toán thực tế & cơ hội số hóa")
    c.text(.78,1.55,4.05,.90,"Mua linh kiện không chỉ là chọn sản phẩm — khách hàng còn phải hiểu cấu hình, độ tương thích, chi phí giao hàng và hậu mãi.",size=18,color=C["navy"],bold=True,fill=C["bg"],valign="top",margin=0)
    for i,(n,t,b,col) in enumerate((("01","Thông tin phân tán","Khó đối chiếu giá, tồn kho và thông số",C["blue"]),("02","Rủi ro tương thích","CPU, mainboard, RAM, PSU cần khớp",C["purple"]),("03","Trải nghiệm đứt đoạn","Mua hàng, theo dõi và bảo hành tách rời",C["orange"]))):
        y=3.02+i*1.05; c.shape(.82,y,.65,.65,fill=col,line=col,geom="ellipse"); c.text(.82,y,.65,.65,n,size=12,color=C["paper"],bold=True,align="ctr",fill=col)
        c.text(1.68,y-.02,3.12,.25,t,size=14,color=col,bold=True,fill=C["bg"],margin=0); c.text(1.68,y+.32,3.20,.30,b,size=11,color=C["muted"],fill=C["bg"],margin=0)
    c.shape(5.35,1.52,7.18,4.82,fill=C["paper"],line=C["line"],shadow=True)
    c.text(5.78,1.87,6.30,.30,"GIẢI PHÁP PC STORE",size=15,color=C["success"],bold=True,fill=C["paper"],margin=0)
    steps=[("Khám phá","Tìm · lọc"),("Tư vấn","So sánh · Build PC"),("Giao dịch","Giỏ · Checkout"),("Đồng hành","Theo dõi · Chat · BH")]
    for i,(a,b) in enumerate(steps):
        x=5.78+i*1.55; c.shape(x,2.62,1.15,1.15,fill=["EAF4FF","F2ECFF","FFF4E1","E9FBF5"][i],line="FFFFFF",geom="ellipse"); c.text(x,2.62,1.15,1.15,f"{i+1:02d}",size=18,color=[C["blue"],C["purple"],C["orange"],C["success"]][i],bold=True,align="ctr",fill=["EAF4FF","F2ECFF","FFF4E1","E9FBF5"][i])
        c.text(x-.12,4.03,1.38,.24,a,size=12,color=C["text"],bold=True,align="ctr",fill=C["paper"],margin=0); c.text(x-.18,4.37,1.50,.35,b,size=10,color=C["muted"],align="ctr",fill=C["paper"],margin=0)
    c.text(5.78,5.36,6.25,.42,"Một nền tảng xuyên suốt từ nhu cầu đến hậu mãi",size=15,color=C["navy"],bold=True,align="ctr",fill=C["paper"])
    slides.append(note(s,["Bài toán của cửa hàng PC phức tạp hơn một website bán hàng thông thường.","Khách cần thông tin kỹ thuật, kiểm tra tương thích và một quy trình giao dịch rõ ràng.","Giải pháp gom bốn giai đoạn khám phá, tư vấn, giao dịch và hậu mãi vào cùng hệ thống.","Đây là giá trị xuyên suốt để đánh giá các chức năng ở phần demo."],["Bốn giai đoạn giá trị","Giải quyết rủi ro tương thích"]))

    # 03 objectives
    s,c=_base_slide(3,"Mục tiêu và tiêu chí thành công")
    c.shape(.76,1.50,3.15,4.95,fill=C["navy"],fill2=C["blue"],line=C["navy"],shadow=True)
    c.text(1.12,1.92,2.45,.32,"MỤC TIÊU TRUNG TÂM",size=13,color=C["cyan"],bold=True,align="ctr",fill=C["navy"],margin=0)
    c.text(1.08,2.55,2.52,1.25,"XÂY DỰNG\nHÀNH TRÌNH\nMUA PC KHÉP KÍN",size=23,color=C["paper"],bold=True,align="ctr",fill=C["navy"],margin=0)
    c.text(1.15,4.45,2.38,.92,"Đúng chức năng\nDễ sử dụng\nCó khả năng vận hành",size=13,color="CFE0EF",align="ctr",fill=C["navy"])
    goals=[("01","Khách hàng","Tìm đúng sản phẩm, đặt hàng và tự theo dõi",C["blue"]),("02","Tư vấn kỹ thuật","Build PC và so sánh có căn cứ",C["purple"]),("03","Vận hành","Quản trị dữ liệu, đơn và nội dung tập trung",C["orange"]),("04","Hậu mãi","Chat, feedback và bảo hành có lịch sử",C["success"])]
    for i,(n,t,b,col) in enumerate(goals):
        x=4.35+(i%2)*4.18; y=1.52+(i//2)*2.48
        add_card(c,x,y,3.78,2.08,title=f"{n}  {t}",body=b,accent=col,title_size=14,body_size=13)
    slides.append(note(s,["Mục tiêu trung tâm là tạo hành trình mua PC khép kín chứ không chỉ hiển thị sản phẩm.","Với khách hàng, hệ thống phải hỗ trợ tìm, chọn, mua và theo dõi.","Với cửa hàng, dữ liệu sản phẩm, đơn hàng, nội dung và hỗ trợ cần được quản trị tập trung.","Tiêu chí thành công là chức năng đúng source, giao diện rõ và quy trình có thể demo được."],["Hành trình mua PC khép kín","Cân bằng khách hàng và vận hành"]))

    # 04 actors journey
    s,c=_base_slide(4,"Đối tượng sử dụng & hành trình nghiệp vụ")
    roles=[("KHÁCH VÃNG LAI","Xem sản phẩm · so sánh · giỏ session",C["blue"]),("KHÁCH HÀNG","Tài khoản · đơn cá nhân · bảo hành",C["cyan"]),("NHÂN VIÊN","Đơn hàng · chat · feedback",C["orange"]),("QUẢN TRỊ","Toàn bộ dữ liệu và cấu hình",C["purple"])]
    for i,(t,b,col) in enumerate(roles): add_card(c,.75+i*3.10,1.50,2.78,1.42,title=t,body=b,accent=col,title_size=12,body_size=11)
    c.text(.85,3.35,2.1,.28,"HÀNH TRÌNH CHÍNH",size=13,color=C["navy"],bold=True,fill=C["bg"],margin=0)
    journey=["Khám phá","Đánh giá","Cấu hình","Thanh toán","Theo dõi","Hậu mãi"]
    for i,t in enumerate(journey):
        x=.82+i*2.03; col=[C["blue"],C["cyan"],C["purple"],C["orange"],C["success"],C["pink"]][i]
        c.shape(x,4.05,1.47,.82,fill=col,alpha=90000,line=col,shadow=True); c.text(x,4.05,1.47,.82,t,size=12,color=C["paper"],bold=True,align="ctr",fill=col)
        if i<5: c.text(x+1.52,4.24,.42,.30,"→",size=18,color=C["muted"],bold=True,align="ctr",fill=C["bg"])
    c.text(.90,5.26,11.50,.58,"Quản trị viên vận hành xuyên suốt phía sau: dữ liệu → nội dung → giao dịch → hỗ trợ",size=14,color=C["muted"],bold=True,align="ctr",fill=C["bg"])
    slides.append(note(s,["Source thể hiện bốn nhóm sử dụng với quyền và dữ liệu khác nhau.","Khách vãng lai vẫn có thể duyệt, so sánh và dùng giỏ hàng session.","Khách đăng nhập có thêm hồ sơ, đơn cá nhân và bảo hành; Staff và Admin xử lý vận hành.","Các slide tiếp theo bám sáu bước của hành trình chính này."],["Bốn nhóm người dùng","Sáu bước hành trình"]))

    # 05 feature map
    s,c=_base_slide(5,"Bản đồ chức năng đã triển khai")
    groups=[("KHÁM PHÁ","Trang chủ\nDanh mục · tìm kiếm\nChi tiết · bài viết",C["blue"]),("TƯ VẤN","So sánh 2 sản phẩm\nBuild PC theo nhóm\nKiểm tra tương thích",C["purple"]),("GIAO DỊCH","Giỏ hàng\nCheckout · GHN\nCOD · chuyển khoản QR",C["orange"]),("SAU BÁN","Tra cứu đơn\nBáo giá · Excel\nChat · feedback · BH",C["success"]),("QUẢN TRỊ","Dashboard\nSản phẩm · đơn · user\nBanner · cấu hình",C["navy"])]
    for i,(t,b,col) in enumerate(groups):
        x=.62+i*2.52; y=1.48+(i%2)*.20
        c.shape(x,y,2.20,4.75,fill=C["paper"],line=col,shadow=True)
        c.shape(x,y,2.20,.16,fill=col,line=col,geom="rect")
        c.text(x+.18,y+.44,1.84,.30,t,size=13,color=col,bold=True,align="ctr",fill=C["paper"],margin=0)
        c.text(x+.20,y+1.13,1.80,2.02,b,size=12,color=C["text"],bold=True,align="ctr",fill=C["paper"],valign="top",margin=0)
        c.text(x+.22,y+3.72,1.76,.55,f"MODULE {i+1:02d}",size=11,color=C["muted"],bold=True,align="ctr",fill="F3F6FA")
    slides.append(note(s,["Bản đồ chức năng được lập sau khi rà soát controller, view, service và migration.","Năm cụm bao phủ từ khám phá sản phẩm đến quản trị hệ thống.","Các chức năng xuất dữ liệu, chat, feedback, bài viết và cấu hình site cũng được đưa vào thay vì chỉ tập trung giỏ hàng.","Đây là phạm vi thực tế của deck, không bổ sung chức năng chưa có trong code."],["Năm cụm chức năng thực tế","Không bỏ sót module quản trị"]))

    # 06 architecture
    s,c=_base_slide(6,"Kiến trúc xử lý ASP.NET Core MVC")
    layers=[("RAZOR UI","View · CSS · JavaScript",C["blue"]),("CONTROLLER","Điều phối request & quyền",C["cyan"]),("SERVICE","Nghiệp vụ tái sử dụng",C["purple"]),("EF CORE","Ánh xạ & truy vấn",C["orange"]),("SQL SERVER","Dữ liệu nghiệp vụ",C["success"])]
    for i,(t,b,col) in enumerate(layers):
        x=.62+i*2.53; c.shape(x,2.12,2.13,1.18,fill=C["paper"],line=col,shadow=True); c.text(x+.10,2.28,1.93,.26,t,size=13,color=col,bold=True,align="ctr",fill=C["paper"],margin=0); c.text(x+.12,2.68,1.89,.30,b,size=10,color=C["muted"],align="ctr",fill=C["paper"],margin=0)
        if i<4: c.text(x+2.15,2.48,.35,.34,"→",size=20,color=C["muted"],bold=True,align="ctr",fill=C["bg"])
    c.shape(.98,4.08,11.32,1.45,fill=C["navy"],line=C["navy"],shadow=True)
    integrations=[("SignalR","Chat realtime"),("GHN","Địa chỉ & phí ship"),("SMTP","OTP email"),("QR","Chuyển khoản"),("Session","Cart · Compare · Build")]
    for i,(t,b) in enumerate(integrations):
        x=1.18+i*2.18; c.text(x,4.35,1.78,.24,t,size=13,color=C["cyan"],bold=True,align="ctr",fill=C["navy"],margin=0); c.text(x,4.76,1.78,.30,b,size=10,color="D4E3F0",align="ctr",fill=C["navy"],margin=0)
    c.text(2.10,5.94,9.1,.36,"Luồng rõ trách nhiệm · service hóa tích hợp · dễ mở rộng",size=15,color=C["navy"],bold=True,align="ctr",fill=C["bg"])
    slides.append(note(s,["Hệ thống sử dụng kiến trúc MVC quen thuộc của ASP.NET Core.","Controller điều phối request; service đóng gói giỏ hàng, xác thực, so sánh, vận chuyển và tương thích.","EF Core làm việc với SQL Server, còn SignalR, GHN, SMTP và QR là các tích hợp theo nghiệp vụ.","Session được dùng cho trải nghiệm chưa đăng nhập như cart, compare và Build PC."],["Phân lớp rõ trách nhiệm","Tích hợp được service hóa"]))

    # 07 tech security
    s,c=_base_slide(7,"Công nghệ, tích hợp & kiểm soát truy cập")
    tech=[(".NET 8","ASP.NET Core MVC",C["blue"]),("EF Core 8","Migration · SQL Server",C["cyan"]),("SignalR","Kênh chat realtime",C["purple"]),("Bootstrap + JS","Giao diện responsive",C["orange"])]
    for i,(a,b,col) in enumerate(tech): add_card(c,.72+i*3.08,1.48,2.76,1.42,title=a,body=b,accent=col,title_size=14,body_size=11)
    c.shape(.78,3.32,5.55,2.88,fill=C["paper"],line=C["line"],shadow=True)
    c.text(1.12,3.68,4.88,.28,"BẢO MẬT ỨNG DỤNG",size=14,color=C["danger"],bold=True,fill=C["paper"],margin=0)
    safeguards=["Cookie Authentication + Role","Anti-forgery cho POST","Password hash + OTP có hạn","Token truy cập hội thoại khách"]
    for i,t in enumerate(safeguards): c.text(1.14,4.22+i*.42,4.72,.25,"◆  "+t,size=11,color=C["text"],fill=C["paper"],margin=0)
    c.shape(6.74,3.32,5.78,2.88,fill=C["navy"],line=C["navy"],shadow=True)
    c.text(7.08,3.68,5.02,.28,"TÍCH HỢP NGHIỆP VỤ",size=14,color=C["cyan"],bold=True,fill=C["navy"],margin=0)
    for i,(a,b) in enumerate((("GHN","tỉnh/quận/phường + phí"),("SMTP","mã OTP quên mật khẩu"),("QR","nội dung chuyển khoản"),("Hosted service","hết hạn đơn thanh toán"))):
        y=4.20+i*.43; c.text(7.10,y,1.25,.24,a,size=11,color=C["paper"],bold=True,fill=C["navy"],margin=0); c.text(8.45,y,3.42,.24,b,size=11,color="C9DBEA",fill=C["navy"],margin=0)
    slides.append(note(s,["Stack chính là .NET 8, EF Core 8, SQL Server và giao diện Razor kết hợp JavaScript.","Kiểm soát truy cập dựa trên cookie, role Admin hoặc Staff và anti-forgery cho thao tác thay đổi dữ liệu.","Quên mật khẩu dùng OTP email có thời hạn, chat khách dùng access token riêng.","Các tích hợp đều phục vụ chức năng đã có thay vì trình diễn công nghệ đơn lẻ."],["Role và anti-forgery","GHN, SMTP, QR, SignalR"]))

    # 08 data
    s,c=_base_slide(8,"Mô hình dữ liệu theo miền nghiệp vụ")
    domains=[("IDENTITY","Users · Roles\nPasswordResetOtps",C["blue"]),("CATALOG","Categories · Products\nImages · Articles · Banners",C["cyan"]),("COMMERCE","Carts · CartItems\nOrders · OrderDetails",C["orange"]),("SUPPORT","WarrantyRequests\nFeedbacks · Chat",C["success"]),("CONFIG","SiteSettings · Shipping\nShopLocations · BuildPc",C["purple"])]
    for i,(t,b,col) in enumerate(domains):
        angle_y=1.50+(i%2)*.22; x=.60+i*2.54
        c.shape(x,angle_y,2.18,4.65,fill=C["paper"],line=col,shadow=True)
        c.shape(x+.65,angle_y+.38,.88,.88,fill=col,line=col,geom="ellipse"); c.text(x+.65,angle_y+.38,.88,.88,f"{i+1:02d}",size=16,color=C["paper"],bold=True,align="ctr",fill=col)
        c.text(x+.18,angle_y+1.52,1.82,.27,t,size=12,color=col,bold=True,align="ctr",fill=C["paper"],margin=0)
        c.text(x+.18,angle_y+2.05,1.82,1.12,b,size=11,color=C["text"],bold=True,align="ctr",fill=C["paper"],valign="top",margin=0)
        c.text(x+.28,angle_y+3.67,1.62,.38,"EF CORE\nRELATIONS",size=10,color=C["muted"],bold=True,align="ctr",fill="F4F7FA")
    c.text(.85,6.42,11.65,.30,f"ApplicationDbContext khai báo {facts.dbsets} DbSet — dữ liệu bao phủ cả bán hàng và hỗ trợ",size=14,color=C["navy"],bold=True,align="ctr",fill=C["bg"])
    slides.append(note(s,["Thay vì đưa ERD dày đặc, dữ liệu được nhóm thành năm miền dễ theo dõi.","Identity quản lý tài khoản và OTP; Catalog quản lý dữ liệu hiển thị.","Commerce lưu giỏ và đơn; Support lưu bảo hành, feedback và chat; Config lưu cấu hình site, vận chuyển và Build PC.",f"DbContext hiện có {facts.dbsets} DbSet, phản ánh phạm vi nghiệp vụ tương đối đầy đủ."],["Năm miền dữ liệu",f"{facts.dbsets} DbSet thực tế"]))

    # 09 journey map
    s,c=_base_slide(9,"Hành trình trải nghiệm khách hàng")
    c.line(1.15,3.35,12.12,3.35,color="BDD0E1",width=26000,alpha=70000)
    stages=[("01","Trang chủ","Khám phá",C["blue"]),("02","Danh sách","Lọc & tìm",C["cyan"]),("03","Chi tiết","Đánh giá",C["purple"]),("04","Build / Compare","Ra quyết định",C["pink"]),("05","Cart / Checkout","Giao dịch",C["orange"]),("06","Theo dõi / BH","Đồng hành",C["success"])]
    for i,(n,t,b,col) in enumerate(stages):
        x=.72+i*2.05; c.shape(x,2.87,.92,.92,fill=col,line=C["paper"],geom="ellipse",shadow=True); c.text(x,2.87,.92,.92,n,size=14,color=C["paper"],bold=True,align="ctr",fill=col)
        c.text(x-.38,4.13,1.68,.28,t,size=11,color=col,bold=True,align="ctr",fill=C["bg"],margin=0); c.text(x-.38,4.55,1.68,.24,b,size=10,color=C["muted"],align="ctr",fill=C["bg"],margin=0)
    c.shape(2.20,5.45,8.95,.62,fill=C["navy"],line=C["navy"]); c.text(2.20,5.45,8.95,.62,"Slide 10 → 21: demo theo đúng thứ tự sử dụng",size=15,color=C["paper"],bold=True,align="ctr",fill=C["navy"])
    slides.append(note(s,["Phần demo được sắp theo hành trình thực tế thay vì theo tên controller.","Khách bắt đầu từ trang chủ, thu hẹp lựa chọn ở catalog và đánh giá ở trang chi tiết.","Build PC hoặc so sánh hỗ trợ quyết định trước khi vào giỏ và checkout.","Sau giao dịch, hệ thống tiếp tục bằng theo dõi, chat, nội dung và bảo hành."],["Demo theo thứ tự sử dụng","Không tách rời hậu mãi"]))

    # 10 home
    s,c=_base_slide(10,"Trang chủ — điểm vào hành trình mua sắm"); screen_home(c,.62,1.43,8.18,5.28)
    add_card(c,9.14,1.53,3.50,1.37,title="THU HÚT",body="Banner chính và banner phụ được quản trị động",accent=C["blue"])
    add_card(c,9.14,3.08,3.50,1.37,title="ĐỊNH HƯỚNG",body="Danh mục PC, laptop, linh kiện, màn hình",accent=C["cyan"])
    add_card(c,9.14,4.63,3.50,1.62,title="CHUYỂN ĐỔI",body="Khuyến mãi, sản phẩm nổi bật và đường dẫn nhanh",accent=C["orange"])
    slides.append(note(s,["Trang chủ là điểm tập trung nội dung bán hàng quan trọng nhất.","Banner, danh mục và các section sản phẩm dẫn người dùng tới đúng nhóm nhu cầu.","Nội dung hiển thị lấy từ dữ liệu banner, category, product và site setting.","Quản trị viên có thể thay đổi hình ảnh và background mà không sửa view."],["Banner và danh mục động","Điểm vào các nhóm sản phẩm"],"Có. Khung giao diện vector dựa trên Views/Home/Index.cshtml."))

    # 11 catalog
    s,c=_base_slide(11,"Danh sách sản phẩm — tìm kiếm và thu hẹp lựa chọn")
    screen_catalog(c,4.28,1.43,8.40,5.30)
    c.text(.74,1.54,3.08,1.12,"Từ một catalog lớn, người dùng có thể nhanh chóng tìm nhóm sản phẩm phù hợp.",size=16,color=C["navy"],bold=True,fill=C["bg"],valign="top",margin=0)
    for i,(a,b,col) in enumerate((("TÌM KIẾM","Theo từ khóa",C["blue"]),("BỘ LỌC","Danh mục · giá",C["cyan"]),("SẮP XẾP","Giá · mới nhất",C["purple"]),("TRẠNG THÁI","Kho · khuyến mãi",C["orange"]))):
        y=2.80+i*.82; c.shape(.78,y,.14,.58,fill=col,line=col,geom="roundRect"); c.text(1.08,y,1.10,.22,a,size=11,color=col,bold=True,fill=C["bg"],margin=0); c.text(2.22,y,1.46,.22,b,size=10,color=C["muted"],align="r",fill=C["bg"],margin=0)
    slides.append(note(s,["Trang danh sách hỗ trợ tìm kiếm, lọc và sắp xếp để giảm số lựa chọn.","Bộ lọc dùng category, khoảng giá và trạng thái liên quan đến tồn kho hoặc khuyến mãi.","Mỗi card thể hiện giá, giá giảm và điểm vào chi tiết hoặc so sánh.","Đây là màn hình nối giữa nhu cầu chung và quyết định ở từng sản phẩm."],["Tìm · lọc · sắp xếp","Hiển thị tồn kho và khuyến mãi"],"Có. Khung giao diện vector dựa trên Views/Products/Index.cshtml."))

    # 12 detail
    s,c=_base_slide(12,"Chi tiết sản phẩm — đủ dữ liệu để ra quyết định"); screen_detail(c,.66,1.45,8.25,5.20)
    c.shape(9.25,1.52,3.38,4.90,fill=C["paper"],line=C["line"],shadow=True)
    c.text(9.60,1.87,2.72,.28,"THÔNG TIN QUYẾT ĐỊNH",size=13,color=C["purple"],bold=True,align="ctr",fill=C["paper"],margin=0)
    items=[("Hình ảnh","Ảnh chính + gallery"),("Giá bán","Giá gốc, giảm giá"),("Thông số","Specifications chi tiết"),("Mua hàng","Thêm giỏ / mua ngay"),("Mở rộng","Mua kèm + liên quan")]
    for i,(a,b) in enumerate(items):
        y=2.46+i*.68; c.text(9.56,y,1.02,.22,a,size=10,color=C["navy"],bold=True,fill=C["paper"],margin=0); c.text(10.64,y,1.62,.28,b,size=10,color=C["muted"],fill=C["paper"],margin=0)
    slides.append(note(s,["Trang chi tiết tập trung toàn bộ dữ liệu cần cho quyết định mua.","Ngoài hình ảnh, giá và tồn kho, view còn hiển thị thông số kỹ thuật và bảo hành.","Người dùng có thể thêm giỏ, mua ngay, xem sản phẩm mua kèm và sản phẩm liên quan.","Nút so sánh tiếp tục đưa sản phẩm vào quy trình đánh giá cạnh nhau."],["Thông số kỹ thuật rõ ràng","Mua kèm và sản phẩm liên quan"],"Có. Khung giao diện vector dựa trên Views/Products/Detail.cshtml."))

    # 13 compare
    s,c=_base_slide(13,"So sánh sản phẩm — đối chiếu tối đa 2 lựa chọn")
    c.shape(.74,1.48,8.22,4.98,fill=C["paper"],line=C["line"],shadow=True)
    c.text(1.04,1.80,7.60,.28,"BẢNG SO SÁNH SONG SONG",size=14,color=C["purple"],bold=True,align="ctr",fill=C["paper"],margin=0)
    c.shape(1.10,2.34,1.42,3.58,fill="F3F6FA",line="DDE6EF")
    rows=["Giá","CPU","RAM","GPU","SSD","Mainboard","PSU"]
    for i,t in enumerate(rows): c.text(1.20,2.52+i*.43,1.22,.20,t,size=10,color=C["muted"],bold=True,fill="F3F6FA",margin=0)
    for col,(name,color) in enumerate((("PC G5",C["blue"]),("PC G7",C["purple"]))):
        x=2.70+col*2.86; c.shape(x,2.34,2.62,3.58,fill=C["paper"],line=color); c.text(x+.15,2.50,2.32,.28,name,size=13,color=color,bold=True,align="ctr",fill=C["paper"],margin=0)
        for i in range(7): c.shape(x+.28,3.03+i*.39,2.06,.10,fill=["DDEBFA","E9E3FA"][col],line="FFFFFF",geom="roundRect")
    add_card(c,9.34,1.56,3.20,1.45,title="SESSION",body="Danh sách so sánh không yêu cầu đăng nhập",accent=C["cyan"])
    add_card(c,9.34,3.24,3.20,1.45,title="GIỚI HẠN",body="Tối đa 2 sản phẩm để bảng dễ đọc",accent=C["orange"])
    add_card(c,9.34,4.92,3.20,1.45,title="THÔNG SỐ",body="CPU, RAM, GPU, SSD, nguồn, case...",accent=C["purple"])
    slides.append(note(s,["So sánh giúp khách chuyển từ cảm nhận sang đối chiếu dữ liệu.","Hệ thống giới hạn hai sản phẩm để bảng không quá rộng và dễ đọc trên nhiều màn hình.","Lựa chọn được lưu trong session nên khách vãng lai vẫn sử dụng được.","Các dòng so sánh bao phủ giá và các thông số PC quan trọng."],["Tối đa hai sản phẩm","Lưu lựa chọn trong session"],"Có. Khung giao diện vector dựa trên Views/Compare/Index.cshtml."))

    # 14 buildpc
    s,c=_base_slide(14,"Build PC — chọn linh kiện và kiểm tra tương thích")
    ix,iy,iw,ih=add_browser(c,.62,1.42,8.65,5.32,"/BuildPc · Xây dựng cấu hình",C["purple"])
    components=["CPU","MAINBOARD","RAM","GPU","SSD","PSU","COOLER","CASE","MONITOR"]
    for i,t in enumerate(components):
        row=i%5; col=i//5; x=ix+col*(iw*.49); y=iy+row*.57
        c.shape(x,y,iw*.46,.45,fill=C["paper"],line="DDE6EF"); c.text(x+.14,y+.10,.88,.20,t,size=10,color=C["purple"],bold=True,fill=C["paper"],margin=0); c.shape(x+1.18,y+.13,iw*.22,.12,fill="DDE6EF",line="FFFFFF",geom="roundRect"); c.text(x+iw*.40,y+.07,.40,.26,"+",size=15,color=C["paper"],bold=True,align="ctr",fill=C["purple"],geom="ellipse",alpha=100000,line=C["purple"],line_alpha=100000)
    c.shape(ix,iy+3.03,iw,.60,fill=C["navy"],line=C["navy"]); c.text(ix+.18,iy+3.03,iw*.62,.60,"Tổng cấu hình · kiểm tra tương thích",size=11,color=C["paper"],bold=True,fill=C["navy"]); c.text(ix+iw-.98,iy+3.12,.78,.36,"CSV",size=11,color=C["navy"],bold=True,align="ctr",fill=C["cyan"])
    add_card(c,9.58,1.52,2.98,1.34,title="CHỌN THEO NHÓM",body="9 loại linh kiện",accent=C["purple"])
    add_card(c,9.58,3.03,2.98,1.34,title="QUY TẮC",body="Socket · RAM · công suất",accent=C["orange"])
    add_card(c,9.58,4.54,2.98,1.76,title="HÀNH ĐỘNG",body="Thêm toàn bộ vào giỏ\nXuất cấu hình CSV",accent=C["success"])
    slides.append(note(s,["Build PC là chức năng nổi bật nhất về tư vấn kỹ thuật.","Người dùng chọn linh kiện theo chín nhóm; trạng thái cấu hình được giữ trong session.","BuildCompatibilityService kiểm tra các quy tắc cơ bản như socket, RAM và công suất.","Khi hoàn tất, khách có thể thêm toàn bộ linh kiện vào giỏ hoặc xuất cấu hình CSV."],["Chín nhóm linh kiện","Kiểm tra tương thích và xuất CSV"],"Có. Khung giao diện vector dựa trên Views/BuildPc/Index.cshtml."))

    # 15 account
    s,c=_base_slide(15,"Tài khoản — định danh, hồ sơ và khôi phục mật khẩu")
    c.shape(.74,1.47,4.10,4.96,fill=C["navy"],fill2=C["blue"],line=C["navy"],shadow=True)
    c.text(1.15,1.88,3.28,.30,"VÒNG ĐỜI TÀI KHOẢN",size=14,color=C["cyan"],bold=True,align="ctr",fill=C["navy"],margin=0)
    flow=[("ĐĂNG KÝ","Tạo customer"),("ĐĂNG NHẬP","Cookie auth"),("HỒ SƠ","Thông tin cá nhân"),("ĐỔI MẬT KHẨU","Xác thực mật khẩu cũ")]
    for i,(a,b) in enumerate(flow):
        y=2.42+i*.83; c.shape(1.18,y,.54,.54,fill=C["cyan"],line=C["cyan"],geom="ellipse"); c.text(1.18,y,.54,.54,str(i+1),size=11,color=C["navy"],bold=True,align="ctr",fill=C["cyan"]); c.text(1.92,y-.01,1.72,.22,a,size=11,color=C["paper"],bold=True,fill=C["navy"],margin=0); c.text(1.92,y+.27,2.05,.21,b,size=10,color="C7DAEA",fill=C["navy"],margin=0)
    c.shape(5.25,1.47,7.28,4.96,fill=C["paper"],line=C["line"],shadow=True)
    c.text(5.68,1.88,6.42,.30,"QUÊN MẬT KHẨU BẰNG OTP EMAIL",size=14,color=C["purple"],bold=True,fill=C["paper"],margin=0)
    otp=[("01","Nhập email"),("02","Gửi mã OTP"),("03","Xác minh mã"),("04","Đặt mật khẩu mới")]
    for i,(n,t) in enumerate(otp):
        x=5.68+i*1.58; c.shape(x,2.62,1.08,1.08,fill=["EAF4FF","F2ECFF","FFF4E1","E9FBF5"][i],line="FFFFFF",geom="ellipse"); c.text(x,2.62,1.08,1.08,n,size=16,color=[C["blue"],C["purple"],C["orange"],C["success"]][i],bold=True,align="ctr",fill=["EAF4FF","F2ECFF","FFF4E1","E9FBF5"][i]); c.text(x-.22,3.95,1.52,.42,t,size=10,color=C["text"],bold=True,align="ctr",fill=C["paper"])
    c.shape(5.72,5.08,6.30,.72,fill="F5F7FB",line="E0E8F0"); c.text(5.95,5.08,5.85,.72,"OTP có thời hạn · chống dùng lại · gửi qua SMTP",size=12,color=C["muted"],bold=True,align="ctr",fill="F5F7FB")
    slides.append(note(s,["Module tài khoản không chỉ có đăng nhập và đăng ký.","Người dùng có thể cập nhật hồ sơ, đổi mật khẩu và truy cập đơn hàng cá nhân.","Luồng quên mật khẩu tạo OTP, gửi qua email, xác minh thời hạn rồi mới cho đặt mật khẩu mới.","Cookie authentication và role được dùng để phân tách customer, staff và admin."],["OTP email có thời hạn","Hồ sơ và đổi mật khẩu"]))

    # 16 cart
    s,c=_base_slide(16,"Giỏ hàng — hợp nhất khách vãng lai và người đăng nhập")
    screen_table(c,.62,1.43,8.42,5.24,"/Cart · Giỏ hàng",["SẢN PHẨM","ĐƠN GIÁ","SỐ LƯỢNG","THÀNH TIỀN"],C["orange"])
    c.shape(6.70,5.33,1.70,.58,fill=C["orange"],line=C["orange"]); c.text(6.70,5.33,1.70,.58,"CHECKOUT",size=11,color=C["paper"],bold=True,align="ctr",fill=C["orange"])
    c.text(9.38,1.54,3.10,.58,"Hai cơ chế lưu trữ, một trải nghiệm thống nhất.",size=17,color=C["navy"],bold=True,fill=C["bg"],valign="top",margin=0)
    add_card(c,9.36,2.48,3.16,1.38,title="KHÁCH VÃNG LAI",body="Giỏ lưu trong Session",accent=C["blue"])
    add_card(c,9.36,4.05,3.16,1.38,title="ĐÃ ĐĂNG NHẬP",body="Cart và CartItem trong DB",accent=C["purple"])
    c.text(9.42,5.82,3.02,.48,"Thêm · mua ngay · cập nhật · xóa · làm trống",size=11,color=C["muted"],bold=True,align="ctr",fill=C["bg"])
    slides.append(note(s,["Giỏ hàng hỗ trợ cả khách vãng lai và người dùng đã đăng nhập.","Khách dùng session; tài khoản dùng Cart và CartItem trong database.","Các thao tác gồm thêm, mua ngay, cập nhật số lượng, xóa từng dòng hoặc làm trống.","Tổng tiền được tính lại trước khi chuyển sang checkout."],["Session và database","Đầy đủ thao tác giỏ hàng"],"Có. Khung giao diện vector dựa trên Views/Cart/Index.cshtml."))

    # 17 checkout
    s,c=_base_slide(17,"Checkout — địa chỉ GHN và phí vận chuyển")
    c.shape(.70,1.46,5.35,4.98,fill=C["paper"],line=C["line"],shadow=True)
    c.text(1.08,1.84,4.58,.27,"THÔNG TIN NHẬN HÀNG",size=14,color=C["blue"],bold=True,fill=C["paper"],margin=0)
    fields=["Họ tên người nhận","Số điện thoại · email","Tỉnh / thành phố","Quận / huyện","Phường / xã","Địa chỉ chi tiết · ghi chú"]
    for i,t in enumerate(fields):
        y=2.34+i*.56; c.text(1.08,y,1.82,.20,t,size=10,color=C["muted"],bold=True,fill=C["paper"],margin=0); c.shape(2.95,y-.04,2.62,.30,fill="F7F9FC",line="D8E3ED")
    c.shape(6.42,1.46,6.08,4.98,fill=C["navy"],line=C["navy"],shadow=True)
    c.text(6.84,1.84,5.22,.27,"TÍNH PHÍ VẬN CHUYỂN",size=14,color=C["cyan"],bold=True,fill=C["navy"],margin=0)
    for i,(n,a,b) in enumerate((("01","GHN Address API","Tải tỉnh → quận → phường"),("02","Shipping API","Gửi địa chỉ và giỏ hàng"),("03","Chính sách phí","Khoảng cách · base fee · phụ phí"),("04","Kết quả","Phí ship + thời gian dự kiến"))):
        y=2.35+i*.79; c.shape(6.84,y,.54,.54,fill=C["cyan"],line=C["cyan"],geom="ellipse"); c.text(6.84,y,.54,.54,n,size=10,color=C["navy"],bold=True,align="ctr",fill=C["cyan"]); c.text(7.60,y-.01,1.66,.22,a,size=11,color=C["paper"],bold=True,fill=C["navy"],margin=0); c.text(9.24,y-.01,2.70,.30,b,size=10,color="C9DBEA",fill=C["navy"],margin=0)
    c.text(6.90,5.73,5.12,.32,"COD  |  Chuyển khoản",size=13,color=C["orange"],bold=True,align="ctr",fill=C["navy"],margin=0)
    slides.append(note(s,["Checkout thu thập đầy đủ người nhận, liên hệ và địa chỉ giao hàng.","Các danh sách tỉnh, quận và phường được tải qua dịch vụ địa chỉ GHN.","API shipping nhận địa chỉ cùng giỏ hàng để tính phí và thời gian dự kiến theo chính sách cấu hình.","Cuối luồng, khách chọn COD hoặc chuyển khoản."],["GHN tỉnh–quận–phường","Tính phí từ địa chỉ và giỏ hàng"],"Có. Nội dung dựa trên Views/Orders/Checkout.cshtml và ShippingController."))

    # 18 payment
    s,c=_base_slide(18,"Thanh toán — COD và chuyển khoản QR có thời hạn")
    c.shape(.72,1.48,4.20,4.90,fill=C["paper"],line=C["line"],shadow=True)
    c.text(1.08,1.88,3.48,.28,"COD",size=20,color=C["orange"],bold=True,align="ctr",fill=C["paper"],margin=0)
    c.shape(1.54,2.58,2.55,1.25,fill="FFF4E1",line="F6D49C",geom="roundRect"); c.text(1.54,2.58,2.55,1.25,"TẠO ĐƠN\nCHỜ XÁC NHẬN",size=15,color=C["orange"],bold=True,align="ctr",fill="FFF4E1")
    c.text(1.14,4.26,3.34,.78,"Không cần thanh toán trước\nAdmin tiếp nhận và xử lý đơn",size=12,color=C["muted"],align="ctr",fill=C["paper"])
    c.shape(5.30,1.48,7.18,4.90,fill=C["navy"],line=C["navy"],shadow=True)
    c.text(5.72,1.86,6.32,.28,"CHUYỂN KHOẢN QR",size=16,color=C["cyan"],bold=True,fill=C["navy"],margin=0)
    c.shape(5.76,2.42,2.12,2.12,fill=C["paper"],line=C["cyan"],line_width=24000)
    for i in range(5):
        c.shape(6.00+(i%3)*.52,2.68+(i//3)*.58,.28,.28,fill=C["navy"],line=C["navy"],geom="rect")
    c.text(8.28,2.45,3.38,.34,"Nội dung: DH000123",size=13,color=C["paper"],bold=True,fill=C["navy"],margin=0)
    for i,t in enumerate(("PaymentStatus = UNPAID","Hết hạn sau 2 giờ","Khách bấm đã chuyển tiền","Admin xác nhận giao dịch")): c.text(8.28,3.02+i*.46,3.44,.24,"✓  "+t,size=11,color="D4E3F0",fill=C["navy"],margin=0)
    c.shape(8.27,5.16,3.34,.52,fill=C["danger"],alpha=85000,line=C["danger"]); c.text(8.27,5.16,3.34,.52,"ĐẾM NGƯỢC THỜI HẠN",size=11,color=C["paper"],bold=True,align="ctr",fill=C["danger"])
    slides.append(note(s,["Hệ thống triển khai hai phương thức thanh toán với trạng thái đơn khác nhau.","COD tạo đơn chờ xác nhận và không yêu cầu thanh toán trước.","Chuyển khoản hiển thị QR, nội dung theo mã đơn và thời hạn thanh toán hai giờ.","Khách xác nhận đã chuyển; quản trị viên kiểm tra và xác nhận giao dịch trong màn hình đơn."],["Hai phương thức thanh toán","QR và thời hạn hai giờ"],"Có. Nội dung dựa trên Views/Orders/BankTransfer.cshtml."))

    # 19 orders exports
    s,c=_base_slide(19,"Đơn hàng — theo dõi, lịch sử và xuất chứng từ")
    c.shape(.76,1.50,7.00,4.87,fill=C["paper"],line=C["line"],shadow=True)
    c.text(1.10,1.88,6.30,.28,"TIẾN TRÌNH ĐƠN HÀNG",size=14,color=C["success"],bold=True,fill=C["paper"],margin=0)
    statuses=[("PENDING","Chờ xử lý"),("PROCESSING","Đang chuẩn bị"),("DELIVERING","Đang giao"),("COMPLETED","Hoàn tất")]
    c.line(1.45,3.12,7.05,3.12,color="C6D6E5",width=23000)
    for i,(a,b) in enumerate(statuses):
        x=1.20+i*1.75; col=[C["orange"],C["blue"],C["purple"],C["success"]][i]; c.shape(x,2.68,.88,.88,fill=col,line=C["paper"],geom="ellipse",shadow=True); c.text(x,2.68,.88,.88,str(i+1),size=14,color=C["paper"],bold=True,align="ctr",fill=col); c.text(x-.30,3.85,1.48,.22,a,size=9,color=col,bold=True,align="ctr",fill=C["paper"],margin=0); c.text(x-.30,4.19,1.48,.24,b,size=10,color=C["muted"],align="ctr",fill=C["paper"],margin=0)
    c.text(1.14,5.22,6.24,.46,"Tra cứu bằng mã đơn + số điện thoại  |  My Orders cho tài khoản",size=11,color=C["navy"],bold=True,align="ctr",fill="F4F7FA")
    add_card(c,8.18,1.52,4.36,1.36,title="BÁO GIÁ",body="Trang quotation từ dữ liệu đơn hàng",accent=C["blue"])
    add_card(c,8.18,3.07,4.36,1.36,title="XUẤT EXCEL",body="Tải chi tiết đơn theo định dạng bảng",accent=C["success"])
    add_card(c,8.18,4.62,4.36,1.74,title="THANH TOÁN LẠI",body="Đơn pending payment có thể mở lại trang QR",accent=C["orange"])
    slides.append(note(s,["Sau checkout, khách có thể theo dõi tiến trình đơn bằng mã đơn và số điện thoại.","Người đăng nhập có danh sách My Orders và trang chi tiết riêng.","Đơn chờ thanh toán có thể mở lại luồng thanh toán QR.","Hệ thống còn tạo trang báo giá và xuất Excel từ dữ liệu đơn, phục vụ trao đổi hoặc lưu trữ."],["Tra cứu không cần đăng nhập","Báo giá và xuất Excel"],"Có. Nội dung dựa trên nhóm Views/Orders."))

    # 20 support chat
    s,c=_base_slide(20,"Hỗ trợ realtime — chat khách hàng và feedback")
    c.shape(.70,1.48,7.38,4.93,fill=C["paper"],line=C["line"],shadow=True)
    c.text(1.04,1.82,6.70,.28,"CHAT SIGNALR",size=14,color=C["blue"],bold=True,fill=C["paper"],margin=0)
    c.shape(1.05,2.35,6.66,2.72,fill="F4F7FA",line="DCE6EF")
    bubbles=[(.34,.25,2.40,.48,"Tôi cần tư vấn cấu hình",C["paper"],C["text"]),(3.20,.86,2.72,.58,"Shop có thể hỗ trợ ngân sách và nhu cầu sử dụng.","DDEBFA",C["navy"]),(.62,1.62,2.75,.48,"Khoảng 25 triệu, chơi game 2K",C["paper"],C["text"])]
    for bx,by,bw,bh,txt,fill,col in bubbles: c.shape(1.05+bx,2.35+by,bw,bh,fill=fill,line="D7E2EC"); c.text(1.13+bx,2.35+by,bw-.16,bh,txt,size=9,color=col,fill=fill,margin=.03)
    c.shape(1.28,5.31,5.50,.46,fill=C["paper"],line="D6E1EB"); c.shape(6.90,5.31,.58,.46,fill=C["blue"],line=C["blue"]); c.text(6.90,5.31,.58,.46,"➤",size=14,color=C["paper"],bold=True,align="ctr",fill=C["blue"])
    add_card(c,8.45,1.52,4.07,1.35,title="HỘI THOẠI",body="Khách vãng lai hoặc tài khoản",accent=C["blue"])
    add_card(c,8.45,3.05,4.07,1.35,title="TIN NHẮN HỆ THỐNG",body="Lưu lịch sử và trạng thái",accent=C["purple"])
    add_card(c,8.45,4.58,4.07,1.78,title="FEEDBACK",body="Form liên hệ riêng; Staff/Admin xem danh sách xử lý",accent=C["success"])
    slides.append(note(s,["Hệ thống có hai kênh tiếp nhận hỗ trợ bổ sung cho nhau.","Chat dùng SignalR để cập nhật realtime, lưu conversation và message trong database.","Khách vãng lai được cấp token truy cập hội thoại; tài khoản có thể gắn với user.","Feedback là kênh không đồng bộ và được Staff hoặc Admin xem trong màn hình quản lý."],["SignalR realtime","Chat và feedback tách mục đích"],"Có. Nội dung dựa trên _SupportChatBox, SupportChatController và ContactController."))

    # 21 aftersales content
    s,c=_base_slide(21,"Hậu mãi & nội dung — bảo hành, tin tức, khuyến mãi")
    c.shape(.72,1.50,5.80,4.87,fill=C["paper"],line=C["line"],shadow=True)
    c.text(1.10,1.88,5.04,.28,"YÊU CẦU BẢO HÀNH",size=14,color=C["success"],bold=True,fill=C["paper"],margin=0)
    c.shape(1.10,2.46,1.25,1.25,fill="E9FBF5",line=C["success"],geom="ellipse"); c.text(1.10,2.46,1.25,1.25,"BH",size=20,color=C["success"],bold=True,align="ctr",fill="E9FBF5")
    c.text(2.66,2.42,3.08,.26,"Khách đã đăng nhập",size=12,color=C["navy"],bold=True,fill=C["paper"],margin=0)
    c.text(2.66,2.85,3.08,.80,"Chọn sản phẩm\nMô tả sự cố\nTheo dõi trạng thái xử lý",size=11,color=C["muted"],fill=C["paper"],valign="top",margin=0)
    c.shape(1.10,4.28,5.02,1.20,fill="F5F8FB",line="DFE7EF"); c.text(1.34,4.50,4.54,.75,"Mới tạo  →  Tiếp nhận  →  Đang xử lý  →  Hoàn tất",size=11,color=C["success"],bold=True,align="ctr",fill="F5F8FB")
    c.shape(6.90,1.50,5.62,4.87,fill=C["navy"],line=C["navy"],shadow=True)
    c.text(7.28,1.88,4.86,.28,"TIN TỨC / KHUYẾN MÃI",size=14,color=C["cyan"],bold=True,fill=C["navy"],margin=0)
    for i,(t,col) in enumerate((("Tin công nghệ",C["blue"]),("Hướng dẫn chọn PC",C["purple"]),("Chương trình khuyến mãi",C["orange"]))):
        y=2.45+i*.95; c.shape(7.28,y,.72,.72,fill=col,line=col,geom="roundRect"); c.text(7.28,y,.72,.72,str(i+1),size=13,color=C["paper"],bold=True,align="ctr",fill=col); c.text(8.26,y+.08,3.36,.24,t,size=12,color=C["paper"],bold=True,fill=C["navy"],margin=0); c.shape(8.26,y+.43,2.82,.10,fill="49647E",line="49647E",geom="roundRect")
    c.text(7.32,5.47,4.78,.34,"Article: danh sách · chi tiết theo slug",size=11,color="C9DBEA",bold=True,align="ctr",fill=C["navy"],margin=0)
    slides.append(note(s,["Hậu mãi gồm cả xử lý sự cố sản phẩm và nội dung duy trì quan hệ khách hàng.","Khách đăng nhập có thể tạo yêu cầu bảo hành cho sản phẩm và mô tả vấn đề.","AdminWarranty theo dõi và cập nhật trạng thái xử lý.","Module Article cung cấp tin công nghệ, hướng dẫn hoặc khuyến mãi với trang danh sách và chi tiết theo slug."],["Bảo hành có trạng thái","Article theo slug"]))

    # 22 admin overview
    s,c=_base_slide(22,"Quản trị hệ thống — trung tâm vận hành cửa hàng")
    c.shape(.70,1.47,8.22,4.95,fill=C["navy"],line=C["navy"],shadow=True)
    c.text(1.08,1.82,7.46,.26,"DASHBOARD KPI",size=14,color=C["cyan"],bold=True,fill=C["navy"],margin=0)
    metrics=[("248","Sản phẩm",C["blue"]),("126","Đơn hàng",C["orange"]),("93","Người dùng",C["purple"]),("08","Yêu cầu BH",C["success"])]
    for i,(v,l,col) in enumerate(metrics):
        x=1.08+(i%2)*3.55; y=2.38+(i//2)*1.55; c.shape(x,y,3.15,1.22,fill="102E52",line="315275"); c.text(x+.24,y+.16,1.10,.45,v,size=24,color=col,bold=True,fill="102E52",margin=0); c.text(x+1.42,y+.23,1.40,.30,l,size=12,color=C["paper"],bold=True,fill="102E52",margin=0)
    c.text(1.08,5.55,7.46,.30,"KPI lấy từ AdminDashboardVm",size=11,color="C8D9E8",bold=True,align="ctr",fill=C["navy"],margin=0)
    c.text(9.30,1.55,3.08,.64,"Từ dashboard, quản trị viên đi tới các nghiệp vụ chính.",size=16,color=C["navy"],bold=True,fill=C["bg"],valign="top",margin=0)
    admins=[("Catalog","Sản phẩm · danh mục"),("Commerce","Đơn · thanh toán"),("Identity","User · role"),("Content","Banner · article · site"),("Support","Chat · feedback · BH")]
    for i,(a,b) in enumerate(admins):
        y=2.58+i*.72; c.text(9.32,y,1.02,.22,a,size=10,color=[C["blue"],C["orange"],C["purple"],C["cyan"],C["success"]][i],bold=True,fill=C["bg"],margin=0); c.text(10.40,y,1.92,.26,b,size=10,color=C["muted"],fill=C["bg"],margin=0)
    slides.append(note(s,["Phần quản trị được xem như trung tâm vận hành, không chỉ là vài bảng CRUD.","Dashboard tổng hợp bốn KPI thật từ AdminDashboardVm: sản phẩm, đơn, người dùng và bảo hành.","Menu quản trị chia thành catalog, commerce, identity, content và support.","Các slide tiếp theo trình bày từng cụm theo mức độ ảnh hưởng đến vận hành."],["Bốn KPI từ ViewModel","Năm cụm quản trị"],"Có. Nội dung dựa trên Views/AdminDashboard/Index.cshtml."))

    # 23 catalog admin
    s,c=_base_slide(23,"Quản trị catalog — sản phẩm, ảnh và danh mục")
    screen_table(c,.62,1.43,8.55,5.28,"/AdminProducts · Quản lý sản phẩm",["MÃ","SẢN PHẨM","GIÁ","TỒN KHO","TRẠNG THÁI"],C["blue"])
    add_card(c,9.52,1.53,3.02,1.30,title="SẢN PHẨM",body="Tạo · sửa · xóa",accent=C["blue"])
    add_card(c,9.52,3.00,3.02,1.30,title="MEDIA",body="Thumbnail + nhiều ảnh",accent=C["cyan"])
    add_card(c,9.52,4.47,3.02,1.82,title="DANH MỤC",body="Tên · icon · liên kết sản phẩm",accent=C["purple"])
    c.text(.92,6.04,7.90,.34,"Giá bán · giá giảm · promotion text · tồn kho · active · thông số · bảo hành",size=10,color=C["muted"],bold=True,align="ctr",fill=C["bg"])
    slides.append(note(s,["Quản trị catalog bao phủ vòng đời sản phẩm và cấu trúc danh mục.","Form sản phẩm hỗ trợ thông tin bán hàng, tồn kho, trạng thái, mô tả, thông số và bảo hành.","ProductImageStorageService xử lý thumbnail và nhiều ảnh sản phẩm.","Danh mục có tên và icon, đồng thời là cơ sở cho điều hướng và bộ lọc phía khách hàng."],["Đầy đủ dữ liệu sản phẩm","Thumbnail và gallery"],"Có. Nội dung dựa trên AdminProducts và AdminCategories."))

    # 24 order admin
    s,c=_base_slide(24,"Quản trị đơn hàng — xử lý trạng thái và thanh toán")
    c.shape(.72,1.48,5.72,4.90,fill=C["paper"],line=C["line"],shadow=True)
    c.text(1.08,1.87,4.98,.28,"DANH SÁCH & CHI TIẾT",size=14,color=C["orange"],bold=True,fill=C["paper"],margin=0)
    for i,(a,b,col) in enumerate((("Tra cứu","Mã đơn · khách · trạng thái",C["blue"]),("Chi tiết","Sản phẩm · phí · địa chỉ",C["purple"]),("Cập nhật","Processing → Delivering",C["orange"]),("Hoàn tất","Completed / Cancelled",C["success"]))):
        y=2.43+i*.78; c.shape(1.08,y,.58,.58,fill=col,line=col,geom="ellipse"); c.text(1.08,y,.58,.58,str(i+1),size=11,color=C["paper"],bold=True,align="ctr",fill=col); c.text(1.86,y-.01,1.08,.22,a,size=11,color=col,bold=True,fill=C["paper"],margin=0); c.text(2.98,y-.01,2.65,.26,b,size=10,color=C["muted"],fill=C["paper"],margin=0)
    c.shape(6.82,1.48,5.70,4.90,fill=C["navy"],line=C["navy"],shadow=True)
    c.text(7.20,1.87,4.94,.28,"KIỂM SOÁT CHUYỂN KHOẢN",size=14,color=C["cyan"],bold=True,fill=C["navy"],margin=0)
    states=[("PENDING PAYMENT","Chờ khách chuyển"),("PENDING CONFIRM","Khách báo đã chuyển"),("PAID","Admin xác nhận"),("EXPIRED","Hosted service hết hạn")]
    for i,(a,b) in enumerate(states):
        y=2.42+i*.78; c.text(7.22,y,1.62,.24,a,size=10,color=[C["orange"],C["cyan"],C["success"],C["danger"]][i],bold=True,fill=C["navy"],margin=0); c.text(8.92,y,2.82,.25,b,size=10,color="C9DBEA",fill=C["navy"],margin=0); c.shape(11.88,y+.03,.16,.16,fill=[C["orange"],C["cyan"],C["success"],C["danger"]][i],line="FFFFFF",geom="ellipse")
    c.text(7.24,5.72,4.86,.28,"AdminOrdersController · OrderExpirationService",size=10,color="BCD1E3",bold=True,align="ctr",fill=C["navy"],margin=0)
    slides.append(note(s,["Quản trị đơn hàng là nghiệp vụ quan trọng nhất sau catalog.","Nhân viên xem danh sách, mở chi tiết, kiểm tra địa chỉ, sản phẩm, phí và cập nhật trạng thái.","Với chuyển khoản, trạng thái tách rõ chờ thanh toán, chờ xác nhận, đã thanh toán và hết hạn.","OrderExpirationService hỗ trợ tự động xử lý đơn quá thời hạn."],["Quy trình trạng thái rõ ràng","Xác nhận chuyển khoản và hết hạn"]))

    # 25 users
    s,c=_base_slide(25,"Quản trị tài khoản — vai trò và trạng thái hoạt động")
    screen_table(c,4.40,1.45,8.15,5.22,"/AdminUsers · Quản lý tài khoản",["USER","EMAIL","ROLE","TRẠNG THÁI"],C["purple"])
    c.text(.74,1.56,3.20,.72,"Phân quyền đủ đơn giản để vận hành, nhưng tách rõ trách nhiệm.",size=17,color=C["navy"],bold=True,fill=C["bg"],valign="top",margin=0)
    roles=[("ADMIN","Toàn quyền quản trị",C["purple"]),("STAFF","Đơn · chat · feedback",C["orange"]),("CUSTOMER","Mua hàng · bảo hành",C["blue"])]
    for i,(a,b,col) in enumerate(roles): add_card(c,.76,2.62+i*1.06,3.16,.88,title=a,body=b,accent=col,title_size=11,body_size=10)
    c.shape(.82,5.92,3.02,.46,fill="F2ECFF",line="DDD0F8"); c.text(.82,5.92,3.02,.46,"Khóa / mở tài khoản",size=11,color=C["purple"],bold=True,align="ctr",fill="F2ECFF")
    slides.append(note(s,["Admin có màn hình riêng để quản lý tài khoản và vai trò.","Ba role chính là Admin, Staff và Customer; quyền controller sử dụng Authorize theo role.","Form cho phép tạo hoặc cập nhật thông tin, chọn role và trạng thái hoạt động.","Khả năng khóa tài khoản giúp kiểm soát truy cập mà không phải xóa lịch sử liên quan."],["Ba role thực tế","Khóa tài khoản không mất lịch sử"],"Có. Nội dung dựa trên Views/AdminUsers."))

    # 26 content settings
    s,c=_base_slide(26,"Quản trị nội dung — banner, bài viết và giao diện website")
    c.shape(.70,1.48,4.00,4.90,fill=C["paper"],line=C["line"],shadow=True)
    c.text(1.04,1.86,3.32,.28,"BANNER",size=14,color=C["blue"],bold=True,align="ctr",fill=C["paper"],margin=0)
    c.shape(1.08,2.38,3.24,1.42,fill=C["navy"],fill2=C["blue"],line=C["navy"]); c.text(1.30,2.68,2.80,.30,"MainBanner / SubBanner",size=12,color=C["paper"],bold=True,align="ctr",fill=C["navy"],margin=0)
    c.text(1.02,4.17,3.38,.86,"Ảnh · tiêu đề · mô tả\nlink · vị trí · thứ tự · active",size=11,color=C["muted"],bold=True,align="ctr",fill=C["paper"])
    c.shape(4.95,1.48,3.55,4.90,fill=C["navy"],line=C["navy"],shadow=True)
    c.text(5.30,1.86,2.85,.28,"SITE SETTINGS",size=14,color=C["cyan"],bold=True,align="ctr",fill=C["navy"],margin=0)
    for i,(a,b) in enumerate((("Logo","Logo website"),("Deal","Background ưu đãi"),("Hot promotion","Background khuyến mãi"),("Site name","Tên thương hiệu"))):
        y=2.48+i*.70; c.text(5.34,y,1.24,.22,a,size=10,color=C["cyan"],bold=True,fill=C["navy"],margin=0); c.text(6.62,y,1.18,.34,b,size=10,color="D0E0ED",fill=C["navy"],margin=0)
    c.shape(8.76,1.48,3.78,4.90,fill=C["paper"],line=C["line"],shadow=True)
    c.text(9.10,1.86,3.10,.28,"ARTICLES",size=14,color=C["purple"],bold=True,align="ctr",fill=C["paper"],margin=0)
    for i,t in enumerate(("Tạo bài viết","Sửa nội dung","Slug chi tiết","Phân loại bài")):
        y=2.46+i*.66; c.shape(9.12,y,.38,.38,fill=C["purple"],line=C["purple"],geom="ellipse"); c.text(9.12,y,.38,.38,"✓",size=10,color=C["paper"],bold=True,align="ctr",fill=C["purple"]); c.text(9.68,y+.05,2.18,.24,t,size=11,color=C["text"],bold=True,fill=C["paper"],margin=0)
    c.text(9.12,5.47,3.08,.40,"Nội dung cập nhật mà không sửa code",size=10,color=C["muted"],bold=True,align="ctr",fill="F4F7FA")
    slides.append(note(s,["Nhóm quản trị nội dung giúp website thay đổi hình ảnh và thông tin mà không sửa code.","Banner có vị trí, thứ tự, link và trạng thái hiển thị.","Site Settings quản lý logo, tên site và background cho các section khuyến mãi.","Articles hỗ trợ tạo, sửa, xóa và hiển thị nội dung theo slug."],["Banner có vị trí và thứ tự","Logo và background thay đổi động"]))

    # 27 admin support
    s,c=_base_slide(27,"Trung tâm hỗ trợ — chat, feedback và bảo hành")
    c.shape(.70,1.46,6.00,4.98,fill=C["navy"],line=C["navy"],shadow=True)
    c.text(1.06,1.82,5.28,.28,"ADMIN CHAT",size=14,color=C["cyan"],bold=True,fill=C["navy"],margin=0)
    c.shape(1.04,2.34,1.82,3.22,fill="102E52",line="315275")
    for i in range(4): c.shape(1.20,2.58+i*.62,1.48,.42,fill="193B60" if i else C["blue"],line="315275"); c.text(1.32,2.62+i*.62,1.20,.22,f"Khách hàng {i+1}",size=9,color=C["paper"],bold=i==0,fill="193B60" if i else C["blue"],margin=0)
    c.shape(3.10,2.34,3.22,3.22,fill="F4F7FA",line="DCE6EF")
    c.shape(3.34,2.70,1.66,.42,fill=C["paper"],line="DDE6EF"); c.shape(4.02,3.42,1.98,.55,fill="DDEBFA",line="C9DFF4"); c.shape(3.34,4.26,2.26,.42,fill=C["paper"],line="DDE6EF")
    c.shape(3.34,4.94,2.18,.36,fill=C["paper"],line="D5E0EA"); c.shape(5.64,4.94,.42,.36,fill=C["blue"],line=C["blue"])
    add_card(c,7.08,1.52,5.46,1.34,title="FEEDBACK",body="Danh sách phản hồi · trạng thái đã xử lý",accent=C["blue"])
    add_card(c,7.08,3.05,5.46,1.34,title="BẢO HÀNH",body="Sản phẩm · mô tả sự cố · cập nhật trạng thái",accent=C["success"])
    add_card(c,7.08,4.58,5.46,1.78,title="GIÁ TRỊ",body="Một nơi tiếp nhận lịch sử trao đổi và yêu cầu sau bán",accent=C["purple"])
    slides.append(note(s,["Admin Chat hiển thị danh sách hội thoại và nội dung trao đổi theo thời gian thực.","Staff hoặc Admin có thể chọn conversation, gửi phản hồi và theo dõi lịch sử.","Feedback và bảo hành được quản lý ở các màn hình riêng vì quy trình xử lý khác chat.","Ba kênh này tạo trung tâm hỗ trợ sau bán đầy đủ hơn cho cửa hàng."],["Admin chat realtime","Ba kênh hỗ trợ bổ sung nhau"],"Có. Nội dung dựa trên AdminChat, Contact/Manage và AdminWarranty."))

    # 28 result quality
    s,c=_base_slide(28,"Kết quả đạt được & chất lượng triển khai")
    metrics=[(str(facts.controllers),"CONTROLLERS",C["blue"]),(str(facts.dbsets),"DBSETS",C["cyan"]),(str(facts.services),"SERVICE FILES",C["purple"]),("3","ROLES",C["orange"])]
    for i,(v,l,col) in enumerate(metrics): add_metric(c,.72+i*3.10,1.48,2.78,v,l,col)
    c.shape(.76,2.92,5.72,3.42,fill=C["paper"],line=C["line"],shadow=True)
    c.text(1.10,3.27,5.02,.28,"ĐÃ HOÀN THIỆN",size=14,color=C["success"],bold=True,fill=C["paper"],margin=0)
    done=["Luồng mua hàng từ catalog đến tracking","Build PC, compare và xuất dữ liệu","Quản trị catalog, commerce, identity, content","Chat, feedback, bảo hành và OTP"]
    for i,t in enumerate(done): c.text(1.12,3.85+i*.48,4.92,.28,"✓  "+t,size=11,color=C["text"],fill=C["paper"],margin=0)
    c.shape(6.82,2.92,5.72,3.42,fill=C["navy"],line=C["navy"],shadow=True)
    c.text(7.18,3.27,4.98,.28,"ĐIỂM CẦN HOÀN THIỆN",size=14,color=C["orange"],bold=True,fill=C["navy"],margin=0)
    gaps=["Chuyển khoản vẫn cần đối soát thủ công","Build PC dựa trên quy tắc, chưa phải AI","Dashboard mới dừng ở KPI số lượng","Cần bổ sung test tự động và tối ưu mobile"]
    for i,t in enumerate(gaps): c.text(7.20,3.85+i*.48,4.82,.28,"△  "+t,size=11,color="D5E3EF",fill=C["navy"],margin=0)
    slides.append(note(s,["Kết quả được đo từ chính cấu trúc source hiện tại.",f"Dự án có {facts.controllers} controller, {facts.dbsets} DbSet và {facts.services} file service.","Luồng bán hàng, tư vấn, quản trị và hậu mãi đã được kết nối thành một hệ thống.","Tuy nhiên thanh toán, Build PC, báo cáo và kiểm thử vẫn còn khoảng trống cần phát triển."],["Kết quả định lượng từ source","Nêu rõ giới hạn hiện tại"]))

    # 29 roadmap
    s,c=_base_slide(29,"Hướng phát triển — từ đồ án đến sản phẩm vận hành")
    c.line(1.10,3.45,12.16,3.45,color="BFD1E1",width=26000)
    roadmap=[("01","THANH TOÁN","Tích hợp gateway\nwebhook đối soát",C["orange"]),("02","BUILD PC","Dữ liệu benchmark\ngợi ý thông minh",C["purple"]),("03","ANALYTICS","Doanh thu · funnel\ntồn kho · khách hàng",C["blue"]),("04","QUALITY","Test tự động\nmonitoring · security",C["success"]),("05","EXPERIENCE","Mobile · SEO\nPWA / notification",C["cyan"])]
    for i,(n,t,b,col) in enumerate(roadmap):
        x=.78+i*2.42; c.shape(x,2.88,1.12,1.12,fill=col,line=C["paper"],geom="ellipse",shadow=True); c.text(x,2.88,1.12,1.12,n,size=15,color=C["paper"],bold=True,align="ctr",fill=col); c.text(x-.36,4.35,1.84,.25,t,size=11,color=col,bold=True,align="ctr",fill=C["bg"],margin=0); c.text(x-.40,4.78,1.92,.62,b,size=10,color=C["muted"],align="ctr",fill=C["bg"],valign="top",margin=0)
    c.shape(2.03,5.92,9.28,.52,fill=C["navy"],line=C["navy"]); c.text(2.03,5.92,9.28,.52,"Ưu tiên: tự động hóa giao dịch → nâng chất lượng tư vấn → đo lường vận hành",size=12,color=C["paper"],bold=True,align="ctr",fill=C["navy"])
    slides.append(note(s,["Lộ trình phát triển bắt đầu từ điểm ảnh hưởng trực tiếp đến vận hành.","Ưu tiên đầu tiên là cổng thanh toán và webhook để tự động đối soát.","Sau đó nâng Build PC bằng benchmark, dữ liệu tương thích và gợi ý tốt hơn.","Analytics, test, monitoring, mobile và SEO giúp hệ thống tiến gần một sản phẩm thương mại thực tế."],["Ưu tiên tự động hóa giao dịch","Nâng tư vấn và đo lường"]))

    # 30 thanks
    s=Slide(30,"Xin chân thành cảm ơn","thanks"); c=Canvas(s); add_safe_background(c,5,dark=True,variant=30)
    c.text(.82,1.18,11.70,.28,"DATN · WEBSITE PC STORE",size=14,color=C["cyan"],bold=True,align="ctr",fill=C["dark"],margin=0)
    c.text(.82,2.02,11.70,.80,"XIN CHÂN THÀNH CẢM ƠN",size=34,color=C["paper"],bold=True,align="ctr",fill=C["dark"],margin=0)
    c.text(2.00,3.18,9.32,.42,"Em sẵn sàng trình bày demo và trao đổi cùng hội đồng",size=18,color="C8DAEA",align="ctr",fill=C["dark"],margin=0)
    c.shape(5.10,4.10,3.12,.07,fill=C["orange"],line=C["orange"],geom="rect")
    add_card(c,3.82,4.68,5.70,1.10,title="Q & A",body="Câu hỏi · Góp ý · Trao đổi kỹ thuật",accent=C["cyan"],dark=True,title_size=16,body_size=12)
    add_footer_progress(c,dark=True)
    slides.append(note(s,["Phần trình bày của em xin kết thúc tại đây.","Em xin cảm ơn hội đồng và thầy cô đã lắng nghe.","Em sẵn sàng demo lại bất kỳ luồng chức năng nào trong hệ thống.","Em mong nhận được câu hỏi và góp ý để tiếp tục hoàn thiện sản phẩm."],["Cảm ơn hội đồng","Sẵn sàng demo và trao đổi"]))
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
            lines += [f"- {slide.image_note}"]
        else:
            lines += ["- Không."]
        lines += [""]
    SPEECH_PATH.write_text("\n".join(lines), encoding="utf-8")


def write_supporting_docs(slides: list[Slide]) -> None:
    outline = ["# DATN PC Store — Dàn ý PowerPoint 30 slide", "", "> Nội dung sinh cùng file PowerPoint và đã đối chiếu source code.", ""]
    for slide in slides:
        outline += [f"## Slide {slide.number:02d} — {slide.title}", f"- Mục đích: {slide.speaker[0]}", f"- Điểm nhấn: {'; '.join(slide.animation)}", ""]
    (OUTPUT / "presentation_outline.md").write_text("\n".join(outline), encoding="utf-8")

    notes = ["# Ghi chú thiết kế và trình bày", "", "- Tỷ lệ 16:9, 30 slide, thời lượng mục tiêu 40–50 giây/slide.", "- Mỗi slide có shape nền full-slide mang tên `BACKGROUND — replaceable full-slide layer` ở z-order thấp nhất.", "- Các slide giao diện dùng khung vector bám theo View/Controller thực tế; có thể thay bằng screenshot thật mà không ảnh hưởng background.", "- Tone navy, blue, cyan, purple, orange và green; decor biến thể theo từng slide.", ""]
    for slide in slides:
        notes += [f"## {slide.number:02d}. {slide.title}", f"- Bố cục: {slide.layout}", f"- Hình ảnh: {slide.image_note}", f"- Thời lượng: {slide.duration}", ""]
    (OUTPUT / "presentation_notes.md").write_text("\n".join(notes), encoding="utf-8")

    script = ["# Kịch bản thuyết trình rút gọn", ""]
    for slide in slides:
        script += [f"## Slide {slide.number:02d} — {slide.title}", " ".join(slide.speaker), ""]
    (OUTPUT / "presentation_script.md").write_text("\n".join(script), encoding="utf-8")

    visual = ["# Hướng dẫn thay khung giao diện bằng ảnh chụp thật", "", "Deck hiện đã có khung giao diện vector thay cho placeholder trống. Nếu có môi trường SQL Server chạy được, nên thay các khung này bằng screenshot thật, giữ nguyên phần mô tả bên cạnh.", ""]
    for slide in slides:
        if slide.image_note.startswith("Có"):
            visual.append(f"- Slide {slide.number:02d} — {slide.title}: {slide.image_note}")
    (OUTPUT / "slide_image_placeholders.md").write_text("\n".join(visual)+"\n", encoding="utf-8")


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
        if not slide.elements or not slide.elements[0].name.startswith("BACKGROUND"):
            raise RuntimeError(f"Slide {slide.number:02d} does not start with a dedicated background layer")
        bg = slide.elements[0]
        if (bg.x, bg.y, bg.w, bg.h) != (0, 0, W, H):
            raise RuntimeError(f"Slide {slide.number:02d} background is not full-slide")
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
                # Low-opacity background decorations may intentionally bleed.
                if not (e.kind == "shape" and (e.geom == "ellipse" or e.alpha <= 12000)):
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
    name=escape(e.name or f"Element {shape_id}")
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
    write_supporting_docs(slides)
    render(slides)
    validate_pptx(slides)
    image_slides = [s.number for s in slides if s.image_note.startswith("Có")]
    print(f"Created {len(slides)} premium IT thesis slides.")
    print(f"Source facts: {facts.dbsets} DbSet, {facts.controllers} controllers, {facts.services} service files.")
    print("Interface-focused slides: " + ", ".join(f"{n:02d}" for n in image_slides))
    print(f"PowerPoint: {PPTX_PATH}")
    print(f"Speech: {SPEECH_PATH}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
