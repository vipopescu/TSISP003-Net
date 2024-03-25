using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TSISP003.SignControllerDataStore.Entities;

namespace TSISP003_Net.SignControllerDataStore.Entities
{
    public class HighResolutionGraphicFrame : Frame
    {
        public byte NumberOfRows { get; set; }
        public byte NumberOfColumns { get; set; }
        public string Graphic { get; set; } = string.Empty;
    }
}