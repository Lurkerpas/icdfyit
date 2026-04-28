<%!
"""
ICD Markdown Tables template.

Renders a Markdown document with separate chapters for Telecommands and Telemetries.
Each packet type gets a sub-chapter with metadata prose and a field table.

Table columns:
  Parameter  – field name; indicator fields are annotated with "[type indicator: <value>]"
  Type       – data type description; fixed-value parameters include "= <hex>" in this cell
  Unit       – physical unit (numeric types only, omitted when not set)
  Size       – wire size in bytes (or bits when not byte-aligned); "variable" for arrays
  Offset     – cumulative bit offset from the first field; "unknown" once a variable-size
               field has been encountered
"""


# ── Data-type helpers ─────────────────────────────────────────────────────────

def _bit_size(dt):
    """Return (bits: int, variable: bool) for a DataType, or (0, False) when dt is None."""
    if dt is None:
        return (0, False)
    kind = dt.Kind.ToString()
    if kind in ('SignedInteger', 'UnsignedInteger', 'Float', 'Boolean', 'BitString'):
        return (int(dt.Scalar.BitSize), False)
    if kind == 'Enumerated':
        return (int(dt.BitSize), False)
    if kind == 'Structure':
        total, variable = 0, False
        for sf in dt.Fields:
            if sf.DataType is None:
                continue
            sz, var = _bit_size(sf.DataType)
            total += sz
            if var:
                variable = True
        return (total, variable)
    if kind == 'Array':
        # The length prefix has a fixed width; the payload itself is variable.
        prefix_bits = int(dt.ArraySize.BitSize) if dt.ArraySize is not None else 0
        return (prefix_bits, True)
    return (0, False)


def _unit(dt):
    """Return the physical unit string for numeric types, or '' when absent."""
    if dt is None:
        return ''
    kind = dt.Kind.ToString()
    if kind in ('SignedInteger', 'UnsignedInteger', 'Float'):
        if dt.Numeric is not None and dt.Numeric.Unit:
            return str(dt.Numeric.Unit)
    return ''


def _type_label(dt, param):
    """
    Human-readable type description.
    Appends '= <value>' when the parameter kind is FixedValue.
    """
    if dt is None:
        return '(unknown type)'
    kind = dt.Kind.ToString()
    if kind == 'SignedInteger':
        label = 'int{}'.format(int(dt.Scalar.BitSize))
    elif kind == 'UnsignedInteger':
        label = 'uint{}'.format(int(dt.Scalar.BitSize))
    elif kind == 'Float':
        label = 'float{}'.format(int(dt.Scalar.BitSize))
    elif kind == 'Boolean':
        label = 'bool{}'.format(int(dt.Scalar.BitSize))
    elif kind == 'BitString':
        label = 'bitstring{}'.format(int(dt.Scalar.BitSize))
    elif kind == 'Enumerated':
        label = '{} (enum, {} bit)'.format(dt.Name, int(dt.BitSize))
    elif kind == 'Structure':
        label = '{} (struct)'.format(dt.Name)
    elif kind == 'Array':
        label = '{} (array)'.format(dt.Name)
    else:
        label = str(dt.Name)

    if param is not None and param.Kind.ToString() == 'FixedValue':
        hv = str(param.HexValue) if param.HexValue else '0'
        label = '{} = {}'.format(label, hv)

    return label


