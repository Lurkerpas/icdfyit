# icdfyit
**Utility for creating Interface Control Documents**

Created to support dealing with large and error prone TC/TM tables, evaluate mako templating capabilities, as well as experiment with Avalonia and LLM-assisted software engineering.

Concepts are based on TC/TM tables often found in the space-industry, and encoded in various spreadsheet-like formats, as well as XTCE. The focus is on simplicity and versatility, not specialization towards a particular standard, like e.g., ECSS PUS C. There is little modelling devoted to headers, as those can be either handcoded (no reason to reimplement CCSDS Space Packet or PUS C TC Header for the n-th time...), or represent information extracted e.g., from CAN or MIL-1553 addresses.

This tool is an experiment, a demonstration, and to some extent, a fusion of experience.

For a more mature tool, developed profesionally and in the process of adoption by the industry, and specialized in ECSS PUS C, go to [OPUS2](https://gitlab.esa.int/PUS-C/opus2)

# Static demo (what can it do for you)

Consider data in `demo/example-asw.xml`

**icdfyit** allows to edit it via GUI with tables. And then, using templates, generate various artifacts that are consistent with each other, as they are derived from the same **single source of truth**. E.g.,:

## markdown ICD

### Boot Status

**Mnemonic:** TMBS  
**ID:** 0x0  
**Header:** TM[Type=1] (Telemetry)  

| Parameter | Type | Unit | Size | Offset |
|-----------|------|------|------|--------|
| Image Index | uint8 |  | 1 B | byte 0 |
| Image Address | uint32 |  | 4 B | byte 1 |
| Reason | boot-reason (enum, 32 bit) |  | 4 B | byte 5 |

## ASN.1/ACN

```
-- Packet  : Boot Status
-- Mnemonic: TMBS
-- ID      : 0x0
-- Header  : Telemetry  (Type=1)
T-Boot-Status ::= SEQUENCE {
    m-image-Index  INTEGER (0..255),
    m-image-Address  INTEGER (0..4294967295),
    m-reason  T-Boot-reason
}
```


(for more info on ASN.1 and ACN, go to [asn1scc](https://github.com/esa/asn1scc))

## RAW C transcoders

```c
bool icdt_boot_Status_encode(IcdUF_ByteBuffer* buffer, const IcdT_Boot_Status* value, uint32_t* errorCode)
{
    /* Error output is mandatory and is cleared on entry. */
    if (!icdt_set_success(errorCode))
    {
        return false;
    }

    /* Validate mandatory pointers before touching memory. */
    if ((buffer == NULL) || (value == NULL))
    {
        return icdt_set_error(errorCode, ICDT_ERROR_NULL_ARGUMENT);
    }

    /* Encode packet fields in wire order from the model. */
    if (!icdt_uint8_encode(buffer, &value->image_Index, errorCode))
    {
        return false;
    }
    if (!icdt_address_encode(buffer, &value->image_Address, errorCode))
    {
        return false;
    }
    if (!icdt_boot_reason_encode(buffer, &value->reason, errorCode))
    {
        return false;
    }

    /* Return success after full packet write and checks. */
    return true;
}

```

Please note that the above are illustrative demonstrations that evolve and may be incorrect.

# Usage

Launch the app (make run is the simplest way!) and open example-asw.xml from the demo. Click around. Then go to demo, and analyze the Makefiles. Run them. Enjoy, learn.
