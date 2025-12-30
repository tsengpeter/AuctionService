#!/bin/bash

# AuctionService 測試執行腳本
# 此腳本用於執行所有測試專案並生成覆蓋率報告
# 適用於 Linux/macOS 環境

set -e  # 遇到錯誤立即退出

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 腳本路徑
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# 預設參數
COVERAGE=false
VERBOSE=false
FILTER=""
PARALLEL=true
REPORT_DIR="$PROJECT_ROOT/test-results"

# 顯示幫助資訊
show_help() {
    echo "AuctionService 測試執行腳本"
    echo ""
    echo "用法: $0 [選項]"
    echo ""
    echo "選項:"
    echo "  -c, --coverage          生成程式碼覆蓋率報告"
    echo "  -v, --verbose           詳細輸出"
    echo "  -f, --filter FILTER     測試篩選器 (例如: 'Category=UnitTest')"
    echo "  -s, --sequential        依序執行測試 (預設為平行執行)"
    echo "  -r, --report-dir DIR    報告輸出目錄 (預設: $REPORT_DIR)"
    echo "  -h, --help              顯示此幫助資訊"
    echo ""
    echo "範例:"
    echo "  $0                      # 執行所有測試"
    echo "  $0 --coverage           # 執行測試並生成覆蓋率報告"
    echo "  $0 --filter 'Category=IntegrationTest'  # 只執行整合測試"
    echo "  $0 --verbose --sequential  # 詳細輸出，依序執行"
}

# 解析命令列參數
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--coverage)
            COVERAGE=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -f|--filter)
            FILTER="$2"
            shift 2
            ;;
        -s|--sequential)
            PARALLEL=false
            shift
            ;;
        -r|--report-dir)
            REPORT_DIR="$2"
            shift 2
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            echo -e "${RED}錯誤: 未知參數 $1${NC}"
            echo ""
            show_help
            exit 1
            ;;
    esac
done

