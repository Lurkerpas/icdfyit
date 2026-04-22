<%!
"""
C transcoder source template.
Generates model-specific encode/decode definitions using utility functions.
"""
import re


def _sanitize(name, upper_first):
    s = re.sub(r'[^A-Za-z0-9_]+', '_', (name or 'unnamed').strip())
    s = re.sub(r'_+', '_', s).strip('_')
    if not s:
        s = 'unnamed'
    if s[0].isdigit():
        s = 'x_' + s
    if upper_first:
        return s[0].upper() + s[1:]
    return s[0].lower() + s[1:]


def T(name):
    return 'IcdT_' + _sanitize(name, True)


def F(name):
    return _sanitize(name, False)


def C(name):
    return _sanitize(name, True).upper()


def parse_int_literal(text):
    s = str(text if text is not None else '').strip()
    if not s:
        raise RuntimeError('Empty numeric literal in model where numeric constant was expected.')
    return int(s, 0)


def detect_supported_type(dt):
    kind = dt.Kind.ToString()

    if kind == 'BitString':
        raise RuntimeError(f"Data Type '{dt.Name}' is BitString and is not supported by utility transcoders.")

    if kind == 'SignedInteger':
        bits = int(dt.Scalar.BitSize)
        if bits not in (8, 16, 24, 32):
            raise RuntimeError(f"Data Type '{dt.Name}' has unsupported signed integer size {bits}.")
        return

    if kind == 'UnsignedInteger':
        bits = int(dt.Scalar.BitSize)
        if bits not in (8, 16, 24, 32):
            raise RuntimeError(f"Data Type '{dt.Name}' has unsupported unsigned integer size {bits}.")
        return

    if kind == 'Boolean':
        bits = int(dt.Scalar.BitSize)
        if bits not in (8, 16, 24, 32):
            raise RuntimeError(f"Data Type '{dt.Name}' has unsupported boolean size {bits}.")
        return

    if kind == 'Float':
        bits = int(dt.Scalar.BitSize)
        if bits != 32:
            raise RuntimeError(f"Data Type '{dt.Name}' has unsupported float size {bits}; only 32-bit is supported.")
        return

    if kind == 'Enumerated':
        bits = int(dt.BitSize)
        if bits not in (8, 16, 24, 32):
            raise RuntimeError(f"Data Type '{dt.Name}' has unsupported enumerated size {bits}.")
        return

    if kind == 'Structure':
        for sf in dt.Fields:
            if sf.DataType is None:
                raise RuntimeError(f"Structure '{dt.Name}' contains field '{sf.Name}' without DataType reference.")
        return

    if kind == 'Array':
        if dt.ElementType is None:
            raise RuntimeError(f"Array '{dt.Name}' has no ElementType reference.")
        if dt.ArraySize is None:
            raise RuntimeError(f"Array '{dt.Name}' has no ArraySize descriptor.")
        size_bits = int(dt.ArraySize.BitSize)
        if size_bits not in (8, 16, 24, 32):
            raise RuntimeError(f"Array '{dt.Name}' has unsupported length-bit-size {size_bits}.")
        if float(dt.ArraySize.Range.Min) < 0:
            raise RuntimeError(f"Array '{dt.Name}' has negative minimum length {dt.ArraySize.Range.Min}.")
        if float(dt.ArraySize.Range.Max) < float(dt.ArraySize.Range.Min):
            raise RuntimeError(f"Array '{dt.Name}' has invalid length range [{dt.ArraySize.Range.Min}, {dt.ArraySize.Range.Max}].")
        return

    raise RuntimeError(f"Data Type '{dt.Name}' has unsupported kind '{kind}'.")


def resolve_closure(start_types):
    ordered = []
    seen = set()

    def visit(dt):
        dt_id = str(dt.Id)
        if dt_id in seen:
            return
        seen.add(dt_id)

        kind = dt.Kind.ToString()
        if kind == 'Structure':
            for sf in dt.Fields:
                visit(sf.DataType)
        elif kind == 'Array':
            visit(dt.ElementType)

        ordered.append(dt)

    for t in start_types:
        visit(t)

    return ordered


def declared_c_type(dt):
    return T(dt.Name)


def enum_has_negative_values(dt):
    for v in dt.Values:
        if len(v.RawValues) > 0 and int(v.RawValues[0]) < 0:
            return True
    return False


