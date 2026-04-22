#include "icdfyit-transcoders.h"

#include <stddef.h>
#include <string.h>

/* Centralized success/error writing keeps all public functions consistent. */
static bool icduf_set_success(uint32_t* errorCode)
{
    /* The API requires the caller to always provide an error-code destination. */
    if (errorCode == NULL)
    {
        return false;
    }

    /* Every successful function call must clear the previous error state. */
    *errorCode = ICDUF_ERROR_NONE;
    return true;
}

/* Small helper to report an error and return false in one expression. */
static bool icduf_set_error(uint32_t* errorCode, IcdUF_ErrorCode error)
{
    /* A missing error pointer is considered a hard call-site contract violation. */
    if (errorCode == NULL)
    {
        return false;
    }

    /* The error code communicates the reason for the failure. */
    *errorCode = (uint32_t)error;
    return false;
}

/* Verifies that buffer metadata is self-consistent before access. */
static bool icduf_validate_buffer_state(const IcdUF_ByteBuffer* buffer, uint32_t* errorCode)
{
    /* The descriptor itself must exist. */
    if (buffer == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Backing data pointer must be available for any read or write operation. */
    if (buffer->data == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Length may never exceed physical capacity. */
    if (buffer->length > buffer->capacity)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_INVALID_ARGUMENT);
    }

    /* Current decode cursor must stay within valid data bounds. */
    if (buffer->position > buffer->length)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_INVALID_ARGUMENT);
    }

    return true;
}

/* Checks available space for append/encode operations. */
static bool icduf_ensure_write_capacity(const IcdUF_ByteBuffer* buffer, uint32_t byteCount, uint32_t* errorCode)
{
    /* Reuse common state validation before performing arithmetic checks. */
    if (!icduf_validate_buffer_state(buffer, errorCode))
    {
        return false;
    }

    /* Reject zero-byte requests because they are usually a caller bug. */
    if (byteCount == 0u)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_INVALID_ARGUMENT);
    }

    /* Subtraction avoids integer overflow that could occur with length + byteCount. */
    if (byteCount > (buffer->capacity - buffer->length))
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_BUFFER_OVERFLOW);
    }

    return true;
}

/* Checks available bytes for decode/read operations. */
static bool icduf_ensure_read_capacity(const IcdUF_ByteBuffer* buffer, uint32_t byteCount, uint32_t* errorCode)
{
    /* Reuse common state validation before bounds checks. */
    if (!icduf_validate_buffer_state(buffer, errorCode))
    {
        return false;
    }

    /* Reject zero-byte requests to keep API behavior explicit. */
    if (byteCount == 0u)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_INVALID_ARGUMENT);
    }

    /* Decode cursor plus requested width must remain within length. */
    if (byteCount > (buffer->length - buffer->position))
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_BUFFER_UNDERFLOW);
    }

    return true;
}

/* Generic unsigned encoder used by all unsigned and boolean APIs. */
static bool icduf_encode_unsigned_generic(
    IcdUF_ByteBuffer* buffer,
    uint32_t value,
    uint32_t byteCount,
    bool isLittleEndian,
    uint32_t* errorCode)
{
    uint32_t maxValue;
    uint32_t writeOffset;
    uint32_t i;

    /* Public API contract requires the last argument to be present. */
    if (!icduf_set_success(errorCode))
    {
        return false;
    }

    /* Validate destination buffer and verify append space first. */
    if (!icduf_ensure_write_capacity(buffer, byteCount, errorCode))
    {
        return false;
    }

    /* Utility functions only support widths defined in the design (1..4 bytes). */
    if ((byteCount < 1u) || (byteCount > 4u))
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_INVALID_ARGUMENT);
    }

    /* Build a dynamic maximum value for sub-32-bit fields. */
    if (byteCount == 4u)
    {
        maxValue = 0xFFFFFFFFu;
    }
    else
    {
        maxValue = (1u << (byteCount * 8u)) - 1u;
    }

    /* Reject values that do not fit into the requested bit width. */
    if (value > maxValue)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_RANGE);
    }

    /* Appends always happen at current logical end of valid bytes. */
    writeOffset = buffer->length;

    /* Emit each byte according to selected endian convention. */
    for (i = 0u; i < byteCount; ++i)
    {
        uint32_t shift;
        uint8_t oneByte;

        /* Byte order only changes the source bit offset, not destination index. */
        if (isLittleEndian)
        {
            shift = i * 8u;
        }
        else
        {
            shift = (byteCount - 1u - i) * 8u;
        }

        /* Store a single byte extracted from the scalar value. */
        oneByte = (uint8_t)((value >> shift) & 0xFFu);
        buffer->data[writeOffset + i] = oneByte;
    }

    /* Increase the valid-data length by encoded byte width. */
    buffer->length += byteCount;
    return true;
}

