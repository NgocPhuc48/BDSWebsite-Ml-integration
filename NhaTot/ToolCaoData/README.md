# ToolCaoData (ASP.NET Core Blazor Server)

Bản C# của tool React `tool-cao-data`.

## Chạy

1. Cài .NET 8 SDK.
2. Dự án mặc định dùng `net8.0` và token được đọc từ `api.txt` hoặc biến môi trường `APIFY_TOKENS`.
3. Từ thư mục solution `NhaTot`, chạy:

```powershell
dotnet run --project .\ToolCaoData\ToolCaoData.csproj
```

4. Mở `http://localhost:5080`.

Hoặc chạy bằng Visual Studio sau khi chọn profile `ToolCaoData`.

## Ánh xạ React -> C#
| React (App.jsx)                    | C#                                              |
|------------------------------------|-------------------------------------------------|
| `useState`, `useRef`               | field trong `@code` của `Pages/Home.razor`      |
| `startCrawling` / `abortApifyRun`  | `Services/ApifyService.cs`                      |
| `normalizeData`, `formatDateTime`  | `ApifyService.NormalizeData` / `FormatDateTime` |
| `COLUMNS` + `exportToExcel`        | `Models/PostColumns.cs`                         |
| `import api.txt?raw`               | `Services/TokenProvider.cs` (đọc phía server)   |
| `AbortController`                  | `CancellationTokenSource`                       |
| Blob + `<a download>`              | `wwwroot/app.js` qua `IJSRuntime`               |