def utility_unsigned(bits, endianness, encode):
    suffix = 'be' if endianness == 'BigEndian' else 'le'
    op = 'encode' if encode else 'decode'
    return f'icduf_{op}_u{bits}_{suffix}'


def utility_signed(bits, endianness, encode):
    suffix = 'be' if endianness == 'BigEndian' else 'le'
    op = 'encode' if encode else 'decode'
    return f'icduf_{op}_s{bits}_{suffix}'


def utility_bool(bits, endianness, encode):
    suffix = 'be' if endianness == 'BigEndian' else 'le'
    op = 'encode' if encode else 'decode'
    return f'icduf_{op}_bool{bits}_{suffix}'


def utility_float32(endianness, encode):
    suffix = 'be' if endianness == 'BigEndian' else 'le'
    op = 'encode' if encode else 'decode'
    return f'icduf_{op}_f32_{suffix}'


def packet_type_enum_name(pt):
    return 'IcdT_PacketType_' + _sanitize(pt.Name, True)


def header_param_name(ht_id):
    return F('header_' + ht_id.Name)
%><%
# Validate all model data types for transcodability as required by the design.
for _dt in model.DataTypes:
    detect_supported_type(_dt)

packet_types = list(model.PacketTypes)
used_params = []
seen_param_ids = set()
for p in packet_types:
    for field in p.Fields:
        if field.Parameter is None:
            raise RuntimeError(f"Packet '{p.Name}' field '{field.Name}' has no Parameter reference.")
        pid = str(field.Parameter.Id)
        if pid not in seen_param_ids:
            seen_param_ids.add(pid)
            used_params.append(field.Parameter)

root_types = []
seen_root_ids = set()
for prm in used_params:
    if prm.DataType is None:
        raise RuntimeError(f"Parameter '{prm.Name}' has no DataType reference.")
    dt_id = str(prm.DataType.Id)
    if dt_id not in seen_root_ids:
        seen_root_ids.add(dt_id)
        root_types.append(prm.DataType)

used_types = resolve_closure(root_types)
%>#include "transcoders.h"

#include <stddef.h>

/* Writes success code in a single place to keep behavior consistent. */
static bool icdt_set_success(uint32_t* errorCode)
{
    /* Error output pointer is mandatory by API contract. */
    if (errorCode == NULL)
    {
        return false;
    }

    /* Every successful call clears any prior error. */
    *errorCode = ICDT_ERROR_NONE;
    return true;
}

/* Writes error code and returns false in one helper. */
static bool icdt_set_error(uint32_t* errorCode, IcdT_ErrorCode error)
{
    /* Error output pointer is mandatory by API contract. */
    if (errorCode == NULL)
    {
        return false;
    }

    /* Expose canonical model-level error to caller. */
    *errorCode = (uint32_t)error;
    return false;
}

/* Converts utility-layer failures to model-layer error domain. */
static bool icdt_fail_from_utility(uint32_t utilityError, uint32_t* errorCode)
{
    /* Keep explicit mapping so callers are insulated from utility enum changes. */
    switch (utilityError)
    {
        case ICDUF_ERROR_NONE:
            return icdt_set_success(errorCode);
        case ICDUF_ERROR_NULL_ARGUMENT:
            return icdt_set_error(errorCode, ICDT_ERROR_NULL_ARGUMENT);
        case ICDUF_ERROR_INVALID_ARGUMENT:
            return icdt_set_error(errorCode, ICDT_ERROR_INVALID_ARGUMENT);
        case ICDUF_ERROR_BUFFER_OVERFLOW:
            return icdt_set_error(errorCode, ICDT_ERROR_BUFFER_OVERFLOW);
        case ICDUF_ERROR_BUFFER_UNDERFLOW:
            return icdt_set_error(errorCode, ICDT_ERROR_BUFFER_UNDERFLOW);
        case ICDUF_ERROR_RANGE:
            return icdt_set_error(errorCode, ICDT_ERROR_RANGE);
        default:
            return icdt_set_error(errorCode, ICDT_ERROR_UTILITY_FAILED);
    }
}