/* Generic unsigned decoder used by all unsigned and boolean APIs. */
static bool icduf_decode_unsigned_generic(
    IcdUF_ByteBuffer* buffer,
    uint32_t* value,
    uint32_t byteCount,
    bool isLittleEndian,
    uint32_t* errorCode)
{
    uint32_t i;
    uint32_t decoded;

    /* Public API contract requires the last argument to be present. */
    if (!icduf_set_success(errorCode))
    {
        return false;
    }

    /* Output pointer must be valid before any write-back is attempted. */
    if (value == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Validate source buffer and ensure enough bytes are available. */
    if (!icduf_ensure_read_capacity(buffer, byteCount, errorCode))
    {
        return false;
    }

    /* Utility functions only support widths defined in the design (1..4 bytes). */
    if ((byteCount < 1u) || (byteCount > 4u))
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_INVALID_ARGUMENT);
    }

    /* Start accumulation from zero before composing the scalar value. */
    decoded = 0u;

    /* Read and assemble each byte according to selected endian convention. */
    for (i = 0u; i < byteCount; ++i)
    {
        uint32_t shift;
        uint32_t oneByte;

        /* Byte order changes where each source byte lands in the output scalar. */
        if (isLittleEndian)
        {
            shift = i * 8u;
        }
        else
        {
            shift = (byteCount - 1u - i) * 8u;
        }

        /* Promote one input byte and merge it into the decoded scalar. */
        oneByte = (uint32_t)buffer->data[buffer->position + i];
        decoded |= (oneByte << shift);
    }

    /* Move read cursor forward by consumed bytes and return the result. */
    buffer->position += byteCount;
    *value = decoded;
    return true;
}

/* Generic signed encoder that validates range before writing bytes. */
static bool icduf_encode_signed_generic(
    IcdUF_ByteBuffer* buffer,
    int32_t value,
    int32_t minValue,
    int32_t maxValue,
    uint32_t byteCount,
    bool isLittleEndian,
    uint32_t* errorCode)
{
    uint32_t encoded;
    uint32_t mask;

    /* Respect the representable numeric range of the target signed width. */
    if ((value < minValue) || (value > maxValue))
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_RANGE);
    }

    /* Convert to two's-complement bit pattern for byte-wise serialization. */
    encoded = (uint32_t)value;

    /* Truncate only when storing into less than 32 bits. */
    if (byteCount < 4u)
    {
        mask = (1u << (byteCount * 8u)) - 1u;
        encoded &= mask;
    }

    /* Reuse common unsigned writer because bit layout is already prepared. */
    return icduf_encode_unsigned_generic(buffer, encoded, byteCount, isLittleEndian, errorCode);
}

