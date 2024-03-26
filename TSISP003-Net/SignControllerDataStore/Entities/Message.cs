namespace TSISP003_Net.SignControllerDataStore.Entities
{
    public class Message
    {
        public byte MessageID { get; set; }
        public byte MessageRevision { get; set; }
        public byte TransitionTime { get; set; }
        public byte FrameID1 { get; set; }
        public byte FrameID1Time { get; set; }
        public byte FrameID2 { get; set; }
        public byte FrameID2Time { get; set; }
        public byte FrameID3 { get; set; }
        public byte FrameID3Time { get; set; }
        public byte FrameID4 { get; set; }
        public byte FrameID4Time { get; set; }
        public byte FrameID5 { get; set; }
        public byte FrameID5Time { get; set; }
        public byte FrameID16 { get; set; }
        public byte FrameID16Time { get; set; }
    }
}