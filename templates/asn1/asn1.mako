<%!
"""
ASN.1 packet definition template.

Generates one ASN.1 module containing:
  1. Named type definitions for every DataType referenced by any field.
  2. One SEQUENCE per Telecommand, each with metadata comments.
  3. One SEQUENCE per Telemetry, each with metadata comments.

Fixed-value parameters (ParameterKind.FixedValue) use a single-value INTEGER
constraint:  fieldName  INTEGER (VALUE..VALUE),  -- fixed = VALUE

Type indicator fields are annotated with a trailing comment.
"""
import re


# ── Name sanitizers ───────────────────────────────────────────────────────────

def _san(name, upper):
    s = re.sub(r'[^A-Za-z0-9]+', '-', (name or 'unknown').strip())
    s = re.sub(r'-+', '-', s).strip('-')
    if not s:
        s = 'unknown'
    return (s[0].upper() + s[1:]) if upper else (s[0].lower() + s[1:])


def TN(name):
    """ASN.1 type reference: T- prefix + UpperCamelCase."""
    return 'T-' + _san(name, True)


def FN(name):
    """ASN.1 field identifier: m- prefix + lowerCamelCase."""
    return 'm-' + _san(name, False)


# ── Integer helpers ───────────────────────────────────────────────────────────

def _parse_int(text):
    try:
        return int(str(text if text is not None else '0').strip(), 0)
    except Exception:
        return 0


def _int_range(bits, signed):
    if signed:
        return -(1 << (bits - 1)), (1 << (bits - 1)) - 1
    return 0, (1 << bits) - 1


# ── Type definitions ──────────────────────────────────────────────────────────

def _typedef_body(dt):
    """RHS of  TypeName ::= <body>  for a named Data Type."""
    kind = dt.Kind.ToString()

    if kind in ('SignedInteger', 'UnsignedInteger'):
        bits   = int(dt.Scalar.BitSize)
        lo, hi = _int_range(bits, kind == 'SignedInteger')
        return 'INTEGER ({0}..{1})'.format(lo, hi)

    if kind == 'Float':
        return 'REAL'

    if kind == 'Boolean':
        return 'BOOLEAN'

    if kind == 'BitString':
        return 'BIT STRING (SIZE({0}))'.format(int(dt.Scalar.BitSize))

    if kind == 'Enumerated':
        vals = list(dt.Values)
        n    = len(vals)
        if n == 0:
            return 'ENUMERATED { unknown (0) }'
        lines = []
        for i, v in enumerate(vals):
            rv    = v.RawValues[0] if len(v.RawValues) > 0 else i
            comma = ',' if i < n - 1 else ''
            lines.append('    {0} ({1}){2}'.format(FN(v.Name), rv, comma))
        return 'ENUMERATED {{\n{0}\n}}'.format('\n'.join(lines))

    if kind == 'Structure':
        flds = list(dt.Fields)
        n    = len(flds)
        if n == 0:
            return 'SEQUENCE {}'
        lines = ['SEQUENCE {']
        for i, sf in enumerate(flds):
            sft   = TN(sf.DataType.Name) if sf.DataType else 'NULL'
            comma = ',' if i < n - 1 else ''
            lines.append('    {0}  {1}{2}'.format(FN(sf.Name), sft, comma))
        lines.append('}')
        return '\n'.join(lines)

    if kind == 'Array':
        elem = TN(dt.ElementType.Name) if dt.ElementType else 'NULL'
        if dt.ArraySize is not None:
            mn = int(float(dt.ArraySize.Range.Min))
            mx = int(float(dt.ArraySize.Range.Max))
            inner = 'SEQUENCE (SIZE({0}..{1})) OF {2}'.format(mn, mx, elem)
        else:
            inner = 'SEQUENCE (SIZE(0..MAX)) OF {0}'.format(elem)
        return 'SEQUENCE {{\n    m-items  {0}\n}}'.format(inner)

    return 'NULL'


