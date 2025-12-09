using System;
using System.Collections.Generic;

namespace WheelOverlay.Models
{
    public class Profile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Default";
        public string DeviceName { get; set; } = "BavarianSimTec Alpha";
        public DisplayLayout Layout { get; set; } = DisplayLayout.Single;
        public List<string> TextLabels { get; set; } = new List<string>();
    }
}
