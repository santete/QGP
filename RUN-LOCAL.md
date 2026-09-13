# QGP — Chạy local (với đăng nhập thật Keycloak / OIDC)

> Mở **PowerShell**, `cd` về thư mục gốc dự án TRƯỚC KHI chạy mọi lệnh:
> ```powershell
> cd C:\Users\nangh\Documents\workplace\ClaudeCode\qms-governance-platform
> ```

---

## 🔑 Tài khoản đăng nhập mẫu (realm Keycloak `qgp`)

Mật khẩu **tất cả** đều là: `Password123!`

| Username   | Vai trò (role)              | Dùng để test         |
|------------|-----------------------------|----------------------|
| `reader1`  | READER                      | Người đọc thường     |
| `author1`  | AUTHOR + READER             | Soạn / sửa tài liệu  |
| `approver1`| APPROVER + READER           | Duyệt tài liệu       |
| `qalead1`  | QA_LEAD + READER            | Báo cáo / cấu hình   |
| `admin1`   | ADMIN + QA_LEAD + READER    | Toàn quyền quản trị  |

**Trang quản trị Keycloak** (đổi user/role/mật khẩu): http://localhost:8081 — tài khoản admin: `admin` / `admin`

> Muốn sửa danh sách user/role mẫu → sửa file `deploy/keycloak/realm-qgp.json` rồi tạo lại container Keycloak
> (`docker compose ... up -d --force-recreate keycloak`). Đây là realm CHỈ dùng cho DEV.

---

## ▶️ Khởi động (3 bước, mỗi bước 1 cửa sổ terminal)

### Bước 1 — Hạ tầng (Postgres + Meili + Redis + Keycloak)
Chạy 1 lần, để nền (không cần cửa sổ riêng):
```powershell
docker compose -f deploy/docker-compose.yml up -d
```
Kiểm tra đã chạy: `docker ps` (thấy `qgp-postgres`, `qgp-meili`, `qgp-redis`, `qgp-keycloak`).
Keycloak khởi động lần đầu hơi lâu (~1–2 phút). Xong khi mở được: http://localhost:8081

### Bước 2 — Backend (.NET) ở chế độ OIDC — **cửa sổ terminal #1**
```powershell
$env:Auth__Mode='Oidc'
$env:Auth__OidcAuthority='http://localhost:8081/realms/qgp'
$env:Auth__RequireHttpsMetadata='false'
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ASPNETCORE_URLS='http://localhost:5048'
# Dùng 127.0.0.1 (KHÔNG 'localhost') để ép IPv4 — tránh timeout do port-forward IPv6 của Docker Desktop:
$env:QGP_DB_CONNECTION='Host=127.0.0.1;Port=5432;Database=qgp_db;Username=qgp;Password=qgp'
dotnet run --project apps/api/src/Qgp.Api/Qgp.Api.csproj --no-launch-profile
```
Chạy đúng khi mở được http://localhost:5048/healthz → hiện `Healthy`.
> Cứ để cửa sổ này chạy. Dừng BE: bấm **Ctrl + C** trong cửa sổ đó.

### Bước 3 — Frontend (React/Vite) ở chế độ OIDC — **cửa sổ terminal #2**
```powershell
npm run dev:oidc --prefix apps/web
```
Chạy đúng khi hiện `Local: http://localhost:5173/`.
> Dừng FE: bấm **Ctrl + C** trong cửa sổ đó.

### Bước 4 — Dùng app
Mở trình duyệt: **http://localhost:5173**
→ bấm **SSO / Đăng nhập** → nhập `author1` / `Password123!` → quay lại app, đã có phiên thật.

> ⚠️ Phải mở bằng `http://localhost:5173` (KHÔNG dùng `127.0.0.1` hay IP LAN — trình duyệt sẽ
> chặn mã hoá PKCE và nút SSO không nhảy trang).

---

## ⏹️ Tắt

```powershell
# Tắt BE và FE: bấm Ctrl+C trong 2 cửa sổ terminal của chúng.
# Tắt hạ tầng (giữ lại dữ liệu):
docker compose -f deploy/docker-compose.yml stop
# Bật lại sau đó: docker compose -f deploy/docker-compose.yml start
```

---

## 🌐 Cổng (ports)

| Dịch vụ            | URL                          |
|--------------------|------------------------------|
| Frontend (Vite)    | http://localhost:5173        |
| Backend (.NET API) | http://localhost:5048        |
| Keycloak (IdP)     | http://localhost:8081        |
| Postgres           | localhost:5432               |
| Meilisearch        | http://localhost:7700        |
| Redis              | localhost:6379               |

---

## 🎛️ 3 chế độ chạy Frontend (chọn 1)

| Lệnh (trong `apps/web` hoặc thêm `--prefix apps/web`) | Chế độ | Cần gì |
|---|---|---|
| `npm run dev`      | **Mock** — dữ liệu giả, offline, không cần BE | không |
| `npm run dev:real` | Gọi BE thật, đăng nhập nhanh (dev-login, chọn role) | BE chạy (chế độ Dev) |
| `npm run dev:oidc` | **Đăng nhập thật qua Keycloak** (như trên) | Keycloak + BE chế độ OIDC |

---

## 🩺 Gặp lỗi thường gặp

- **Bấm SSO không nhảy trang** → đang mở bằng IP thay vì `localhost`; hoặc BE/Keycloak chưa chạy.
- **Sau đăng nhập gọi API bị 401** → BE chưa ở chế độ OIDC (thiếu các `$env:Auth__...` ở Bước 2), hoặc quên `--no-launch-profile` nên BE chạy nhầm cổng. Kiểm tra `http://localhost:5048/healthz`.
- **`npm run ...` báo "missing script"** → chưa vào `apps/web` (hoặc thiếu `--prefix apps/web`).
- **`dotnet`/`docker` báo không tìm thấy file** → chưa `cd` về thư mục gốc dự án.
- **Keycloak mở không được** → đợi thêm ~1–2 phút lần đầu; xem log: `docker logs qgp-keycloak`.
- **BE crash lúc start: "Timeout during reading attempt" / transient failure (Npgsql)** → `localhost` trỏ IPv6 `::1`, port-forward IPv6 của Docker Desktop hay flaky sau khi build image. Dùng `Host=127.0.0.1` trong `QGP_DB_CONNECTION` (đã có ở Bước 2). Nếu vẫn lỗi: `docker restart qgp-postgres` rồi chạy lại `dotnet run`.
