using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PIModTool.Core.ViewModels.Components;
using PIModTool.Lib;
using PIModTool.Lib.Types;
using Sgml;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

// TODO: Refactor stems to use an enum instead of numbers to identify stems for clarity - shouldn't be hard but skipping for now due to time crunch

namespace PIModTool.Core.ViewModels
{
    public class YukEditorViewModel : MvxViewModel, IDisposable
    {
        private readonly string[] _stemLabels = ["In The Pack / Active (Gladiator)", "Takedown", "Ultimate", "Unwreck", "Boost / Neutral (Gladiator)", "First Place / In Combat (Gladiator)", "Shields Down / 2 Takedowns (Gladiator)", "Left Behind / Death (Gladiator)"];

        private string _loadingScreenMessage = "LOADING";
        private string? _fileName;
        private string _songTitle = "Unknown";
        private string _artistName = "Unknown";
        private bool _stemSelectorActive;
        private bool _busy;
        private bool _previewing;
        private IMessageService _messageService;
        private IMvxNavigationService _navigationService;
        private long? projectSamples;
        private ObservableCollection<AudioStemViewModel> _stemPickers = new ObservableCollection<AudioStemViewModel>();
        private List<SmallFFile> _smallF;
        private SmallFFile? _ampMetadata = null;
        private StemSampleProvider[] _previewStems = new StemSampleProvider[8];
        private VolumeSampleProvider[] _stemVolumes = new VolumeSampleProvider[8];
        private ObservableCollection<float> _mixVolumes = new ObservableCollection<float>();
        private WaveOutEvent? _previewPlayer = null;
        private int _positionStem = 0;
        private int _lastStem = 0;
        private TimeSpan _playbackTime = TimeSpan.Zero;
        private TimeSpan _totalTime;

        public IMvxCommand BackButtonCommand => new MvxAsyncCommand(ChangeView<MainViewModel>);
        public IMvxCommand NewYukCommand => new MvxCommand(NewYuk);
        public IMvxCommand OpenYukCommand => new MvxAsyncCommand(OpenYuk);
        public IMvxCommand SaveYukCommand => new MvxAsyncCommand(SaveYuk);
        public IMvxCommand TogglePreviewsCommand => new MvxAsyncCommand(TogglePreviewsAsync);
        public IMvxCommand OpenAmpCommand => new MvxAsyncCommand(OpenAmp);
        public IMvxCommand ExportWavCommand => new MvxAsyncCommand(ExportWav);
        public IMvxCommand ChangePreviewStemCommand => new MvxAsyncCommand<int>(ChangePreviewStem);
        public IMvxCommand FetchSongDetailsCommand => new MvxAsyncCommand(FetchSongDetails);
        public IMvxCommand SaveSongDetailsCommand => new MvxAsyncCommand(SaveSongDetails);

        public string LoadingScreenMessage { 
            get { return _loadingScreenMessage; }
            set { SetProperty(ref _loadingScreenMessage, value); }
        }

        public string FileName
        {
            get {  return _fileName; }
            set { SetProperty(ref _fileName, value); }
        }
        public string SongTitle
        {
            get { return _songTitle; }
            set { SetProperty(ref _songTitle, value); }
        }
        public string ArtistName
        {
            get { return _artistName; }
            set { SetProperty(ref _artistName, value); }
        }
        public ObservableCollection<AudioStemViewModel> StemPickers
        {
            get { return _stemPickers; }
        }

        public SmallFFile? AmpMetadata
        {
            get { return _ampMetadata; }
            set
            {
                SetProperty(ref _ampMetadata, value);
                RaisePropertyChanged(() => AmpMetadataSelected);
            }
        }
        public bool AmpMetadataSelected
        {
            get { return AmpMetadata != null; }
        }

