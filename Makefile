SOLUTION       := icdfyit.sln
APP_PROJECT    := src/IcdFyIt.App/IcdFyIt.App.csproj
CLI_PROJECT    := src/IcdFyIt.Cli/IcdFyIt.Cli.csproj
REPORTER_PROJECT := src/IcdFyIt.GuiReporter/IcdFyIt.GuiReporter.csproj
DOCS_DIR       := docs
INSTALL_CONFIG ?= Release
LOCAL_BIN      ?= $(HOME)/.local/bin
LOCAL_LIB      ?= $(HOME)/.local/lib/icdfyit
APP_INSTALL_DIR := $(LOCAL_LIB)/IcdFyIt.App
CLI_INSTALL_DIR := $(LOCAL_LIB)/IcdFyIt.Cli
REPORTER_INSTALL_DIR := $(LOCAL_LIB)/IcdFyIt.GuiReporter

.PHONY: all build run report test docs install clean help

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

## Execute the test suite and list each executed test
test:
	@echo "Running tests (showing each executed test)..."
	dotnet test $(SOLUTION) --logger "console;verbosity=detailed"

## Generate HTML documentation into $(DOCS_DIR)/
docs:
	doxygen Doxyfile

## Publish executables and install launchers into $(LOCAL_BIN)/
install:
	mkdir -p $(LOCAL_BIN) $(APP_INSTALL_DIR) $(CLI_INSTALL_DIR) $(REPORTER_INSTALL_DIR)
	dotnet publish $(APP_PROJECT) -c $(INSTALL_CONFIG) -o $(APP_INSTALL_DIR)
	dotnet publish $(CLI_PROJECT) -c $(INSTALL_CONFIG) -o $(CLI_INSTALL_DIR)
	dotnet publish $(REPORTER_PROJECT) -c $(INSTALL_CONFIG) -o $(REPORTER_INSTALL_DIR)
	printf '%s\n' '#!/usr/bin/env sh' 'exec dotnet "$(APP_INSTALL_DIR)/IcdFyIt.App.dll" "$$@"' > $(LOCAL_BIN)/icdfyit-app
	printf '%s\n' '#!/usr/bin/env sh' 'exec dotnet "$(CLI_INSTALL_DIR)/IcdFyIt.Cli.dll" "$$@"' > $(LOCAL_BIN)/icdfyit-cli
	printf '%s\n' '#!/usr/bin/env sh' 'exec dotnet "$(REPORTER_INSTALL_DIR)/IcdFyIt.GuiReporter.dll" "$$@"' > $(LOCAL_BIN)/icdfyit-reporter
	chmod +x $(LOCAL_BIN)/icdfyit-app $(LOCAL_BIN)/icdfyit-cli $(LOCAL_BIN)/icdfyit-reporter
	@case ":$$PATH:" in \
		*":$(LOCAL_BIN):"*) echo "Installed launchers in $(LOCAL_BIN): icdfyit-app, icdfyit-cli, icdfyit-reporter" ;; \
		*) echo "Installed launchers in $(LOCAL_BIN), but this path is not currently in PATH."; \
		   echo "Add this line to your shell profile: export PATH=\"$(LOCAL_BIN):\$$PATH\"" ;; \
	esac

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
	@echo "  install Publish binaries and install local launchers into $(LOCAL_BIN)/"
	@echo "  clean   Clean build outputs and generated docs"
	@echo "  help    Show this message"
