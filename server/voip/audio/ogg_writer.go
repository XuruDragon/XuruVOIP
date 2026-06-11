package audio

import (
	"encoding/binary"
	"math/rand"
	"os"
	"time"
)

var crcTable [256]uint32

func init() {
	for i := 0; i < 256; i++ {
		r := uint32(i) << 24
		for j := 0; j < 8; j++ {
			if (r & 0x80000000) != 0 {
				r = (r << 1) ^ 0x04C11DB7
			} else {
				r <<= 1
			}
		}
		crcTable[i] = r
	}
}

func calculateCRC(data []byte) uint32 {
	var crc uint32
	for _, b := range data {
		crc = (crc << 8) ^ crcTable[byte(crc>>24)^b]
	}
	return crc
}

// OggWriter packages raw Opus frames into an Ogg/Opus file
type OggWriter struct {
	file         *os.File
	serial       uint32
	pageSequence uint32
	granulePos   int64
	bufferedPkt  []byte
}

// NewOggWriter initializes a new Ogg/Opus file and writes the headers
func NewOggWriter(filePath string) (*OggWriter, error) {
	f, err := os.Create(filePath)
	if err != nil {
		return nil, err
	}

	// Generate a random serial number for the stream
	r := rand.New(rand.NewSource(time.Now().UnixNano()))
	serial := r.Uint32()

	w := &OggWriter{
		file:   f,
		serial: serial,
	}

	// 1. Write OpusHead Page (BOS)
	head := make([]byte, 19)
	copy(head[0:8], "OpusHead")
	head[8] = 1                                     // Version
	head[9] = 1                                     // Channel Count (Mono)
	binary.LittleEndian.PutUint16(head[10:12], 312) // Pre-skip
	binary.LittleEndian.PutUint32(head[12:16], 48000) // Sample Rate
	binary.LittleEndian.PutUint16(head[16:18], 0)     // Output Gain
	head[18] = 0                                     // Mapping Family

	if err := w.writePage(0x02, 0, [][]byte{head}); err != nil {
		f.Close()
		return nil, err
	}

	// 2. Write OpusTags Page
	tags := []byte("OpusTags\x08\x00\x00\x00XuruVOIP\x00\x00\x00\x00")
	if err := w.writePage(0x00, 0, [][]byte{tags}); err != nil {
		f.Close()
		return nil, err
	}

	return w, nil
}

// WriteOpusPacket buffers/writes a raw Opus packet
func (w *OggWriter) WriteOpusPacket(packet []byte) error {
	if len(packet) == 0 {
		return nil
	}

	if w.bufferedPkt != nil {
		// Write the previously buffered packet
		if err := w.writePage(0x00, w.granulePos, [][]byte{w.bufferedPkt}); err != nil {
			return err
		}
		// Increment granule position: 960 samples for 20ms frame at 48kHz
		w.granulePos += 960
	}

	// Buffer the current packet
	w.bufferedPkt = make([]byte, len(packet))
	copy(w.bufferedPkt, packet)
	return nil
}

// Close flushes the buffered packet with the EOS flag and closes the file
func (w *OggWriter) Close() error {
	if w.bufferedPkt != nil {
		// Write the last packet with EOS (0x04) flag
		_ = w.writePage(0x04, w.granulePos, [][]byte{w.bufferedPkt})
		w.bufferedPkt = nil
	} else {
		// Write an empty EOS page if no packets were buffered
		_ = w.writePage(0x04, w.granulePos, [][]byte{})
	}
	return w.file.Close()
}

// writePage serializes packets into an Ogg page
func (w *OggWriter) writePage(headerType byte, granulePos int64, packets [][]byte) error {
	// Calculate segment sizes
	var segments []byte
	var payloadSize int
	for _, pkt := range packets {
		size := len(pkt)
		for size >= 255 {
			segments = append(segments, 255)
			size -= 255
			payloadSize += 255
		}
		segments = append(segments, byte(size))
		payloadSize += size
	}

	if len(segments) > 255 {
		// For safety in this audio server context, we only put 1 packet per page,
		// so segment count will never exceed 255.
		segments = segments[:255]
	}

	headerLen := 27 + len(segments)
	page := make([]byte, headerLen+payloadSize)

	// Ogg Header
	copy(page[0:4], "OggS")
	page[4] = 0 // Stream structure version
	page[5] = headerType
	binary.LittleEndian.PutUint64(page[6:14], uint64(granulePos))
	binary.LittleEndian.PutUint32(page[14:18], w.serial)
	binary.LittleEndian.PutUint32(page[18:22], w.pageSequence)
	binary.LittleEndian.PutUint32(page[22:26], 0) // CRC (filled later)
	page[26] = byte(len(segments))
	copy(page[27:27+len(segments)], segments)

	// Payload
	offset := 27 + len(segments)
	for _, pkt := range packets {
		copy(page[offset:], pkt)
		offset += len(pkt)
	}

	// Calculate and write CRC
	crc := calculateCRC(page)
	binary.LittleEndian.PutUint32(page[22:26], crc)

	// Write to file
	_, err := w.file.Write(page)
	if err != nil {
		return err
	}

	w.pageSequence++
	return nil
}