        public YukEditorViewModel(IMessageService messageService, IMvxNavigationService navigationService)
        {
            _messageService = messageService;
            _navigationService = navigationService;
            for (int i = 0; i < 8; i++)
            {
                StemPickers.Add(CreateStemPicker(i));
                MixVolumes.Add(1f);
            }
            MixVolumes.CollectionChanged += MixVolumes_CollectionChanged;
        }

        private void MixVolumes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ChangeStemVolume(e.NewStartingIndex);
        }

        public void Dispose()
        {
            // Cleanup all audio players
            PreviewPlayer?.Stop();
            PreviewPlayer?.Dispose();
            PreviewPlayer = null;
            for (int i = 0; i < StemPickers.Count; i++)
            {
                StemPickers[i].AudioData?.Dispose();
                StemPickers[i].AudioData = null;
            }
        }

        public override void ViewDisappearing()
        {
            base.ViewDisappearing();
            Dispose();
        }

        private AudioStemViewModel CreateStemPicker(int i)
        {
            AudioStemViewModel stem = new AudioStemViewModel(i, _stemLabels[i], _messageService, ValidStem);
            stem.StemChanged += async (sender, e) => {
                if (PreviewStems[i] != null)
                {
                    PreviewStems[i].ChangeStem(StemPickers[i].AudioData.ToSampleProvider());
                    for (int j = 0; j < StemPickers.Count; j++)
                    {
                        LoopingWaveStream? audioStream = StemPickers[j].AudioData;
                        if (audioStream != null)
                        {
                            audioStream.Position = 0;
                        }

                    }
                }
            };
            return stem;
        }

        private bool ValidStem(long numSamples)
        {
            if (!projectSamples.HasValue)
            {
                projectSamples = numSamples;
                return true;
            }

            return numSamples == projectSamples;
        }

        public bool StemSelectorActive
        {
            get { return _stemSelectorActive; }
            set { SetProperty(ref _stemSelectorActive, value); }
        }

        public bool Busy
        {
            get { return _busy; }
            set { SetProperty(ref _busy, value); }
        }

        public bool Previewing
        {
            get { return _previewing; }
            set { SetProperty(ref _previewing, value); }
        }

        public WaveOutEvent PreviewPlayer
        {
            get { return _previewPlayer; }
            set { SetProperty(ref _previewPlayer, value); }
        }

        public VolumeSampleProvider[] StemVolumes
        {
            get { return _stemVolumes; }
        }

        public ObservableCollection<float> MixVolumes
        {
            get { return _mixVolumes; }
        }

        public StemSampleProvider[] PreviewStems
        {
            get { return _previewStems; }
        }

        public int PositionStem { get { return _positionStem; } }
        public int LastStem { get { return _lastStem; } }

        public TimeSpan PlaybackTime { 
            get { return _playbackTime; }
            set { 
                SetProperty(ref _playbackTime, value);
                RaisePropertyChanged(() => TimeText);
                RaisePropertyChanged(() => PlaybackProgress);
            }
        }

        public TimeSpan TotalTime
        {
            get { return _totalTime; }
            set { 
                SetProperty(ref _totalTime, value);
                RaisePropertyChanged(() => TimeText);
            }
        }

        public string TimeText
        {
            get
            {
                return $"{FormatTime(PlaybackTime)}/{FormatTime(TotalTime)}";
            }
        }

