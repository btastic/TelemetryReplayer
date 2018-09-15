using Caliburn.Micro;
using F1Telemetry;
using F1Telemetry.Models.Raw.F12018;

namespace TelemetryReplayer.ViewModels
{
    public class DebugModel : PropertyChangedBase
    {
        private float sessionTime;
        private PacketType packetType;
        private uint frameIdentifier;
        private byte[] _data;

        public float SessionTime
        {
            get { return sessionTime; }
            set
            {
                sessionTime = value;
                NotifyOfPropertyChange();
            }
        }

        public PacketType PacketType
        {
            get { return packetType; }
            set
            {
                packetType = value;
                NotifyOfPropertyChange();
            }
        }

        public uint FrameIdentifier
        {
            get { return frameIdentifier; }
            set
            {
                frameIdentifier = value;
                NotifyOfPropertyChange();
            }
        }

        public byte[] Data
        {
            get { return _data; }
            set
            {
                _data = value;
                NotifyOfPropertyChange();
            }
        }

        public static DebugModel FromBinaryPacket(BinaryPacket packet)
        {
            return new DebugModel
            {
                Data = packet.Data,
                FrameIdentifier = packet.PacketHeader.FrameIdentifier,
                PacketType = packet.PacketHeader.PacketType,
                SessionTime = packet.PacketHeader.SessionTime,
            };
        }
    }
}
