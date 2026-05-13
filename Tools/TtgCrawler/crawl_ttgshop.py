#!/usr/bin/env python3
import json
import re
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple
from urllib.parse import urljoin, urlparse

import pyodbc
import requests
from bs4 import BeautifulSoup

CATEGORY_MAP = {
    "CPU": ["cpu", "vi-xu-ly", "bo-vi-xu-ly"],
    "Mainboard": ["mainboard", "bo-mach-chu"],
    "RAM": ["ram", "bo-nho-trong"],
    "Card đồ họa": ["vga", "card-man-hinh", "gpu"],
    "Ổ cứng": ["o-cung", "ssd", "hdd"],
    "Nguồn": ["nguon", "psu"],
    "Tản nhiệt": ["tan-nhiet", "cooler"],
    "Vỏ case": ["vo-case", "case"],
    "Màn hình": ["man-hinh", "monitor"],
}

COMPONENT_TYPE_MAP = {
    "CPU": "CPU",
    "Mainboard": "MAINBOARD",
    "RAM": "RAM",
    "Card đồ họa": "GPU",
    "Ổ cứng": "STORAGE",
    "Nguồn": "PSU",
    "Tản nhiệt": "COOLER",
    "Vỏ case": "CASE",
    "Màn hình": "MONITOR",
}


@dataclass
class ProductItem:
    name: str
    price: float
    image_url: str
    product_url: str
    category_name: str
    brand: str = ""
    warranty: str = ""
    stock_status: str = ""
    specs: Dict[str, str] = field(default_factory=dict)


def load_config() -> Dict[str, Any]:
    cfg_path = Path(__file__).with_name("appsettings.Crawler.json")
    with cfg_path.open("r", encoding="utf-8") as f:
        return json.load(f)


def clean_price(value: Any) -> float:
    if value is None:
        return 0.0
    if isinstance(value, (int, float)):
        return float(value)
    s = re.sub(r"[^\d]", "", str(value))
    return float(s) if s else 0.0


def normalize_url(base_url: str, maybe_url: str) -> str:
    if not maybe_url:
        return ""
    return urljoin(base_url, maybe_url)


def slug_from_url(url: str) -> str:
    path = urlparse(url).path.strip("/")
    return path[-220:] if path else ""


def parse_specs_from_text(text: str) -> Dict[str, str]:
    patterns = {
        "Socket": r"(LGA\s?\d{3,4}|AM\d|sTRX\d+)",
        "Chipset": r"\b([ABHXZ]\d{3})\b",
        "RAMType": r"\b(DDR[3-6])\b",
        "Wattage": r"(\d{2,4}\s?W)",
        "Capacity": r"(\d+\s?(TB|GB))",
        "FormFactor": r"(ATX|mATX|Micro-ATX|Mini-ITX|E-ATX)",
    }
    found: Dict[str, str] = {}
    for key, pattern in patterns.items():
        m = re.search(pattern, text, re.IGNORECASE)
        if m:
            found[key] = m.group(1)
    return found


