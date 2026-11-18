# Tool thu thập Facebook ban đầu

Đây là công cụ React/Vite độc lập, được giữ lại làm tài liệu nguồn. Website NhaTot đã có luồng SocialBot chạy qua backend C#; không cần chạy tool này để dùng website.

## Chạy riêng khi phát triển

```powershell
npm ci
Copy-Item src/api.example.txt src/api.txt
# Thay YOUR_APIFY_TOKEN trong src/api.txt bằng token riêng trước khi cào.
npm run dev
```

`src/api.txt` được Git bỏ qua. Nếu không có file này, ứng dụng vẫn build nhưng không thể cào. Token dùng ở tool React được đọc trong trình duyệt; không dùng bản build chứa token để phát hành công khai. Dùng trang Admin/SocialBot/KetNoi của NhaTot cho luồng server mã hóa token.

Tài liệu tích hợp hiện tại: [INTEGRATION.md](INTEGRATION.md).
