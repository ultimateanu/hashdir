.PHONY: format build-release dev-container-build dev-container-run

format:
	find . -type f \( -name "*.fs" -o -name "*.fsx" \) -not -path "*obj*" | xargs fantomas

build-release:
	dotnet fsi src/make_release.fsx build

podman-build-dev:
	podman build -t dotnet-dev -f Containerfile

podman-run-dev:
	podman run --rm -it -v $(shell pwd):/mnt/code:z dotnet-dev
