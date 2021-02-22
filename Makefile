.PHONY: help build test run clean docker docker-up docker-down install uninstall lint format docs examples publish

# Variables
DOTNET := dotnet
DOTNET_VERSION := 10.0
PROJECT := ef-migration-diff
BUILD_CONFIG := Release
OUTPUT_DIR := ./publish
CACHE_DIR := .cache

# Colors for output
RESET := \033[0m
GREEN := \033[32m
BLUE := \033[34m
YELLOW := \033[33m

help: ## Display this help message
	@echo "$(BLUE)ef-migration-diff - Build and Development Commands$(RESET)"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  $(GREEN)%-15s$(RESET) %s\n", $$1, $$2}'
	@echo ""
	@echo "$(BLUE)Examples:$(RESET)"
	@echo "  make build       - Build the project in Release mode"
	@echo "  make test        - Run all tests"
	@echo "  make run         - Run the CLI tool with help"
	@echo "  make clean       - Remove build artifacts"
	@echo "  make docker      - Build Docker image"
	@echo "  make install     - Install as global dotnet tool"

clean: ## Remove build artifacts and caches
	@echo "$(YELLOW)Cleaning build artifacts...$(RESET)"
	$(DOTNET) clean -c $(BUILD_CONFIG)
	rm -rf $(OUTPUT_DIR)
	rm -rf $(CACHE_DIR)
	rm -rf bin/ obj/
	find . -type d -name "*.dcproj" -exec rm -rf {} +
	@echo "$(GREEN)✓ Clean complete$(RESET)"

restore: ## Restore NuGet packages
	@echo "$(YELLOW)Restoring packages...$(RESET)"
	$(DOTNET) restore
	@echo "$(GREEN)✓ Restore complete$(RESET)"

build: restore ## Build the project in Release mode
	@echo "$(YELLOW)Building $(PROJECT)...$(RESET)"
	$(DOTNET) build -c $(BUILD_CONFIG) --no-restore
	@echo "$(GREEN)✓ Build complete$(RESET)"

rebuild: clean build ## Clean and rebuild

test: ## Run all unit tests
	@echo "$(YELLOW)Running tests...$(RESET)"
	$(DOTNET) test -c $(BUILD_CONFIG) --no-build --verbosity normal
	@echo "$(GREEN)✓ Tests complete$(RESET)"

lint: ## Run code analysis
	@echo "$(YELLOW)Running code analysis...$(RESET)"
	$(DOTNET) build --no-restore /p:TreatWarningsAsErrors=true
	@echo "$(GREEN)✓ Analysis complete$(RESET)"

format: ## Format code with dotnet format
	@echo "$(YELLOW)Formatting code...$(RESET)"
	$(DOTNET) format
	@echo "$(GREEN)✓ Formatting complete$(RESET)"

format-check: ## Check if code needs formatting
	@echo "$(YELLOW)Checking code format...$(RESET)"
	$(DOTNET) format --verify-no-changes --verbosity diagnostic
	@echo "$(GREEN)✓ Format check complete$(RESET)"

run: build ## Run the CLI tool
	@echo "$(YELLOW)Running $(PROJECT)...$(RESET)"
	$(DOTNET) run -- --help

run-compare: build ## Run compare command (example)
	@echo "$(YELLOW)Running comparison example...$(RESET)"
	$(DOTNET) run -- compare --branch1 main --branch2 develop 2>/dev/null || echo "Note: Branch may not exist in this repo"

run-validate: build ## Run validation command
	@echo "$(YELLOW)Running validation...$(RESET)"
	$(DOTNET) run -- validate

publish: build ## Publish Release build
	@echo "$(YELLOW)Publishing $(PROJECT)...$(RESET)"
	$(DOTNET) publish -c $(BUILD_CONFIG) -o $(OUTPUT_DIR) --no-build
	@echo "$(GREEN)✓ Publish complete: $(OUTPUT_DIR)$(RESET)"

install: publish ## Install as global dotnet tool
	@echo "$(YELLOW)Installing as global tool...$(RESET)"
	$(DOTNET) tool install --global --add-source $(OUTPUT_DIR) ef-migration-diff
	@echo "$(GREEN)✓ Installation complete$(RESET)"
	@echo "Run 'ef-migration-diff --version' to verify"

uninstall: ## Uninstall global dotnet tool
	@echo "$(YELLOW)Uninstalling global tool...$(RESET)"
	$(DOTNET) tool uninstall --global ef-migration-diff
	@echo "$(GREEN)✓ Uninstall complete$(RESET)"

update-tool: ## Update global dotnet tool
	@echo "$(YELLOW)Updating global tool...$(RESET)"
	$(DOTNET) tool update --global ef-migration-diff
	@echo "$(GREEN)✓ Update complete$(RESET)"

docker: ## Build Docker image
	@echo "$(YELLOW)Building Docker image...$(RESET)"
	docker build -t $(PROJECT):latest .
	docker tag $(PROJECT):latest $(PROJECT):1.2.0
	@echo "$(GREEN)✓ Docker image built$(RESET)"
	@echo "Run 'docker run --rm -v \$$PWD:/workspace $(PROJECT):latest' to use"

docker-test: docker ## Test Docker image
	@echo "$(YELLOW)Testing Docker image...$(RESET)"
	docker run --rm $(PROJECT):latest --version
	docker run --rm $(PROJECT):latest --help
	@echo "$(GREEN)✓ Docker tests passed$(RESET)"

docker-up: ## Start Docker Compose services
	@echo "$(YELLOW)Starting Docker Compose services...$(RESET)"
	docker-compose up -d
	@echo "$(GREEN)✓ Services started$(RESET)"

docker-down: ## Stop Docker Compose services
	@echo "$(YELLOW)Stopping Docker Compose services...$(RESET)"
	docker-compose down
	@echo "$(GREEN)✓ Services stopped$(RESET)"

docker-logs: ## View Docker Compose logs
	docker-compose logs -f

docs: ## Build documentation (currently just copies files)
	@echo "$(YELLOW)Documentation ready at:$(RESET)"
	@echo "  - $(GREEN)docs/getting-started.md$(RESET)"
	@echo "  - $(GREEN)docs/architecture.md$(RESET)"
	@echo "  - $(GREEN)docs/api-reference.md$(RESET)"
	@echo "  - $(GREEN)docs/deployment.md$(RESET)"
	@echo "  - $(GREEN)docs/faq.md$(RESET)"

examples: ## List available examples
	@echo "$(BLUE)Available Examples:$(RESET)"
	@ls -lh examples/*.cs | awk '{print "  $(GREEN)" $$9 "$(RESET)"}'

changelog: ## View changelog
	@less CHANGELOG.md

version: ## Display version information
	@$(DOTNET) --version
	@echo "$(PROJECT) - Tool version will be shown when installed"

info: ## Display project information
	@echo "$(BLUE)Project: ef-migration-diff$(RESET)"
	@echo "$(BLUE)Description: Compare Entity Framework migrations between branches$(RESET)"
	@echo ""
	@echo "$(BLUE)Build Information:$(RESET)"
	@$(DOTNET) --info | grep "Version\|OS\|.NET\|Architecture"
	@echo ""
	@echo "$(BLUE)Project Files:$(RESET)"
	@find . -maxdepth 1 -name "*.csproj" -exec basename {} \;

check: format-check lint test ## Run all checks (format, lint, test)
	@echo "$(GREEN)✓ All checks passed$(RESET)"

ci: clean restore build lint test ## Run CI pipeline (clean, restore, build, lint, test)
	@echo "$(GREEN)✓ CI pipeline complete$(RESET)"

.DEFAULT_GOAL := help
