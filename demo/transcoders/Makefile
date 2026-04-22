DOTNET ?= dotnet
CC ?= gcc

CLI_PROJECT := ../../src/IcdFyIt.Cli
MODEL := example_model.xml
SETTINGS := settings.xml
TEMPLATE_SET := c-transcoders-demo
OUT_DIR := generated

CFLAGS := -std=c17 -Wall -Wextra -pedantic

.PHONY: all validate generate compile clean

all: compile

validate:
	$(DOTNET) run --project $(CLI_PROJECT) -- validate --model $(MODEL)

generate:
	$(DOTNET) run --project $(CLI_PROJECT) -- export --model $(MODEL) --settings $(SETTINGS) --template-set $(TEMPLATE_SET) --output $(OUT_DIR)

compile: generate
	$(CC) $(CFLAGS) -I$(OUT_DIR) -c $(OUT_DIR)/icdfyit-transcoders.c -o $(OUT_DIR)/icdfyit-transcoders.o
	$(CC) $(CFLAGS) -I$(OUT_DIR) -c $(OUT_DIR)/transcoders.c -o $(OUT_DIR)/transcoders.o
	ar rcs $(OUT_DIR)/libtranscoders.a $(OUT_DIR)/icdfyit-transcoders.o $(OUT_DIR)/transcoders.o

clean:
	rm -rf $(OUT_DIR)