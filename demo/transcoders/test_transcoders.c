#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>

#include "generated/transcoders.h"

#define CHECK(cond, msg) \
    do { \
        if (!(cond)) { \
            fprintf(stderr, "FAIL: %s (line %d)\n", (msg), __LINE__); \
            return false; \
        } \
    } while (0)

/* Reference packet bytes used as decode source and encode expectation. */
static const uint8_t kTc2SetThresholdPacket[] = {
    0x00, 0x00, 0x00, 0x01, /* Subservice = SetThreshold */
    0x2A,                   /* RequestId */
    0x07,                   /* SensorId */
    0x01,                   /* Enabled */
    0x12, 0x34              /* Threshold */
};

static const uint8_t kTc2RequestHistoryPacket[] = {
    0x00, 0x00, 0x00, 0x02, /* Subservice = RequestHistory */
    0x10                    /* RequestId */
};

static const uint8_t kTm2HistoryReportPacket[] = {
    0x44,                   /* RequestId */
    0x00, 0x00, 0x00, 0x03, /* Samples.count */
    0x01, 0x02,             /* Samples[0] */
    0xAB, 0xCD,             /* Samples[1] */
    0x00, 0x01,             /* Samples[2] */
    0x00                    /* Success = false */
};

static bool init_decode_buffer(
    IcdUF_ByteBuffer* buffer,
    uint8_t* storage,
    uint32_t capacity,
    const uint8_t* data,
    uint32_t dataLen,
    uint32_t* errorCode)
{
    if (!icduf_buffer_init(buffer, storage, capacity, errorCode))
    {
        return false;
    }

    memcpy(storage, data, dataLen);
    return icduf_buffer_set_length(buffer, dataLen, errorCode);
}

static bool test_tc2_set_threshold(void)
{
    uint8_t storage[64] = {0};
    uint32_t errorCode = 0;
    IcdUF_ByteBuffer buffer;
    IcdT_Tc2_set_threshold packet;

    CHECK(init_decode_buffer(
              &buffer,
              storage,
              (uint32_t)sizeof(storage),
              kTc2SetThresholdPacket,
              (uint32_t)sizeof(kTc2SetThresholdPacket),
              &errorCode),
          "init_decode_buffer(tc2_set_threshold)");

    CHECK(icdt_tc2_set_threshold_decode(&buffer, &packet, &errorCode),
          "decode tc2_set_threshold");

    CHECK(packet.subservice == IcdT_Tc2Subservice_SetThreshold, "decoded subservice");
    CHECK(packet.requestId == 0x2A, "decoded requestId");
    CHECK(packet.payload.sensorId == 0x07, "decoded sensorId");
    CHECK(packet.payload.enabled == true, "decoded enabled");
    CHECK(packet.payload.threshold == 0x1234, "decoded threshold");

    CHECK(icduf_buffer_clear(&buffer, &errorCode), "buffer_clear(tc2_set_threshold)");
    CHECK(icdt_tc2_set_threshold_encode(&buffer, &packet, &errorCode),
          "encode tc2_set_threshold");

    CHECK(buffer.length == (uint32_t)sizeof(kTc2SetThresholdPacket), "encoded length tc2_set_threshold");
    CHECK(memcmp(storage, kTc2SetThresholdPacket, sizeof(kTc2SetThresholdPacket)) == 0,
          "encoded bytes tc2_set_threshold");

    return true;
}

static bool test_tc2_request_history(void)
{
    uint8_t storage[64] = {0};
    uint32_t errorCode = 0;
    IcdUF_ByteBuffer buffer;
    IcdT_Tc2_request_history packet;

    CHECK(init_decode_buffer(
              &buffer,
              storage,
              (uint32_t)sizeof(storage),
              kTc2RequestHistoryPacket,
              (uint32_t)sizeof(kTc2RequestHistoryPacket),
              &errorCode),
          "init_decode_buffer(tc2_request_history)");

    CHECK(icdt_tc2_request_history_decode(&buffer, &packet, &errorCode),
          "decode tc2_request_history");

    CHECK(packet.subservice == IcdT_Tc2Subservice_RequestHistory, "decoded subservice history");
    CHECK(packet.requestId == 0x10, "decoded requestId history");

    CHECK(icduf_buffer_clear(&buffer, &errorCode), "buffer_clear(tc2_request_history)");
    CHECK(icdt_tc2_request_history_encode(&buffer, &packet, &errorCode),
          "encode tc2_request_history");

    CHECK(buffer.length == (uint32_t)sizeof(kTc2RequestHistoryPacket), "encoded length tc2_request_history");
    CHECK(memcmp(storage, kTc2RequestHistoryPacket, sizeof(kTc2RequestHistoryPacket)) == 0,
          "encoded bytes tc2_request_history");

    return true;
}

