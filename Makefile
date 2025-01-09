.PHONY: format
format:
	find . -type f \( -name "*.fs" -o -name "*.fsx" \) -not -path "*obj*" | xargs fantomas

.PHONY: build-release
build-release:
	dotnet fsi src/make_release.fsx build
