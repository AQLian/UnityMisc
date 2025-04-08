#include "reliable.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

#define WINDOW_SIZE 32
#define MAX_BUFFER_SIZE 1024
#define RESEND_TIMEOUT 1.0

struct sender_packet {
    uint16_t sequence;
    uint8_t* data;
    int size;
    double send_time;
    int acked;
};

struct receiver_packet {
    uint16_t sequence;
    uint8_t* data;
    int size;
    int valid;
};

struct context {
    struct reliable_endpoint_t* sender;
    struct reliable_endpoint_t* receiver;
    struct sender_packet sender_window[WINDOW_SIZE];
    int sender_in_flight;
    struct receiver_packet receiver_buffer[MAX_BUFFER_SIZE];
    uint16_t receiver_next_expected_sequence;
};

static void sender_transmit_packet(void* context, uint64_t id, uint16_t sequence, uint8_t* packet_data, int packet_bytes) {
    struct context* ctx = (struct context*)context;
    printf("Sender: Transmitting packet %d\n", sequence);
    reliable_endpoint_receive_packet(ctx->receiver, packet_data, packet_bytes);
}

static void receiver_transmit_packet(void* context, uint64_t id, uint16_t sequence, uint8_t* packet_data, int packet_bytes) {
    struct context* ctx = (struct context*)context;
    reliable_endpoint_receive_packet(ctx->sender, packet_data, packet_bytes);
}

static int sender_process_packet(void* context, uint64_t id, uint16_t sequence, uint8_t* packet_data, int packet_bytes) {
    return 1;
}

static int receiver_process_packet(void* context, uint64_t id, uint16_t sequence, uint8_t* packet_data, int packet_bytes) {
    struct context* ctx = (struct context*)context;
    int buffer_index = sequence % MAX_BUFFER_SIZE;

    if (ctx->receiver_buffer[buffer_index].valid && ctx->receiver_buffer[buffer_index].sequence != sequence) {
        printf("Receiver: Ignoring duplicate or overflow for sequence %d\n", sequence);
        return 1;
    }

    if (!ctx->receiver_buffer[buffer_index].valid) {
        ctx->receiver_buffer[buffer_index].sequence = sequence;
        ctx->receiver_buffer[buffer_index].data = malloc(packet_bytes);
        memcpy(ctx->receiver_buffer[buffer_index].data, packet_data, packet_bytes);
        ctx->receiver_buffer[buffer_index].size = packet_bytes;
        ctx->receiver_buffer[buffer_index].valid = 1;
        printf("Receiver: Buffered packet %d\n", sequence);
    }

    while (1) {
        int next_index = ctx->receiver_next_expected_sequence % MAX_BUFFER_SIZE;
        if (!ctx->receiver_buffer[next_index].valid || 
            ctx->receiver_buffer[next_index].sequence != ctx->receiver_next_expected_sequence) {
            break;
        }

        printf("Receiver: Delivering packet %d in order: %.*s\n", 
               ctx->receiver_next_expected_sequence, 
               ctx->receiver_buffer[next_index].size, 
               ctx->receiver_buffer[next_index].data);

        free(ctx->receiver_buffer[next_index].data);
        ctx->receiver_buffer[next_index].valid = 0;
        ctx->receiver_next_expected_sequence++;
    }

    return 1;
}

void init_context(struct context* ctx) {
    struct reliable_config_t sender_config, receiver_config;
    reliable_default_config(&sender_config);
    reliable_default_config(&receiver_config);

    sender_config.transmit_packet_function = sender_transmit_packet;
    sender_config.process_packet_function = sender_process_packet;
    sender_config.context = ctx;

    receiver_config.transmit_packet_function = receiver_transmit_packet;
    receiver_config.process_packet_function = receiver_process_packet;
    receiver_config.context = ctx;

    ctx->sender = reliable_endpoint_create(&sender_config, 0.0);
    ctx->receiver = reliable_endpoint_create(&receiver_config, 0.0);

    memset(ctx->sender_window, 0, sizeof(ctx->sender_window));
    ctx->sender_in_flight = 0;

    memset(ctx->receiver_buffer, 0, sizeof(ctx->receiver_buffer));
    ctx->receiver_next_expected_sequence = 0;
}