def _fmt_size(bits, variable):
    if variable:
        return 'variable'
    if bits % 8 == 0:
        return '{} B'.format(bits // 8)
    return '{} bit'.format(bits)


def _fmt_offset(bit_offset):
    """Format a cumulative bit offset, or return 'unknown' for None."""
    if bit_offset is None:
        return 'unknown'
    byte_off = bit_offset // 8
    bit_rem  = bit_offset % 8
    if bit_rem == 0:
        return 'byte {}'.format(byte_off)
    return 'byte {}, bit {}'.format(byte_off, bit_rem)


# ── Header-type helpers ───────────────────────────────────────────────────────

def _header_id_values(packet):
    """Return a comma-separated list of 'IDname=value' for the packet's header ID values."""
    ht = packet.HeaderType
    if ht is None:
        return ''
    parts = []
    for hid in ht.Ids:
        match = next(
            (v for v in packet.HeaderIdValues if str(v.IdRef) == str(hid.Id)),
            None
        )
        val = str(match.Value) if match is not None else '?'
        parts.append('{}={}'.format(hid.Name, val))
    return ', '.join(parts)


def _header_line(packet):
    """One-line header description: 'MNEMONIC[id=val, …] (Header Type Name)'."""
    ht = packet.HeaderType
    if ht is None:
        return 'none'
    ids = _header_id_values(packet)
    mnemonic = str(ht.Mnemonic) if ht.Mnemonic else ''
    if mnemonic:
        return '{}[{}] ({})'.format(mnemonic, ids, ht.Name)
    return '{}[{}]'.format(ht.Name, ids)


# ── Row builder ───────────────────────────────────────────────────────────────

def _build_rows(packet):
    """
    Return a list of row dicts for each field in the packet.
    Tracks cumulative bit offset; sets offset to None after a variable-size field.
    """
    rows = []
    bit_offset = 0          # None after the first variable-size field

    for field in packet.Fields:
        param = field.Parameter
        dt    = param.DataType if param is not None else None

        bits, variable = _bit_size(dt)
        type_str   = _type_label(dt, param)
        unit_str   = _unit(dt)
        size_str   = _fmt_size(bits, variable)
        offset_str = _fmt_offset(bit_offset)

        # Parameter name cell: annotate type-indicator fields
        name = str(field.Name)
        if field.IsTypeIndicator:
            ind_val = str(field.IndicatorValue) if field.IndicatorValue else ''
            if ind_val:
                name = '{} [type indicator: {}]'.format(name, ind_val)
            else:
                name = '{} [type indicator]'.format(name)

        rows.append({
            'name':   name,
            'type':   type_str,
            'unit':   unit_str,
            'size':   size_str,
            'offset': offset_str,
        })

        # Advance the running offset
        if bit_offset is not None:
            if variable:
                bit_offset = None   # subsequent offsets are unknown
            else:
                bit_offset += bits

    return rows
%>\
<%def name="render_packet(packet, heading_level)">\
<%
    heading = '#' * heading_level
    rows = _build_rows(packet)
%>\
${heading} ${packet.Name}

% if packet.Mnemonic:
**Mnemonic:** ${packet.Mnemonic | trim}  
% endif
**ID:** 0x${'{:X}'.format(packet.NumericId)}  
**Header:** ${_header_line(packet)}  
% if packet.Description:

${packet.Description | trim}
% endif

% if rows:
| Parameter | Type | Unit | Size | Offset |
|-----------|------|------|------|--------|
% for row in rows:
| ${row['name']} | ${row['type']} | ${row['unit']} | ${row['size']} | ${row['offset']} |
% endfor
% else:
*No fields defined.*
% endif
</%def>\
<%
telecommands = [p for p in model.PacketTypes if p.Kind.ToString() == 'Telecommand']
telemetries  = [p for p in model.PacketTypes if p.Kind.ToString() == 'Telemetry']
%>\
# ${model.Metadata.Name or 'ICD'} — Packet Reference

% if model.Metadata.Version:
**Version:** ${model.Metadata.Version | trim}  
% endif
% if model.Metadata.Date:
**Date:** ${model.Metadata.Date | trim}  
% endif
% if model.Metadata.Description:

${model.Metadata.Description | trim}

% endif
---

## Telecommands

% if not telecommands:
*No telecommands defined.*
% else:
% for packet in telecommands:
${render_packet(packet, 3)}
% endfor
% endif

---

## Telemetries

% if not telemetries:
*No telemetries defined.*
% else:
% for packet in telemetries:
${render_packet(packet, 3)}
% endfor
% endif