        private static string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
        }

        public double PlaybackProgress
        {
            get => TotalTime.TotalSeconds == 0
                ? 0
                : PlaybackTime.TotalSeconds / TotalTime.TotalSeconds;
            set
            {
                SeekToFraction(value);
                RaisePropertyChanged();
            }
        }

        private void SeekToFraction(double fraction)
        {
            fraction = Math.Clamp(fraction, 0, 1);


            for (int i = 0; i < StemPickers.Count; i++)
            {
                AudioStemViewModel stem = StemPickers[i];
                if (stem.AudioData == null) { return; }

                TimeSpan newTime = TimeSpan.FromSeconds(stem.AudioData.TotalTime.TotalSeconds * fraction);

                stem.AudioData.CurrentTime = newTime;
                PlaybackTime = newTime;
            }
        }

        public void UpdatePlaybackTimer()
        {
            AudioStemViewModel stem = StemPickers[0];
            if (stem.AudioData != null)
            {
                PlaybackTime = stem.AudioData.CurrentTime;
            }
        }

        public async Task ChangeView<TViewModel>() where TViewModel : MvxViewModel
        {
            await _navigationService.Navigate<TViewModel>();
        }

        public void NewYuk()
        {
            StemSelectorActive = true;
        }

        public async Task OpenYuk()
        {
            string? filePath = await _messageService.ShowOpenFileDialogAsync("Select a .yuk file", ".yuk file|*.yuk|All files|*.*");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            LoadingScreenMessage = "LOADING";
            Busy = true;

            FileStream yukStream = File.OpenRead(filePath);

            MemoryStream[] stems = YukHandler.Deinterlace(yukStream);
            MemoryStream[] stemStreams = new MemoryStream[stems.Length];

            for (int i = 0; i < stems.Length; i++)
            {
                byte[]? stemBytes = await YukHandler.ConvertToWav(stems[i].ToArray());
                if (stemBytes == null)
                {
                    await _messageService.ShowErrorAsync("There was an unknown error opening your .yuk file. Please contact the developer.");
                    Busy = false;
                    return;
                }
                stems[i].Close();
                stemStreams[i] = new MemoryStream(stemBytes, writable: false);

                WaveFileReader wavReader = new WaveFileReader(stemStreams[i]);
                StemPickers[i].AudioData = new LoopingWaveStream(WaveFormatConversionStream.CreatePcmStream(wavReader));
                projectSamples = wavReader.SampleCount;
            }

            StemSelectorActive = true;
            FileName = Path.GetFileName(filePath);
            Busy = false;
        }

        public async Task SaveYuk()
        {
            for (int i = 0; i < StemPickers.Count; i++)
            {
                if (StemPickers[i].AudioData == null)
                {
                    await _messageService.ShowErrorAsync("All stems must be set before you can save a .yuk.");
                    return;
                }
            }

            if (PreviewPlayer == null)
            {
                await InitializePlayer();
            }

            string? filePath = await _messageService.ShowSaveFileDialogAsync("Select location to save .yuk file", FileName, ".yuk file|*.yuk|All files|*.*");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            LoadingScreenMessage = "SAVING";
            Busy = true; // Show loading screen

            List<byte[]>? atracStems = await Task.Run(async () => {
                List<byte[]> result = new List<byte[]>();

                // Convert stems to atrac
                for (int i = 0; i < StemPickers.Count; i++)
                {
                    StemPickers[i].AudioData.Loop = false;
                    long tmpPos = StemPickers[i].AudioData.Position;
                    float tmpVol = StemVolumes[i].Volume;
                    StemVolumes[i].Volume = 1f;
                    StemPickers[i].AudioData.Position = 0;

                    // Convert stream to byte[] and send to YukHandler
                    using MemoryStream stemStream = new MemoryStream();
                    ISampleProvider sampleProvider = StemPickers[i].AudioData.ToSampleProvider();

                    WaveFormat targetFormat = new WaveFormat(48000, 16, 2);
                    SampleToWaveProvider16 waveProvider = new SampleToWaveProvider16(sampleProvider);

                    // Write wav to buffer
                    using (WaveFileWriter stemWriter = new WaveFileWriter(stemStream, targetFormat))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;

                        while ((bytesRead = waveProvider.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            stemWriter.Write(buffer, 0, bytesRead);
                        }
                    }

                    byte[] stemBytes = stemStream.ToArray();

                    StemPickers[i].AudioData.Loop = true;
                    StemPickers[i].AudioData.Position = tmpPos;
                    StemVolumes[i].Volume = tmpVol;

                    byte[]? conversionResult = await YukHandler.ConvertToAtrac(stemBytes);
                    if (conversionResult == null)
                    {
                        await _messageService.ShowNotifAsync("Saving .yuk files requires Sony's at3tool, which cannot legally be provided with this software.\n\nPlease place a legitimate copy of Sony's ps3_at3tool.exe in the \"/tools/at3tool/\" directory, then try again.");
                        Busy = false;
                        return null;
                    }

                    result.Add(conversionResult);
                }
                return result;
            });
            
            if(atracStems == null)
            {
                return;
            }

            MemoryStream[] atracStreams = new MemoryStream[8];
            for(int i = 0; i < atracStems.Count; i++)
            {
                byte[] headerlessStem = atracStems[i].Skip(464).ToArray();

                atracStreams[i] = new MemoryStream(headerlessStem);
                atracStreams[i].Position = 0;
            }

            // Interlace the file using YukHandler
            MemoryStream yukStream = YukHandler.Interlace(atracStreams);

            // Open a FileStream and save the file
            FileStream yukFile = File.OpenWrite(filePath);
            yukFile.Write(yukStream.ToArray());
            yukFile.Close();

            // Cleanup
            for(int i = 0; i < atracStreams.Length; i++)
            {
                atracStreams[i].Close();
            }
            yukStream.Close();

            Busy = false;
        }

        private async Task InitializePlayer()
        {
            MixingSampleProvider previewMixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)) { ReadFully = true };
            for (int i = 0; i < StemPickers.Count; i++)
            {
                PreviewStems[i] = new StemSampleProvider(StemPickers[i].AudioData.ToSampleProvider());
                StemVolumes[i] = new VolumeSampleProvider(PreviewStems[i]) { Volume = i == 0 ? MixVolumes[i] : 0f };
                //Debug.WriteLine("Sample Rate: " + stem.WaveFormat.SampleRate);
                previewMixer.AddMixerInput(StemVolumes[i]);
            }
            PreviewPlayer = new WaveOutEvent { DesiredLatency = 100, DeviceNumber = -1 };
            try
            {
                PreviewPlayer.Init(previewMixer);
            }
            catch
            {
                await _messageService.ShowErrorAsync("There was an error starting audio playback. This usually happens when you have no audio devices plugged in.");
            }
            StemPickers[0].IsPlaying = true;
            TotalTime = StemPickers[0].AudioData.TotalTime;
        }

        public async Task TogglePreviewsAsync()
        {
            for (int i = 0; i < StemPickers.Count; i++)
            {
                if (!StemPickers[i].IsFilePicked)
                {
                    await _messageService.ShowErrorAsync("You must select all stems before previewing!");
                    return;
                }
            }

            if (PreviewPlayer == null)
            {
                await InitializePlayer();
            }

            if (!Previewing)
            {
                PreviewPlayer.Play();
            }
            else
            {
                PreviewPlayer.Pause();
            }
            Previewing = !Previewing;

        }

        public async Task OpenAmp()
        {
            string? filePath = await _messageService.ShowOpenFileDialogAsync("Select the game's SmallF.dat", "SmallF.dat|*.dat|All files|*.*");
            if (filePath == null) { return; }
            List<SmallFFile>? files = SmallFHandler.ReadSmallF(filePath);
            if (files == null) { return; }

            SmallFFile? ampMetadata = files.Find((x) => x.Path.Contains("ampmetadata.xml"));

            if (ampMetadata == null)
            {
                await _messageService.ShowErrorAsync("Could not locate ampmetadata.xml within SmallF.dat.");
                return;
            }

            AmpMetadata = ampMetadata;
            _smallF = files;
        }

        public async Task ChangePreviewStem(int stemNum)
        {
            switch (stemNum)
            {
                case 0:
                case 5:
                case 7:
                    await FadeToStem(stemNum, 3f);
                    break;
                default:
                    await FadeToStem(stemNum, 0.5f);
                    break;
            }
        }

        private CancellationTokenSource? _cancelStemFade;

        private void ChangeStemVolume(int stemNum)
        {
            if (StemVolumes[stemNum] != null && StemPickers[stemNum].IsPlaying)
            {
                StemVolumes[stemNum].Volume = MixVolumes[stemNum];
            }
        }

        public async Task FadeToStem(int stemNum, float duration)
        {
            _cancelStemFade?.Cancel();
            _cancelStemFade = new CancellationTokenSource();

            const int fadeResolution = 50; // Adjust if choppy
            int timeStep = (int)((duration * 1000) / fadeResolution);
            timeStep = Math.Max(1, timeStep); // Clamp for tiny values

            CancellationToken token = _cancelStemFade.Token;

            try
            {
                switch (stemNum)
                {
                    case 0:
                    case 5:
                    case 7:
                        _positionStem = stemNum;
                        _lastStem = stemNum;
                        break;
                    case 1:
                    case 2:
                    case 6:
                        _lastStem = stemNum;
                        break;
                }
                float[] originalVolumes = StemVolumes.Select(x => x.Volume).ToArray();

                for (int i = 0; i <= fadeResolution; i++)
                {
                    token.ThrowIfCancellationRequested();
                    float time = i / (float)fadeResolution;
                    for (int j = 0; j < PreviewStems.Length; j++)
                    {
                        if (j == stemNum)
                        {
                            StemVolumes[j].Volume = originalVolumes[j] + (MixVolumes[j] - originalVolumes[j]) * MathF.Sin(time * MathF.PI / 2);
                            if (!StemPickers[j].IsPlaying)
                            {
                                StemPickers[j].IsPlaying = true;
                            }
                        }
                        else if (StemPickers[j].IsPlaying)
                        {
                            {
                                StemVolumes[j].Volume = originalVolumes[j] * MathF.Cos(time * MathF.PI / 2);
                            }
                        }
                    }
                    await Task.Delay(timeStep, token);
                }
            }
            catch(OperationCanceledException)
            {
                return;
            }
            catch
            {
                // TODO: Add proper support for pre-play stem switching that isn't just ignoring it
                return;
            }
            
            for (int i = 0; i < PreviewStems.Length; i++)
            {
                if (i == stemNum)
                {
                    StemVolumes[i].Volume = MixVolumes[i];
                }
                else if (StemPickers[i].IsPlaying)
                {
                    StemVolumes[i].Volume = 0;
                    StemPickers[i].IsPlaying = false;
                }
            }
        }

        public async Task ExportWav()
        {
            string? folderName = await _messageService.ShowSaveFolderDialogAsync("Select a directory to extract wavs to");
            if (string.IsNullOrEmpty(folderName))
            {
                return;
            }

            if (PreviewPlayer == null)
            {
                await InitializePlayer();
            }

            SaveWavs(folderName);
        }

        private async Task SaveWavs(string folderName)
        {
            int numSkipped = 0;
            for (int i = 0; i < StemPickers.Count; i++)
            {
                if (StemPickers[i].AudioData == null)
                {
                    numSkipped++;
                    continue;
                }
                StemPickers[i].AudioData.Loop = false;
                long tmpPos = StemPickers[i].AudioData.Position;
                float tmpVol = StemVolumes[i].Volume;
                StemVolumes[i].Volume = 1f;
                StemPickers[i].AudioData.Position = 0;
                if (FileName != null)
                {
                    WaveFileWriter.CreateWaveFile(folderName + "/" + Path.GetFileNameWithoutExtension(FileName) + "_Stem" + (i + 1) + ".wav", StemPickers[i].AudioData);
                }
                else
                {
                    WaveFileWriter.CreateWaveFile(folderName + "/Untitled_Stem" + (i + 1) + ".wav", StemPickers[i].AudioData);
                }
                StemPickers[i].AudioData.Loop = true;
                StemPickers[i].AudioData.Position = tmpPos;
                StemVolumes[i].Volume = tmpVol;
            }

            if(numSkipped > 0)
            {
                await _messageService.ShowNotifAsync($"{numSkipped} stems were skipped due to not being set.");
            }
        }

        private async Task FetchSongDetails()
        {
            if(AmpMetadata == null || String.IsNullOrEmpty(FileName)) {
                await _messageService.ShowNotifAsync("No data for this song was found in ampmetadata.xml. Make sure your filename matches exactly. If you are creating a new song, you don't need to press this button.");
                return; 
            }

            string xmlData = Encoding.UTF8.GetString(AmpMetadata.Data);

            using StringReader xmlReader = new StringReader(xmlData);
            using SgmlReader ampSgml = new SgmlReader
            {
                DocType = "IGNORE",
                InputStream = xmlReader,
            };

            XDocument ampXml = XDocument.Load(ampSgml);

            XElement? songDetails = ampXml.Descendants("AMPAdaptiveSong").FirstOrDefault(s => (string)s.Element("filename") == FileName);

            if(songDetails == null)
            {
                await _messageService.ShowNotifAsync("No data for this song was found in ampmetadata.xml. Make sure your filename matches exactly. If you are creating a new song, you don't need to press this button.");
                return;
            }

            SongTitle = (string)songDetails.Element("nicename");
            ArtistName = (string)songDetails.Element("artistname");

            float[] volumes = songDetails.Elements().Where(e => e.Name.LocalName.StartsWith("AMPadaptive_")).Take(9).TakeLast(8).Select(val => float.Parse(val.Value, CultureInfo.InvariantCulture)).ToArray();

            /*
             * TODO: Make this shit cleaner because this is atrocious.
             * Use a map tied to LocalName or something idk just don't hardcode indices like this
             * Temporarily:
             * The order volumes are stored in the metadata are:
             * 0, 1, 2, 4, 5, 6, 7, 3
             * Therefore this dumbass if statement maps them correctly
             */
            for (int i = 0; i < volumes.Length; i++)
            {
                if(i == 7)
                {
                    MixVolumes[3] = volumes[i];
                }
                else if(i > 2)
                {
                    MixVolumes[i + 1] = volumes[i];
                }
                else
                {
                    MixVolumes[i] = volumes[i];
                }
            }
        }

        // Helper function: Either changes the value of an existing field, or adds it if it doesn't exist
        private static void SetXMLValue(XElement parent, string name, object value)
        {
            XElement? element = parent.Element(name);
            if (element == null)
            {
                string indent = "  ";
                parent.Add(new XText(Environment.NewLine + indent), new XElement(name, value));
            }
            else
            {
                element.Value = value.ToString()!;
            }
        }

        private async Task SaveSongDetails()
        {
            if (AmpMetadata == null || String.IsNullOrEmpty(FileName))
            {
                await _messageService.ShowErrorAsync("You must specify a filename.");
                return;
            }
            if (!FileName.EndsWith(".yuk"))
            {
                await _messageService.ShowErrorAsync("Your file must have the .yuk extension, otherwise the game won't be able to find it!");
                return;
            }
            if(!FileName.EndsWith("dm.yuk") && !FileName.EndsWith("race.yuk"))
            {
                await _messageService.ShowNotifAsync("Your filename does not end with \"race\" or \"dm\". This means the game will never randomly pick it as a Race or Deathmatch (Gladiator) song, and will ONLY play the song if the filename is explicitly referenced in an EventScript.\n\nOnly proceed if this is your intention, otherwise you should go back and change the filename.");
            }

            string? filePath = await _messageService.ShowSaveFileDialogAsync("Choose a place to save your modified SmallF.dat", "smallf.dat", "SmallF.dat|*.dat|All files|*.*");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            string xmlData = Encoding.UTF8.GetString(AmpMetadata.Data);

            using StringReader xmlReader = new StringReader(xmlData);
            using SgmlReader ampSgml = new SgmlReader
            {
                DocType = "IGNORE",
                InputStream = xmlReader,
            };

            XDocument ampXml = XDocument.Load(ampSgml);
            XElement? songDetails = ampXml.Descendants("AMPAdaptiveSong").FirstOrDefault(s => (string)s.Element("filename") == FileName);

            bool newSong = false;

            if (songDetails == null)
            {
                newSong = true;
                songDetails = new XElement("AMPAdaptiveSong");
                XElement? lastSong = ampXml.Descendants("AMPAdaptiveSong").LastOrDefault();
                if(lastSong == null)
                {
                    await _messageService.ShowErrorAsync("PIModTool was unable to locate the AMPAdaptiveSong section in SmallF.dat!");
                    return;
                }
                lastSong.AddAfterSelf(new XText(Environment.NewLine + "  "), songDetails);
            }

            SetXMLValue(songDetails, "filename", FileName);
            SetXMLValue(songDetails, "nicename", SongTitle);
            SetXMLValue(songDetails, "artistname", ArtistName);
            if (newSong)
            {
                SetXMLValue(songDetails, "AMPadaptive_Song", "1");
            }
            SetXMLValue(songDetails, "AMPadaptive_Default", MixVolumes[0].ToString(CultureInfo.InvariantCulture));
            SetXMLValue(songDetails, "AMPadaptive_Kill", MixVolumes[1].ToString(CultureInfo.InvariantCulture));
            SetXMLValue(songDetails, "AMPadaptive_Rampage", MixVolumes[2].ToString(CultureInfo.InvariantCulture));
            SetXMLValue(songDetails, "AMPadaptive_Boost", MixVolumes[4].ToString(CultureInfo.InvariantCulture));
            SetXMLValue(songDetails, "AMPadaptive_Victory", MixVolumes[5].ToString(CultureInfo.InvariantCulture));
            SetXMLValue(songDetails, "AMPadaptive_Fallback", MixVolumes[6].ToString(CultureInfo.InvariantCulture));
            SetXMLValue(songDetails, "AMPadaptive_Last", MixVolumes[7].ToString(CultureInfo.InvariantCulture));
            SetXMLValue(songDetails, "AMPadaptive_Unwreck", MixVolumes[3].ToString(CultureInfo.InvariantCulture));
            if (newSong)
            {
                // TODO: Add mixer support for these values (and figure out what dream is for - maybe instant replay?)
                string[] unchangedValues = ["Dream", "DMDefault", "DMHunter", "DMAttack", "DMKill", "DMMultikill", "DMRampage", "DMRival", "DMDeath", "DMDream"];
                foreach(string value in unchangedValues)
                {
                    SetXMLValue(songDetails, "AMPadaptive_" + value, "1");
                }
                songDetails.Add(new XText(Environment.NewLine));
            }

            ampXml.Declaration = new XDeclaration("1.0", "utf-8", "yes");

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), // no BOM
                Indent = true, 
                OmitXmlDeclaration = false
            };

            using MemoryStream xmlStream = new MemoryStream();
            using (XmlWriter writer = XmlWriter.Create(xmlStream, settings))
            {
                ampXml.Save(writer);
            }
            AmpMetadata.Data = xmlStream.ToArray();

            // Correct errors caused by normalizing to standard XML format
            string xmlStr = Encoding.UTF8.GetString(AmpMetadata.Data);

            xmlStr = xmlStr.Replace("\n  </FullCue>", "</Cue>\n  ");

            // Track and restore the uncorrected version so we can continue to edit it with proper XML parsing
            byte[] tmpData = new byte[AmpMetadata.Data.Length]; 
            Array.Copy(AmpMetadata.Data, tmpData, AmpMetadata.Data.Length);
            AmpMetadata.Data = Encoding.UTF8.GetBytes(xmlStr);

            _smallF[_smallF.FindIndex(x => x.Path.Contains("ampmetadata.xml"))] = AmpMetadata;

            SmallFHandler.SaveSmallF(_smallF, filePath);

            AmpMetadata.Data = new byte[tmpData.Length];
            Array.Copy(tmpData, AmpMetadata.Data, tmpData.Length);
        }
    }
}
