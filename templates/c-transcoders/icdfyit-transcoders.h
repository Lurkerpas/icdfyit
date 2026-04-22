#ifndef ICDFYIT_TRANSCODERS_H
#define ICDFYIT_TRANSCODERS_H

#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/** @brief Byte width for 8-bit values. */
#define ICDUF_U8_BYTE_COUNT   (1u)
/** @brief Byte width for 16-bit values. */
#define ICDUF_U16_BYTE_COUNT  (2u)
/** @brief Byte width for 24-bit values. */
#define ICDUF_U24_BYTE_COUNT  (3u)
/** @brief Byte width for 32-bit values. */
#define ICDUF_U32_BYTE_COUNT  (4u)
/** @brief Byte width for 32-bit float values. */
#define ICDUF_F32_BYTE_COUNT  (4u)

/** @brief Minimum representable signed 24-bit integer. */
#define ICDUF_S24_MIN_VALUE   (-8388608)
/** @brief Maximum representable signed 24-bit integer. */
#define ICDUF_S24_MAX_VALUE   (8388607)

/** @brief Utility-transcoder error codes. */
typedef enum IcdUF_ErrorCode
{
    ICDUF_ERROR_NONE = 0u,
    ICDUF_ERROR_NULL_ARGUMENT = 1u,
    ICDUF_ERROR_INVALID_ARGUMENT = 2u,
    ICDUF_ERROR_BUFFER_OVERFLOW = 3u,
    ICDUF_ERROR_BUFFER_UNDERFLOW = 4u,
    ICDUF_ERROR_RANGE = 5u
} IcdUF_ErrorCode;

/** @brief Mutable byte buffer used by transcoder utility functions. */
typedef struct IcdUF_ByteBuffer
{
    /** @brief Backing storage provided by the caller. */
    uint8_t* data;
    /** @brief Total number of bytes available in @ref data. */
    uint32_t capacity;
    /** @brief Number of bytes currently considered valid for decode/read. */
    uint32_t length;
    /** @brief Current decode/read position inside [0, length]. */
    uint32_t position;
} IcdUF_ByteBuffer;

/**
 * @brief Initializes a byte-buffer descriptor over caller-provided storage.
 * @param buffer Buffer descriptor to initialize.
 * @param storage Caller-provided static/preallocated byte storage.
 * @param capacity Capacity of @p storage in bytes.
 * @param errorCode Output error code.
 * @return true on success, false on error.
 */
bool icduf_buffer_init(IcdUF_ByteBuffer* buffer, uint8_t* storage, uint32_t capacity, uint32_t* errorCode);

/**
 * @brief Clears valid content and resets read position to zero.
 * @param buffer Buffer descriptor to clear.
 * @param errorCode Output error code.
 * @return true on success, false on error.
 */
bool icduf_buffer_clear(IcdUF_ByteBuffer* buffer, uint32_t* errorCode);

/**
 * @brief Resets read position to zero while keeping current length.
 * @param buffer Buffer descriptor to rewind.
 * @param errorCode Output error code.
 * @return true on success, false on error.
 */
bool icduf_buffer_rewind(IcdUF_ByteBuffer* buffer, uint32_t* errorCode);

/**
 * @brief Sets the number of valid bytes in the buffer.
 * @param buffer Buffer descriptor to update.
 * @param length New valid byte count, must be <= capacity.
 * @param errorCode Output error code.
 * @return true on success, false on error.
 */
bool icduf_buffer_set_length(IcdUF_ByteBuffer* buffer, uint32_t length, uint32_t* errorCode);

/**
 * @brief Sets the current read position in the buffer.
 * @param buffer Buffer descriptor to update.
 * @param position New read position, must be <= current length.
 * @param errorCode Output error code.
 * @return true on success, false on error.
 */
bool icduf_buffer_set_position(IcdUF_ByteBuffer* buffer, uint32_t position, uint32_t* errorCode);

/**
 * @brief Returns number of bytes still available for decoding.
 * @param buffer Buffer descriptor to query.
 * @param remaining Output number of unread bytes.
 * @param errorCode Output error code.
 * @return true on success, false on error.
 */
bool icduf_buffer_get_remaining_read(const IcdUF_ByteBuffer* buffer, uint32_t* remaining, uint32_t* errorCode);

/**
 * @brief Returns number of bytes still available for appending.
 * @param buffer Buffer descriptor to query.
 * @param remaining Output number of free bytes.
 * @param errorCode Output error code.
 * @return true on success, false on error.
 */