/* Generic signed decoder that performs sign extension after byte assembly. */
static bool icduf_decode_signed_generic(
    IcdUF_ByteBuffer* buffer,
    int32_t* value,
    uint32_t byteCount,
    bool isLittleEndian,
    uint32_t* errorCode)
{
    uint32_t raw;
    uint32_t signBit;
    uint32_t fullMask;

    /* Decoded destination must exist before writing converted value. */
    if (value == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Decode raw bits first using the shared unsigned decoding path. */
    if (!icduf_decode_unsigned_generic(buffer, &raw, byteCount, isLittleEndian, errorCode))
    {
        return false;
    }

    /* 32-bit signed values already occupy full width and need no extension. */
    if (byteCount < 4u)
    {
        /* Build sign bit and full-width mask for the selected encoded width. */
        signBit = 1u << ((byteCount * 8u) - 1u);
        fullMask = (1u << (byteCount * 8u)) - 1u;

        /* Extend upper bits when sign bit is set to preserve negative values. */
        if ((raw & signBit) != 0u)
        {
            raw |= ~fullMask;
        }
    }

    /* Final cast is safe because sign extension already normalized the value. */
    *value = (int32_t)raw;
    return true;
}

/** See header for documentation. */
bool icduf_buffer_init(IcdUF_ByteBuffer* buffer, uint8_t* storage, uint32_t capacity, uint32_t* errorCode)
{
    /* Caller must provide output storage for diagnostics. */
    if (!icduf_set_success(errorCode))
    {
        return false;
    }

    /* Descriptor pointer is required for all initialization paths. */
    if (buffer == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Utility buffer requires valid preallocated storage. */
    if (storage == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Zero capacity cannot store any encoded value and is treated as invalid. */
    if (capacity == 0u)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_INVALID_ARGUMENT);
    }

    /* Initialize descriptor fields to a clean empty-buffer state. */
    buffer->data = storage;
    buffer->capacity = capacity;
    buffer->length = 0u;
    buffer->position = 0u;
    return true;
}

/** See header for documentation. */
bool icduf_buffer_clear(IcdUF_ByteBuffer* buffer, uint32_t* errorCode)
{
    /* Clear operation starts by validating API contract and descriptor. */
    if (!icduf_set_success(errorCode))
    {
        return false;
    }

    /* Existing descriptor must be valid before mutation. */
    if (!icduf_validate_buffer_state(buffer, errorCode))
    {
        return false;
    }

    /* Clearing content resets both logical length and decode cursor. */
    buffer->length = 0u;
    buffer->position = 0u;
    return true;
}

/** See header for documentation. */
bool icduf_buffer_rewind(IcdUF_ByteBuffer* buffer, uint32_t* errorCode)
{
    /* Rewind operation starts by validating API contract and descriptor. */
    if (!icduf_set_success(errorCode))
    {
        return false;
    }

    /* Existing descriptor must be valid before cursor updates. */
    if (!icduf_validate_buffer_state(buffer, errorCode))
    {
        return false;
    }

    /* Rewind only affects decode position and keeps existing length intact. */
    buffer->position = 0u;
    return true;
}

/** See header for documentation. */
bool icduf_buffer_set_length(IcdUF_ByteBuffer* buffer, uint32_t length, uint32_t* errorCode)
{
    /* Length update starts by validating API contract and descriptor. */
    if (!icduf_set_success(errorCode))
    {
        return false;
    }

    /* Existing descriptor must be valid before length updates. */
    if (!icduf_validate_buffer_state(buffer, errorCode))
    {
        return false;
    }

    /* New length cannot exceed physical storage capacity. */
    if (length > buffer->capacity)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_INVALID_ARGUMENT);
    }

    /* Assign new length, then clamp position to keep state consistent. */
    buffer->length = length;
    if (buffer->position > length)
    {
        buffer->position = length;
    }
    return true;
}

/** See header for documentation. */
bool icduf_buffer_set_position(IcdUF_ByteBuffer* buffer, uint32_t position, uint32_t* errorCode)
{
    /* Position update starts by validating API contract and descriptor. */
    if (!icduf_set_success(errorCode))
    {
        return false;
    }

    /* Existing descriptor must be valid before cursor updates. */
    if (!icduf_validate_buffer_state(buffer, errorCode))
    {
        return false;
    }

    /* New position cannot exceed current valid-data length. */
    if (position > buffer->length)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_INVALID_ARGUMENT);
    }

    /* Store caller-requested decode cursor position. */
    buffer->position = position;
    return true;
}

