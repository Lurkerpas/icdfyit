# icdfyit
**Utility for creating Interface Control Documents**

Created to support dealing with large and error prone TC/TM tables, evaluate mako templating capabilities, as well as experiment with Avalonia and LLM-assisted software engineering.

Concepts are based on TC/TM tables often found in the space-industry, and encoded in various spreadsheet-like formats, as well as XTCE. The focus is on simplicity and versatility, not specialization towards a particular standard, like e.g., ECSS PUS C. There is little modelling devoted to headers, as those can be either handcoded (no reason to reimplement CCSDS Space Packet or PUS C TC Header for the n-th time...), or represent information extracted e.g., from CAN or MIL-1553 addresses.

This tool is an experiment, a demonstration, and to some extent, a fusion of experience.

For a more mature tool, developed profesionally and in the process of adoption by the industry, and specialized in ECSS PUS C, go to [OPUS2](https://gitlab.esa.int/PUS-C/opus2)

# Static demo (what can it do for you)

Consider data in ```demo/example-asw.xml```

**icdfyit** allows to edit it via GUI with tables. And then, using templates, generate e.g.,:

## markdown ICD


## ASN.1/ACN

## RAW C transcoders

(for more info on ACN, go to [asn1scc](https://github.com/esa/asn1scc))

Please note that the above are illustrative demonstrations that evolve and may be incorrect.

# Usage

Launch the app (make run is the simplest way!) and open example-asw.xml from the demo. Click around. Then go to demo, and analyze the Makefiles. Run them. Enjoy, learn.