# 檢查必要工具
check_dependencies() {
    local missing_tools=()

    if ! command -v dotnet &> /dev/null; then
        missing_tools+=("dotnet")
    fi

    if [[ "$COVERAGE" == true ]]; then
        if ! command -v reportgenerator &> /dev/null; then
            echo -e "${YELLOW}警告: reportgenerator 未安裝，將安裝到全域工具...${NC}"
            dotnet tool install -g dotnet-reportgenerator-globaltool || {
                echo -e "${RED}錯誤: 無法安裝 reportgenerator${NC}"
                exit 1
            }
        fi
    fi

    if [[ ${#missing_tools[@]} -gt 0 ]]; then
        echo -e "${RED}錯誤: 缺少必要工具: ${missing_tools[*]}${NC}"
        echo "請安裝 .NET SDK: https://dotnet.microsoft.com/download"
        exit 1
    fi
}

# 設定詳細輸出
setup_verbose() {
    if [[ "$VERBOSE" == true ]]; then
        set -x
    fi
}

# 建立報告目錄
create_report_dir() {
    mkdir -p "$REPORT_DIR"
    echo -e "${BLUE}測試報告將輸出到: $REPORT_DIR${NC}"
}

# 執行單一測試專案
run_test_project() {
    local project_path="$1"
    local project_name="$(basename "$project_path" .csproj)"
    local output_dir="$REPORT_DIR/$project_name"

    echo -e "${BLUE}執行測試專案: $project_name${NC}"

    local test_args=("dotnet" "test" "$project_path")

    # 新增覆蓋率參數
    if [[ "$COVERAGE" == true ]]; then
        mkdir -p "$output_dir"
        test_args+=(
            "--collect:\"XPlat Code Coverage\""
            "--results-directory" "$output_dir"
        )
    fi

    # 新增篩選器
    if [[ -n "$FILTER" ]]; then
        test_args+=("--filter" "$FILTER")
    fi

    # 新增詳細程度
    if [[ "$VERBOSE" == true ]]; then
        test_args+=("--verbosity" "detailed")
    else
        test_args+=("--verbosity" "minimal")
    fi

    # 設定平行執行
    if [[ "$PARALLEL" == false ]]; then
        test_args+=("--no-build")
    fi

    # 執行測試
    if "${test_args[@]}"; then
        echo -e "${GREEN}✓ $project_name 測試通過${NC}"
        return 0
    else
        echo -e "${RED}✗ $project_name 測試失敗${NC}"
        return 1
    fi
}

# 執行所有測試專案
run_all_tests() {
    local test_projects=(
        "$PROJECT_ROOT/tests/AuctionService.UnitTests/AuctionService.UnitTests.csproj"
        "$PROJECT_ROOT/tests/AuctionService.IntegrationTests/AuctionService.IntegrationTests.csproj"
        "$PROJECT_ROOT/tests/AuctionService.ContractTests/AuctionService.ContractTests.csproj"
    )

    local failed_projects=()
    local total_projects=${#test_projects[@]}
    local passed_projects=0

    echo -e "${BLUE}找到 $total_projects 個測試專案${NC}"

    # 預先建置所有專案
    echo -e "${BLUE}建置所有專案...${NC}"
    if ! dotnet build "$PROJECT_ROOT/AuctionService.sln" --verbosity minimal; then
        echo -e "${RED}錯誤: 專案建置失敗${NC}"
        exit 1
    fi

    # 執行測試專案
    for project in "${test_projects[@]}"; do
        if [[ -f "$project" ]]; then
            if run_test_project "$project"; then
                ((passed_projects++))
            else
                failed_projects+=("$(basename "$project" .csproj)")
            fi
        else
            echo -e "${YELLOW}警告: 測試專案不存在: $project${NC}"
        fi
    done

    # 顯示測試結果摘要
    echo ""
    echo -e "${BLUE}=== 測試結果摘要 ===${NC}"
    echo "總測試專案數: $total_projects"
    echo -e "通過: ${GREEN}$passed_projects${NC}"
    echo -e "失敗: ${RED}${#failed_projects[@]}${NC}"

    if [[ ${#failed_projects[@]} -gt 0 ]]; then
        echo -e "${RED}失敗的專案: ${failed_projects[*]}${NC}"
        return 1
    else
        echo -e "${GREEN}所有測試專案均通過！${NC}"
        return 0
    fi
}

# 生成覆蓋率報告
generate_coverage_report() {
    if [[ "$COVERAGE" != true ]]; then
        return 0
    fi

    echo -e "${BLUE}生成覆蓋率報告...${NC}"

    local coverage_files=()
    local coverage_pattern="$REPORT_DIR/*/coverage.cobertura.xml"

    # 尋找覆蓋率檔案
    for file in $coverage_pattern; do
        if [[ -f "$file" ]]; then
            coverage_files+=("$file")
        fi
    done

    if [[ ${#coverage_files[@]} -eq 0 ]]; then
        echo -e "${YELLOW}警告: 未找到覆蓋率檔案${NC}"
        return 0
    fi

    local report_args=(
        "reportgenerator"
        "-reports:${coverage_files[*]}"
        "-targetdir:$REPORT_DIR/coverage-report"
        "-reporttypes:Html;Badges;Cobertura"
        "-verbosity:Info"
    )

    if "${report_args[@]}"; then
        echo -e "${GREEN}覆蓋率報告生成完成${NC}"
        echo -e "${BLUE}HTML 報告: file://$REPORT_DIR/coverage-report/index.html${NC}"
        echo -e "${BLUE}徽章檔案: $REPORT_DIR/coverage-report/badge_linecoverage.svg${NC}"
    else
        echo -e "${RED}覆蓋率報告生成失敗${NC}"
        return 1
    fi
}

# 顯示執行時間
show_execution_time() {
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    echo -e "${BLUE}總執行時間: ${duration} 秒${NC}"
}

# 主函數
main() {
    local start_time=$(date +%s)

    echo -e "${BLUE}=== AuctionService 測試執行器 ===${NC}"

    check_dependencies
    setup_verbose
    create_report_dir

    # 顯示執行參數
    echo "覆蓋率報告: $( [[ "$COVERAGE" == true ]] && echo "是" || echo "否" )"
    echo "詳細輸出: $( [[ "$VERBOSE" == true ]] && echo "是" || echo "否" )"
    echo "測試篩選器: $( [[ -n "$FILTER" ]] && echo "$FILTER" || echo "無" )"
    echo "平行執行: $( [[ "$PARALLEL" == true ]] && echo "是" || echo "否" )"
    echo ""

    # 執行測試
    if run_all_tests; then
        generate_coverage_report
        echo ""
        echo -e "${GREEN}🎉 所有測試執行完成！${NC}"
        show_execution_time
        exit 0
    else
        echo ""
        echo -e "${RED}❌ 測試執行失敗${NC}"
        show_execution_time
        exit 1
    fi
}

# 檢查是否從正確目錄執行
if [[ ! -f "$PROJECT_ROOT/AuctionService.sln" ]]; then
    echo -e "${RED}錯誤: 請從 AuctionService 專案根目錄執行此腳本${NC}"
    exit 1
fi

# 執行主函數
main "$@"