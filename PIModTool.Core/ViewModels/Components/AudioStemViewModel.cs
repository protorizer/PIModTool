using MvvmCross.Commands;
using MvvmCross.ViewModels;
using NAudio.Utils;
using NAudio.Wave;
using PIModTool.Core.Utilities;
using System.Diagnostics;


namespace PIModTool.Core.ViewModels.Components
{
    public class AudioStemViewModel: MvxViewModel
    {
        private int _stemIndex;
        private LoopingWaveStream? _audioData;
        private long _numSamples;
        private IMessageService _messageService;
        public IMvxCommand PickFileCommand => new MvxAsyncCommand(PickFileAsync);
        public delegate bool FileValidator(long numSamples);
        private readonly FileValidator? _isValidFile;
        public event EventHandler StemChanged;

        // Positioning
        public int Row { get; }
        public int Column { get; }
        public string Label { get; }

        public AudioStemViewModel(int index, string label, IMessageService messageService, FileValidator? validator = null)
        {
            _stemIndex = index;
            _messageService = messageService;
            _isValidFile = validator;

            Row = index / 4;     // 0,0,0,0,1,1,1,1
            Column = index % 4;  // 0,1,2,3,0,1,2,3
            Label = label;
        }

        public bool IsFilePicked
        {
            get
            {
                return AudioData != null;
            }
        }

        public WaveformPeak[]? AudioPeaks
        {
            get
            {
                if (!IsFilePicked)
                {
                    return null;
                }
                // TODO: Remove hardcoded width here
                return WaveformUtilities.CalculatePeaks(AudioData, 275);
            }
        }

        public LoopingWaveStream? AudioData
        {
            get { return _audioData; }
            set
            {
                if (SetProperty(ref _audioData, value))
                {
                    RaisePropertyChanged(() => IsFilePicked);
                    RaisePropertyChanged(() => AudioPeaks);
                    if (_audioData != null)
                    {
                        _audioData.Position = 0;
                    }
                }
                if (AudioData != null)
                {
                    StemChanged?.Invoke(this, new EventArgs());
                }
            }
        }

        private bool _isPlaying = false;
        public bool IsPlaying { 
            get { return _isPlaying;  }
            set { SetProperty(ref _isPlaying, value); }
        }

        public long NumSamples
        {
            get { return _numSamples; }
            set { SetProperty(ref _numSamples, value); }
        }

        public int StemIndex
        {
            get { return _stemIndex; }
        }

        private async Task PickFileAsync()
        {
            // Open file picker, send to NAudio to get raw audio data, set NumSamples based on length etc.
            string? filePath = await _messageService.ShowOpenFileDialogAsync("Select a .wav file", "WAV file|*.wav|All files|*.*");
            if(filePath == null) { return; }

            FileStream wavFile = File.OpenRead(filePath);
            WaveFileReader reader;
            try
            {
                reader = new WaveFileReader(wavFile);
            }
            catch
            {
                await _messageService.ShowErrorAsync("The file you selected is not a valid WAV file.");
                return;
            }

            // Detect and convert codecs
            if (reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            {
                switch (reader.WaveFormat.Encoding)
                {
                    case WaveFormatEncoding.IeeeFloat:
                        WaveFloatTo16Provider converter = new WaveFloatTo16Provider(reader);
                        MemoryStream conversionStream = new MemoryStream();

                        WaveFileWriter.WriteWavFileToStream(conversionStream, converter);
                        conversionStream.Position = 0;

                        reader.Dispose();
                        reader = new WaveFileReader(conversionStream);

                        break;
                    default:
                        await _messageService.ShowErrorAsync("Unsupported WAV codec: " + reader.WaveFormat.Encoding.ToString() + ". Please contact the developer.");
                        break;
                }
            }

            NumSamples = reader.SampleCount;
            if (_isValidFile != null && !_isValidFile(NumSamples))
            {
                // Invalid
                await _messageService.ShowErrorAsync("The WAV file you selected is not the same length as the other stems.\n\nLimitations of the game's .yuk format requires all stems to be exactly the same length.\n\nConsider trimming your stems in a program like Audacity so they have identical sample counts.");
                return;
            }

            AudioData = new LoopingWaveStream(WaveFormatConversionStream.CreatePcmStream(reader));
        }
    }
}
