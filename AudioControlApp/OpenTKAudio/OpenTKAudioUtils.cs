using System;
using System.IO;
using OpenTK.Audio.OpenAL;

namespace OpenTKAudio.Engine
{
    internal static class OpenTKAudioUtils
    {

        // taken from OpenTK example
        internal static byte []LoadWave (string fileName, out int channels, out int bits, out int rate)
        {
            Stream fileStream = File.Open (fileName, FileMode.Open);

            using (BinaryReader reader = new BinaryReader (fileStream)) {
                string signature = new string (reader.ReadChars (4));
                int riff_chunk_size = reader.ReadInt32 ();
                string format = new string (reader.ReadChars (4));
                string format_signature = new string (reader.ReadChars (4));


                int format_chunk_size = reader.ReadInt32 ();
                int audio_format = reader.ReadInt16 ();
                channels = reader.ReadInt16 ();
                rate = reader.ReadInt32 ();
                int byte_rate = reader.ReadInt32 ();
                int block_align = reader.ReadInt16 ();
                bits = reader.ReadInt16 ();

                string data_signature = new string (reader.ReadChars (4));
                int data_chunk_size = reader.ReadInt32 ();

                return reader.ReadBytes ((int)reader.BaseStream.Length);
            }
        }

        // taken from OpenTK example too
        internal static ALFormat WaveFormat (int channels, int bits)
        {
            switch (channels) {
                case 1:
                    return bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
                case 2:
                    return bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
                default:
                    throw new NotSupportedException ("The specified sound format is not supported.");
            }
        }
    }
}