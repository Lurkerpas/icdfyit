SOLUTION       := icdfyit.sln
APP_PROJECT    := src/IcdFyIt.App/IcdFyIt.App.csproj
REPORTER_PROJECT := src/IcdFyIt.GuiReporter/IcdFyIt.GuiReporter.csproj
DOCS_DIR       := docs

.PHONY: all build run report test docs clean help

all: build

## Build the entire solution (Debug configuration)
build:
	dotnet build $(SOLUTION)

## Run the application
run:
	dotnet run --project $(APP_PROJECT)

## Capture PNG screenshots of all screens and dialogs at all size scales into GuiReport/
report:
	dotnet run --project $(REPORTER_PROJECT) -- --model demo/testmodel.xml --output reports/GuiReport

## Execute the test suite
test:
	dotnet test $(SOLUTION)

## Generate HTML documentation into $(DOCS_DIR)/
docs:
	doxygen Doxyfile

## Remove build artefacts and generated documentation
clean:
	dotnet clean $(SOLUTION)
	rm -rf $(DOCS_DIR)

## Print available targets
help:
	@echo "Usage: make [target]"
	@echo ""
	@echo "Targets:"
	@echo "  build   Build the entire solution (default)"
	@echo "  run     Run the application"
	@echo "  report  Capture screenshots into GuiReport/ (uses demo/testmodel.xml)"
	@echo "  test    Execute the test suite"
	@echo "  docs    Generate Doxygen HTML documentation into $(DOCS_DIR)/"
	@echo "  clean   Clean build outputs and generated docs"
	@echo "  help    Show this message"