bool icduf_buffer_get_remaining_write(const IcdUF_ByteBuffer* buffer, uint32_t* remaining, uint32_t* errorCode);

/** @brief Encodes uint8 in big-endian form. */
bool icduf_encode_u8_be(IcdUF_ByteBuffer* buffer, uint8_t value, uint32_t* errorCode);
/** @brief Encodes uint16 in big-endian form. */
bool icduf_encode_u16_be(IcdUF_ByteBuffer* buffer, uint16_t value, uint32_t* errorCode);
/** @brief Encodes uint24 in big-endian form. */
bool icduf_encode_u24_be(IcdUF_ByteBuffer* buffer, uint32_t value, uint32_t* errorCode);
/** @brief Encodes uint32 in big-endian form. */
bool icduf_encode_u32_be(IcdUF_ByteBuffer* buffer, uint32_t value, uint32_t* errorCode);

/** @brief Encodes uint8 in little-endian form. */
bool icduf_encode_u8_le(IcdUF_ByteBuffer* buffer, uint8_t value, uint32_t* errorCode);
/** @brief Encodes uint16 in little-endian form. */
bool icduf_encode_u16_le(IcdUF_ByteBuffer* buffer, uint16_t value, uint32_t* errorCode);
/** @brief Encodes uint24 in little-endian form. */
bool icduf_encode_u24_le(IcdUF_ByteBuffer* buffer, uint32_t value, uint32_t* errorCode);
/** @brief Encodes uint32 in little-endian form. */
bool icduf_encode_u32_le(IcdUF_ByteBuffer* buffer, uint32_t value, uint32_t* errorCode);

/** @brief Decodes uint8 from big-endian form. */
bool icduf_decode_u8_be(IcdUF_ByteBuffer* buffer, uint8_t* value, uint32_t* errorCode);
/** @brief Decodes uint16 from big-endian form. */
bool icduf_decode_u16_be(IcdUF_ByteBuffer* buffer, uint16_t* value, uint32_t* errorCode);
/** @brief Decodes uint24 from big-endian form. */
bool icduf_decode_u24_be(IcdUF_ByteBuffer* buffer, uint32_t* value, uint32_t* errorCode);
/** @brief Decodes uint32 from big-endian form. */
bool icduf_decode_u32_be(IcdUF_ByteBuffer* buffer, uint32_t* value, uint32_t* errorCode);

/** @brief Decodes uint8 from little-endian form. */
bool icduf_decode_u8_le(IcdUF_ByteBuffer* buffer, uint8_t* value, uint32_t* errorCode);
/** @brief Decodes uint16 from little-endian form. */
bool icduf_decode_u16_le(IcdUF_ByteBuffer* buffer, uint16_t* value, uint32_t* errorCode);
/** @brief Decodes uint24 from little-endian form. */
bool icduf_decode_u24_le(IcdUF_ByteBuffer* buffer, uint32_t* value, uint32_t* errorCode);
/** @brief Decodes uint32 from little-endian form. */
bool icduf_decode_u32_le(IcdUF_ByteBuffer* buffer, uint32_t* value, uint32_t* errorCode);

/** @brief Encodes int8 in big-endian form. */
bool icduf_encode_s8_be(IcdUF_ByteBuffer* buffer, int8_t value, uint32_t* errorCode);
/** @brief Encodes int16 in big-endian form. */
bool icduf_encode_s16_be(IcdUF_ByteBuffer* buffer, int16_t value, uint32_t* errorCode);
/** @brief Encodes int24 in big-endian form. */
bool icduf_encode_s24_be(IcdUF_ByteBuffer* buffer, int32_t value, uint32_t* errorCode);
/** @brief Encodes int32 in big-endian form. */
bool icduf_encode_s32_be(IcdUF_ByteBuffer* buffer, int32_t value, uint32_t* errorCode);

/** @brief Encodes int8 in little-endian form. */
bool icduf_encode_s8_le(IcdUF_ByteBuffer* buffer, int8_t value, uint32_t* errorCode);
/** @brief Encodes int16 in little-endian form. */
bool icduf_encode_s16_le(IcdUF_ByteBuffer* buffer, int16_t value, uint32_t* errorCode);
/** @brief Encodes int24 in little-endian form. */
bool icduf_encode_s24_le(IcdUF_ByteBuffer* buffer, int32_t value, uint32_t* errorCode);
/** @brief Encodes int32 in little-endian form. */
bool icduf_encode_s32_le(IcdUF_ByteBuffer* buffer, int32_t value, uint32_t* errorCode);