def _typedef_comment(dt):
    """Optional end-of-line comment for a named type."""
    kind = dt.Kind.ToString()
    if kind == 'SignedInteger':
        return '  -- {0}-bit signed integer, {1}'.format(
            int(dt.Scalar.BitSize), dt.Scalar.Endianness)
    if kind == 'UnsignedInteger':
        return '  -- {0}-bit unsigned integer, {1}'.format(
            int(dt.Scalar.BitSize), dt.Scalar.Endianness)
    if kind == 'Float':
        return '  -- IEEE 754 {0}-bit, {1}'.format(
            int(dt.Scalar.BitSize), dt.Scalar.Endianness)
    if kind == 'Boolean':
        return '  -- {0}-bit boolean'.format(int(dt.Scalar.BitSize))
    if kind == 'BitString':
        return '  -- {0}-bit string'.format(int(dt.Scalar.BitSize))
    if kind == 'Enumerated':
        return '  -- {0}-bit enumerated, {1}'.format(int(dt.BitSize), dt.Endianness)
    return ''


# ── Field helpers ─────────────────────────────────────────────────────────────

def _field_type(dt, is_fixed, hex_val):
    """
    ASN.1 type expression for a SEQUENCE field.
    Scalars are inlined; complex types reference the named type.
    Fixed-value integer fields use INTEGER (VALUE..VALUE).
    """
    if dt is None:
        return 'NULL'
    kind = dt.Kind.ToString()

    if kind == 'SignedInteger':
        bits = int(dt.Scalar.BitSize)
        if is_fixed:
            v = _parse_int(hex_val)
            return 'INTEGER ({0}..{0})'.format(v)
        lo, hi = _int_range(bits, True)
        return 'INTEGER ({0}..{1})'.format(lo, hi)

    if kind == 'UnsignedInteger':
        bits = int(dt.Scalar.BitSize)
        if is_fixed:
            v = _parse_int(hex_val)
            return 'INTEGER ({0}..{0})'.format(v)
        lo, hi = _int_range(bits, False)
        return 'INTEGER ({0}..{1})'.format(lo, hi)

    if kind == 'Float':
        return 'REAL'

    if kind == 'Boolean':
        return 'BOOLEAN'

    if kind == 'BitString':
        return 'BIT STRING (SIZE({0}))'.format(int(dt.Scalar.BitSize))

    # Enumerated, Structure, Array: reference by name
    return TN(dt.Name)


def _field_comment(f, dt, is_fixed, hex_val):
    """Trailing comment parts for a SEQUENCE field line."""
    parts = []
    if f.IsTypeIndicator:
        parts.append('type indicator' + (' = {}'.format(f.IndicatorValue) if f.IndicatorValue else ''))
    if is_fixed and hex_val:
        parts.append('fixed = {}'.format(hex_val))
    if dt is not None and dt.Kind.ToString() in ('SignedInteger', 'UnsignedInteger', 'Float'):
        if dt.Numeric is not None and dt.Numeric.Unit:
            parts.append('unit: {}'.format(dt.Numeric.Unit))
    return ('  -- ' + ', '.join(parts)) if parts else ''


def _field_line(f, comma):
    """Complete  fieldName  Type[,]  [-- comment]  line for a SEQUENCE field."""
    param    = f.Parameter
    dt       = param.DataType if param is not None else None
    is_fixed = param is not None and param.Kind.ToString() == 'FixedValue'
    hex_val  = str(param.HexValue) if (is_fixed and param is not None and param.HexValue) else None
    ftype    = _field_type(dt, is_fixed, hex_val)
    comment  = _field_comment(f, dt, is_fixed, hex_val)
    return '    {0}  {1}{2}{3}'.format(FN(f.Name), ftype, comma, comment)


# ── Packet comment block ──────────────────────────────────────────────────────