% for dt in used_types:
<%
kind = dt.Kind.ToString()
%>
% if kind == 'Enumerated':
/* Validates whether decoded/encoded enum value belongs to configured value set. */
static bool icdt_${F(dt.Name)}_is_valid(${declared_c_type(dt)} value)
{
    /* Switch-based validation is explicit and constant-time for each known literal. */
    switch (value)
    {
% if len(list(dt.Values)) == 0:
        default:
            return false;
% else:
% for ev in dt.Values:
        case ${T(dt.Name)}_${_sanitize(ev.Name, True)}:
            return true;
% endfor
        default:
            return false;
% endif
    }
}

% endif
% endfor
/* ── Data Type transcoder definitions ──────────────────────────────────────── */
% for dt in used_types:
<%
kind = dt.Kind.ToString()
%>
bool icdt_${F(dt.Name)}_encode(IcdUF_ByteBuffer* buffer, const ${declared_c_type(dt)}* value, uint32_t* errorCode)
{
    uint32_t utilErr = ICDUF_ERROR_NONE;

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

% if kind == 'SignedInteger':
    /* Apply configured numeric range constraint before encoding. */
% if dt.Numeric is not None:
    if (((double)(*value) < ${float(dt.Numeric.Range.Min)}) || ((double)(*value) > ${float(dt.Numeric.Range.Max)}))
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }
% endif

    /* Delegate low-level byte encoding to utility layer. */
    if (!${utility_signed(int(dt.Scalar.BitSize), dt.Scalar.Endianness.ToString(), True)}(buffer, *value, &utilErr))
    {
        return icdt_fail_from_utility(utilErr, errorCode);
    }

% elif kind == 'UnsignedInteger':
    /* Apply configured numeric range constraint before encoding. */
% if dt.Numeric is not None:
    if (((double)(*value) < ${float(dt.Numeric.Range.Min)}) || ((double)(*value) > ${float(dt.Numeric.Range.Max)}))
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }
% endif

    /* Delegate low-level byte encoding to utility layer. */
    if (!${utility_unsigned(int(dt.Scalar.BitSize), dt.Scalar.Endianness.ToString(), True)}(buffer, *value, &utilErr))
    {
        return icdt_fail_from_utility(utilErr, errorCode);
    }

% elif kind == 'Boolean':
    /* Delegate low-level byte encoding to utility layer. */
    if (!${utility_bool(int(dt.Scalar.BitSize), dt.Scalar.Endianness.ToString(), True)}(buffer, *value, &utilErr))
    {
        return icdt_fail_from_utility(utilErr, errorCode);
    }

% elif kind == 'Float':
    /* Apply configured numeric range constraint before encoding. */
% if dt.Numeric is not None:
    if (((double)(*value) < ${float(dt.Numeric.Range.Min)}) || ((double)(*value) > ${float(dt.Numeric.Range.Max)}))
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }
% endif

    /* Delegate low-level byte encoding to utility layer. */
    if (!${utility_float32(dt.Scalar.Endianness.ToString(), True)}(buffer, *value, &utilErr))
    {
        return icdt_fail_from_utility(utilErr, errorCode);
    }

% elif kind == 'Enumerated':
    /* Enumerated values must be one of configured literals. */
    if (!icdt_${F(dt.Name)}_is_valid(*value))
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }

    /* Delegate low-level byte encoding to utility layer. */
% if enum_has_negative_values(dt):
    if (!${utility_signed(int(dt.BitSize), dt.Endianness.ToString(), True)}(buffer, *value, &utilErr))
% else:
    if (!${utility_unsigned(int(dt.BitSize), dt.Endianness.ToString(), True)}(buffer, *value, &utilErr))
% endif
    {
        return icdt_fail_from_utility(utilErr, errorCode);
    }

% elif kind == 'Structure':
    /* Encode each structure field in declared order. */
% for sf in dt.Fields:
    if (!icdt_${F(sf.DataType.Name)}_encode(buffer, &value->${F(sf.Name)}, errorCode))
    {
        return false;
    }
% endfor