void send_packet(struct context* ctx, const char* message) {
    if (ctx->sender_in_flight >= WINDOW_SIZE) {
        printf("Sender: Window full (%d in flight), cannot send\n", ctx->sender_in_flight);
        return;
    }

    uint16_t sequence = reliable_endpoint_next_packet_sequence(ctx->sender);
    int packet_size = strlen(message) + 1;
    uint8_t* packet_data = malloc(packet_size);
    memcpy(packet_data, message, packet_size);

    int index = ctx->sender_in_flight;
    ctx->sender_window[index].sequence = sequence;
    ctx->sender_window[index].data = packet_data;
    ctx->sender_window[index].size = packet_size;
    ctx->sender_window[index].send_time = reliable_time();
    ctx->sender_window[index].acked = 0;
    ctx->sender_in_flight++;

    reliable_endpoint_send_packet(ctx->sender, packet_data, packet_size);
    printf("Sender: Added packet %d to window (in flight: %d)\n", sequence, ctx->sender_in_flight);
}

void process_acks(struct context* ctx) {
    int num_acks;
    uint16_t* acks = reliable_endpoint_get_acks(ctx->sender, &num_acks);

    for (int i = 0; i < num_acks; i++) {
        for (int j = 0; j < ctx->sender_in_flight; j++) {
            if (ctx->sender_window[j].sequence == acks[i] && !ctx->sender_window[j].acked) {
                ctx->sender_window[j].acked = 1;
                printf("Sender: Packet %d acknowledged\n", acks[i]);
                free(ctx->sender_window[j].data);
                for (int k = j; k < ctx->sender_in_flight - 1; k++) {
                    ctx->sender_window[k] = ctx->sender_window[k + 1];
                }
                ctx->sender_in_flight--;
                break;
            }
        }
    }
    reliable_endpoint_clear_acks(ctx->sender);
}

void resend_unacked(struct context* ctx, double current_time) {
    for (int i = 0; i < ctx->sender_in_flight; i++) {
        if (!ctx->sender_window[i].acked && 
            (current_time - ctx->sender_window[i].send_time) > RESEND_TIMEOUT) {
            printf("Sender: Resending unacked packet %d\n", ctx->sender_window[i].sequence);
            reliable_endpoint_send_packet(ctx->sender, ctx->sender_window[i].data, ctx->sender_window[i].size);
            ctx->sender_window[i].send_time = current_time;
        }
    }
}

void cleanup_context(struct context* ctx) {
    for (int i = 0; i < ctx->sender_in_flight; i++) {
        free(ctx->sender_window[i].data);
    }
    for (int i = 0; i < MAX_BUFFER_SIZE; i++) {
        if (ctx->receiver_buffer[i].valid) {
            free(ctx->receiver_buffer[i].data);
        }
    }
    reliable_endpoint_destroy(ctx->sender);
    reliable_endpoint_destroy(ctx->receiver);
}

int main() {
    struct context ctx;
    init_context(&ctx);

    double current_time = 0.0;
    int packet_counter = 0;

    ctx.sender->sequence = 65533;

    while (packet_counter < 40) {
        current_time += 0.1;
        reliable_endpoint_update(ctx.sender, current_time);
        reliable_endpoint_update(ctx.receiver, current_time);

        char message[32];
        snprintf(message, sizeof(message), "Packet %d", packet_counter);
        send_packet(&ctx, message);

        process_acks(&ctx);
        resend_unacked(&ctx, current_time);

        if (ctx->sender_in_flight < WINDOW_SIZE || num_acks > 0) {
            packet_counter++;
        }

        usleep(100000);
    }

    current_time += 1.0;
    reliable_endpoint_update(ctx.sender, current_time);
    reliable_endpoint_update(ctx.receiver, current_time);
    process_acks(&ctx);

    cleanup_context(&ctx);
    return 0;
}