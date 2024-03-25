using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TSISP003.SignControllerDataStore.Entities;

namespace TSISP003_Net.SignControllerDataStore.Entities
{
    public class TextFrame : Frame
    {
        public byte Font { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}