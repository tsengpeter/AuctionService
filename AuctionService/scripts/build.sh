#!/bin/bash

# AuctionService Linux/macOS 建置腳本
# 此腳本用於在 Unix-like 環境中建置 AuctionService 專案

set -e  # 遇到錯誤立即退出

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 函數定義
log_info() {
    echo -e "${BLUE}==> $1${NC}"
}

log_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

log_error() {
    echo -e "${RED}✗ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

# 預設參數
CONFIGURATION="Release"
SKIP_TESTS=false
SKIP_INTEGRATION_TESTS=false
CLEAN=false
PACKAGE=false
VERBOSE=false

# 解析命令列參數
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --skip-integration-tests)
            SKIP_INTEGRATION_TESTS=true
            shift
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        --package)
            PACKAGE=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            echo "AuctionService 建置腳本"
            echo ""
            echo "用法: $0 [選項]"
            echo ""
            echo "選項:"
            echo "  -c, --configuration <config>    建置配置 (Debug/Release)，預設: Release"
            echo "  --skip-tests                    略過單元測試執行"
            echo "  --skip-integration-tests       略過整合測試執行"
            echo "  --clean                        在建置前清除所有輸出目錄"
            echo "  --package                      產生 NuGet 套件"
            echo "  -v, --verbose                  啟用詳細輸出"
            echo "  -h, --help                     顯示此說明"
            echo ""
            echo "範例:"
            echo "  $0 --configuration Debug --verbose"
            echo "  $0 --skip-tests --package"
            exit 0
            ;;
        *)
            log_error "未知參數: $1"
            echo "使用 -h 或 --help 查看說明"
            exit 1
            ;;
    esac
done

# 腳本變數
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
SOLUTION_FILE="$PROJECT_ROOT/AuctionService.sln"
OUTPUT_DIR="$PROJECT_ROOT/artifacts"
TEST_RESULTS_DIR="$OUTPUT_DIR/test-results"
PACKAGES_DIR="$OUTPUT_DIR/packages"

# 專案路徑
API_PROJECT="$PROJECT_ROOT/src/AuctionService.Api/AuctionService.Api.csproj"
CORE_PROJECT="$PROJECT_ROOT/src/AuctionService.Core/AuctionService.Core.csproj"
INFRASTRUCTURE_PROJECT="$PROJECT_ROOT/src/AuctionService.Infrastructure/AuctionService.Infrastructure.csproj"
SHARED_PROJECT="$PROJECT_ROOT/src/AuctionService.Shared/AuctionService.Shared.csproj"

# 測試專案路徑
UNIT_TESTS_PROJECT="$PROJECT_ROOT/tests/AuctionService.UnitTests/AuctionService.UnitTests.csproj"
INTEGRATION_TESTS_PROJECT="$PROJECT_ROOT/tests/AuctionService.IntegrationTests/AuctionService.IntegrationTests.csproj"
CONTRACT_TESTS_PROJECT="$PROJECT_ROOT/tests/AuctionService.ContractTests/AuctionService.ContractTests.csproj"

# 檢查 .NET SDK
check_dotnet_sdk() {
    log_info "檢查 .NET SDK 版本"

    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK 未安裝"
        exit 1
    fi

    local dotnet_version
    dotnet_version=$(dotnet --version)
    log_success ".NET SDK 版本: $dotnet_version"

    # 檢查是否為 .NET 10.0
    if [[ ! $dotnet_version =~ ^10\.0\. ]]; then
        log_warning "建議使用 .NET 10.0 SDK，目前版本: $dotnet_version"
    fi
}

# 清除建置輸出
clean_build() {
    log_info "清除建置輸出"

    # 清除專案輸出目錄
    local clean_dirs=(
        "$PROJECT_ROOT/src/AuctionService.Api/bin"
        "$PROJECT_ROOT/src/AuctionService.Api/obj"
        "$PROJECT_ROOT/src/AuctionService.Core/bin"
        "$PROJECT_ROOT/src/AuctionService.Core/obj"
        "$PROJECT_ROOT/src/AuctionService.Infrastructure/bin"
        "$PROJECT_ROOT/src/AuctionService.Infrastructure/obj"
        "$PROJECT_ROOT/src/AuctionService.Shared/bin"
        "$PROJECT_ROOT/src/AuctionService.Shared/obj"
        "$PROJECT_ROOT/tests/AuctionService.UnitTests/bin"
        "$PROJECT_ROOT/tests/AuctionService.UnitTests/obj"
        "$PROJECT_ROOT/tests/AuctionService.IntegrationTests/bin"
        "$PROJECT_ROOT/tests/AuctionService.IntegrationTests/obj"
        "$PROJECT_ROOT/tests/AuctionService.ContractTests/bin"
        "$PROJECT_ROOT/tests/AuctionService.ContractTests/obj"
        "$OUTPUT_DIR"
    )

    for dir in "${clean_dirs[@]}"; do
        if [[ -d $dir ]]; then
            if [[ $VERBOSE == true ]]; then
                echo "清除目錄: $dir"
            fi
            rm -rf "$dir"
        fi
    done

    log_success "建置輸出已清除"
}

