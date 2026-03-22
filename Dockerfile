# ─────────────────────────────────────────────────────────────────────────────
# Stage 1: Build / 建置階段
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
# 先複製解決方案與專案檔以利用 Docker 層快取
COPY AuctionService.slnx ./
COPY src/AuctionService.Api/AuctionService.Api.csproj               src/AuctionService.Api/
COPY src/AuctionService.Shared/AuctionService.Shared.csproj         src/AuctionService.Shared/
COPY src/Modules/Member/Member.csproj                                src/Modules/Member/
COPY src/Modules/Auction/Auction.csproj                              src/Modules/Auction/
COPY src/Modules/Bidding/Bidding.csproj                              src/Modules/Bidding/
COPY src/Modules/Ordering/Ordering.csproj                            src/Modules/Ordering/
COPY src/Modules/Notification/Notification.csproj                    src/Modules/Notification/

# Restore dependencies / 還原相依套件
RUN dotnet restore src/AuctionService.Api/AuctionService.Api.csproj

# Copy the rest of the source / 複製其餘原始碼
COPY src/ src/

# Publish in Release mode / 以 Release 模式發佈
RUN dotnet publish src/AuctionService.Api/AuctionService.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ─────────────────────────────────────────────────────────────────────────────
# Stage 2: Runtime / 執行階段
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create a non-root user for security / 建立非 root 使用者以提升安全性
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

# Set ownership / 設定擁有權
RUN chown -R appuser:appgroup /app
USER appuser

# Expose HTTP port / 開放 HTTP 連接埠
EXPOSE 8080

# Environment defaults (override via docker run -e or docker-compose)
# 環境預設值（可透過 docker run -e 或 docker-compose 覆寫）
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AuctionService.Api.dll"]
