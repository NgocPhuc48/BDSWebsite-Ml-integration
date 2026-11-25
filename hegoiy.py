import re
import pandas as pd
from flask import Flask, jsonify, request, send_from_directory

app = Flask(__name__, static_folder='.', static_url_path='')

EXCEL_FILE = "2024.9.12 Du lieu San giao dich bat dong san 2017-2022- TTQLN.xlsx"

REGIONS = {
    "Khu Đông": ["Quận 2", "Quận 9", "Thủ Đức"],
    "Khu Nam": ["Quận 7", "Nhà Bè", "Quận 8", "Bình Chánh"],
    "Khu Trung tâm": ["Quận 1", "Quận 3", "Quận 4", "Quận 5", "Quận 6", "Quận 10", "Quận 11", "Bình Thạnh", "Phú Nhuận"],
    "Khu Tây Bắc": ["Tân Bình", "Tân Phú", "Gò Vấp", "Bình Tân", "Quận 12"]
}

def normalize_district(q, addr=""):
    q = str(q).strip()
    if q in ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '11', '12']:
        return f"Quận {q}"
    if q in ("", "nan", "None"):
        m = re.search(
            r'(Quận\s*\d+|Q\.\s*\d+|Phú Nhuận|Bình Thạnh|Tân Bình|Tân Phú|Gò Vấp|Bình Tân|Thủ Đức|Nhà Bè|Bình Chánh|Hóc Môn|Củ Chi|Cần Giờ)',
            str(addr), re.IGNORECASE
        )
        if m:
            val = re.sub(r'^Q\.\s*', 'Quận ', m.group(1), flags=re.IGNORECASE)
            return val.title()
        return "Không rõ"
    if q == "Tân Phú" and "Quận 7" in str(addr):
        return "Quận 7"
    if q == "Bình Tân" and "Tân Bình" in str(addr):
        return "Tân Bình"
    return q

def load_data():
    sheets_map = [
        ("MỚI", "Thành lập mới"),
        ("ĐIỀU CHỈNH", "Điều chỉnh"),
        ("XÓA SÀN", "Ngưng hoạt động (Xóa sàn)")
    ]
    records = []
    for sheet_name, status_label in sheets_map:
        df = pd.read_excel(EXCEL_FILE, sheet_name=sheet_name)
        current_year = None
        for _, row in df.iterrows():
            col0 = str(row.iloc[0]).strip() if pd.notna(row.iloc[0]) else ""
            if "năm" in col0.lower() or "201" in col0 or "202" in col0:
                m = re.search(r'20\d{2}', col0)
                if m:
                    current_year = int(m.group(0))
                continue
            if col0.isdigit():
                addr_san = str(row.iloc[8]).strip() if pd.notna(row.iloc[8]) else ""
                quan_raw = str(row.iloc[9]).strip() if pd.notna(row.iloc[9]) else ""
                sdt = str(row.iloc[10]).strip() if pd.notna(row.iloc[10]) else (
                    str(row.iloc[6]).strip() if pd.notna(row.iloc[6]) else ""
                )
                mst = str(row.iloc[2]).strip().replace(".0", "") if pd.notna(row.iloc[2]) else ""
                records.append({
                    "trang_thai": status_label,
                    "nam": current_year,
                    "ten_dn": str(row.iloc[1]).strip() if pd.notna(row.iloc[1]) else "",
                    "mst": mst,
                    "dia_chi_dn": str(row.iloc[3]).strip() if pd.notna(row.iloc[3]) else "",
                    "nguoi_dai_dien": str(row.iloc[5]).strip() if pd.notna(row.iloc[5]) else "",
                    "ten_san": str(row.iloc[7]).strip() if pd.notna(row.iloc[7]) else "",
                    "dia_diem_san": addr_san,
                    "quan": normalize_district(quan_raw, addr_san),
                    "sdt": sdt,
                    "ngay_thanh_lap": str(row.iloc[11]).replace(" 00:00:00", "").strip() if pd.notna(row.iloc[11]) else "",
                    "nguoi_quan_ly": str(row.iloc[12]).strip() if pd.notna(row.iloc[12]) else "",
                    "so_thong_bao": str(row.iloc[13]).strip() if pd.notna(row.iloc[13]) else "",
                    "ngay_thong_bao": str(row.iloc[14]).replace(" 00:00:00", "").strip() if pd.notna(row.iloc[14]) else "",
                })
    return pd.DataFrame(records)

def compute_recommendations(df, district="ALL", keyword="", include_nearby=True, hide_closed=True):
    closed_msts = set(df[df["trang_thai"] == "Ngưng hoạt động (Xóa sàn)"]["mst"]) - {""}
    adjusted_msts = set(df[df["trang_thai"] == "Điều chỉnh"]["mst"]) - {""}

    target_region = None
    for reg, dists in REGIONS.items():
        if district in dists:
            target_region = reg
            break

    results = []
    kw = keyword.strip().lower()

    for _, row in df.iterrows():
        is_closed = (row["trang_thai"] == "Ngưng hoạt động (Xóa sàn)") or (row["mst"] in closed_msts)
        if hide_closed and is_closed:
            continue

        # Bộ lọc từ khóa
        if kw:
            haystack = f"{row['ten_san']} {row['ten_dn']} {row['mst']} {row['dia_diem_san']} {row['nguoi_quan_ly']}".lower()
            if kw not in haystack:
                continue

        # Tính điểm gợi ý (Match Score)
        score = 50
        reasons = []

        if district != "ALL":
            if row["quan"] == district:
                score += 35
                reasons.append(f"Đúng quận mục tiêu ({district})")
            elif include_nearby and target_region and row["quan"] in REGIONS[target_region]:
                score += 18
                reasons.append(f"Cùng cụm {target_region} ({row['quan']})")
            else:
                continue

        if row["mst"] in adjusted_msts:
            score += 10
            reasons.append("Có cập nhật/điều chỉnh thông tin tại Sở XD")
        if row["nam"] and row["nam"] <= 2019 and not is_closed:
            score += 5
            reasons.append(f"Hoạt động lâu năm (từ {row['nam']})")
        if is_closed:
            score -= 40
            reasons.append("Cảnh báo: Thuộc danh sách ngưng hoạt động")

        item = row.to_dict()
        item["score"] = max(0, min(100, score))
        item["is_closed"] = is_closed
        item["has_adjusted"] = row["mst"] in adjusted_msts
        item["reasons"] = reasons
        results.append(item)

    results.sort(key=lambda x: (x["score"], x["nam"] or 0), reverse=True)
    return results

@app.route("/")
def index():
    return send_from_directory(".", "index.html")

@app.route("/api/recommend")
def api_recommend():
    df = load_data()
    district = request.args.get("district", "ALL")
    keyword = request.args.get("keyword", "")
    include_nearby = request.args.get("nearby", "true").lower() == "true"
    hide_closed = request.args.get("hide_closed", "true").lower() == "true"

    recs = compute_recommendations(df, district, keyword, include_nearby, hide_closed)
    districts = sorted([q for q in df["quan"].unique() if q and q != "Không rõ"])
    return jsonify({
        "total": len(recs),
        "districts": districts,
        "recommendations": recs
    })

@app.route("/api/history/<mst>")
def api_history(mst):
    df = load_data()
    history = df[df["mst"] == mst].to_dict(orient="records")
    return jsonify({"mst": mst, "history": history})

if __name__ == "__main__":
    app.run(debug=True, port=5000)
