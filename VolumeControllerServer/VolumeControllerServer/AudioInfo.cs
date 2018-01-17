using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolumeControllerServer
{
    class AudioInfo
    {
        public string Name { get; set; }
        public int Pid { get; set; }
        public float? Volume { get; set; }
        public bool? IsMuted { get; set; }
    }
}
