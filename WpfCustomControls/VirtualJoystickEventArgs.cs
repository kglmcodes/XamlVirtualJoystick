using System;
using static WpfCustomControls.OnScreenJoystick;

namespace WpfCustomControls
{
    public class VirtualJoystickEventArgs:EventArgs
    {
        public double Angle { get; set; }
        public double Distance { get; set; }
        public Direction Dir { get; set; }
    }
}