def _header_id_values(packet):
    ht = packet.HeaderType
    if ht is None:
        return ''
    parts = []
    for hid in ht.Ids:
        match = next(
            (v for v in packet.HeaderIdValues if str(v.IdRef) == str(hid.Id)), None)
        val = str(match.Value) if match is not None else '?'
        parts.append('{0}={1}'.format(hid.Name, val))
    return ', '.join(parts)


def _packet_comments(pt):
    lines = ['-- Packet  : {}'.format(pt.Name)]
    if pt.Mnemonic:
        lines.append('-- Mnemonic: {}'.format(str(pt.Mnemonic).strip()))
    lines.append('-- ID      : 0x{:X}'.format(pt.NumericId))
    if pt.HeaderType:
        ids = _header_id_values(pt)
        lines.append('-- Header  : {}  ({})'.format(pt.HeaderType.Name, ids))
    if pt.Description:
        for ln in str(pt.Description).strip().splitlines():
            lines.append('-- {}'.format(ln.strip()))
    return '\n'.join(lines)


# ── Type collection ───────────────────────────────────────────────────────────

def collect_types(packets):
    """All DataTypes used by the packets, in dependency order (leaves first)."""
    ordered, seen = [], set()

    def visit(dt):
        if dt is None:
            return
        key = str(dt.Id)
        if key in seen:
            return
        seen.add(key)
        kind = dt.Kind.ToString()
        if kind == 'Structure':
            for sf in dt.Fields:
                visit(sf.DataType)
        elif kind == 'Array':
            visit(dt.ElementType)
        ordered.append(dt)

    for pt in packets:
        for f in pt.Fields:
            if f.Parameter is not None:
                visit(f.Parameter.DataType)
    return ordered
%>
<%
module_name  = TN(model.Metadata.Name) if model.Metadata.Name else 'IcdPackets'
all_packets  = list(model.PacketTypes)
all_types    = collect_types(all_packets)
telecommands = [p for p in all_packets if p.Kind.ToString() == 'Telecommand']
telemetries  = [p for p in all_packets if p.Kind.ToString() == 'Telemetry']
%>\
-- =============================================================================
-- AUTO-GENERATED BY icdfyit -- DO NOT EDIT
-- ICD     : ${model.Metadata.Name or '(unnamed)'}
% if model.Metadata.Version:
-- Version : ${model.Metadata.Version | trim}
% endif
% if model.Metadata.Date:
-- Date    : ${model.Metadata.Date | trim}
% endif
-- =============================================================================

${module_name}Packets DEFINITIONS AUTOMATIC TAGS ::= BEGIN
% if all_types:

-- ---------------------------------------------------------------------------
-- Data Types
-- ---------------------------------------------------------------------------

% for dt in all_types:
${TN(dt.Name)} ::= ${_typedef_body(dt)}${_typedef_comment(dt)}

% endfor
% endif
% if telecommands:

-- ---------------------------------------------------------------------------
-- Telecommands
-- ---------------------------------------------------------------------------

% for pt in telecommands:
<%
    fields   = list(pt.Fields)
    n        = len(fields)
%>
${_packet_comments(pt)}
${TN(pt.Name)} ::= SEQUENCE {
% if n == 0:
    -- (no fields defined)
% else:
% for i, f in enumerate(fields):
${_field_line(f, ',' if i < n - 1 else '')}
% endfor
% endif
}

% endfor
% endif
% if telemetries:

-- ---------------------------------------------------------------------------
-- Telemetries
-- ---------------------------------------------------------------------------

% for pt in telemetries:
<%
    fields   = list(pt.Fields)
    n        = len(fields)
%>
${_packet_comments(pt)}
${TN(pt.Name)} ::= SEQUENCE {
% if n == 0:
    -- (no fields defined)
% else:
% for i, f in enumerate(fields):
${_field_line(f, ',' if i < n - 1 else '')}
% endfor
% endif
}

% endfor
% endif

END