class TtgCrawler:
    def __init__(self, cfg: Dict[str, Any]):
        self.cfg = cfg
        self.base_url = cfg["BaseUrl"].rstrip("/")
        self.delay_sec = max(1, int(cfg.get("DelayMs", 1500)) / 1000)
        self.max_pages = int(cfg.get("MaxPagesPerCategory", 30))
        self.timeout = int(cfg.get("RequestTimeoutSec", 30))
        self.session = requests.Session()
        self.session.headers.update({"User-Agent": cfg.get("UserAgent", "DATN-TTG-Crawler/1.0")})

    def _request(self, url: str, params: Optional[Dict[str, Any]] = None) -> requests.Response:
        resp = self.session.get(url, params=params, timeout=self.timeout)
        if resp.status_code in (403, 429):
            raise RuntimeError(f"Website chặn request ({resp.status_code}), dừng crawler theo yêu cầu an toàn.")
        resp.raise_for_status()
        time.sleep(self.delay_sec)
        return resp

    def discover_json_endpoint(self) -> Optional[str]:
        buildpc_url = normalize_url(self.base_url, self.cfg.get("BuildPcPath", "/buildpc"))
        print(f"[INFO] Truy cập {buildpc_url} để tìm endpoint JSON...")
        html = self._request(buildpc_url).text
        endpoint_patterns = [
            r'https?://ttgshop\.vn[^"\']*(api|ajax)[^"\']*',
            r'/[^"\']*(api|ajax)[^"\']*',
        ]
        for pat in endpoint_patterns:
            for match in re.findall(pat, html, flags=re.IGNORECASE):
                candidate = match if isinstance(match, str) else match[0]
                endpoint = normalize_url(self.base_url, candidate)
                if self._is_json_endpoint(endpoint):
                    print(f"[INFO] Tìm thấy JSON endpoint: {endpoint}")
                    return endpoint

        soup = BeautifulSoup(html, "html.parser")
        for script in soup.find_all("script"):
            content = script.text or ""
            for m in re.findall(r"(/[^\"']*(?:api|ajax)[^\"']*)", content, flags=re.IGNORECASE):
                endpoint = normalize_url(self.base_url, m)
                if self._is_json_endpoint(endpoint):
                    print(f"[INFO] Tìm thấy JSON endpoint trong script: {endpoint}")
                    return endpoint
        print("[WARN] Không tìm thấy endpoint JSON, sẽ fallback parse HTML.")
        return None

    def _is_json_endpoint(self, url: str) -> bool:
        try:
            r = self.session.get(url, timeout=10)
            ct = r.headers.get("Content-Type", "")
            if r.ok and "json" in ct.lower():
                return True
        except Exception:
            return False
        return False

    def crawl_all(self) -> List[ProductItem]:
        endpoint = self.discover_json_endpoint()
        all_items: List[ProductItem] = []
        for category, keywords in CATEGORY_MAP.items():
            print(f"[INFO] Đang crawl nhóm: {category}")
            items = self.crawl_category(category, keywords, endpoint)
            print(f"[INFO] Nhóm {category}: lấy được {len(items)} sản phẩm")
            all_items.extend(items)
        return all_items

    def crawl_category(self, category_name: str, keywords: List[str], endpoint: Optional[str]) -> List[ProductItem]:
        items: List[ProductItem] = []
        if endpoint:
            items = self._crawl_by_json_api(category_name, keywords, endpoint)
        if not items:
            items = self._crawl_by_html(category_name, keywords)
        dedup: Dict[str, ProductItem] = {x.product_url.lower(): x for x in items if x.product_url}
        return list(dedup.values())

    def _crawl_by_json_api(self, category_name: str, keywords: List[str], endpoint: str) -> List[ProductItem]:
        found: List[ProductItem] = []
        for kw in keywords:
            for page in range(1, self.max_pages + 1):
                try:
                    resp = self._request(endpoint, params={"q": kw, "page": page})
                    payload = resp.json()
                except Exception:
                    break
                rows = payload if isinstance(payload, list) else payload.get("data") or payload.get("items") or []
                if not rows:
                    break
                for row in rows:
                    item = self._map_json_item(row, category_name)
                    if item:
                        found.append(item)
        return found

    def _map_json_item(self, row: Dict[str, Any], category_name: str) -> Optional[ProductItem]:
        name = row.get("name") or row.get("product_name")
        if not name:
            return None
        url = row.get("url") or row.get("link") or row.get("product_url") or ""
        image = row.get("image") or row.get("thumbnail") or ""
        full_text = " ".join([str(row.get(k, "")) for k in row.keys()])
        return ProductItem(
            name=name.strip(),
            price=clean_price(row.get("price") or row.get("sale_price") or row.get("final_price")),
            image_url=normalize_url(self.base_url, image),
            product_url=normalize_url(self.base_url, url),
            category_name=category_name,
            brand=(row.get("brand") or "").strip(),
            warranty=(row.get("warranty") or "").strip(),
            stock_status=(row.get("stock_status") or row.get("inventory_status") or "").strip(),
            specs=parse_specs_from_text(full_text),
        )

    def _crawl_by_html(self, category_name: str, keywords: List[str]) -> List[ProductItem]:
        found: List[ProductItem] = []
        for kw in keywords:
            for page in range(1, self.max_pages + 1):
                url = f"{self.base_url}/tim-kiem"
                try:
                    resp = self._request(url, params={"q": kw, "page": page})
                except Exception as ex:
                    print(f"[WARN] Lỗi crawl HTML kw={kw} page={page}: {ex}")
                    break
                soup = BeautifulSoup(resp.text, "html.parser")
                cards = soup.select(".product-item, .product, .item-product")
                if not cards:
                    break
                for card in cards:
                    item = self._parse_product_card(card, category_name)
                    if item:
                        found.append(item)
        return found

    def _parse_product_card(self, card, category_name: str) -> Optional[ProductItem]:
        name_el = card.select_one(".product-name, .name, h3 a, h2 a")
        if not name_el:
            return None
        name = name_el.get_text(strip=True)
        href = name_el.get("href", "")
        img_el = card.select_one("img")
        img = img_el.get("data-src") or img_el.get("src") if img_el else ""
        price_el = card.select_one(".price, .product-price, .price-sale")
        price = clean_price(price_el.get_text(" ", strip=True) if price_el else "0")
        card_text = card.get_text(" ", strip=True)
        return ProductItem(
            name=name,
            price=price,
            image_url=normalize_url(self.base_url, img),
            product_url=normalize_url(self.base_url, href),
            category_name=category_name,
            brand="",
            warranty="",
            stock_status="Còn hàng" if "còn hàng" in card_text.lower() else "",
            specs=parse_specs_from_text(card_text),
        )