/** See header for documentation. */
bool icduf_buffer_get_remaining_read(const IcdUF_ByteBuffer* buffer, uint32_t* remaining, uint32_t* errorCode)
{
    /* Query starts by validating API contract and output pointer. */
    if (!icduf_set_success(errorCode))
    {
        return false;
    }

    /* Destination pointer must exist for returning the computed value. */
    if (remaining == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Existing descriptor must be valid before calculation. */
    if (!icduf_validate_buffer_state(buffer, errorCode))
    {
        return false;
    }

    /* Remaining read bytes are the tail after current decode cursor. */
    *remaining = buffer->length - buffer->position;
    return true;
}

/** See header for documentation. */
bool icduf_buffer_get_remaining_write(const IcdUF_ByteBuffer* buffer, uint32_t* remaining, uint32_t* errorCode)
{
    /* Query starts by validating API contract and output pointer. */
    if (!icduf_set_success(errorCode))
    {
        return false;
    }

    /* Destination pointer must exist for returning the computed value. */
    if (remaining == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Existing descriptor must be valid before calculation. */
    if (!icduf_validate_buffer_state(buffer, errorCode))
    {
        return false;
    }

    /* Remaining write bytes are the free bytes after logical end. */
    *remaining = buffer->capacity - buffer->length;
    return true;
}

/* Macro keeps wrappers compact while still producing explicit named functions. */
#define ICDUF_DEFINE_ENCODE_UNSIGNED(FUNC_NAME, TYPE_NAME, BYTE_COUNT, LITTLE_ENDIAN) \
    bool FUNC_NAME(IcdUF_ByteBuffer* buffer, TYPE_NAME value, uint32_t* errorCode) \
    { \
        /* Forward to shared unsigned encoder with fixed width and endian. */ \
        return icduf_encode_unsigned_generic(buffer, (uint32_t)value, (BYTE_COUNT), (LITTLE_ENDIAN), errorCode); \
    }

/* Macro keeps wrappers compact while still producing explicit named functions. */
#define ICDUF_DEFINE_DECODE_UNSIGNED(FUNC_NAME, TYPE_NAME, BYTE_COUNT, LITTLE_ENDIAN) \
    bool FUNC_NAME(IcdUF_ByteBuffer* buffer, TYPE_NAME* value, uint32_t* errorCode) \
    { \
        uint32_t decoded; \
        /* Decode via generic path and cast back to requested destination type. */ \
        if (!icduf_decode_unsigned_generic(buffer, &decoded, (BYTE_COUNT), (LITTLE_ENDIAN), errorCode)) \
        { \
            return false; \
        } \
        *value = (TYPE_NAME)decoded; \
        return true; \
    }

/* Macro builds fixed-endian wrappers for signed encoder APIs. */
#define ICDUF_DEFINE_ENCODE_SIGNED(FUNC_NAME, TYPE_NAME, BYTE_COUNT, MIN_VAL, MAX_VAL, LITTLE_ENDIAN) \
    bool FUNC_NAME(IcdUF_ByteBuffer* buffer, TYPE_NAME value, uint32_t* errorCode) \
    { \
        /* Delegate range-checking and serialization to generic signed encoder. */ \
        return icduf_encode_signed_generic(buffer, (int32_t)value, (MIN_VAL), (MAX_VAL), (BYTE_COUNT), (LITTLE_ENDIAN), errorCode); \
    }

/* Macro builds fixed-endian wrappers for signed decoder APIs. */
#define ICDUF_DEFINE_DECODE_SIGNED(FUNC_NAME, TYPE_NAME, BYTE_COUNT, LITTLE_ENDIAN) \
    bool FUNC_NAME(IcdUF_ByteBuffer* buffer, TYPE_NAME* value, uint32_t* errorCode) \
    { \
        int32_t decoded; \
        /* Decode via generic path and cast to target signed destination type. */ \
        if (!icduf_decode_signed_generic(buffer, &decoded, (BYTE_COUNT), (LITTLE_ENDIAN), errorCode)) \
        { \
            return false; \
        } \
        *value = (TYPE_NAME)decoded; \
        return true; \
    }

ICDUF_DEFINE_ENCODE_UNSIGNED(icduf_encode_u8_be, uint8_t, ICDUF_U8_BYTE_COUNT, false)
ICDUF_DEFINE_ENCODE_UNSIGNED(icduf_encode_u16_be, uint16_t, ICDUF_U16_BYTE_COUNT, false)
ICDUF_DEFINE_ENCODE_UNSIGNED(icduf_encode_u24_be, uint32_t, ICDUF_U24_BYTE_COUNT, false)
ICDUF_DEFINE_ENCODE_UNSIGNED(icduf_encode_u32_be, uint32_t, ICDUF_U32_BYTE_COUNT, false)

ICDUF_DEFINE_ENCODE_UNSIGNED(icduf_encode_u8_le, uint8_t, ICDUF_U8_BYTE_COUNT, true)
ICDUF_DEFINE_ENCODE_UNSIGNED(icduf_encode_u16_le, uint16_t, ICDUF_U16_BYTE_COUNT, true)
ICDUF_DEFINE_ENCODE_UNSIGNED(icduf_encode_u24_le, uint32_t, ICDUF_U24_BYTE_COUNT, true)
ICDUF_DEFINE_ENCODE_UNSIGNED(icduf_encode_u32_le, uint32_t, ICDUF_U32_BYTE_COUNT, true)

ICDUF_DEFINE_DECODE_UNSIGNED(icduf_decode_u8_be, uint8_t, ICDUF_U8_BYTE_COUNT, false)
ICDUF_DEFINE_DECODE_UNSIGNED(icduf_decode_u16_be, uint16_t, ICDUF_U16_BYTE_COUNT, false)
ICDUF_DEFINE_DECODE_UNSIGNED(icduf_decode_u24_be, uint32_t, ICDUF_U24_BYTE_COUNT, false)
ICDUF_DEFINE_DECODE_UNSIGNED(icduf_decode_u32_be, uint32_t, ICDUF_U32_BYTE_COUNT, false)

ICDUF_DEFINE_DECODE_UNSIGNED(icduf_decode_u8_le, uint8_t, ICDUF_U8_BYTE_COUNT, true)
ICDUF_DEFINE_DECODE_UNSIGNED(icduf_decode_u16_le, uint16_t, ICDUF_U16_BYTE_COUNT, true)
ICDUF_DEFINE_DECODE_UNSIGNED(icduf_decode_u24_le, uint32_t, ICDUF_U24_BYTE_COUNT, true)
ICDUF_DEFINE_DECODE_UNSIGNED(icduf_decode_u32_le, uint32_t, ICDUF_U32_BYTE_COUNT, true)

ICDUF_DEFINE_ENCODE_SIGNED(icduf_encode_s8_be, int8_t, ICDUF_U8_BYTE_COUNT, INT8_MIN, INT8_MAX, false)
ICDUF_DEFINE_ENCODE_SIGNED(icduf_encode_s16_be, int16_t, ICDUF_U16_BYTE_COUNT, INT16_MIN, INT16_MAX, false)
ICDUF_DEFINE_ENCODE_SIGNED(icduf_encode_s24_be, int32_t, ICDUF_U24_BYTE_COUNT, ICDUF_S24_MIN_VALUE, ICDUF_S24_MAX_VALUE, false)
ICDUF_DEFINE_ENCODE_SIGNED(icduf_encode_s32_be, int32_t, ICDUF_U32_BYTE_COUNT, INT32_MIN, INT32_MAX, false)

ICDUF_DEFINE_ENCODE_SIGNED(icduf_encode_s8_le, int8_t, ICDUF_U8_BYTE_COUNT, INT8_MIN, INT8_MAX, true)
ICDUF_DEFINE_ENCODE_SIGNED(icduf_encode_s16_le, int16_t, ICDUF_U16_BYTE_COUNT, INT16_MIN, INT16_MAX, true)
ICDUF_DEFINE_ENCODE_SIGNED(icduf_encode_s24_le, int32_t, ICDUF_U24_BYTE_COUNT, ICDUF_S24_MIN_VALUE, ICDUF_S24_MAX_VALUE, true)
ICDUF_DEFINE_ENCODE_SIGNED(icduf_encode_s32_le, int32_t, ICDUF_U32_BYTE_COUNT, INT32_MIN, INT32_MAX, true)

ICDUF_DEFINE_DECODE_SIGNED(icduf_decode_s8_be, int8_t, ICDUF_U8_BYTE_COUNT, false)
ICDUF_DEFINE_DECODE_SIGNED(icduf_decode_s16_be, int16_t, ICDUF_U16_BYTE_COUNT, false)
ICDUF_DEFINE_DECODE_SIGNED(icduf_decode_s24_be, int32_t, ICDUF_U24_BYTE_COUNT, false)
ICDUF_DEFINE_DECODE_SIGNED(icduf_decode_s32_be, int32_t, ICDUF_U32_BYTE_COUNT, false)

ICDUF_DEFINE_DECODE_SIGNED(icduf_decode_s8_le, int8_t, ICDUF_U8_BYTE_COUNT, true)
ICDUF_DEFINE_DECODE_SIGNED(icduf_decode_s16_le, int16_t, ICDUF_U16_BYTE_COUNT, true)
ICDUF_DEFINE_DECODE_SIGNED(icduf_decode_s24_le, int32_t, ICDUF_U24_BYTE_COUNT, true)
ICDUF_DEFINE_DECODE_SIGNED(icduf_decode_s32_le, int32_t, ICDUF_U32_BYTE_COUNT, true)

/** See header for documentation. */
bool icduf_encode_f32_be(IcdUF_ByteBuffer* buffer, float value, uint32_t* errorCode)
{
    uint32_t raw;

    /* Reinterpret float bits without violating strict-aliasing rules. */
    memcpy(&raw, &value, sizeof(raw));

    /* Reuse unsigned 32-bit big-endian encoder for payload emission. */
    return icduf_encode_unsigned_generic(buffer, raw, ICDUF_F32_BYTE_COUNT, false, errorCode);
}

/** See header for documentation. */
bool icduf_encode_f32_le(IcdUF_ByteBuffer* buffer, float value, uint32_t* errorCode)
{
    uint32_t raw;

    /* Reinterpret float bits without violating strict-aliasing rules. */
    memcpy(&raw, &value, sizeof(raw));

    /* Reuse unsigned 32-bit little-endian encoder for payload emission. */
    return icduf_encode_unsigned_generic(buffer, raw, ICDUF_F32_BYTE_COUNT, true, errorCode);
}

/** See header for documentation. */
bool icduf_decode_f32_be(IcdUF_ByteBuffer* buffer, float* value, uint32_t* errorCode)
{
    uint32_t raw;

    /* Destination pointer must exist before writing decoded value. */
    if (value == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Decode raw bits with shared unsigned path in big-endian mode. */
    if (!icduf_decode_unsigned_generic(buffer, &raw, ICDUF_F32_BYTE_COUNT, false, errorCode))
    {
        return false;
    }

    /* Reconstruct float value from raw IEEE-754 bytes. */
    memcpy(value, &raw, sizeof(raw));
    return true;
}

/** See header for documentation. */
bool icduf_decode_f32_le(IcdUF_ByteBuffer* buffer, float* value, uint32_t* errorCode)
{
    uint32_t raw;

    /* Destination pointer must exist before writing decoded value. */
    if (value == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Decode raw bits with shared unsigned path in little-endian mode. */
    if (!icduf_decode_unsigned_generic(buffer, &raw, ICDUF_F32_BYTE_COUNT, true, errorCode))
    {
        return false;
    }

    /* Reconstruct float value from raw IEEE-754 bytes. */
    memcpy(value, &raw, sizeof(raw));
    return true;
}

/** See header for documentation. */
bool icduf_encode_bool8_be(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode)
{
    /* Booleans are encoded as canonical integer values 0 or 1. */
    return icduf_encode_unsigned_generic(buffer, value ? 1u : 0u, ICDUF_U8_BYTE_COUNT, false, errorCode);
}

/** See header for documentation. */
bool icduf_encode_bool16_be(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode)
{
    /* Booleans are encoded as canonical integer values 0 or 1. */
    return icduf_encode_unsigned_generic(buffer, value ? 1u : 0u, ICDUF_U16_BYTE_COUNT, false, errorCode);
}

/** See header for documentation. */
bool icduf_encode_bool24_be(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode)
{
    /* Booleans are encoded as canonical integer values 0 or 1. */
    return icduf_encode_unsigned_generic(buffer, value ? 1u : 0u, ICDUF_U24_BYTE_COUNT, false, errorCode);
}

/** See header for documentation. */
bool icduf_encode_bool32_be(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode)
{
    /* Booleans are encoded as canonical integer values 0 or 1. */
    return icduf_encode_unsigned_generic(buffer, value ? 1u : 0u, ICDUF_U32_BYTE_COUNT, false, errorCode);
}

/** See header for documentation. */
bool icduf_encode_bool8_le(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode)
{
    /* Booleans are encoded as canonical integer values 0 or 1. */
    return icduf_encode_unsigned_generic(buffer, value ? 1u : 0u, ICDUF_U8_BYTE_COUNT, true, errorCode);
}

/** See header for documentation. */
bool icduf_encode_bool16_le(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode)
{
    /* Booleans are encoded as canonical integer values 0 or 1. */
    return icduf_encode_unsigned_generic(buffer, value ? 1u : 0u, ICDUF_U16_BYTE_COUNT, true, errorCode);
}

/** See header for documentation. */
bool icduf_encode_bool24_le(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode)
{
    /* Booleans are encoded as canonical integer values 0 or 1. */
    return icduf_encode_unsigned_generic(buffer, value ? 1u : 0u, ICDUF_U24_BYTE_COUNT, true, errorCode);
}

/** See header for documentation. */
bool icduf_encode_bool32_le(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode)
{
    /* Booleans are encoded as canonical integer values 0 or 1. */
    return icduf_encode_unsigned_generic(buffer, value ? 1u : 0u, ICDUF_U32_BYTE_COUNT, true, errorCode);
}

/** See header for documentation. */
bool icduf_decode_bool8_be(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode)
{
    uint32_t raw;

    /* Destination pointer must exist before writing decoded value. */
    if (value == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Decode integer payload and normalize any non-zero to true. */
    if (!icduf_decode_unsigned_generic(buffer, &raw, ICDUF_U8_BYTE_COUNT, false, errorCode))
    {
        return false;
    }

    *value = (raw != 0u);
    return true;
}

/** See header for documentation. */
bool icduf_decode_bool16_be(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode)
{
    uint32_t raw;

    /* Destination pointer must exist before writing decoded value. */
    if (value == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Decode integer payload and normalize any non-zero to true. */
    if (!icduf_decode_unsigned_generic(buffer, &raw, ICDUF_U16_BYTE_COUNT, false, errorCode))
    {
        return false;
    }

    *value = (raw != 0u);
    return true;
}

/** See header for documentation. */
bool icduf_decode_bool24_be(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode)
{
    uint32_t raw;

    /* Destination pointer must exist before writing decoded value. */
    if (value == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Decode integer payload and normalize any non-zero to true. */
    if (!icduf_decode_unsigned_generic(buffer, &raw, ICDUF_U24_BYTE_COUNT, false, errorCode))
    {
        return false;
    }

    *value = (raw != 0u);
    return true;
}

/** See header for documentation. */
bool icduf_decode_bool32_be(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode)
{
    uint32_t raw;

    /* Destination pointer must exist before writing decoded value. */
    if (value == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Decode integer payload and normalize any non-zero to true. */
    if (!icduf_decode_unsigned_generic(buffer, &raw, ICDUF_U32_BYTE_COUNT, false, errorCode))
    {
        return false;
    }

    *value = (raw != 0u);
    return true;
}

/** See header for documentation. */
bool icduf_decode_bool8_le(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode)
{
    uint32_t raw;

    /* Destination pointer must exist before writing decoded value. */
    if (value == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Decode integer payload and normalize any non-zero to true. */
    if (!icduf_decode_unsigned_generic(buffer, &raw, ICDUF_U8_BYTE_COUNT, true, errorCode))
    {
        return false;
    }

    *value = (raw != 0u);
    return true;
}

/** See header for documentation. */
bool icduf_decode_bool16_le(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode)
{
    uint32_t raw;

    /* Destination pointer must exist before writing decoded value. */
    if (value == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Decode integer payload and normalize any non-zero to true. */
    if (!icduf_decode_unsigned_generic(buffer, &raw, ICDUF_U16_BYTE_COUNT, true, errorCode))
    {
        return false;
    }

    *value = (raw != 0u);
    return true;
}

/** See header for documentation. */
bool icduf_decode_bool24_le(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode)
{
    uint32_t raw;

    /* Destination pointer must exist before writing decoded value. */
    if (value == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Decode integer payload and normalize any non-zero to true. */
    if (!icduf_decode_unsigned_generic(buffer, &raw, ICDUF_U24_BYTE_COUNT, true, errorCode))
    {
        return false;
    }

    *value = (raw != 0u);
    return true;
}

/** See header for documentation. */
bool icduf_decode_bool32_le(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode)
{
    uint32_t raw;

    /* Destination pointer must exist before writing decoded value. */
    if (value == NULL)
    {
        return icduf_set_error(errorCode, ICDUF_ERROR_NULL_ARGUMENT);
    }

    /* Decode integer payload and normalize any non-zero to true. */
    if (!icduf_decode_unsigned_generic(buffer, &raw, ICDUF_U32_BYTE_COUNT, true, errorCode))
    {
        return false;
    }

    *value = (raw != 0u);
    return true;
}