% elif kind == 'Array':
    uint32_t lengthOnWire = value->count;

    /* Count must fit static storage and configured min/max limits. */
    if (value->count > ICDT_${C(dt.Name)}_MAX_LENGTH)
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }
    if ((value->count < (uint32_t)(${int(float(dt.ArraySize.Range.Min))})) ||
        (value->count > (uint32_t)(${int(float(dt.ArraySize.Range.Max))})))
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }

    /* Encode declared element count first using utility primitive. */
    if (!${utility_unsigned(int(dt.ArraySize.BitSize), dt.ArraySize.Endianness.ToString(), True)}(buffer, lengthOnWire, &utilErr))
    {
        return icdt_fail_from_utility(utilErr, errorCode);
    }

    /* Encode each array element in order. */
    for (uint32_t i = 0u; i < value->count; ++i)
    {
        if (!icdt_${F(dt.ElementType.Name)}_encode(buffer, &value->items[i], errorCode))
        {
            return false;
        }
    }

% endif
    /* Return clean success after all checks and writes are complete. */
    return true;
}

bool icdt_${F(dt.Name)}_decode(IcdUF_ByteBuffer* buffer, ${declared_c_type(dt)}* value, uint32_t* errorCode)
{
    uint32_t utilErr = ICDUF_ERROR_NONE;

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

% if kind == 'SignedInteger':
    /* Decode low-level representation from utility layer. */
    if (!${utility_signed(int(dt.Scalar.BitSize), dt.Scalar.Endianness.ToString(), False)}(buffer, value, &utilErr))
    {
        return icdt_fail_from_utility(utilErr, errorCode);
    }

    /* Enforce configured numeric constraint on decoded value. */
% if dt.Numeric is not None:
    if (((double)(*value) < ${float(dt.Numeric.Range.Min)}) || ((double)(*value) > ${float(dt.Numeric.Range.Max)}))
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }
% endif

% elif kind == 'UnsignedInteger':
    /* Decode low-level representation from utility layer. */
    if (!${utility_unsigned(int(dt.Scalar.BitSize), dt.Scalar.Endianness.ToString(), False)}(buffer, value, &utilErr))
    {
        return icdt_fail_from_utility(utilErr, errorCode);
    }

    /* Enforce configured numeric constraint on decoded value. */
% if dt.Numeric is not None:
    if (((double)(*value) < ${float(dt.Numeric.Range.Min)}) || ((double)(*value) > ${float(dt.Numeric.Range.Max)}))
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }
% endif

% elif kind == 'Boolean':
    /* Decode low-level representation from utility layer. */
    if (!${utility_bool(int(dt.Scalar.BitSize), dt.Scalar.Endianness.ToString(), False)}(buffer, value, &utilErr))
    {
        return icdt_fail_from_utility(utilErr, errorCode);
    }

% elif kind == 'Float':
    /* Decode low-level representation from utility layer. */
    if (!${utility_float32(dt.Scalar.Endianness.ToString(), False)}(buffer, value, &utilErr))
    {
        return icdt_fail_from_utility(utilErr, errorCode);
    }

    /* Enforce configured numeric constraint on decoded value. */
% if dt.Numeric is not None:
    if (((double)(*value) < ${float(dt.Numeric.Range.Min)}) || ((double)(*value) > ${float(dt.Numeric.Range.Max)}))
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }
% endif

% elif kind == 'Enumerated':
    /* Decode low-level representation from utility layer. */
% if enum_has_negative_values(dt):
    if (!${utility_signed(int(dt.BitSize), dt.Endianness.ToString(), False)}(buffer, value, &utilErr))
% else:
    if (!${utility_unsigned(int(dt.BitSize), dt.Endianness.ToString(), False)}(buffer, (uint32_t*)value, &utilErr))
% endif
    {
        return icdt_fail_from_utility(utilErr, errorCode);
    }

    /* Enumerated values must be one of configured literals. */
    if (!icdt_${F(dt.Name)}_is_valid(*value))
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }

% elif kind == 'Structure':
    /* Decode each structure field in declared order. */
% for sf in dt.Fields:
    if (!icdt_${F(sf.DataType.Name)}_decode(buffer, &value->${F(sf.Name)}, errorCode))
    {
        return false;
    }
% endfor