def get_or_create_category(conn, name: str) -> int:
    cur = conn.cursor()
    cur.execute("SELECT TOP 1 Id FROM Categories WHERE Name = ?", name)
    row = cur.fetchone()
    if row:
        return int(row[0])
    cur.execute(
        "INSERT INTO Categories (Name, IconClass, CreatedAt, UpdatedAt) OUTPUT INSERTED.Id VALUES (?, ?, GETUTCDATE(), GETUTCDATE())",
        name,
        "bi bi-cpu",
    )
    return int(cur.fetchone()[0])


def upsert_product(conn, item: ProductItem, category_id: int) -> Tuple[bool, bool]:
    comp_type = COMPONENT_TYPE_MAP[item.category_name]
    slug = slug_from_url(item.product_url) or re.sub(r"[^a-z0-9-]", "-", item.name.lower())[:220]
    stock_qty = 0 if "hết" in item.stock_status.lower() else 10
    is_in_stock = 0 if stock_qty == 0 else 1
    specs_text = json.dumps(item.specs, ensure_ascii=False)
    cur = conn.cursor()
    cur.execute(
        """
        SELECT TOP 1 Id FROM Products
        WHERE Slug = ? OR Name = ?
        ORDER BY Id
        """,
        slug,
        item.name,
    )
    existing = cur.fetchone()
    if existing:
        cur.execute(
            """
            UPDATE Products
            SET Price=?, SalePrice=?, DiscountPrice=NULL,
                ThumbnailImage=?, Brand=?, WarrantyDuration=?, WarrantyMonths=?,
                StockQuantity=?, IsInStock=?, CategoryId=?, ComponentType=?,
                Specifications=?, DetailDescription=?, UpdatedAt=GETUTCDATE()
            WHERE Id=?
            """,
            item.price,
            item.price,
            item.image_url[:1000],
            item.brand[:80],
            (item.warranty or "12 tháng")[:50],
            int(re.search(r"(\d+)", item.warranty).group(1)) if re.search(r"(\d+)", item.warranty or "") else 12,
            stock_qty,
            is_in_stock,
            category_id,
            comp_type,
            specs_text,
            item.product_url[:1000],
            int(existing[0]),
        )
        return False, True

    product_code = f"TTG-{abs(hash(item.product_url or item.name)) % 100000000:08d}"
    short_desc = f"Nguồn dữ liệu: {item.product_url}"[:500]
    cur.execute(
        """
        INSERT INTO Products
        (Name, Slug, ProductCode, Brand, Price, DiscountPrice, SalePrice, StockQuantity, ThumbnailImage,
         ShortDescription, Description, DetailDescription, Specifications, WarrantyMonths, WarrantyDuration,
         IsActive, IsInStock, HasSoftwareLicense, ComponentType, CpuSocket, RamType, CategoryId, CreatedAt, UpdatedAt)
        VALUES (?, ?, ?, ?, ?, NULL, ?, ?, ?, ?, ?, ?, ?, ?, ?, 1, ?, 0, ?, ?, ?, ?, GETUTCDATE(), GETUTCDATE())
        """,
        item.name[:200],
        slug,
        product_code,
        item.brand[:80],
        item.price,
        item.price,
        stock_qty,
        item.image_url[:1000],
        short_desc,
        item.warranty[:500],
        item.product_url[:1000],
        specs_text,
        int(re.search(r"(\d+)", item.warranty).group(1)) if re.search(r"(\d+)", item.warranty or "") else 12,
        (item.warranty or "12 tháng")[:50],
        is_in_stock,
        comp_type,
        item.specs.get("Socket", "")[:20] or None,
        item.specs.get("RAMType", "")[:20] or None,
        category_id,
    )
    return True, False


def main():
    cfg = load_config()
    crawler = TtgCrawler(cfg)
    print("[INFO] Bắt đầu crawl TTGShop (chế độ thủ công, không realtime).")
    try:
        items = crawler.crawl_all()
    except Exception as ex:
        print(f"[ERROR] Dừng crawler: {ex}")
        return

    if not items:
        print("[WARN] Không lấy được sản phẩm nào.")
        return

    conn = pyodbc.connect(cfg["ConnectionString"])
    added = 0
    updated = 0

    try:
        for category_name in CATEGORY_MAP.keys():
            cat_items = [x for x in items if x.category_name == category_name]
            if not cat_items:
                continue
            category_id = get_or_create_category(conn, category_name)
            local_add = 0
            local_upd = 0
            for item in cat_items:
                is_add, is_upd = upsert_product(conn, item, category_id)
                local_add += 1 if is_add else 0
                local_upd += 1 if is_upd else 0
            added += local_add
            updated += local_upd
            conn.commit()
            print(f"[INFO] Import {category_name}: total={len(cat_items)}, thêm={local_add}, cập nhật={local_upd}")
    finally:
        conn.close()

    print(f"[DONE] Tổng sản phẩm crawl={len(items)}, thêm mới={added}, cập nhật={updated}")


if __name__ == "__main__":
    main()
