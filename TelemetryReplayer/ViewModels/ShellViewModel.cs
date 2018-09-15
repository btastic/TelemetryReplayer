using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Caliburn.Micro;
using F1Telemetry;
using F1Telemetry.Models.Raw.F12018;
using MessagePack;
using Newtonsoft.Json;
using TelemetryReplayer.Utility;

namespace TelemetryReplayer.ViewModels
{
    public enum Status
    {
        LoadFile,
        LoadingFile,
        FileLoaded,
        Playing
    }

    public enum ReplaySpeed
    {
        x1 = 1,
        x2 = 2,
        x4 = 4,
        x8 = 8
    }

    [Export(typeof(IShell))]
    public class ShellViewModel : PropertyChangedBase, IShell
    {
        public string ReplaySpeedText
        {
            get
            {
                switch (ReplaySpeed)
                {
                    case ReplaySpeed.x1:
                        return "x1";
                    case ReplaySpeed.x2:
                        return "x2";
                    case ReplaySpeed.x4:
                        return "x4";
                    case ReplaySpeed.x8:
                        return "x8";
                    default:
                        return "xx";
                }
            }
        }

        public string PlayLabel
        {
            get
            {
                return Status == Status.Playing ? "Stop" : "Play";
            }
        }

        public bool CanStop
        {
            get
            {
                return Status == Status.FileLoaded || Status == Status.Playing;
            }
        }

        public bool CanToggleReplaySpeed
        {
            get
            {
                return Status == Status.FileLoaded || Status == Status.Playing;
            }
        }

        public bool CanTogglePlay
        {
            get
            {
                return Status == Status.FileLoaded || Status == Status.Playing;
            }
        }

        public bool IsPlaying
        {
            get
            {
                return Status == Status.Playing;
            }
        }

