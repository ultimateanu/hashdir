.PHONY: help format build-release podman-build-dev podman-run-dev

.DEFAULT_GOAL = help
help: ## (@main) Show this help
	@echo "Usage: make [target]"
	@echo ""
	@echo "Available targets:"
	@echo ""
	@awk -F ':|##' '/^[^\t].+?:.*?##/ { \
		tag = match($$NF, /\(@[^)]+\)/); \
		category = substr($$NF, RSTART+2, RLENGTH-3); \
		description = substr($$NF, RSTART+RLENGTH+1); \
		targets[category] = targets[category] sprintf("  \033[36m%-30s\033[0m %s\n", $$1, description); \
	} \
	END { \
		for (cat in targets) { \
			printf "%s:\n%s\n", cat, targets[cat]; \
		} \
	}' $(MAKEFILE_LIST)

build: ##(@main) Build everything
	dotnet build

test: ##(@main) Run all unit tests
	dotnet test

format: ##(@main) Format all F# code
	find . -type f \( -name "*.fs" -o -name "*.fsx" \) -not -path "*obj*" | xargs fantomas

build-release: ##(@main) Do a release build
	dotnet fsi src/make_release.fsx build

podman-build-dev: ## (@dev-ide) Build image
	podman build -t dotnet-dev -f Containerfile

podman-run-dev: ## (@dev-ide) Run image
	podman run --rm -it -v $(shell pwd):/mnt/code:z dotnet-dev
