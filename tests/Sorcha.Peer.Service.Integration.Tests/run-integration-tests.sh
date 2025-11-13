#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

set -e

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR"
TEST_PROJECT="$PROJECT_DIR/Sorcha.Peer.Service.Integration.Tests.csproj"
RESULTS_DIR="$PROJECT_DIR/TestResults"

# Colors
COLOR_SUCCESS='\033[0;32m'
COLOR_ERROR='\033[0;31m'
COLOR_INFO='\033[0;36m'
COLOR_WARNING='\033[0;33m'
COLOR_RESET='\033[0m'

# Default options
TEST_FILTER=""
COVERAGE=false
VERBOSE=false
WATCH=false
PARALLEL=true

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -f|--filter)
            TEST_FILTER="$2"
            shift 2
            ;;
        -c|--coverage)
            COVERAGE=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -w|--watch)
            WATCH=true
            shift
            ;;
        --no-parallel)
            PARALLEL=false
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  -f, --filter FILTER    Filter tests by name"
            echo "  -c, --coverage         Generate code coverage"
            echo "  -v, --verbose          Enable verbose output"
            echo "  -w, --watch            Run in watch mode"
            echo "  --no-parallel          Disable parallel execution"
            echo "  -h, --help             Show this help message"
            echo ""
            echo "Examples:"
            echo "  $0"
            echo "  $0 --filter PeerDiscoveryTests"
            echo "  $0 --coverage --verbose"
            exit 0
            ;;
        *)
            echo -e "${COLOR_ERROR}Unknown option: $1${COLOR_RESET}"
            exit 1
            ;;
    esac
done

# Helper functions
print_header() {
    echo ""
    echo -e "${COLOR_INFO}═══════════════════════════════════════════════════════${COLOR_RESET}"
    echo -e "${COLOR_INFO} $1${COLOR_RESET}"
    echo -e "${COLOR_INFO}═══════════════════════════════════════════════════════${COLOR_RESET}"
    echo ""
}

print_step() {
    echo -e "${COLOR_INFO}▶ $1${COLOR_RESET}"
}

print_success() {
    echo -e "${COLOR_SUCCESS}✓ $1${COLOR_RESET}"
}

print_error() {
    echo -e "${COLOR_ERROR}✗ $1${COLOR_RESET}"
}

print_warning() {
    echo -e "${COLOR_WARNING}⚠ $1${COLOR_RESET}"
}

# Display banner
print_header "Sorcha Peer Service - Integration Tests"

# Validate .NET installation
print_step "Checking .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK not found. Please install .NET 10.0 or later."
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
print_success ".NET SDK version: $DOTNET_VERSION"

# Validate test project exists
if [ ! -f "$TEST_PROJECT" ]; then
    print_error "Test project not found: $TEST_PROJECT"
    exit 1
fi

# Clean previous test results
if [ -d "$RESULTS_DIR" ]; then
    print_step "Cleaning previous test results..."
    rm -rf "$RESULTS_DIR"
    print_success "Test results cleaned"
fi

# Build test arguments
TEST_ARGS=("test" "$TEST_PROJECT")

# Add filter if specified
if [ -n "$TEST_FILTER" ]; then
    print_step "Applying test filter: $TEST_FILTER"
    TEST_ARGS+=("--filter" "FullyQualifiedName~$TEST_FILTER")
fi

# Add verbosity
if [ "$VERBOSE" = true ]; then
    TEST_ARGS+=("--logger" "console;verbosity=detailed")
else
    TEST_ARGS+=("--logger" "console;verbosity=normal")
fi

# Add coverage
if [ "$COVERAGE" = true ]; then
    print_step "Enabling code coverage..."
    TEST_ARGS+=("--collect:XPlat Code Coverage" "--results-directory" "$RESULTS_DIR")
fi

# Add parallel execution
if [ "$PARALLEL" = false ]; then
    print_step "Disabling parallel test execution..."
    TEST_ARGS+=("--parallel" "none")
fi

# Add watch mode
if [ "$WATCH" = true ]; then
    TEST_ARGS+=("--watch")
fi

# Display test configuration
echo ""
echo -e "${COLOR_INFO}Test Configuration:${COLOR_RESET}"
echo -e "  Project: $TEST_PROJECT"
echo -e "  Filter: ${TEST_FILTER:-None (all tests)}"
echo -e "  Coverage: $COVERAGE"
echo -e "  Verbose: $VERBOSE"
echo -e "  Watch: $WATCH"
echo -e "  Parallel: $PARALLEL"
echo ""

# Run tests
print_header "Running Tests"
START_TIME=$(date +%s)

if dotnet "${TEST_ARGS[@]}"; then
    END_TIME=$(date +%s)
    DURATION=$((END_TIME - START_TIME))
    echo ""
    print_success "All tests passed! (Duration: ${DURATION}s)"

    # Generate coverage report if enabled
    if [ "$COVERAGE" = true ]; then
        print_header "Code Coverage Report"

        # Find coverage file
        COVERAGE_FILE=$(find "$RESULTS_DIR" -name "coverage.cobertura.xml" -type f | head -n 1)

        if [ -n "$COVERAGE_FILE" ]; then
            print_success "Coverage file: $COVERAGE_FILE"

            # Try to install and run reportgenerator if available
            print_step "Attempting to generate HTML coverage report..."
            if dotnet tool install --global dotnet-reportgenerator-globaltool 2>/dev/null || true; then
                REPORT_DIR="$RESULTS_DIR/CoverageReport"

                if dotnet reportgenerator \
                    -reports:"$COVERAGE_FILE" \
                    -targetdir:"$REPORT_DIR" \
                    -reporttypes:Html; then

                    INDEX_FILE="$REPORT_DIR/index.html"
                    print_success "Coverage report generated: $INDEX_FILE"

                    # Try to open in browser
                    if command -v xdg-open &> /dev/null; then
                        print_step "Opening coverage report in browser..."
                        xdg-open "$INDEX_FILE" 2>/dev/null &
                    elif command -v open &> /dev/null; then
                        print_step "Opening coverage report in browser..."
                        open "$INDEX_FILE" 2>/dev/null &
                    fi
                fi
            else
                print_warning "Could not generate HTML report. Install with: dotnet tool install --global dotnet-reportgenerator-globaltool"
            fi
        else
            print_warning "Coverage file not found in $RESULTS_DIR"
        fi
    fi

    exit 0
else
    END_TIME=$(date +%s)
    DURATION=$((END_TIME - START_TIME))
    echo ""
    print_error "Tests failed! (Duration: ${DURATION}s)"
    exit 1
fi
