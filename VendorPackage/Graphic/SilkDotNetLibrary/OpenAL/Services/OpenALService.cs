using Silk.NET.Direct3D.Compilers;
using Silk.NET.OpenAL;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SilkDotNetLibrary.OpenAL.Services
{
    public class OpenALService : IDisposable
    {
        protected ALContext _alContext;
        protected AL _aL;
        protected unsafe Device* _device;
        protected unsafe Context* _context;
        protected bool _disposedValue;

        public unsafe OpenALService()
        {
            _alContext = ALContext.GetApi();
            _aL = AL.GetApi();
            Device* device = _alContext.OpenDevice("");
            if (device == null)
            {
                throw new Exception("Failed to open OpenAL device.");
            }

            Context* context = _alContext.CreateContext(device, null);
            _alContext.MakeContextCurrent(context);
            _aL.GetError();
        }

        /// <summary>
        /// https://github.com/OurDotNetOrganization/Silk.NET/blob/main/examples/CSharp/OpenAL%20Demos/WavePlayer/Program.cs
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="numberOfChannels"></param>
        /// <param name="sampleRate"></param>
        /// <param name="bitsPerSample"></param>
        /// <param name="format"></param>
        /// <param name="sourceBoolean"></param>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task PlaySoundAsync(
            string filePath,
            short numberOfChannels,
            int sampleRate,
            short bitsPerSample,
            BufferFormat format,
            SourceBoolean sourceBoolean = SourceBoolean.Looping,
            CancellationToken cancellationToken = default)
        {

            ReadOnlySpan<byte> file = await File.ReadAllBytesAsync(filePath, cancellationToken);
            ParseAndPlayWaveFile(sourceBoolean, file, cancellationToken : cancellationToken);
        }

        private unsafe void ParseAndPlayWaveFile(
            SourceBoolean sourceBoolean,
            ReadOnlySpan<byte> file,
            short numberOfChannels = 0,
            int sampleRate = 0,
            short bitsPerSample = 0,
            BufferFormat format = 0,
            int index = 0,
            CancellationToken cancellationToken = default)
        {
            if (!file.Slice(index, 4).SequenceEqual(Encoding.ASCII.GetBytes("RIFF")))
            {
                throw new InvalidDataException("Not a valid RIFF file.");
            }
            else
            {
                var chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(file.Slice(index, 4));
                index += 4;

                if (!file.Slice(index, 4).SequenceEqual(Encoding.ASCII.GetBytes("WAVE")))
                {
                    throw new InvalidDataException("Not a valid WAVE file.");
                }

                short blockAlign = 0;

                uint source = _aL.GenSource();
                uint buffer = _aL.GenBuffer();

                //bind source properties
                _aL.SetSourceProperty(source, sourceBoolean, true);

                while (index + 4 < file.Length)
                {
                    string subChunkID = Encoding.ASCII.GetString(file.Slice(index, 4).ToArray());
                    index += 4;
                    uint subChunkSize = BinaryPrimitives.ReadUInt32LittleEndian(file.Slice(index, 4));
                    index += 4;
                    if (subChunkID == "fmt ")
                    {
                        if (subChunkSize != 16)
                        {
                            throw new InvalidDataException("Invalid fmt chunk size.");
                        }
                        ushort audioFormat = BinaryPrimitives.ReadUInt16LittleEndian(file.Slice(index, 2));
                        if (audioFormat != 1)
                        {
                            throw new InvalidDataException("Only PCM format is supported.");
                        }
                        index += 2;
                        numberOfChannels = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                        index += 2;
                        sampleRate = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
                        index += 4;
                        var byteRate = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
                        index += 4;
                        blockAlign = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                        index += 2;
                        bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                        index += 2;
                        // Skip any extra format bytes
                        index += (int)(subChunkSize - 16);

                        // Determine the OpenAL format
                        format = numberOfChannels switch
                        {
                            1 when bitsPerSample == 8 => BufferFormat.Mono8,
                            1 when bitsPerSample == 16 => BufferFormat.Mono16,
                            2 when bitsPerSample == 8 => BufferFormat.Stereo8,
                            2 when bitsPerSample == 16 => BufferFormat.Stereo16,
                            _ => throw new InvalidDataException("Unsupported audio format."),
                        };
                    }
                    else if (subChunkID == "data")
                    {
                        ReadOnlySpan<byte> audioData = file.Slice(index, (int)subChunkSize);
                        fixed (byte* pData = audioData)
                        {
                            _aL.BufferData(buffer, format, pData, (int)subChunkSize, sampleRate);
                        }
                        index += (int)subChunkSize;
                    }
                    else if (subChunkID == "JUNK")
                    {
                        // this exists to align things
                        index += (int)subChunkSize;
                    }
                    else if (subChunkID == "iXML")
                    {
                        string iXMLData = Encoding.ASCII.GetString(file.Slice(index, (int)subChunkSize).ToArray());
                        index += (int)subChunkSize;
                    }
                    else
                    {
                        Console.WriteLine($"Unknown Section: {subChunkID}");
                        index += (int)subChunkSize;
                    }
                }
                //bind buffer to source and play
                _aL.SetSourceProperty(source, SourceInteger.Buffer, buffer);
                _aL.SourcePlay(source);
                _ = Task.Run(SourceStop(_aL, source, cancellationToken), cancellationToken);
            }
        }

        private static Func<Task?> SourceStop(AL aL, uint source, CancellationToken cancellationToken)
        {
            return async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(250, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        aL.SourceStop(source);
                        aL.DeleteSource(source);
                    }
                }
            };
        }

        protected unsafe virtual void OnDisDisposepose()
        {
            _alContext.CloseDevice(_device);
            _alContext.MakeContextCurrent(null);
            _alContext.DestroyContext(_context);
            _aL.Dispose();
            _alContext.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    OnDisDisposepose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}