% elif kind == 'Array':
    uint32_t lengthOnWire = 0u;

    /* Decode declared element count first using utility primitive. */
    if (!${utility_unsigned(int(dt.ArraySize.BitSize), dt.ArraySize.Endianness.ToString(), False)}(buffer, &lengthOnWire, &utilErr))
    {
        return icdt_fail_from_utility(utilErr, errorCode);
    }

    /* Count must fit static storage and configured min/max limits. */
    if (lengthOnWire > ICDT_${C(dt.Name)}_MAX_LENGTH)
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }
    if ((lengthOnWire < (uint32_t)(${int(float(dt.ArraySize.Range.Min))})) ||
        (lengthOnWire > (uint32_t)(${int(float(dt.ArraySize.Range.Max))})))
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }

    /* Store validated count and decode elements in order. */
    value->count = lengthOnWire;
    for (uint32_t i = 0u; i < value->count; ++i)
    {
        if (!icdt_${F(dt.ElementType.Name)}_decode(buffer, &value->items[i], errorCode))
        {
            return false;
        }
    }

% endif
    /* Return clean success after all checks and reads are complete. */
    return true;
}

% endfor
/* ── Packet transcoder definitions ─────────────────────────────────────────── */
% for pt in packet_types:
bool icdt_${F(pt.Name)}_encode(IcdUF_ByteBuffer* buffer, const ${T(pt.Name)}* value, uint32_t* errorCode)
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
% for pf in pt.Fields:
% if pf.IsTypeIndicator:
    /* Type-indicator field '${pf.Name}' must match configured constant. */
    if (((int64_t)value->${F(pf.Name)}) != ${parse_int_literal(pf.IndicatorValue)}LL)
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }
% endif
    if (!icdt_${F(pf.Parameter.DataType.Name)}_encode(buffer, &value->${F(pf.Name)}, errorCode))
    {
        return false;
    }
% endfor

    /* Return success after full packet write and checks. */
    return true;
}

bool icdt_${F(pt.Name)}_decode(IcdUF_ByteBuffer* buffer, ${T(pt.Name)}* value, uint32_t* errorCode)
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

    /* Decode packet fields in wire order from the model. */
% for pf in pt.Fields:
    if (!icdt_${F(pf.Parameter.DataType.Name)}_decode(buffer, &value->${F(pf.Name)}, errorCode))
    {
        return false;
    }
% if pf.IsTypeIndicator:
    /* Type-indicator field '${pf.Name}' must match configured constant. */
    if (((int64_t)value->${F(pf.Name)}) != ${parse_int_literal(pf.IndicatorValue)}LL)
    {
        return icdt_set_error(errorCode, ICDT_ERROR_CONSTRAINT);
    }
% endif
% endfor

    /* Return success after full packet read and checks. */
    return true;
}

% endfor
/* ── Header resolver definitions (per HeaderType) ─────────────────────────── */
% for ht in model.HeaderTypes:
bool icdt_${F(ht.Name)}_get_packet_type(
% for hid in ht.Ids:
    int64_t ${header_param_name(hid)},
% endfor
    IcdT_PacketType* packetType,
    uint32_t* errorCode)
{
    /* Error output is mandatory and is cleared on entry. */
    if (!icdt_set_success(errorCode))
    {
        return false;
    }

    /* Destination pointer is required for result reporting. */
    if (packetType == NULL)
    {
        return icdt_set_error(errorCode, ICDT_ERROR_NULL_ARGUMENT);
    }

    /* Default to Unknown until a precise match is found. */
    *packetType = IcdT_PacketType_Unknown;

% for pt in packet_types:
% if pt.HeaderType is not None and str(pt.HeaderType.Id) == str(ht.Id):
<%
hid_values = {str(e.IdRef): parse_int_literal(e.Value) for e in pt.HeaderIdValues}
missing = [hid.Name for hid in ht.Ids if str(hid.Id) not in hid_values]
if len(missing) > 0:
    raise RuntimeError(f"Packet '{pt.Name}' is bound to Header Type '{ht.Name}' but is missing header ID values for: {', '.join(missing)}")
%>
    /* Check whether header IDs match Packet Type '${pt.Name}'. */
    if (
% if len(list(ht.Ids)) == 0:
        true
% else:
% for i, hid in enumerate(ht.Ids):
        (${header_param_name(hid)} == ${hid_values[str(hid.Id)]}LL)${' &&' if i < len(list(ht.Ids)) - 1 else ''}
% endfor
% endif
    )
    {
        *packetType = ${packet_type_enum_name(pt)};
        return true;
    }

% endif
% endfor
    /* No packet matched the provided header values. */
    return icdt_set_error(errorCode, ICDT_ERROR_UNKNOWN_PACKET_TYPE);
}

% endfor
