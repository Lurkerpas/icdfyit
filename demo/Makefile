DOTNET  ?= dotnet
ASN1SCC ?= asn1scc
CC      ?= gcc

CLI_PROJECT := ../src/IcdFyIt.Cli
MODEL       := example-asw.xml
SETTINGS    := settings.xml
OUT_DIR     := generated
ICD_DIR     := $(OUT_DIR)/icd
ASN1_DIR    := $(OUT_DIR)/asn1
TC_DIR      := $(OUT_DIR)/transcoders

CFLAGS := -std=c17 -Wall -Wextra -pedantic

.PHONY: all generate-icd generate-asn1 generate-transcoders \
        compile-asn1 compile-transcoders clean

all: compile-transcoders generate-icd

# ── Code generation ────────────────────────────────────────────────────────────

generate-icd:
	$(DOTNET) run --project $(CLI_PROJECT) -- export \
	    --model $(MODEL) --settings $(SETTINGS) \
	    --template-set icd-md --output $(ICD_DIR)

generate-asn1:
	$(DOTNET) run --project $(CLI_PROJECT) -- export \
	    --model $(MODEL) --settings $(SETTINGS) \
	    --template-set asn1 --output $(ASN1_DIR)

generate-transcoders:
	$(DOTNET) run --project $(CLI_PROJECT) -- export \
	    --model $(MODEL) --settings $(SETTINGS) \
	    --template-set c-transcoders --output $(TC_DIR)

# ── Compilation ────────────────────────────────────────────────────────────────

compile-asn1: generate-asn1
	$(ASN1SCC) -c -ACN -o $(ASN1_DIR) $(ASN1_DIR)/icd.asn $(ASN1_DIR)/icd.acn

compile-transcoders: generate-transcoders compile-asn1
	$(CC) $(CFLAGS) -I$(TC_DIR) -I$(ASN1_DIR) \
	    -c $(TC_DIR)/icdfyit-transcoders.c -o $(TC_DIR)/icdfyit-transcoders.o
	$(CC) $(CFLAGS) -I$(TC_DIR) -I$(ASN1_DIR) \
	    -c $(TC_DIR)/transcoders.c -o $(TC_DIR)/transcoders.o

# ── Housekeeping ───────────────────────────────────────────────────────────────

clean:
	rm -rf $(OUT_DIR)
