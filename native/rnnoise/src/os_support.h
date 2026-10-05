// RoomMute compatibility shim for RNNoise 0.2 scalar path (upstream header is absent).
#ifndef ROOMMUTE_OS_SUPPORT_H
#define ROOMMUTE_OS_SUPPORT_H
#include <string.h>
#define OPUS_CLEAR(dst, n) memset((dst), 0, (n)*sizeof(*(dst)))
#endif
