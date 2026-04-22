<%!
"""
C transcoder header template.
Generates model-specific type declarations and encode/decode function declarations.
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


def ctype_for_scalar_bits(is_signed, bits):
    if bits == 8:
        return 'int8_t' if is_signed else 'uint8_t'
    if bits == 16:
        return 'int16_t' if is_signed else 'uint16_t'
    if bits == 24:
        return 'int32_t' if is_signed else 'uint32_t'
    if bits == 32:
        return 'int32_t' if is_signed else 'uint32_t'
    return None


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
    kind = dt.Kind.ToString()

    if kind == 'SignedInteger':
        return T(dt.Name)
    if kind == 'UnsignedInteger':
        return T(dt.Name)
    if kind == 'Boolean':
        return T(dt.Name)
    if kind == 'Float':
        return T(dt.Name)
    if kind == 'Enumerated':
        return T(dt.Name)
    if kind == 'Structure':
        return T(dt.Name)
    if kind == 'Array':
        return T(dt.Name)

    raise RuntimeError(f"Cannot map Data Type '{dt.Name}' to C type.")


def header_param_name(ht_id):
    return F('header_' + ht_id.Name)


def include_guard(module_name):
    return 'ICDT_' + C(module_name) + '_H'
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

guard = include_guard('transcoders_generated')
%>#ifndef ${guard}
#define ${guard}

#include <stdbool.h>
#include <stdint.h>

#include "icdfyit-transcoders.h"

#ifdef __cplusplus
extern "C" {
#endif

/** @brief Model-level transcoder error codes (static set, model-independent). */
typedef enum IcdT_ErrorCode
{
    ICDT_ERROR_NONE = 0u,
    ICDT_ERROR_NULL_ARGUMENT = 1u,
    ICDT_ERROR_INVALID_ARGUMENT = 2u,
    ICDT_ERROR_BUFFER_OVERFLOW = 3u,
    ICDT_ERROR_BUFFER_UNDERFLOW = 4u,
    ICDT_ERROR_RANGE = 5u,
    ICDT_ERROR_CONSTRAINT = 6u,
    ICDT_ERROR_UNSUPPORTED_MODEL = 7u,
    ICDT_ERROR_UNKNOWN_PACKET_TYPE = 8u,
    ICDT_ERROR_UTILITY_FAILED = 9u
} IcdT_ErrorCode;

/** @brief Packet Type discriminator for model-level packet decode selection. */
typedef enum IcdT_PacketType
{
    IcdT_PacketType_Unknown = 0,
% for idx, pt in enumerate(packet_types):
    IcdT_PacketType_${_sanitize(pt.Name, True)} = ${idx + 1},
% endfor
} IcdT_PacketType;

/* ── Data Type declarations (only types reachable from packet parameters) ── */
% for dt in used_types:
<%
kind = dt.Kind.ToString()
%>
% if kind == 'SignedInteger':
/** @brief Data Type '${dt.Name}' (${dt.Scalar.BitSize}-bit signed integer). */
typedef ${ctype_for_scalar_bits(True, int(dt.Scalar.BitSize))} ${T(dt.Name)};
% elif kind == 'UnsignedInteger':
/** @brief Data Type '${dt.Name}' (${dt.Scalar.BitSize}-bit unsigned integer). */
typedef ${ctype_for_scalar_bits(False, int(dt.Scalar.BitSize))} ${T(dt.Name)};
% elif kind == 'Boolean':
/** @brief Data Type '${dt.Name}' (${dt.Scalar.BitSize}-bit boolean on wire, bool in memory). */
typedef bool ${T(dt.Name)};
% elif kind == 'Float':
/** @brief Data Type '${dt.Name}' (32-bit float). */
typedef float ${T(dt.Name)};
% elif kind == 'Enumerated':
/** @brief Data Type '${dt.Name}' (enumerated). */
typedef enum ${T(dt.Name)}
{
% if len(list(dt.Values)) == 0:
    ${T(dt.Name)}_Unknown = 0
% else:
% for i, ev in enumerate(dt.Values):
    ${T(dt.Name)}_${_sanitize(ev.Name, True)} = ${parse_int_literal(ev.RawValues[0] if len(ev.RawValues) > 0 else i)}${',' if i < len(list(dt.Values)) - 1 else ''}
% endfor
% endif
} ${T(dt.Name)};
% elif kind == 'Structure':
/** @brief Data Type '${dt.Name}' (structure). */
typedef struct ${T(dt.Name)}
{
% for sf in dt.Fields:
    ${declared_c_type(sf.DataType)} ${F(sf.Name)};
% endfor
} ${T(dt.Name)};
% elif kind == 'Array':
/** @brief Maximum element count for array Data Type '${dt.Name}'. */
#define ICDT_${C(dt.Name)}_MAX_LENGTH (${int(float(dt.ArraySize.Range.Max))}u)

/** @brief Data Type '${dt.Name}' (array with explicit count and fixed max storage). */
typedef struct ${T(dt.Name)}
{
    uint32_t count;
    ${declared_c_type(dt.ElementType)} items[ICDT_${C(dt.Name)}_MAX_LENGTH];
} ${T(dt.Name)};
% endif

% endfor
/* ── Packet Type declarations ─────────────────────────────────────────────── */
% for pt in packet_types:
/** @brief Packet Type '${pt.Name}'. */
typedef struct ${T(pt.Name)}
{
% for pf in pt.Fields:
    ${declared_c_type(pf.Parameter.DataType)} ${F(pf.Name)};
% endfor
} ${T(pt.Name)};

% endfor
/* ── Data Type transcoder function declarations ───────────────────────────── */
% for dt in used_types:
/**
 * @brief Encodes Data Type '${dt.Name}' to buffer.
 * @param buffer Destination byte buffer.
 * @param value Input value.
 * @param errorCode Output error code.
 * @return true on success, false on error.
 */
bool icdt_${F(dt.Name)}_encode(IcdUF_ByteBuffer* buffer, const ${declared_c_type(dt)}* value, uint32_t* errorCode);

/**
 * @brief Decodes Data Type '${dt.Name}' from buffer.
 * @param buffer Source byte buffer.
 * @param value Output value.
 * @param errorCode Output error code.
 * @return true on success, false on error.
 */
bool icdt_${F(dt.Name)}_decode(IcdUF_ByteBuffer* buffer, ${declared_c_type(dt)}* value, uint32_t* errorCode);

% endfor
/* ── Packet transcoder function declarations ──────────────────────────────── */
% for pt in packet_types:
/**
 * @brief Encodes Packet Type '${pt.Name}' to buffer.
 * @param buffer Destination byte buffer.
 * @param value Input packet.
 * @param errorCode Output error code.
 * @return true on success, false on error.
 */
bool icdt_${F(pt.Name)}_encode(IcdUF_ByteBuffer* buffer, const ${T(pt.Name)}* value, uint32_t* errorCode);

/**
 * @brief Decodes Packet Type '${pt.Name}' from buffer.
 * @param buffer Source byte buffer.
 * @param value Output packet.
 * @param errorCode Output error code.
 * @return true on success, false on error.
 */
bool icdt_${F(pt.Name)}_decode(IcdUF_ByteBuffer* buffer, ${T(pt.Name)}* value, uint32_t* errorCode);

% endfor
/* ── Header resolver declarations (per HeaderType) ────────────────────────── */
% for ht in model.HeaderTypes:
/**
 * @brief Resolves packet type from Header Type '${ht.Name}' ID values.
 * @param buffer Packet payload buffer at field start used for type-indicator disambiguation.
 * @param packetType Output resolved packet type.
 * @param errorCode Output error code.
 * @return true when a packet type match is found, false otherwise.
 */
bool icdt_${F(ht.Name)}_get_packet_type(
% for i, hid in enumerate(ht.Ids):
    int64_t ${header_param_name(hid)},
% endfor
    IcdUF_ByteBuffer* buffer,
    IcdT_PacketType* packetType,
    uint32_t* errorCode);

% endfor
#ifdef __cplusplus
}
#endif

#endif