/** @brief Decodes int8 from big-endian form. */
bool icduf_decode_s8_be(IcdUF_ByteBuffer* buffer, int8_t* value, uint32_t* errorCode);
/** @brief Decodes int16 from big-endian form. */
bool icduf_decode_s16_be(IcdUF_ByteBuffer* buffer, int16_t* value, uint32_t* errorCode);
/** @brief Decodes int24 from big-endian form. */
bool icduf_decode_s24_be(IcdUF_ByteBuffer* buffer, int32_t* value, uint32_t* errorCode);
/** @brief Decodes int32 from big-endian form. */
bool icduf_decode_s32_be(IcdUF_ByteBuffer* buffer, int32_t* value, uint32_t* errorCode);

/** @brief Decodes int8 from little-endian form. */
bool icduf_decode_s8_le(IcdUF_ByteBuffer* buffer, int8_t* value, uint32_t* errorCode);
/** @brief Decodes int16 from little-endian form. */
bool icduf_decode_s16_le(IcdUF_ByteBuffer* buffer, int16_t* value, uint32_t* errorCode);
/** @brief Decodes int24 from little-endian form. */
bool icduf_decode_s24_le(IcdUF_ByteBuffer* buffer, int32_t* value, uint32_t* errorCode);
/** @brief Decodes int32 from little-endian form. */
bool icduf_decode_s32_le(IcdUF_ByteBuffer* buffer, int32_t* value, uint32_t* errorCode);

/** @brief Encodes float32 in big-endian form. */
bool icduf_encode_f32_be(IcdUF_ByteBuffer* buffer, float value, uint32_t* errorCode);
/** @brief Encodes float32 in little-endian form. */
bool icduf_encode_f32_le(IcdUF_ByteBuffer* buffer, float value, uint32_t* errorCode);

/** @brief Decodes float32 from big-endian form. */
bool icduf_decode_f32_be(IcdUF_ByteBuffer* buffer, float* value, uint32_t* errorCode);
/** @brief Decodes float32 from little-endian form. */
bool icduf_decode_f32_le(IcdUF_ByteBuffer* buffer, float* value, uint32_t* errorCode);

/** @brief Encodes boolean as 8-bit integer in big-endian form. */
bool icduf_encode_bool8_be(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode);
/** @brief Encodes boolean as 16-bit integer in big-endian form. */
bool icduf_encode_bool16_be(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode);
/** @brief Encodes boolean as 24-bit integer in big-endian form. */
bool icduf_encode_bool24_be(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode);
/** @brief Encodes boolean as 32-bit integer in big-endian form. */
bool icduf_encode_bool32_be(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode);

/** @brief Encodes boolean as 8-bit integer in little-endian form. */
bool icduf_encode_bool8_le(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode);
/** @brief Encodes boolean as 16-bit integer in little-endian form. */
bool icduf_encode_bool16_le(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode);
/** @brief Encodes boolean as 24-bit integer in little-endian form. */
bool icduf_encode_bool24_le(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode);
/** @brief Encodes boolean as 32-bit integer in little-endian form. */
bool icduf_encode_bool32_le(IcdUF_ByteBuffer* buffer, bool value, uint32_t* errorCode);

/** @brief Decodes boolean from 8-bit integer in big-endian form. */
bool icduf_decode_bool8_be(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode);
/** @brief Decodes boolean from 16-bit integer in big-endian form. */
bool icduf_decode_bool16_be(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode);
/** @brief Decodes boolean from 24-bit integer in big-endian form. */
bool icduf_decode_bool24_be(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode);
/** @brief Decodes boolean from 32-bit integer in big-endian form. */
bool icduf_decode_bool32_be(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode);

/** @brief Decodes boolean from 8-bit integer in little-endian form. */
bool icduf_decode_bool8_le(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode);
/** @brief Decodes boolean from 16-bit integer in little-endian form. */
bool icduf_decode_bool16_le(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode);
/** @brief Decodes boolean from 24-bit integer in little-endian form. */
bool icduf_decode_bool24_le(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode);
/** @brief Decodes boolean from 32-bit integer in little-endian form. */
bool icduf_decode_bool32_le(IcdUF_ByteBuffer* buffer, bool* value, uint32_t* errorCode);

#ifdef __cplusplus
}
#endif

#endif
