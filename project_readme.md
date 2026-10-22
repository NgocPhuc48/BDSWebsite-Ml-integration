# 🏡 NhaTot - Hệ Thống Mua Bán, Tư Vấn & Định Giá Bất Động Sản Thông Minh

> **Đồ án phát triển phần mềm Full-Stack tích hợp AI/Machine Learning**  
> **Thương hiệu dự án:** NhaTot (Phân khúc Bất động sản & Dự đoán giá đất)

---

## 📋 Mục lục
1. [Giới thiệu Dự án](#1-giới-thiệu-dự-án)
2. [TÍnh năng Cốt lõi](#2-tính-năng-cốt-lõi)
3. [Kiến trúc Hệ thống](#3-kiến-trúc-hệ-thống)
4. [Công nghệ Sử dụng](#4-công-nghệ-sử-dụng)
5. [Thiết kế Cơ sở Dữ liệu (28 Bảng)](#5-thiết-kế-cơ-sở-dữ-liệu-28-bảng)
6. [Cấu trúc Thư mục Source Code](#6-cấu-trúc-thư-mục-source-code)
7. [Phân công Vai trò & Quy trình Agile/Scrum](#7-phân-công-vai-trò--quy-trình-agilescrum)
8. [Hướng dẫn Cài đặt & Khởi chạy (Local Setup)](#8- hướng-dẫn-cài-đặt--khởi-chạy-local-setup)
9. [Quy chuẩn Git & Quy trình Đóng góp](#9-quy-chuẩn-git--quy-trình-đóng-góp)

---

## 1. Giới thiệu Dự án
**NhaTot** là nền tảng web bất động sản hiện đại, cung cấp giải pháp toàn diện cho người mua, người bán, môi giới và quản trị viên. Nền tảng nổi bật với tính năng **Định giá đất tự động bằng AI (Machine Learning)**, hỗ trợ người dùng đưa ra quyết định giao dịch hợp lý dựa trên dữ liệu lịch sử và các thuộc tính thực tế của tài sản.

* **Đối tượng sử dụng:** Khách hàng tìm mua/thành viên rao bán, Môi giới/Tư vấn viên, Quản trị viên hệ thống (Admin/Moderator).
* **Quy mô dự án:** Bài tập lớn / Đồ án tốt nghiệp thực hiện bởi nhóm 4-5 sinh viên.

---

## 2. Tính năng Cốt lõi

### 👤 1. Phân hệ Tài khoản & Phân quyền (RBAC)
* Đăng ký, đăng nhập (JWT / Session), xác thực OTP/Email.
* Phân quyền người dùng: `ROLE_USER`, `ROLE_AGENT` (Môi giới), `ROLE_ADMIN`.
* Quản lý hồ sơ cá nhân và nhật ký hoạt động.

### 🏠 2. Phân hệ Bất động sản & Tin đăng
* Đăng tin bán/cho thuê BĐS với đầy đủ thông tin chi tiết (Đất, Nhà phố, Căn hộ...).
* Hỗ trợ lưu trữ danh sách ảnh tài sản và giấy tờ pháp lý (Sổ đỏ/Sổ hồng).
* Duyệt tin đăng đa tầng (Chờ duyệt, Đã duyệt, Từ chối, Hết hạn).
* Lọc tin nâng cao theo Tỉnh/Thành, Quận/Huyện, Phường/Xã, Khoảng giá, Diện tích, Hướng, Số phòng...

### 🗺️ 3. Phân hệ Bản đồ Tương tác (GIS)
* Tích hợp bản đồ Leaflet.js / Mapbox hiển thị vị trí BĐS trực quan theo tọa độ (Vĩ độ, Kinh độ).
* Tìm kiếm BĐS trong bán kính quanh một địa điểm cụ thể.

### 🤖 4. Phân hệ Định giá AI (Machine Learning Engine)
* Nhập thông số BĐS (Diện tích, Mặt tiền, Độ rộng đường vào, Tọa độ, Pháp lý...) để nhận dự đoán giá thị trường.
* Trả về kết quả: Giá gợi ý trung bình (VNĐ/m² & Tổng giá), khoảng giá sàn/trần và chỉ số độ tin cậy.
* Quản lý phiên bản Model AI ($R^2$, $RMSE$, $MAE$) và lưu vết toàn bộ lịch sử định giá.

### 💬 5. Phân hệ Tư vấn & Khớp Nhu cầu
* Đăng ký nhu cầu tìm kiếm BĐS của khách hàng.
* Tự động phân công tư vấn viên/môi giới phụ trách.
* Đặt lịch hẹn xem đất/nhà trực tiếp giữa khách hàng và môi giới.

### 🕷️ 6. Phân hệ Thu thập Dữ liệu (Crawler Pipeline)
* Tự động cào dữ liệu từ các trang web BĐS bên ngoài để làm giàu tập dữ liệu huấn luyện AI.
* Cơ chế lọc trùng lặp thông tin bằng Mã hóa chuỗi SHA-256 (`HashNoiDung`).

---

## 3. Kiến trúc Hệ thống

Hệ thống áp dụng mô hình **Kiến trúc Đa dịch vụ Decoupled (Hybrid Architecture)**:

```text
[ Client Browser ]
        │  (HTTP / JSON)
        ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        FRONTEND LAYER (ReactJS)                        │
│   React 18 + Redux Toolkit / React Query + TailwindCSS + Leaflet.js    │
└─────────────────────────────────┬──────────────────────────────────────┘
                                  │
                                  ▼
┌────────────────────────────────────────────────────────────────────────┐
│                     BACKEND LAYER (.NET Core MVC / Web API)            │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ ASP.NET Core 8.0 Web API & Controllers                           │  │
│  │ ├── Authentication & Authorization (JWT / Identity)              │  │
│  │ ├── Business Logic Layer (Services)                              │  │
│  │ └── Data Access Layer (Entity Framework Core / Dapper)          │  │
│  └──────────────────────────────┬───────────────────────────────────┘  │
│                                 │ (HTTP REST API / JSON)               │
│                                 ▼                                      │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ Python AI ML Engine (FastAPI Service)                            │  │
│  │ ├── Feature Engineering & Preprocessing                          │  │
│  │ ├── Model Loader (.pkl / .joblib - XGBoost / RandomForest)       │  │
│  │ └── Prediction API Server                                        │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                        │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ Python Crawler Service (Background ETL Pipeline)                 │  │
│  │ ├── Scraper (BeautifulSoup / Selenium)                           │  │
│  │ └── Data Cleaner & SHA-256 Deduplication                         │  │
│  └──────────────────────────────┬───────────────────────────────────┘  │
└─────────────────────────────────┼──────────────────────────────────────┘
                                  │
                                  ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        DATA & STORAGE LAYER                            │
│  ┌─────────────────────────────────┐  ┌─────────────────────────────┐  │
│  │ SQL Server 2019+                │  │ Local / Cloud File Storage  │  │
│  │ (28 Tables / 7 TableGroups)     │  │ (Ảnh BĐS, Model Artifacts)  │  │
│  └─────────────────────────────────┘  └─────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Công nghệ Sử dụng

| Tầng phát triển | Công nghệ / Thư viện sử dụng |
| :--- | :--- |
| **Frontend** | ReactJS 18, Vite / Create React App, Redux Toolkit, React Router v6, TailwindCSS / Ant Design, Leaflet.js (Bản đồ), Axios. |
| **Backend Core** | ASP.NET Core 8.0 (MVC / Web API), C# 12, Entity Framework Core / Dapper, AutoMapper, FluentValidation, JWT Authentication. |
| **AI / Machine Learning** | Python 3.10+, FastAPI, Scikit-learn, XGBoost, LightGBM, Pandas, NumPy, Joblib. |
| **Crawler / ETL** | Python, BeautifulSoup4, Requests, Selenium, Hashlib (SHA-256). |
| **Database** | SQL Server 2019+ (T-SQL, Stored Procedures, Filtered Indexes). |
| **DevOps & Tools** | Git/GitHub, Docker (Option), Postman, SSMS, Visual Studio 2022 / VS Code. |

---

## 5. Thiết kế Cơ sở Dữ liệu (28 Bảng)

CSDL **`BatDongSanNgheAn`** (Dùng chung cho thương hiệu **NhaTot**) được thiết kế chuẩn hóa 3NF gồm 28 bảng chia thành 7 Phân hệ:

```text
1. TaiKhoan (3 bảng): NguoiDung, VaiTro, NguoiDung_VaiTro
2. DiaLy_BatDongSan (8 bảng): TinhThanh, QuanHuyen, PhuongXa, LoaiBDS, BatDongSan, ChiTietDat, ChiTietNha, AnhBDS
3. TinDang (2 bảng): TinDang, LichSuGiaTin
4. TuVan (5 bảng): NhuCauTimKiem, YeuCauTuVan, PhanCongTuVan, LichHen, YeuThich
5. HocMay (3 bảng): PhienBanMoHinh, LichSuDinhGia, KetQuaDanhGiaMoHinh
6. ThuThapDuLieu (3 bảng): NguonDuLieu, DuLieuThuThap, LichSuXuLyDuLieu
7. QuanTri (4 bảng): ThongBao, BaoCaoTinDang, NhatKyHeThong, CauHinhHeThong
```

*Sơ đồ DBML chi tiết được lưu trữ tại file: `database/schema.dbml`.*  
*Script SQL Server khởi tạo toàn bộ 28 bảng nằm tại: `database/init_schema.sql`.*

---

## 6. Cấu trúc Thư mục Source Code

```text
NhaTot/
├── database/                    # Tài liệu CSDL & Script SQL
│   ├── init_schema.sql          # Script T-SQL tạo 28 bảng & mối quan hệ
│   ├── seed_data.sql            # Script chèn dữ liệu mẫu
│   └── schema.dbml              # File thiết kế CSDL cho dbdiagram.io
├── backend/                     # Solution ASP.NET Core (.NET 8)
│   ├── NhaTot.API/              # Web API Controller & Configurations
│   ├── NhaTot.Core/             # Entities, Interfaces, DTOs, Business Services
│   ├── NhaTot.Infrastructure/   # EF Core DbContext, Repositories, Migrations
│   └── NhaTot.sln
├── frontend/                    # Source code ReactJS Frontend
│   ├── src/
│   │   ├── components/          # Reusable UI Components (Navbar, Footer, Modal)
│   │   ├── pages/               # Views (Home, ListingDetail, Valuation, Admin)
│   │   ├── services/            # Axios API calls
│   │   ├── store/               # Redux State Management
│   │   └── App.jsx
│   ├── package.json
│   └── vite.config.js
├── ai-service/                  # Python FastAPI ML Service
│   ├── models/                  # File binary model đã train (.pkl, .joblib)
│   ├── pipelines/               # Feature engineering & Preprocessing
│   ├── main.py                  # FastAPI Application Entry Point
│   └── requirements.txt
├── crawler/                     # Tiến trình cào dữ liệu tự động
│   ├── scrapers/                # Python scripts cào dữ liệu từ các trang BĐS
│   ├── cleaner.py               # Xử lý làm sạch & mã hóa SHA-256
│   └── main.py
├── .gitignore
└── README.md
```

---

## 7. Phân công Vai trò & Quy trình Agile/Scrum

### 👥 Phân công Vai trò (Nhóm 5 thành viên)
* **Member 1 (Scrum Master / Lead Backend):** Dựng khung ASP.NET Core Web API, EF Core DBContext, Authentication JWT, Quản trị hệ thống (`QuanTri`).
* **Member 2 (Backend / Database Specialist):** Triển khai 28 bảng SQL Server, các Stored Procedures, APIs Tin đăng (`TinDang`) và Địa lý BĐS (`DiaLy_BatDongSan`).
* **Member 3 (Frontend Lead - ReactJS):** Dựng UI/UX ReactJS, tích hợp Bản đồ Leaflet.js, State Management (Redux), Form đăng tin & Lọc tìm kiếm.
* **Member 4 (AI / Data Engineer):** Thu thập dữ liệu (Crawler), làm sạch dữ liệu, Huấn luyện Model ML dự đoán giá đất, viết FastAPI REST API kết nối với Backend.
* **Member 5 (Frontend / Full-stack Support):** Làm giao diện Đặt lịch/Tư vấn (`TuVan`), Lịch sử định giá, Đánh giá vi phạm, Kiểm thử (Testing) và tài liệu hóa.

---

## 8. Hướng dẫn Cài đặt & Khởi chạy (Local Setup)

### Yêu cầu Tiền đề (Prerequisites)
* Node.js v18+ & npm
* .NET 8.0 SDK
* Python 3.10+ & pip
* SQL Server 2019+ & SSMS

---

### Bước 1: Khởi tạo Cơ sở Dữ liệu (SQL Server)
1. Mở **SQL Server Management Studio (SSMS)**.
2. Mở file `database/init_schema.sql` và thực thi (Execute) để tạo database `BatDongSanNgheAn` cùng 28 bảng.
3. (Tuỳ chọn) Chạy file `database/seed_data.sql` để chèn dữ liệu mẫu.

---

### Bước 2: Khởi chạy AI Service (Python FastAPI)
```bash
cd ai-service
python -m venv venv
# On Windows:
venv\Scripts\activate
# On Linux/Mac:
source venv/bin/activate

pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```
*AI Service sẽ lắng nghe tại: `http://localhost:8000` (Swagger UI: `http://localhost:8000/docs`).*

---

### Bước 3: Khởi chạy Backend (.NET Core Web API)
1. Cấu hình Chuỗi kết nối Database trong `backend/NhaTot.API/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=BatDongSanNgheAn;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```
2. Khởi chạy dự án:
   ```bash
   cd backend/NhaTot.API
   dotnet restore
   dotnet run
   ```
*Backend API sẽ chạy tại: `https://localhost:7123` hoặc `http://localhost:5123` (Swagger UI mở tự động).*

---

### Bước 4: Khởi chạy Frontend (ReactJS)
```bash
cd frontend
npm install
npm run dev
```
*Ứng dụng Web sẽ hoạt động tại: `http://localhost:5173`.*

---

## 9. Quy chuẩn Git & Quy trình Đóng góp

### 🌿 Cấu trúc Branching
* `main`: Nhánh production ổn định nhất.
* `develop`: Nhánh tích hợp code chính của các thành viên.
* `feature/issue-[ID]-[ten-tinh-nang]`: Nhánh phát triển tính năng mới *(Ví dụ: `feature/issue-2-db-init`, `feature/issue-5-valuation-ui`)*.
* `bugfix/issue-[ID]-[mo-ta-loi]`: Nhánh sửa lỗi.

### 📝 Quy chuẩn Commit Message
```text
[Loại]: Mô tả ngắn gọn công việc làm được (#IssueID)

Ví dụ:
feat: Thêm API dự đoán giá đất kết nối FastAPI (#12)
fix: Sửa lỗi văng màn hình khi nạp bản đồ Leaflet (#18)
docs: Cập nhật README file và sơ đồ kiến trúc (#1)
```

---

* © 2026 **NhaTot Team** - Bất Động Sản Thông Minh. All Rights Reserved.