        public bool IsLoading
        {
            get
            {
                return Status == Status.LoadingFile;
            }
        }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case Status.LoadFile:
                        return "Please select a file";
                    case Status.LoadingFile:
                        return $"Loading {SelectedReplay.Name}, please wait";
                    case Status.FileLoaded:
                        return $"{SelectedReplay.Name} loaded. Replay has {ReplayData.Count} data points.";
                    case Status.Playing:
                        return $"Playing {SelectedReplay.Name}";
                    default:
                        return "Unknown state";
                }
            }
        }

        public string PlayStatusText
        {
            get
            {
                switch (Status)
                {
                    case Status.Playing:
                        return $"Playing {ReplayDataIndex} / {ReplayData.Count}";
                    default:
                        return "Not playing yet.";
                }
            }
        }

        private ReplaySpeed _replaySpeed = ReplaySpeed.x1;
        public ReplaySpeed ReplaySpeed
        {
            get { return _replaySpeed; }
            set
            {
                _replaySpeed = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(() => ReplaySpeedText);
            }
        }

        private Status _status;
        public Status Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(() => StatusText);
                NotifyOfPropertyChange(() => IsPlaying);
                NotifyOfPropertyChange(() => IsLoading);
                NotifyOfPropertyChange(() => CanTogglePlay);
                NotifyOfPropertyChange(() => PlayLabel);
                NotifyOfPropertyChange(() => CanToggleReplaySpeed);
                NotifyOfPropertyChange(() => CanStop);
            }
        }

        private FileInfo _selectedReplay;
        public FileInfo SelectedReplay
        {
            get { return _selectedReplay; }
            set
            {
                _selectedReplay = value;
                NotifyOfPropertyChange();
            }
        }

        private DebugModel _selectedDebugPacket;
        public DebugModel SelectedDebugPacket
        {
            get { return _selectedDebugPacket; }
            set
            {
                _selectedDebugPacket = value;
                NotifyOfPropertyChange();
            }
        }

        private bool _debug;
        public bool Debug
        {
            get { return _debug; }
            set
            {
                _debug = value;
                NotifyOfPropertyChange();
                if (!_debug)
                {
                    DebugData.Clear();
                    DebugDetails = string.Empty;
                }
            }
        }

        private string _debugDetails;
        public string DebugDetails
        {
            get { return _debugDetails; }
            set
            {
                _debugDetails = value;
                NotifyOfPropertyChange();
            }
        }


        private ObservableCollection<FileInfo> _replayFiles;
        public ObservableCollection<FileInfo> ReplayFiles
        {
            get { return _replayFiles; }
            set
            {
                _replayFiles = value;
                NotifyOfPropertyChange();
            }
        }

        private ObservableCollection<DebugModel> _debugData = new ObservableCollection<DebugModel>();
        public ObservableCollection<DebugModel> DebugData
        {
            get { return _debugData; }
            set
            {
                _debugData = value;
                NotifyOfPropertyChange();
            }
        }

        private List<BinaryPacket> _replayData;
        public List<BinaryPacket> ReplayData
        {
            get { return _replayData; }
            private set
            {
                _replayData = value;
                NotifyOfPropertyChange();
            }
        }

        private int _replayDataIndex;
        public int ReplayDataIndex
        {
            get { return _replayDataIndex; }
            set
            {
                _replayDataIndex = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(() => PlayStatusText);
            }
        }

        private CancellationTokenSource _cancelTasks;

        public ShellViewModel()
        {
            InitReplayFiles();
        }

        private void InitReplayFiles()
        {
            var directoryInfo = new DirectoryInfo(@"data");
            ReplayFiles = new ObservableCollection<FileInfo>(directoryInfo.GetFiles("*.f1s"));
        }

        public async Task OnReplaySelectedAsync(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] != null)
            {
                Status = Status.LoadingFile;
                ReplayData = await LoadReplay((FileInfo)e.AddedItems[0]);
                ReplayDataIndex = 0;
                DebugData.Clear();
                DebugDetails = string.Empty;
                Status = Status.FileLoaded;
            }
        }

        public void OnDebugDetailsTextChanged(object sender, TextChangedEventArgs e)
        {
            ((TextBox)sender).ScrollToHome();
        }

        public void OnDebugDataSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedDebugData = ((DebugModel)e.AddedItems[0]);
            switch (selectedDebugData.PacketType)
            {
                case PacketType.Motion:
                    var motionData = StructUtility.ConvertToPacket<PacketMotionData>(selectedDebugData.Data);
                    DebugDetails = JsonConvert.SerializeObject(motionData, Formatting.Indented);
                    return;
                case PacketType.Session:
                    var sessionData = StructUtility.ConvertToPacket<PacketSessionData>(selectedDebugData.Data);
                    DebugDetails = JsonConvert.SerializeObject(sessionData, Formatting.Indented);
                    return;
                case PacketType.LapData:
                    var lapData = StructUtility.ConvertToPacket<PacketLapData>(selectedDebugData.Data);
                    DebugDetails = JsonConvert.SerializeObject(lapData, Formatting.Indented);
                    return;
                case PacketType.Event:
                    var eventData = StructUtility.ConvertToPacket<EventPacket>(selectedDebugData.Data);
                    DebugDetails = JsonConvert.SerializeObject(eventData, Formatting.Indented);
                    return;
                case PacketType.Participants:
                    var participantsData = StructUtility.ConvertToPacket<PacketParticipantsData>(selectedDebugData.Data);
                    DebugDetails = JsonConvert.SerializeObject(participantsData, Formatting.Indented);
                    return;
                case PacketType.CarSetups:
                    var carSetupsData = StructUtility.ConvertToPacket<PacketCarSetupData>(selectedDebugData.Data);
                    DebugDetails = JsonConvert.SerializeObject(carSetupsData, Formatting.Indented);
                    return;
                case PacketType.CarTelemetry:
                    var carTelemetryData = StructUtility.ConvertToPacket<PacketCarTelemetryData>(selectedDebugData.Data);
                    DebugDetails = JsonConvert.SerializeObject(carTelemetryData, Formatting.Indented);
                    return;
                case PacketType.CarStatus:
                    var carStatusData = StructUtility.ConvertToPacket<PacketCarStatusData>(selectedDebugData.Data);
                    DebugDetails = JsonConvert.SerializeObject(carStatusData, Formatting.Indented);
                    return;
            }
        }

        private async Task<List<BinaryPacket>> LoadReplay(FileInfo file)
        {
            return await Task<List<BinaryPacket>>.Run(() =>
            {
                var data = File.ReadAllBytes(file.FullName);
                return LZ4MessagePackSerializer.Deserialize<List<BinaryPacket>>(data);
            });
        }

        public void Stop()
        {
            if (IsPlaying)
            {
                _cancelTasks.Cancel();
                Status = Status.FileLoaded;
                return;
            }

            ReplayDataIndex = 0;
        }

        public void ToggleReplaySpeed()
        {
            if (ReplaySpeed == ReplaySpeed.x8)
            {
                ReplaySpeed = ReplaySpeed.x1;
            }
            else
            {
                ReplaySpeed = ReplaySpeed.Next();
            }
        }

        public async Task TogglePlayAsync()
        {
            if (IsPlaying)
            {
                _cancelTasks.Cancel();
                Status = Status.FileLoaded;
                return;
            }

            _cancelTasks = new CancellationTokenSource();
            Status = Status.Playing;

            await Task.Run(() =>
            {
                var lastPackageTimeSpan = new TimeSpan();

                var groupEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20777);
                using (var udpSocket = new UdpClient())
                {
                    udpSocket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                    for (; ReplayDataIndex < ReplayData.Count; ReplayDataIndex++)
                    {
                        if (_cancelTasks.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        BinaryPacket item = ReplayData[ReplayDataIndex];

                        if (!lastPackageTimeSpan.Equals(default(TimeSpan)))
                        {
                            Thread.Sleep(1000 / (60 * (int)ReplaySpeed));
                        }

                        udpSocket.Send(item.Data, item.Data.Length, groupEP);

                        if (Debug)
                        {
                            Execute.OnUIThread(() =>
                            {
                                DebugData.Insert(0, DebugModel.FromBinaryPacket(item));
                            });
                        }

                        if (ReplayDataIndex == ReplayData.Count - 1)
                        {
                            lastPackageTimeSpan = default(TimeSpan);
                        }
                        else
                        {
                            lastPackageTimeSpan = item.TimeSpan;
                        }
                    }
                }
            }, _cancelTasks.Token);
        }
    }
}