static bool test_tm2_history_report(void)
{
    uint8_t storage[128] = {0};
    uint32_t errorCode = 0;
    IcdUF_ByteBuffer buffer;
    IcdT_Tm2_history_report packet;

    CHECK(init_decode_buffer(
              &buffer,
              storage,
              (uint32_t)sizeof(storage),
              kTm2HistoryReportPacket,
              (uint32_t)sizeof(kTm2HistoryReportPacket),
              &errorCode),
          "init_decode_buffer(tm2_history_report)");

    CHECK(icdt_tm2_history_report_decode(&buffer, &packet, &errorCode),
          "decode tm2_history_report");

    CHECK(packet.payload.requestId == 0x44, "decoded tm requestId");
    CHECK(packet.payload.samples.count == 3u, "decoded samples.count");
    CHECK(packet.payload.samples.items[0] == 0x0102, "decoded sample[0]");
    CHECK(packet.payload.samples.items[1] == 0xABCD, "decoded sample[1]");
    CHECK(packet.payload.samples.items[2] == 0x0001, "decoded sample[2]");
    CHECK(packet.payload.success == false, "decoded success");

    CHECK(icduf_buffer_clear(&buffer, &errorCode), "buffer_clear(tm2_history_report)");
    CHECK(icdt_tm2_history_report_encode(&buffer, &packet, &errorCode),
          "encode tm2_history_report");

    CHECK(buffer.length == (uint32_t)sizeof(kTm2HistoryReportPacket), "encoded length tm2_history_report");
    CHECK(memcmp(storage, kTm2HistoryReportPacket, sizeof(kTm2HistoryReportPacket)) == 0,
          "encoded bytes tm2_history_report");

    return true;
}

static bool test_header_resolvers(void)
{
    uint32_t errorCode = 0;
    IcdT_PacketType packetType = IcdT_PacketType_Unknown;

    CHECK(icdt_pUSC_TC_get_packet_type(2, 1, &packetType, &errorCode),
          "resolve tc2_set_threshold");
    CHECK(packetType == IcdT_PacketType_Tc2_set_threshold, "resolved tc2_set_threshold enum");

    CHECK(icdt_pUSC_TC_get_packet_type(2, 2, &packetType, &errorCode),
          "resolve tc2_request_history");
    CHECK(packetType == IcdT_PacketType_Tc2_request_history, "resolved tc2_request_history enum");

    CHECK(icdt_pUSC_TM_get_packet_type(2, 1, &packetType, &errorCode),
          "resolve tm2_history_report");
    CHECK(packetType == IcdT_PacketType_Tm2_history_report, "resolved tm2_history_report enum");

    CHECK(!icdt_pUSC_TC_get_packet_type(7, 9, &packetType, &errorCode),
          "unknown TC header should fail");
    CHECK(errorCode == ICDT_ERROR_UNKNOWN_PACKET_TYPE, "unknown TC header error code");

    return true;
}

typedef bool (*TestFunction)(void);

static void run_test(const char* testName, TestFunction testFunction, int* passCount, int* failCount)
{
    printf("%s: ", testName);
    fflush(stdout);

    if (testFunction())
    {
        printf("PASS\n");
        (*passCount)++;
        return;
    }

    printf("FAIL\n");
    (*failCount)++;
}

int main(void)
{
    int passCount = 0;
    int failCount = 0;

    run_test("test_tc2_set_threshold", test_tc2_set_threshold, &passCount, &failCount);
    run_test("test_tc2_request_history", test_tc2_request_history, &passCount, &failCount);
    run_test("test_tm2_history_report", test_tm2_history_report, &passCount, &failCount);
    run_test("test_header_resolvers", test_header_resolvers, &passCount, &failCount);

    printf("Summary: PASS=%d FAIL=%d\n", passCount, failCount);
    return (failCount == 0) ? 0 : 1;
}