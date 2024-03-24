using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TSISP003_Net.SignControllerDataStore.Entities
{
    public class SignController
    {
        public bool OnlineStatus { get; set; }
        public DateTime DateChange { get; set; }

        public ushort ControllerChecksum { get; set; }

        // TO COMPLETE
    }
}