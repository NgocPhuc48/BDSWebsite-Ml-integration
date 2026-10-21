import pandas as pd
import numpy as np
import os
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestRegressor
from sklearn.metrics import r2_score, mean_absolute_error

current_dir = os.path.dirname(os.path.abspath(__file__))
file_path = os.path.join(current_dir, 'chung cu chotot.csv')

if not os.path.exists(file_path):
    file_path = 'chung cu chotot.csv'

print(f"Đang đọc dữ liệu từ: {file_path}")
df = pd.read_csv(file_path)
#làm sạch dữ liệu: loại bỏ các cột không cần thiết và các giá trị NaN
df.columns = df.columns.str.strip()

def clean_numeric_column(series):
    series = series.astype(str).str.replace(r'[^\d.]', '', regex=True)
    return pd.to_numeric(series, errors='coerce')

df['area_clean'] = clean_numeric_column(df['area'])
df['price_clean'] = clean_numeric_column(df['price_VND'])

# Lọc các dòng hợp lệ
df_clean = df.dropna(subset=['area_clean', 'price_clean']).copy()
df_clean = df_clean[(df_clean['area_clean'] > 0) & (df_clean['price_clean'] > 0)]

print(f"-> Đã huấn luyện thành công trên {len(df_clean)} căn hộ mẫu.")

has_location = 'location' in df_clean.columns

if has_location:
    df_clean['location'] = df_clean['location'].fillna('Khác').astype(str)
    X = pd.get_dummies(df_clean[['area_clean', 'location']], columns=['location'], drop_first=True)
else:
    X = df_clean[['area_clean']]

y = df_clean['price_clean']

# 4. HUẤN LUYỆN MÔ HÌNH
X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

model = RandomForestRegressor(n_estimators=100, random_state=42)
model.fit(X_train, y_train)

# Đánh giá sơ bộ
y_pred = model.predict(X_test)
r2 = r2_score(y_test, y_pred)
print(f"-> Độ chính xác của mô hình (R² Score): {r2 * 100:.2f}%\n")

# 5. CHO NGƯỜI DÙNG NHẬP THÔNG TIN VÀ DỰ ĐOÁN
print("="*50)
print("     CHƯƠNG TRÌNH AI DỰ ĐOÁN GIÁ CHUNG CƯ / NHÀ ĐẤT")
print("="*50)

# Lấy danh sách các khu vực có sẵn trong tập dữ liệu (nếu có)
available_locations = []
if has_location:
    available_locations = sorted(df_clean['location'].unique().tolist())
while True:
    try:
        print("\n--- NHẬP THÔNG TIN CĂN HỘ CẦN DỰ ĐOÁN ---")
        input_area = float(input("1. Nhập diện tích (m²): "))
        
        selected_location = None
        if has_location and available_locations:
            print("\nDanh sách khu vực khả dụng:")
            for idx, loc in enumerate(available_locations, 1):
                print(f"  [{idx}] {loc}")
            
            loc_idx = int(input(f"2. Chọn số tương ứng với khu vực (1 - {len(available_locations)}): "))
            if 1 <= loc_idx <= len(available_locations):
                selected_location = available_locations[loc_idx - 1]
            else:
                selected_location = available_locations[0]

        # Tạo DataFrame mẫu để chứa đầu vào của người dùng
        input_data = pd.DataFrame(0, index=[0], columns=X.columns)
        input_data['area_clean'] = input_area
        
        # Nếu có chọn location thì gán giá trị 1 cho cột One-Hot tương ứng
        if selected_location:
            loc_column = f"location_{selected_location}"
            if loc_column in input_data.columns:
                input_data[loc_column] = 1

        # Thực hiện dự đoán
        predicted_price = model.predict(input_data)[0]

        # Hiển thị kết quả
        print("\n" + "="*40)
        print("KẾT QUẢ DỰ ĐOÁN CỦA MÔ HÌNH AI:")
        print(f"- Diện tích     : {input_area} m²")
        if selected_location:
            print(f"- Vị trí        : {selected_location}")
        print(f"==> GIÁ DỰ ĐOÁN : {predicted_price:,.0f} VNĐ ({predicted_price / 1e9:.2f} tỷ VNĐ)")
        print("="*40)

    except ValueError:
        print("Vui lòng nhập số hợp lệ!")
    
    # Hỏi người dùng có muốn tiếp tục hay không
    cont = input("\nBạn có muốn dự đoán căn khác không? (y/n): ").strip().lower()
    if cont != 'y':
        print("\nCảm ơn bạn đã sử dụng chương trình!")
        break