using System;

namespace Rewired
{
    public class Controller
    {
        public Guid hardwareTypeGuid { get; set; }
        public bool isConnected { get; set; }
        public ControllerType type { get; set; }
    }
}