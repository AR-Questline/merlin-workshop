using System;
using System.IO;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Audio {
    public class WaveFileGenerator {
        static WaveHeader s_header;
        static WaveFormatChunk s_format;
        static WaveDataChunk s_data;
        static bool s_initialized;

        static void Initialize() {
            // Init chunks
            s_header = new WaveHeader();
            s_format = new WaveFormatChunk();
            s_data = new WaveDataChunk();

            // Fill the data array with sample data

            // Number of samples = sample rate * channels * bytes per sample
            uint numSamples = s_format.dwSamplesPerSec * s_format.wChannels;

            // Initialize the 16-bit array
            s_data.shortArray = new short[numSamples];

            int amplitude = 0; // Max amplitude for 16-bit audio
            double freq = 440.0f; // Concert A: 440Hz

            // The "angle" used in the function, adjusted for the number of channels and sample rate.
            // This value is like the period of the wave.
            double t = (Math.PI * 2 * freq) / (s_format.dwSamplesPerSec * s_format.wChannels);

            for (uint i = 0; i < numSamples - 1; i++) {
                // Fill with a simple sine wave at max amplitude
                for (int channel = 0; channel < s_format.wChannels; channel++) {
                    s_data.shortArray[i + channel] = Convert.ToInt16(amplitude * Math.Sin(t * i));
                }
            }

            // Calculate data chunk size in bytes
            s_data.dwChunkSize = (uint) (s_data.shortArray.Length * (s_format.wBitsPerSample / 8));

            s_initialized = true;
        }

        public static void CreateAndSave(string filePath) {
            // Initialize data
            if (!s_initialized) {
                Initialize();
            }
            
            // Create a file (it always overwrites)
            FileStream fileStream = new FileStream(filePath, FileMode.Create);

            // Use BinaryWriter to write the bytes to the file
            BinaryWriter writer = new BinaryWriter(fileStream);

            // Write the header
            writer.Write(s_header.sGroupID.ToCharArray());
            writer.Write(s_header.dwFileLength);
            writer.Write(s_header.sRiffType.ToCharArray());

            // Write the format chunk
            writer.Write(s_format.sChunkID.ToCharArray());
            writer.Write(s_format.dwChunkSize);
            writer.Write(s_format.wFormatTag);
            writer.Write(s_format.wChannels);
            writer.Write(s_format.dwSamplesPerSec);
            writer.Write(s_format.dwAvgBytesPerSec);
            writer.Write(s_format.wBlockAlign);
            writer.Write(s_format.wBitsPerSample);

            // Write the data chunk
            writer.Write(s_data.sChunkID.ToCharArray());
            writer.Write(s_data.dwChunkSize);
            foreach (short dataPoint in s_data.shortArray) {
                writer.Write(dataPoint);
            }

            writer.Seek(4, SeekOrigin.Begin);
            uint filesize = (uint) writer.BaseStream.Length;
            writer.Write(filesize - 8);

            // Clean up
            writer.Close();
            fileStream.Close();
        }
        // === Data Classes
        class WaveHeader {
            public readonly string sGroupID; // RIFF
            public readonly uint dwFileLength; // total file length minus 8, which is taken up by RIFF
            public readonly string sRiffType; // always WAVE

            /// <summary>
            /// Initializes a WaveHeader object with the default values.
            /// </summary>
            public WaveHeader() {
                dwFileLength = 0;
                sGroupID = "RIFF";
                sRiffType = "WAVE";
            }
        }

        class WaveFormatChunk {
            public readonly string sChunkID; // Four bytes: "fmt "
            public readonly uint dwChunkSize; // Length of header in bytes
            public readonly ushort wFormatTag; // 1 (MS PCM)
            public readonly ushort wChannels; // Number of channels
            public readonly uint dwSamplesPerSec; // Frequency of the audio in Hz... 44100
            public readonly uint dwAvgBytesPerSec; // for estimating RAM allocation
            public readonly ushort wBlockAlign; // sample frame size, in bytes
            public readonly ushort wBitsPerSample; // bits per sample

            /// <summary>
            /// Initializes a format chunk with the following properties:
            /// Sample rate: 44100 Hz
            /// Channels: Stereo
            /// Bit depth: 16-bit
            /// </summary>
            public WaveFormatChunk() {
                sChunkID = "fmt ";
                dwChunkSize = 16;
                wFormatTag = 1;
                wChannels = 2;
                dwSamplesPerSec = 44100;
                wBitsPerSample = 16;
                wBlockAlign = (ushort) (wChannels * (wBitsPerSample / 8));
                dwAvgBytesPerSec = dwSamplesPerSec * wBlockAlign;
            }
        }

        class WaveDataChunk {
            public readonly string sChunkID; // "data"
            public uint dwChunkSize; // Length of header in bytes
            public short[] shortArray; // 8-bit audio

            /// <summary>
            /// Initializes a new data chunk with default values.
            /// </summary>
            public WaveDataChunk() {
                shortArray = Array.Empty<short>();
                dwChunkSize = 0;
                sChunkID = "data";
            }
        }
    }
}
