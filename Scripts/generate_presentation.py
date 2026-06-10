#!/usr/bin/env python3
"""Create a 30-slide DATN PC Store graduation-defense presentation.

The deck is generated only with OOXML shapes and text. It does not download or
embed external images, base64 data, PDFs, or videos. Screenshot placeholders are
kept intentionally so the presenter can insert real application captures later.
All product claims in this file were verified against the repository source.
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
README_PATH = OUTPUT / "README_PRESENTATION.md"

W, H, EMU = 13.333, 7.5, 914400
NAVY, BLUE, SKY = "0B3D91", "1E88E5", "E8F3FF"
BG, TEXT, MUTED = "F7FAFC", "12324A", "64748B"
AMBER, WHITE, GREEN, RED = "F59E0B", "FFFFFF", "16A085", "E45757"
BORDER, PALE, DARK = "C9D8E8", "EEF5FA", "072B66"
TOTAL = 30


@dataclass
class Shape:
    x: float; y: float; w: float; h: float
    text: str = ""; fill: str = WHITE; line: str = WHITE
    size: int = 16; color: str = TEXT; bold: bool = False
    radius: bool = False; align: str = "l"; dashed: bool = False
    valign: str = "ctr"; line_width: int = 12700


@dataclass
class Slide:
    number: int; title: str; layout: str
    shapes: list[Shape] = field(default_factory=list)
    notes: list[str] = field(default_factory=list)
    image_note: str = ""


def box(x, y, w, h, text="", **kw):
    return Shape(x, y, w, h, text, **kw)


def text(x, y, w, h, value, size=16, color=TEXT, bold=False, align="l", **kw):
    return box(x, y, w, h, value, fill=kw.pop("fill", BG), line=kw.pop("line", BG),
               size=size, color=color, bold=bold, align=align, **kw)


def title_bar(slide: Slide, eyebrow: str = "ĐỒ ÁN TỐT NGHIỆP") -> None:
    slide.shapes += [
        text(.58, .20, 2.8, .24, eyebrow, 9, BLUE, True),
        text(.58, .48, 11.9, .58, slide.title, 28, NAVY, True),
        box(.58, 1.08, 1.10, .05, fill=AMBER, line=AMBER),
        box(1.68, 1.08, 10.96, .05, fill=BORDER, line=BORDER),
    ]


def footer(slide: Slide, dark=False) -> None:
    color = "D7E8FF" if dark else MUTED
    slide.shapes += [
        text(.58, 7.08, 3.0, .18, "DATN PC Store", 9, color, True,
             fill=DARK if dark else BG, line=DARK if dark else BG),
        text(11.80, 7.08, .85, .18, f"{slide.number:02d}/{TOTAL:02d}", 9, color, True, "r",
             fill=DARK if dark else BG, line=DARK if dark else BG),
    ]


def camera_placeholder(x, y, w, h, label, url):
    return [
        box(x, y, w, h, "", fill="F3F8FD", line=BLUE, radius=True, dashed=True, line_width=19050),
        text(x+.25, y+h*.28, w-.5, .48, "▣  THÊM ẢNH", 20, BLUE, True, "ctr", fill="F3F8FD", line="F3F8FD"),
        text(x+.30, y+h*.48, w-.6, .46, label, 14, NAVY, True, "ctr", fill="F3F8FD", line="F3F8FD"),
        text(x+.30, y+h-.52, w-.6, .25, f"URL: {url}", 9, MUTED, False, "ctr", fill="F3F8FD", line="F3F8FD"),
    ]


def add_cover_slide() -> Slide:
    s = Slide(1, "DATN PC STORE", "Cover")
    s.shapes += [
        box(0, 0, W, H, fill=DARK, line=DARK), box(0, 0, .18, H, fill=AMBER, line=AMBER),
        box(9.55, -.2, 4.1, 4.1, fill=NAVY, line=NAVY, radius=True),
        box(10.35, .55, 2.45, 2.45, "PC\nSTORE", fill=BLUE, line=BLUE, size=27, color=WHITE, bold=True, radius=True, align="ctr"),
        text(.82, .70, 4.7, .28, "BÁO CÁO BẢO VỆ ĐỒ ÁN TỐT NGHIỆP", 11, "9DCCFF", True, fill=DARK, line=DARK),
        text(.82, 1.28, 8.3, .76, "WEBSITE BÁN LINH KIỆN\nMÁY TÍNH", 34, WHITE, True, fill=DARK, line=DARK),
        box(.82, 2.92, 1.35, .07, fill=AMBER, line=AMBER),
        text(.82, 3.20, 7.8, .45, "ASP.NET Core MVC • SQL Server • SignalR", 18, "CFE7FF", True, fill=DARK, line=DARK),
        box(.82, 4.18, 5.25, 1.42, "SINH VIÊN THỰC HIỆN\n[HỌ VÀ TÊN]  •  [MSSV]", fill="0D4B9E", line="2A69B8", size=16, color=WHITE, bold=True, radius=True),
        box(6.30, 4.18, 5.25, 1.42, "GIẢNG VIÊN HƯỚNG DẪN\n[HỌ VÀ TÊN GIẢNG VIÊN]", fill="123E76", line="2A69B8", size=16, color=WHITE, bold=True, radius=True),
        text(.82, 6.22, 6.0, .25, "Hà Nội • 2026", 12, "9DCCFF", False, fill=DARK, line=DARK),
    ]
    s.notes = ["Kính thưa thầy cô và hội đồng, em xin trình bày đề tài DATN PC Store.", "Đề tài xây dựng website bán linh kiện máy tính trên nền ASP.NET Core MVC.", "Hệ thống bao quát quy trình từ tra cứu sản phẩm đến đặt hàng và quản trị.", "Phần trình bày tập trung vào vấn đề, giải pháp, triển khai và kết quả đạt được."]
    footer(s, True); return s


def add_section_slide(number, title, index, subtitle) -> Slide:
    s = Slide(number, title, "Section divider")
    s.shapes += [box(0, 0, W, H, fill=NAVY, line=NAVY), box(.68, .62, 1.02, 1.02, index, fill=AMBER, line=AMBER, size=24, color=DARK, bold=True, radius=True, align="ctr"),
                 text(.78, 2.05, 11.4, .88, title.upper(), 38, WHITE, True, fill=NAVY, line=NAVY),
                 box(.78, 3.15, 1.4, .07, fill=AMBER, line=AMBER),
                 text(.78, 3.52, 10.8, .54, subtitle, 19, "D7E8FF", False, fill=NAVY, line=NAVY),
                 text(10.85, 5.30, 1.45, 1.45, "◆", 40, "7DB9F5", True, "ctr", fill="124DA0", line="2A69B8", radius=True)]
    s.notes = [f"Phần {index} trình bày {title.lower()} của đề tài.", subtitle + ".", "Các nội dung được chọn dựa trên chức năng đã triển khai trong source code.", "Sau phần này, em sẽ chuyển sang nhóm nội dung tiếp theo của bài bảo vệ."]
    footer(s, True); return s


def add_problem_slide() -> Slide:
    s = Slide(2, "Đặt vấn đề", "Problem / 3 cards"); title_bar(s, "01 • BỐI CẢNH")
    cards=[("01","NHU CẦU THỊ TRƯỜNG","Nhu cầu mua linh kiện và máy tính ngày càng tăng."),("02","TRẢI NGHIỆM KHÁCH HÀNG","Cần tra cứu, so sánh và đặt hàng nhanh."),("03","VẬN HÀNH CỬA HÀNG","Cần quản lý sản phẩm, tồn kho và đơn hàng.")]
    for i,(n,h,b) in enumerate(cards):
        x=.66+i*4.18; s.shapes += [box(x,1.52,3.80,4.55,fill=WHITE,line=BORDER,radius=True), box(x+.24,1.78,.67,.67,n,fill=BLUE,line=BLUE,size=16,color=WHITE,bold=True,radius=True,align="ctr"), text(x+.24,2.72,3.25,.48,h,18,NAVY,True,fill=WHITE,line=WHITE), text(x+.24,3.45,3.20,1.12,b,17,TEXT,False,fill=WHITE,line=WHITE), box(x+.24,5.48,1.15,.08,fill=AMBER,line=AMBER)]
    s.notes=["Thị trường linh kiện máy tính có nhiều chủng loại và thông số kỹ thuật.","Người mua cần một kênh giúp tìm, lọc và đối chiếu sản phẩm nhanh hơn.","Quy trình mua hàng cũng cần liên kết giỏ hàng, thanh toán và theo dõi đơn.","Về phía cửa hàng, dữ liệu sản phẩm và đơn hàng cần được quản lý tập trung.","Đó là bài toán thực tế mà đề tài hướng đến."]
    footer(s); return s


def add_goal_slide(number=4) -> Slide:
    s=Slide(number,"Mục tiêu đề tài","Goal / target"); title_bar(s,"01 • ĐỊNH HƯỚNG")
    s.shapes += [box(.62,1.45,4.05,4.95,"XÂY DỰNG\nNỀN TẢNG\nBÁN LINH KIỆN PC",fill=NAVY,line=NAVY,size=27,color=WHITE,bold=True,radius=True,align="ctr"), box(2.05,5.35,1.18,1.18,"◎",fill=AMBER,line=AMBER,size=34,color=NAVY,bold=True,radius=True,align="ctr")]
    goals=[("01","Tìm kiếm • lọc • so sánh"),("02","Build PC và kiểm tra tương thích"),("03","Đặt hàng • thanh toán • theo dõi"),("04","Quản trị sản phẩm • đơn • người dùng")]
    for i,(n,v) in enumerate(goals):
        y=1.48+i*1.18; s.shapes += [box(5.03,y,.72,.72,n,fill=BLUE,line=BLUE,size=14,color=WHITE,bold=True,radius=True,align="ctr"), box(5.55,y,6.62,.72,v,fill=WHITE,line=BORDER,size=17,color=TEXT,bold=True,radius=True)]
    s.notes=["Mục tiêu trung tâm là xây dựng website thương mại điện tử cho linh kiện PC.","Khách hàng có thể tìm kiếm, lọc và so sánh sản phẩm.","Module Build PC hỗ trợ chọn linh kiện, tính tổng và đưa cảnh báo tương thích.","Quy trình mua hàng bao gồm giỏ hàng, vận chuyển, thanh toán và theo dõi đơn.","Admin có các màn hình quản lý dữ liệu vận hành chính."]
    footer(s); return s


def add_usecase_slide() -> Slide:
    s=Slide(8,"Sơ đồ use case tổng quan","Use case diagram"); title_bar(s,"02 • PHÂN TÍCH HỆ THỐNG")
    # actors
    for x,label in [(.62,"KHÁCH HÀNG"),(11.25,"ADMIN")]:
        s.shapes += [box(x,2.25,1.35,1.35,"○\n╱│╲\n╱ ╲",fill=PALE,line=BLUE,size=18,color=NAVY,bold=True,radius=True,align="ctr"),text(x-.15,3.78,1.65,.3,label,12,NAVY,True,"ctr")]
    cases=[("Xem / tìm / lọc",2.38,1.42),("So sánh",4.62,1.42),("Build PC",6.86,1.42),("Giỏ hàng",9.10,1.42),("Đặt hàng",3.48,2.62),("Thanh toán",5.72,2.62),("Theo dõi đơn",7.96,2.62),("Chat hỗ trợ",4.62,3.82),("Quản lý sản phẩm",6.86,3.82),("Quản lý đơn hàng",6.86,5.02)]
    for label,x,y in cases: s.shapes.append(box(x,y,1.82,.70,label,fill=WHITE,line=BLUE,size=12,color=TEXT,bold=True,radius=True,align="ctr"))
    # visual connector rails
    s.shapes += [box(1.92,2.88,8.95,.025,fill=BORDER,line=BORDER),box(10.88,2.88,.35,.025,fill=BORDER,line=BORDER),box(6.61,2.05,.025,3.65,fill=BORDER,line=BORDER)]
    s.notes=["Sơ đồ tổng quan có hai tác nhân chính là khách hàng và quản trị viên.","Khách hàng sử dụng các chức năng tra cứu, so sánh, Build PC và mua hàng.","Khách đã đăng nhập có thể theo dõi đơn và gửi yêu cầu bảo hành.","Chat hỗ trợ kết nối khách hàng với phía quản trị qua SignalR.","Admin tập trung vào quản lý sản phẩm, đơn hàng và dữ liệu vận hành."]
    footer(s); return s


def add_architecture_slide() -> Slide:
    s=Slide(9,"Kiến trúc hệ thống","Architecture diagram"); title_bar(s,"02 • KIẾN TRÚC")
    flow=[("CLIENT\nBROWSER",.55,2.28),("MVC\nCONTROLLER",3.00,2.28),("SERVICE\nLAYER",5.45,2.28),("ENTITY\nFRAMEWORK",7.90,2.28),("SQL\nSERVER",10.35,2.28)]
    for i,(v,x,y) in enumerate(flow):
        s.shapes.append(box(x,y,1.93,1.12,v,fill=NAVY if i in (0,4) else WHITE,line=BLUE,size=15,color=WHITE if i in (0,4) else NAVY,bold=True,radius=True,align="ctr"))
        if i<4: s.shapes += [box(x+1.94,2.80,.48,.06,fill=AMBER,line=AMBER),text(x+2.05,2.58,.25,.35,"›",25,AMBER,True,"ctr")]
    branches=[("SIGNALR HUB","Chat thời gian thực",1.15),("GHN / ROUTE","Địa chỉ và phí giao",3.82),("SMTP EMAIL","OTP quên mật khẩu",6.49),("QR BANK","Chuyển khoản đơn hàng",9.16)]
    for h,b,x in branches: s.shapes += [box(x,4.45,2.25,1.10,f"{h}\n{b}",fill=SKY,line=BORDER,size=13,color=NAVY,bold=True,radius=True,align="ctr"),box(x+1.10,3.42,.025,1.02,fill=BORDER,line=BORDER)]
    s.notes=["Hệ thống sử dụng kiến trúc MVC truyền thống của ASP.NET Core.","Request từ trình duyệt đi qua Controller rồi đến các service nghiệp vụ.","Entity Framework Core đảm nhiệm truy cập cơ sở dữ liệu SQL Server.","SignalR phục vụ chat hỗ trợ theo thời gian thực.","Các nhánh tích hợp thực tế gồm GHN, dịch vụ tuyến đường, SMTP và QR chuyển khoản."]
    footer(s); return s


def add_database_slide(number=11) -> Slide:
    s=Slide(number,"Cơ sở dữ liệu","Database groups"); title_bar(s,"02 • DỮ LIỆU")
    groups=[("SẢN PHẨM","Category\nProduct\nProductImage\nBanner",BLUE),("TÀI KHOẢN","Role\nUser\nPasswordResetOtp",GREEN),("MUA HÀNG","Cart • CartItem\nOrder • OrderDetail",AMBER),("DỊCH VỤ","Warranty • Request\nBuildPcConfig • Item",NAVY),("NỘI DUNG & HỖ TRỢ","Article • Feedback\nSiteSetting • Chat",RED)]
    positions=[(.60,1.48,3.78,2.05),(4.60,1.48,3.78,2.05),(8.60,1.48,3.78,2.05),(2.60,3.85,3.78,2.05),(6.60,3.85,3.78,2.05)]
    for (h,b,c),(x,y,w,hg) in zip(groups,positions): s.shapes += [box(x,y,w,hg,fill=WHITE,line=BORDER,radius=True),box(x,y,w,.48,h,fill=c,line=c,size=14,color=WHITE,bold=True,radius=True,align="ctr"),text(x+.25,y+.72,w-.5,1.08,b,15,TEXT,True,"ctr",fill=WHITE,line=WHITE)]
    s.notes=["ApplicationDbContext khai báo 22 DbSet tương ứng 22 bảng dữ liệu chính.","Nhóm sản phẩm quản lý danh mục, sản phẩm, ảnh và banner.","Nhóm mua hàng gồm giỏ hàng, đơn hàng và chi tiết đơn.","Build PC, bảo hành, vận chuyển và cấu hình website được lưu thành các thực thể riêng.","Chat hỗ trợ sử dụng hai bảng Conversation và Message."]
    footer(s); return s


def add_relationship_slide() -> Slide:
    s=Slide(12,"Quan hệ dữ liệu chính","Database relationship diagram"); title_bar(s,"02 • MÔ HÌNH DỮ LIỆU")
    nodes=[("USER",.55,1.58),("ORDER",3.06,1.58),("ORDER DETAIL",5.57,1.58),("PRODUCT",8.08,1.58),("CATEGORY",10.59,1.58),("CART",1.80,3.28),("CART ITEM",4.31,3.28),("WARRANTY",6.82,3.28),("BUILD PC",9.33,3.28),("CHAT",5.57,5.00)]
    for label,x,y in nodes: s.shapes.append(box(x,y,2.05,.80,label,fill=NAVY if label in ("USER","PRODUCT") else WHITE,line=BLUE,size=13,color=WHITE if label in ("USER","PRODUCT") else NAVY,bold=True,radius=True,align="ctr"))
    # relation lines/arrows as clean rails
    for x,y,w,h in [(2.60,1.96,.43,.04),(5.11,1.96,.43,.04),(7.62,1.96,.43,.04),(10.13,1.96,.43,.04),(2.78,2.38,.04,.89),(5.29,2.38,.04,.89),(8.00,2.38,.04,.89),(10.35,2.38,.04,.89),(6.58,4.08,.04,.90)]: s.shapes.append(box(x,y,w,h,fill=AMBER,line=AMBER))
    s.shapes += [text(.72,6.14,11.7,.28,"1 ─ N: User–Order • Order–Detail • Category–Product • Cart–Item • Conversation–Message",12,MUTED,True,"ctr")]
    s.notes=["Quan hệ cốt lõi bắt đầu từ User tạo Order và Order chứa nhiều OrderDetail.","Mỗi chi tiết đơn liên kết đến một Product.","Product thuộc Category và có thể xuất hiện trong CartItem, Warranty hoặc BuildPcItem.","Giỏ hàng được lưu cho người dùng đăng nhập; khách vãng lai dùng session.","ChatConversation liên kết nhiều ChatMessage để giữ lịch sử hỗ trợ."]
    footer(s); return s


def add_showcase_slide(number,title,bullets,label,url,side="right") -> Slide:
    s=Slide(number,title,"Showcase image"); title_bar(s,"03 • GIAO DIỆN & NGHIỆP VỤ")
    if side=="right": tx,px=.68,4.55; pw=8.08
    else: px,tx=.68,8.98; pw=8.00
    s.shapes += camera_placeholder(px,1.45,pw,4.95,label,url)
    for i,b in enumerate(bullets):
        y=1.58+i*1.02; s.shapes += [box(tx,y,.46,.46,str(i+1),fill=AMBER,line=AMBER,size=12,color=NAVY,bold=True,radius=True,align="ctr"),text(tx+.62,y-.03,3.35,.65,b,16,TEXT,True,fill=BG,line=BG)]
    s.notes=[f"Slide này minh họa màn hình {title.lower()} của hệ thống.",f"Source code xác nhận các nội dung chính gồm: {', '.join(bullets).lower()}.","Khung bên cạnh được dành để chèn ảnh chụp giao diện thật khi chạy ứng dụng.","Cách bố trí nhấn mạnh kết quả triển khai thay vì trình bày quá nhiều chữ."]
    s.image_note=f"{label} — chụp tại {url}"; footer(s); return s


def add_timeline_slide(number,title,steps,note,image=None) -> Slide:
    s=Slide(number,title,"Timeline"); title_bar(s,"03 • QUY TRÌNH NGHIỆP VỤ")
    y=2.40; s.shapes.append(box(.92,y+.40,11.38,.07,fill=BORDER,line=BORDER))
    gap=11.0/(len(steps)-1)
    for i,st in enumerate(steps):
        x=.92+i*gap; s.shapes += [box(x,y,.86,.86,str(i+1),fill=BLUE if i<len(steps)-1 else AMBER,line=BLUE if i<len(steps)-1 else AMBER,size=18,color=WHITE if i<len(steps)-1 else NAVY,bold=True,radius=True,align="ctr"),text(x-.42,y+1.17,1.72,.78,st,14,NAVY,True,"ctr")]
    if image: s.shapes += camera_placeholder(2.15,4.72,9.02,1.35,image[0],image[1])
    else: s.shapes += [box(2.05,4.78,9.20,.78,note,fill=SKY,line=BORDER,size=15,color=NAVY,bold=True,radius=True,align="ctr")]
    s.notes=[f"Quy trình {title.lower()} được mô tả theo thứ tự từ trái sang phải.","Mỗi bước tương ứng với một trạng thái hoặc thao tác có trong luồng xử lý thực tế.",note + ".","Timeline giúp hội đồng theo dõi luồng nghiệp vụ mà không cần đọc nhiều bullet."]
    if image: s.image_note=f"{image[0]} — chụp tại {image[1]}"
    footer(s); return s


def add_admin_slide() -> Slide:
    s=Slide(25,"Trang quản trị","Admin dashboard"); title_bar(s,"04 • QUẢN TRỊ HỆ THỐNG")
    s.shapes += camera_placeholder(.62,1.42,8.25,5.05,"DASHBOARD QUẢN TRỊ","/AdminDashboard")
    items=["Dashboard thống kê","Sản phẩm & danh mục","Đơn hàng","Người dùng","Banner & cài đặt","Chat & bảo hành"]
    for i,v in enumerate(items):
        x=9.18+(i%2)*1.72; y=1.45+(i//2)*1.56
        s.shapes.append(box(x,y,1.48,1.23,v,fill=NAVY if i==0 else WHITE,line=BLUE,size=12,color=WHITE if i==0 else NAVY,bold=True,radius=True,align="ctr"))
    s.notes=["Khu vực quản trị được bảo vệ bằng quyền Admin.","Dashboard hiển thị số sản phẩm, đơn hàng, người dùng và yêu cầu bảo hành.","Menu quản trị còn có danh mục, banner, cài đặt website và chat hỗ trợ.","Các module phản ánh trực tiếp các controller và Razor View trong source."]
    s.image_note="Dashboard quản trị — chụp tại /AdminDashboard"; footer(s); return s


def add_stats_slide() -> Slide:
    s=Slide(27,"Kết quả đạt được","Stats cards"); title_bar(s,"05 • KẾT QUẢ")
    stats=[("22","DBSET / BẢNG","ApplicationDbContext"),("20","CONTROLLER","Khách hàng + Admin"),("4","NHÓM NGƯỜI DÙNG","Guest • Customer • Staff • Admin"),("8","NHÓM CÔNG NGHỆ","Web • Data • Integration"),]
    for i,(n,h,b) in enumerate(stats):
        x=.68+i*3.13; s.shapes += [box(x,1.55,2.80,3.40,fill=WHITE,line=BORDER,radius=True),text(x+.18,1.90,2.44,.88,n,34,BLUE,True,"ctr",fill=WHITE,line=WHITE),text(x+.18,2.94,2.44,.48,h,14,NAVY,True,"ctr",fill=WHITE,line=WHITE),box(x+.72,3.56,1.36,.06,fill=AMBER,line=AMBER),text(x+.20,3.88,2.40,.62,b,12,MUTED,False,"ctr",fill=WHITE,line=WHITE)]
    s.shapes += [box(2.20,5.48,8.92,.68,"MVC • EF Core • SQL Server • JavaScript • SignalR • SMTP • GHN • QR",fill=NAVY,line=NAVY,size=15,color=WHITE,bold=True,radius=True,align="ctr")]
    s.notes=["Kết quả được lượng hóa trực tiếp từ cấu trúc source code.","ApplicationDbContext hiện khai báo 22 DbSet và thư mục Controllers có 20 controller.","Hệ thống phục vụ khách vãng lai cùng ba vai trò được seed: Customer, Staff và Admin.","Tám nhóm công nghệ chính đã được áp dụng xuyên suốt ứng dụng.","Các con số này không dựa trên dữ liệu giả lập hay ước lượng ngoài source."]
    footer(s); return s


def add_thank_you_slide() -> Slide:
    s=Slide(30,"Xin chân thành cảm ơn","Thank you")
    s.shapes += [box(0,0,W,H,fill=DARK,line=DARK),box(.68,.72,11.98,5.85,fill=NAVY,line="2A69B8",radius=True),text(1.18,1.38,10.98,.32,"DATN PC STORE • ASP.NET CORE MVC",12,"9DCCFF",True,"ctr",fill=NAVY,line=NAVY),text(1.18,2.15,10.98,1.08,"XIN CHÂN THÀNH\nCẢM ƠN",40,WHITE,True,"ctr",fill=NAVY,line=NAVY),box(5.72,3.62,1.88,.08,fill=AMBER,line=AMBER),text(2.05,4.08,9.20,.66,"Em xin chân thành cảm ơn thầy/cô\nvà hội đồng đã lắng nghe.",19,"D7E8FF",False,"ctr",fill=NAVY,line=NAVY),box(5.72,5.28,1.88,.62,"Q & A",fill=AMBER,line=AMBER,size=17,color=NAVY,bold=True,radius=True,align="ctr")]
    s.notes=["Phần trình bày của em xin được kết thúc tại đây.","Em xin chân thành cảm ơn giảng viên hướng dẫn và hội đồng đã lắng nghe.","Em mong nhận được các góp ý để tiếp tục hoàn thiện hệ thống.","Em xin phép được trả lời các câu hỏi của thầy cô."]
    footer(s,True); return s


def simple_cards(number,title,cards,layout="3 cards",eyebrow="01 • TỔNG QUAN") -> Slide:
    s=Slide(number,title,layout); title_bar(s,eyebrow)
    cols=3 if len(cards)<=6 else 4; rows=(len(cards)+cols-1)//cols
    cw=3.82 if cols==3 else 2.86; gap=.30; start=.66
    ch=2.0 if rows==2 else 3.9
    for i,(h,b,c) in enumerate(cards):
        r,cidx=divmod(i,cols); x=start+cidx*(cw+gap); y=1.48+r*(ch+.28)
        s.shapes += [box(x,y,cw,ch,fill=WHITE,line=BORDER,radius=True),box(x+.22,y+.22,.56,.56,"◆",fill=c,line=c,size=13,color=WHITE,bold=True,radius=True,align="ctr"),text(x+.92,y+.24,cw-1.15,.38,h,16,NAVY,True,fill=WHITE,line=WHITE),text(x+.24,y+.92,cw-.48,ch-1.12,b,14,TEXT,False,fill=WHITE,line=WHITE)]
    s.notes=[f"Slide này trình bày {title.lower()} theo các nhóm nội dung chính.","Các nhóm được rút gọn để tránh danh sách bullet dài.","Mỗi nội dung đều đã được đối chiếu với controller, model, service hoặc view tương ứng.","Cách trình bày dạng thẻ giúp phân biệt rõ vai trò của từng nhóm."]
    footer(s); return s


def comparison_slide() -> Slide:
    s=Slide(22,"So sánh sản phẩm","Comparison table"); title_bar(s,"03 • TÍNH NĂNG HỖ TRỢ")
    headers=[("TIÊU CHÍ",.62,2.15),("SẢN PHẨM A",3.09,4.33),("SẢN PHẨM B",7.56,4.33)]
    for v,x,w in headers:s.shapes.append(box(x,1.50,w,.62,v,fill=NAVY,line=NAVY,size=14,color=WHITE,bold=True,radius=True,align="ctr"))
    rows=["Giá bán","CPU / Socket","RAM / Chuẩn DDR","GPU / Bộ nhớ","Khuyến mãi"]
    for i,r in enumerate(rows):
        y=2.18+i*.72; s.shapes += [box(.62,y,2.15,.63,r,fill=SKY,line=BORDER,size=12,color=NAVY,bold=True,radius=True,align="ctr"),box(3.09,y,4.33,.63,"Dữ liệu sản phẩm thực tế",fill=WHITE,line=BORDER,size=12,color=MUTED,radius=True,align="ctr"),box(7.56,y,4.33,.63,"Dữ liệu sản phẩm thực tế",fill=WHITE,line=BORDER,size=12,color=MUTED,radius=True,align="ctr")]
    s.shapes += camera_placeholder(3.09,5.94,8.80,.58,"ẢNH SO SÁNH SẢN PHẨM","/Compare")
    s.notes=["Hệ thống cho phép lưu tối đa hai sản phẩm trong session để so sánh.","Màn hình so sánh tổng hợp giá và các hàng thông số kỹ thuật.","ViewModel hỗ trợ các dòng CPU, RAM, GPU, SSD, mainboard, nguồn, case và tản nhiệt.","Bảng trên minh họa cách đối chiếu hai sản phẩm theo cùng tiêu chí."]
    s.image_note="Màn hình so sánh — chụp tại /Compare"; footer(s); return s


def two_column_slide(number,title,left,right,placeholder=None) -> Slide:
    s=Slide(number,title,"2 columns"); title_bar(s,"04 • QUẢN TRỊ / ĐÁNH GIÁ")
    for idx,(head,items,color) in enumerate([(left[0],left[1],BLUE),(right[0],right[1],AMBER)]):
        x=.68+idx*6.18; s.shapes += [box(x,1.48,5.78,4.90,fill=WHITE,line=BORDER,radius=True),box(x,1.48,5.78,.68,head,fill=color,line=color,size=17,color=WHITE if idx==0 else NAVY,bold=True,radius=True,align="ctr")]
        for i,item in enumerate(items): s.shapes += [box(x+.30,2.42+i*.76,.38,.38,"✓" if idx==0 else "→",fill=PALE,line=PALE,size=12,color=color,bold=True,radius=True,align="ctr"),text(x+.82,2.34+i*.76,4.50,.55,item,14,TEXT,True,fill=WHITE,line=WHITE)]
    if placeholder: s.image_note=placeholder
    s.notes=[f"Nội dung {title.lower()} được chia thành hai nhóm để dễ đối chiếu.",f"Cột bên trái tập trung vào {left[0].lower()}.",f"Cột bên phải tập trung vào {right[0].lower()}.","Các nhận định được giữ ở mức thực tế và phù hợp với phạm vi đồ án."]
    footer(s); return s


def build_slides() -> list[Slide]:
    slides=[add_cover_slide(),add_problem_slide()]
    slides.append(simple_cards(3,"Lý do chọn đề tài",[("THỰC TIỄN","Gắn với nhu cầu mua linh kiện và vận hành cửa hàng.",BLUE),("PHÙ HỢP CHUYÊN MÔN","Vận dụng ASP.NET Core MVC, EF Core và SQL Server.",GREEN),("ĐỦ NGHIỆP VỤ","Có luồng khách hàng, hỗ trợ và quản trị.",AMBER)]))
    slides.append(add_goal_slide())
    slides.append(simple_cards(5,"Đối tượng sử dụng",[("KHÁCH VÃNG LAI","Xem, tìm, lọc, so sánh và dùng giỏ session.",BLUE),("KHÁCH HÀNG","Đặt hàng, theo dõi đơn, hồ sơ và bảo hành.",GREEN),("QUẢN TRỊ VIÊN","Quản lý dữ liệu và vận hành hệ thống.",AMBER),("NHÂN VIÊN HỖ TRỢ","Tiếp nhận hội thoại tại màn hình Admin Chat.",RED)],"4 cards"))
    scope = add_section_slide(6,"Phạm vi hệ thống","02","Website thương mại điện tử cho PC và linh kiện; chỉ trình bày module đã triển khai.")
    scope.shapes += [box(.78,4.42,3.45,.72,"KHÁCH HÀNG & MUA HÀNG",fill="124DA0",line="2A69B8",size=13,color=WHITE,bold=True,radius=True,align="ctr"),box(4.52,4.42,3.45,.72,"QUẢN TRỊ & HỖ TRỢ",fill="124DA0",line="2A69B8",size=13,color=WHITE,bold=True,radius=True,align="ctr"),box(8.26,4.42,3.45,.72,"GHN • SMTP • SIGNALR • QR",fill="124DA0",line="2A69B8",size=13,color=WHITE,bold=True,radius=True,align="ctr")]
    scope.notes = ["Phạm vi đề tài là website thương mại điện tử cho PC và linh kiện.","Nội dung bao gồm luồng khách hàng, quản trị và hỗ trợ sau bán hàng.","Các tích hợp được trình bày chỉ khi có đăng ký service hoặc luồng xử lý trong source.","Đề tài không tuyên bố các chức năng chưa được triển khai."]
    slides.append(scope)
    slides.append(two_column_slide(7,"Yêu cầu chức năng",("KHÁCH HÀNG",["Tìm kiếm, lọc và xem sản phẩm","So sánh tối đa hai sản phẩm","Build PC và thêm cấu hình vào giỏ","Đặt hàng, thanh toán, theo dõi","Chat hỗ trợ và yêu cầu bảo hành"]),("QUẢN TRỊ VIÊN",["Quản lý sản phẩm và danh mục","Quản lý đơn, xác nhận chuyển khoản","Quản lý người dùng và banner","Xử lý bảo hành, chat hỗ trợ","Cấu hình website và vận chuyển"])))
    slides += [add_usecase_slide(),add_architecture_slide()]
    tech=[("ASP.NET CORE MVC","Nền tảng web .NET 8",NAVY),("RAZOR VIEW","Render giao diện phía server",BLUE),("EF CORE","ORM truy cập dữ liệu",GREEN),("SQL SERVER","Cơ sở dữ liệu quan hệ",AMBER),("BOOTSTRAP / CSS","Thiết kế giao diện responsive",BLUE),("JAVASCRIPT","Tương tác giỏ, checkout, Build PC",GREEN),("SIGNALR","Chat hỗ trợ thời gian thực",RED),("SMTP • GHN • QR","Email, vận chuyển, chuyển khoản",AMBER)]
    slides.append(simple_cards(10,"Công nghệ sử dụng",tech,"8 technology cards","02 • CÔNG NGHỆ"))
    slides += [add_database_slide(),add_relationship_slide()]
    modules=[("SẢN PHẨM","Danh mục, tìm kiếm, lọc, chi tiết",BLUE),("TÀI KHOẢN","Đăng ký, đăng nhập, OTP, hồ sơ",GREEN),("GIỎ HÀNG","Session và database",AMBER),("ĐƠN HÀNG","Checkout, trạng thái, theo dõi",NAVY),("THANH TOÁN / SHIP","COD, chuyển khoản, GHN",RED),("QUẢN TRỊ","Sản phẩm, đơn, user, banner",BLUE)]
    slides.append(simple_cards(13,"Các module chính",modules,"Infographic 6 modules","03 • TRIỂN KHAI"))
    slides.append(add_showcase_slide(14,"Trang chủ",["Banner và nhóm sản phẩm","Điều hướng danh mục nhanh","Tìm kiếm toàn site"],"TRANG CHỦ","/Home/Index","right"))
    slides.append(add_showcase_slide(15,"Danh sách sản phẩm",["Tìm theo từ khóa","Lọc danh mục và khoảng giá","Sắp xếp kết quả"],"DANH SÁCH SẢN PHẨM","/Products","left"))
    slides.append(add_showcase_slide(16,"Chi tiết sản phẩm",["Giá và khuyến mãi","Ảnh, mô tả, thông số","Thêm giỏ và so sánh"],"CHI TIẾT SẢN PHẨM","/Products/Detail/{id}","right"))
    slides.append(add_timeline_slide(17,"Giỏ hàng",["Thêm sản phẩm","Cập nhật số lượng","Tính tổng tiền","Tiến hành đặt hàng"],"Giỏ session cho khách; giỏ database cho tài khoản",("ẢNH GIỎ HÀNG","/Cart")))
    slides[-1].layout = "Process flow"
    slides.append(add_timeline_slide(18,"Quy trình đặt hàng",["Thông tin nhận hàng","Chọn vận chuyển","Chọn thanh toán","Tạo đơn","Theo dõi trạng thái"],"GHN cung cấp địa chỉ và phí giao hàng khi cấu hình hợp lệ"))
    slides.append(two_column_slide(19,"Thanh toán QR / chuyển khoản",("THÔNG TIN ĐƠN HÀNG",["Mã đơn và tổng thanh toán","Nội dung chuyển khoản tự sinh","Thời hạn thanh toán hai giờ"]),("QR & TRẠNG THÁI",["QR ngân hàng từ cấu hình site","Khách xác nhận đã chuyển khoản","Admin xác nhận giao dịch"]),"Thanh toán chuyển khoản — /Orders/BankTransfer/{id}"))
    slides[-1].shapes += camera_placeholder(8.10,5.48,3.95,.70,"ẢNH THANH TOÁN QR","/Orders/BankTransfer/{id}")
    slides.append(add_timeline_slide(20,"Theo dõi đơn hàng",["Chờ thanh toán","Chờ xác nhận","Đang xử lý","Đang giao","Hoàn thành"],"Trạng thái thật: PendingPayment → PendingConfirmation → Processing → Delivering → Completed",("ẢNH THEO DÕI ĐƠN","/Orders/Tracking/{id}")))
    slides.append(add_showcase_slide(21,"Build PC",["Chọn linh kiện theo nhóm","Cảnh báo socket, DDR, PSU","Tính tổng và thêm cả bộ vào giỏ"],"BUILD PC","/BuildPc","left"))
    slides.append(comparison_slide())
    slides.append(add_showcase_slide(23,"Dịch vụ sau bán hàng",["Gửi yêu cầu bảo hành","Theo dõi trạng thái xử lý","Xuất báo giá đơn hàng"],"BẢO HÀNH / BÁO GIÁ","/Warranty • /Orders/Quotation","right"))
    s=Slide(24,"Chat hỗ trợ SignalR","SignalR support diagram"); title_bar(s,"03 • HỖ TRỢ KHÁCH HÀNG")
    s.shapes += [box(.72,2.02,2.45,1.12,"USER / GUEST",fill=NAVY,line=NAVY,size=17,color=WHITE,bold=True,radius=True,align="ctr"),box(5.04,2.02,3.20,1.12,"SIGNALR\nCHAT HUB",fill=BLUE,line=BLUE,size=17,color=WHITE,bold=True,radius=True,align="ctr"),box(10.15,2.02,2.45,1.12,"ADMIN / SUPPORT",fill=NAVY,line=NAVY,size=17,color=WHITE,bold=True,radius=True,align="ctr"),box(3.20,2.54,1.80,.07,fill=AMBER,line=AMBER),box(8.28,2.54,1.83,.07,fill=AMBER,line=AMBER),text(3.45,2.16,1.32,.28,"↔",22,AMBER,True,"ctr"),text(8.55,2.16,1.32,.28,"↔",22,AMBER,True,"ctr")]
    s.shapes += camera_placeholder(2.15,4.08,9.02,1.82,"CHAT HỖ TRỢ","widget toàn site • /AdminChat")
    s.notes=["Chat hỗ trợ được triển khai bằng SignalR và ChatHub.","Khách vãng lai hoặc người dùng đăng nhập đều có thể mở hội thoại.","Tin nhắn được lưu vào ChatConversation và ChatMessage.","Admin xem danh sách hội thoại, phản hồi, đánh dấu đã đọc và đóng phiên chat."]
    s.image_note="Chat widget và Admin Chat — widget toàn site, /AdminChat"; footer(s); slides.append(s)
    slides.append(add_admin_slide())
    slides.append(two_column_slide(26,"Quản lý sản phẩm và đơn hàng",("QUẢN LÝ SẢN PHẨM",["Tạo, sửa, xóa và tìm kiếm","Ảnh sản phẩm và thông số","Giá, tồn kho và khuyến mãi"]),("QUẢN LÝ ĐƠN HÀNG",["Xem danh sách và chi tiết","Cập nhật trạng thái đơn","Xác nhận chuyển khoản"]),"Ảnh /AdminProducts và /AdminOrders"))
    slides[-1].shapes += camera_placeholder(2.02,5.45,9.30,.78,"QUẢN LÝ SẢN PHẨM / ĐƠN HÀNG","/AdminProducts • /AdminOrders")
    slides.append(add_stats_slide())
    slides.append(two_column_slide(28,"Hạn chế",("HIỆN TẠI",["Dữ liệu mẫu cần thay bằng dữ liệu thật","Chưa thấy dự án kiểm thử tự động","QR là chuyển khoản, chưa đối soát tự động","Cần kiểm thử thêm trên nhiều thiết bị"]),("CẦN CẢI THIỆN",["Chuẩn hóa dữ liệu vận hành","Bổ sung unit và integration test","Tích hợp webhook thanh toán","Tối ưu mobile và accessibility"])))
    slides.append(add_timeline_slide(29,"Hướng phát triển",["Thanh toán tự động","Gợi ý cấu hình PC","Báo cáo quản trị","SEO & hiệu năng","Chăm sóc khách hàng"],"Roadmap ưu tiên tự động hóa, thông minh hóa và mở rộng vận hành"))
    slides[-1].layout = "Roadmap"
    slides.append(add_thank_you_slide())
    assert [s.number for s in slides] == list(range(1,31))
    return slides


def emu(v): return int(v*EMU)


def run_xml(value,size,color,bold=False):
    b=' b="1"' if bold else ''
    return f'<a:r><a:rPr lang="vi-VN" sz="{size*100}"{b} dirty="0"><a:solidFill><a:srgbClr val="{color}"/></a:solidFill><a:latin typeface="Aptos"/><a:ea typeface="Arial"/></a:rPr><a:t>{escape(value)}</a:t></a:r>'


def shape_xml(sh: Shape, sid: int) -> str:
    geom="roundRect" if sh.radius else "rect"; dash='<a:prstDash val="dash"/>' if sh.dashed else ''
    paragraphs=[]
    for part in sh.text.split("\n"):
        paragraphs.append(f'<a:p><a:pPr algn="{sh.align}" marL="0" indent="0"/>{run_xml(part,sh.size,sh.color,sh.bold)}<a:endParaRPr lang="vi-VN" sz="{sh.size*100}"/></a:p>')
    tx='' if not sh.text else '<p:txBody><a:bodyPr wrap="square" lIns="109728" tIns="54864" rIns="109728" bIns="54864" anchor="'+sh.valign+'"/><a:lstStyle/>'+''.join(paragraphs)+'</p:txBody>'
    return f'<p:sp><p:nvSpPr><p:cNvPr id="{sid}" name="Shape {sid}"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x="{emu(sh.x)}" y="{emu(sh.y)}"/><a:ext cx="{emu(sh.w)}" cy="{emu(sh.h)}"/></a:xfrm><a:prstGeom prst="{geom}"><a:avLst/></a:prstGeom><a:solidFill><a:srgbClr val="{sh.fill}"/></a:solidFill><a:ln w="{sh.line_width}"><a:solidFill><a:srgbClr val="{sh.line}"/></a:solidFill>{dash}</a:ln></p:spPr>{tx}</p:sp>'


def slide_xml(slide: Slide) -> str:
    shapes=[shape_xml(box(0,0,W,H,fill=BG,line=BG),2)]
    shapes += [shape_xml(sh,i+3) for i,sh in enumerate(slide.shapes)]
    return '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"><p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>'+''.join(shapes)+'</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sld>'


def render(slides):
    count=len(slides)
    overrides=''.join(f'<Override PartName="/ppt/slides/slide{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slide+xml"/>' for i in range(1,count+1))
    content=f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/ppt/presentation.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml"/><Override PartName="/ppt/slideMasters/slideMaster1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml"/><Override PartName="/ppt/slideLayouts/slideLayout1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/><Override PartName="/ppt/theme/theme1.xml" ContentType="application/vnd.openxmlformats-officedocument.theme+xml"/><Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/><Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>{overrides}</Types>'''
    rootrels='''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="ppt/presentation.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/><Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/></Relationships>'''
    ids=''.join(f'<p:sldId id="{255+i}" r:id="rId{i+2}"/>' for i in range(1,count+1))
    presentation=f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><p:presentation xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"><p:sldMasterIdLst><p:sldMasterId id="2147483648" r:id="rId1"/></p:sldMasterIdLst><p:sldIdLst>{ids}</p:sldIdLst><p:sldSz cx="{emu(W)}" cy="{emu(H)}" type="screen16x9"/><p:notesSz cx="6858000" cy="9144000"/></p:presentation>'''
    rels=['<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster" Target="slideMasters/slideMaster1.xml"/>']+[f'<Relationship Id="rId{i+2}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide" Target="slides/slide{i}.xml"/>' for i in range(1,count+1)]
    presrels='<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'+''.join(rels)+'</Relationships>'
    master='''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><p:sldMaster xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"><p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr></p:spTree></p:cSld><p:clrMap accent1="accent1" accent2="accent2" accent3="accent3" accent4="accent4" accent5="accent5" accent6="accent6" bg1="lt1" bg2="lt2" folHlink="folHlink" hlink="hlink" tx1="dk1" tx2="dk2"/><p:sldLayoutIdLst><p:sldLayoutId id="1" r:id="rId1"/></p:sldLayoutIdLst></p:sldMaster>'''
    masterrels='''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme" Target="../theme/theme1.xml"/></Relationships>'''
    layout='''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><p:sldLayout xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main" type="blank"><p:cSld name="Blank"><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr></p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sldLayout>'''
    layoutrels='''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster" Target="../slideMasters/slideMaster1.xml"/></Relationships>'''
    theme=f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="DATN PC Store"><a:themeElements><a:clrScheme name="DATN"><a:dk1><a:srgbClr val="{TEXT}"/></a:dk1><a:lt1><a:srgbClr val="{WHITE}"/></a:lt1><a:dk2><a:srgbClr val="{NAVY}"/></a:dk2><a:lt2><a:srgbClr val="{BG}"/></a:lt2><a:accent1><a:srgbClr val="{BLUE}"/></a:accent1><a:accent2><a:srgbClr val="{AMBER}"/></a:accent2><a:accent3><a:srgbClr val="{GREEN}"/></a:accent3><a:accent4><a:srgbClr val="{RED}"/></a:accent4><a:accent5><a:srgbClr val="{BORDER}"/></a:accent5><a:accent6><a:srgbClr val="{SKY}"/></a:accent6><a:hlink><a:srgbClr val="{BLUE}"/></a:hlink><a:folHlink><a:srgbClr val="{NAVY}"/></a:folHlink></a:clrScheme><a:fontScheme name="Aptos"><a:majorFont><a:latin typeface="Aptos Display"/></a:majorFont><a:minorFont><a:latin typeface="Aptos"/></a:minorFont></a:fontScheme><a:fmtScheme name="DATN"><a:fillStyleLst><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:fillStyleLst><a:lnStyleLst><a:ln w="12700"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln></a:lnStyleLst><a:effectStyleLst><a:effectStyle><a:effectLst/></a:effectStyle></a:effectStyleLst><a:bgFillStyleLst><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:bgFillStyleLst></a:fmtScheme></a:themeElements></a:theme>'''
    core='''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/"><dc:title>DATN PC Store - Bảo vệ đồ án tốt nghiệp</dc:title><dc:creator>DATN PC Store</dc:creator><dc:description>30 slide tạo từ source code đã xác minh.</dc:description></cp:coreProperties>'''
    app=f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties"><Application>Microsoft Office PowerPoint</Application><PresentationFormat>Widescreen</PresentationFormat><Slides>{count}</Slides><Notes>0</Notes></Properties>'''
    sliderel='''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout1.xml"/></Relationships>'''
    OUTPUT.mkdir(exist_ok=True)
    with zipfile.ZipFile(PPTX_PATH,"w",zipfile.ZIP_DEFLATED) as z:
        files={"[Content_Types].xml":content,"_rels/.rels":rootrels,"ppt/presentation.xml":presentation,"ppt/_rels/presentation.xml.rels":presrels,"ppt/slideMasters/slideMaster1.xml":master,"ppt/slideMasters/_rels/slideMaster1.xml.rels":masterrels,"ppt/slideLayouts/slideLayout1.xml":layout,"ppt/slideLayouts/_rels/slideLayout1.xml.rels":layoutrels,"ppt/theme/theme1.xml":theme,"docProps/core.xml":core,"docProps/app.xml":app}
        for p,v in files.items():z.writestr(p,v)
        for s in slides:
            z.writestr(f"ppt/slides/slide{s.number}.xml",slide_xml(s));z.writestr(f"ppt/slides/_rels/slide{s.number}.xml.rels",sliderel)


def write_notes(slides):
    lines=["# Ghi chú thuyết trình — DATN PC Store","","> Nội dung được đối chiếu với source code ngày 10/06/2026. Mỗi slide có lời thuyết trình ngắn và ghi chú ảnh thực tế cần bổ sung.",""]
    for s in slides:
        lines += [f"## Slide {s.number:02d} — {s.title}",""]+[f"{i+1}. {n}" for i,n in enumerate(s.notes)]+[""]
        lines += [f"**Ghi chú ảnh:** {s.image_note or 'Không cần chèn ảnh.'}",""]
    NOTES_PATH.write_text("\n".join(lines),encoding="utf-8")


def validate(slides):
    if len(slides)!=30: raise RuntimeError("Deck phải có đúng 30 slide")
    if any(len(s.notes)<4 or len(s.notes)>7 for s in slides): raise RuntimeError("Mỗi slide cần 4-7 câu notes")
    with zipfile.ZipFile(PPTX_PATH) as z:
        if z.testzip(): raise RuntimeError("PPTX ZIP lỗi")
        generated=[n for n in z.namelist() if re.fullmatch(r"ppt/slides/slide\d+\.xml",n)]
        if len(generated)!=30: raise RuntimeError(f"PPTX chỉ có {len(generated)} slide")
        xml="\n".join(z.read(n).decode() for n in generated)
        required=["Đặt vấn đề","Build PC","SIGNALR","22","XIN CHÂN THÀNH"]
        missing=[x for x in required if escape(x) not in xml]
        if missing: raise RuntimeError("Thiếu nội dung: "+", ".join(missing))
    if PPTX_PATH.stat().st_size<30000: raise RuntimeError("PPTX nhỏ bất thường")


def main():
    slides=build_slides(); write_notes(slides); render(slides); validate(slides)
    print(f"Đã tạo {len(slides)} slide bằng OOXML fallback (không cần python-pptx).")
    print(f"PowerPoint: {PPTX_PATH}")
    print(f"Ghi chú: {NOTES_PATH}")
    print("Layouts: "+", ".join(dict.fromkeys(s.layout for s in slides)))
    print("Slides cần ảnh: "+", ".join(f"{s.number:02d}" for s in slides if s.image_note))
    return 0

if __name__=="__main__": sys.exit(main())