# 還原 NuGet 套件
restore_packages() {
    log_info "還原 NuGet 套件"

    local restore_args=("dotnet" "restore" "$SOLUTION_FILE")

    if [[ $VERBOSE == true ]]; then
        restore_args+=("--verbosity" "detailed")
    fi

    if ! "${restore_args[@]}"; then
        log_error "NuGet 套件還原失敗"
        exit 1
    fi

    log_success "NuGet 套件還原完成"
}

# 編譯專案
build_projects() {
    log_info "編譯專案 (配置: $CONFIGURATION)"

    local build_args=("dotnet" "build" "$SOLUTION_FILE" "--configuration" "$CONFIGURATION")

    if [[ $VERBOSE == true ]]; then
        build_args+=("--verbosity" "detailed")
    fi

    if ! "${build_args[@]}"; then
        log_error "專案編譯失敗"
        exit 1
    fi

    log_success "專案編譯完成"
}

# 執行單元測試
run_unit_tests() {
    log_info "執行單元測試"

    mkdir -p "$TEST_RESULTS_DIR"

    local test_args=("dotnet" "test" "$UNIT_TESTS_PROJECT" "--configuration" "$CONFIGURATION"
                    "--logger" "trx;LogFileName=unit-tests.trx"
                    "--results-directory" "$TEST_RESULTS_DIR")

    if [[ $VERBOSE == true ]]; then
        test_args+=("--verbosity" "detailed")
    fi

    if ! "${test_args[@]}"; then
        log_error "單元測試執行失敗"
        exit 1
    fi

    log_success "單元測試執行完成"
}

# 執行整合測試
run_integration_tests() {
    log_info "執行整合測試"

    mkdir -p "$TEST_RESULTS_DIR"

    local test_args=("dotnet" "test" "$INTEGRATION_TESTS_PROJECT" "--configuration" "$CONFIGURATION"
                    "--logger" "trx;LogFileName=integration-tests.trx"
                    "--results-directory" "$TEST_RESULTS_DIR")

    if [[ $VERBOSE == true ]]; then
        test_args+=("--verbosity" "detailed")
    fi

    if ! "${test_args[@]}"; then
        log_error "整合測試執行失敗"
        exit 1
    fi

    log_success "整合測試執行完成"
}

# 執行契約測試
run_contract_tests() {
    log_info "執行契約測試"

    mkdir -p "$TEST_RESULTS_DIR"

    local test_args=("dotnet" "test" "$CONTRACT_TESTS_PROJECT" "--configuration" "$CONFIGURATION"
                    "--logger" "trx;LogFileName=contract-tests.trx"
                    "--results-directory" "$TEST_RESULTS_DIR")

    if [[ $VERBOSE == true ]]; then
        test_args+=("--verbosity" "detailed")
    fi

    if ! "${test_args[@]}"; then
        log_error "契約測試執行失敗"
        exit 1
    fi

    log_success "契約測試執行完成"
}

# 產生 NuGet 套件
create_packages() {
    log_info "產生 NuGet 套件"

    mkdir -p "$PACKAGES_DIR"

    local projects_to_pack=(
        "$CORE_PROJECT"
        "$INFRASTRUCTURE_PROJECT"
        "$SHARED_PROJECT"
    )

    for project in "${projects_to_pack[@]}"; do
        local pack_args=("dotnet" "pack" "$project" "--configuration" "$CONFIGURATION"
                        "--output" "$PACKAGES_DIR" "--no-build")

        if [[ $VERBOSE == true ]]; then
            pack_args+=("--verbosity" "detailed")
        fi

        if ! "${pack_args[@]}"; then
            log_error "NuGet 套件產生失敗 for $project"
            exit 1
        fi
    done

    log_success "NuGet 套件產生完成"
}

# 顯示建置摘要
show_summary() {
    log_info "建置摘要"

    echo "配置: $CONFIGURATION"
    echo "輸出目錄: $OUTPUT_DIR"

    if [[ -d $TEST_RESULTS_DIR ]]; then
        local trx_count
        trx_count=$(find "$TEST_RESULTS_DIR" -name "*.trx" 2>/dev/null | wc -l)
        echo "測試結果: $trx_count 個測試報告檔案"
    fi

    if [[ -d $PACKAGES_DIR ]]; then
        local nupkg_count
        nupkg_count=$(find "$PACKAGES_DIR" -name "*.nupkg" 2>/dev/null | wc -l)
        echo "NuGet 套件: $nupkg_count 個套件檔案"
    fi

    log_success "建置程序完成"
}

# 主程式
main() {
    log_info "開始 AuctionService 建置程序"
    echo "專案根目錄: $PROJECT_ROOT"
    echo "解決方案檔案: $SOLUTION_FILE"

    # 環境檢查
    check_dotnet_sdk

    # 建置步驟
    if [[ $CLEAN == true ]]; then
        clean_build
    fi

    restore_packages
    build_projects

    if [[ $SKIP_TESTS == false ]]; then
        run_unit_tests

        if [[ $SKIP_INTEGRATION_TESTS == false ]]; then
            run_integration_tests
        fi

        run_contract_tests
    fi

    if [[ $PACKAGE == true ]]; then
        create_packages
    fi

    # 顯示摘要
    show_summary

    log_success "所有建置步驟完成！"
}

# 錯誤處理
trap 'log_error "建置程序被中斷"' INT TERM

# 執行主程式
main "$@"