namespace WheelOverlay.Models
{
    public class WheelDefinition
    {
        public required string DeviceName { get; set; }
        public int TextFieldCount { get; set; }
        
        // Could expand this later with button mappings, rotary logic types, etc.
        
        public static readonly WheelDefinition[] SupportedWheels = new[]
        {
            new WheelDefinition 
            { 
                DeviceName = "BavarianSimTec Alpha", 
                TextFieldCount = 8 
            },
            // For verification/future support
            new WheelDefinition
            {
                DeviceName = "Generic 2-Input Wheel",
                TextFieldCount = 2
            }
        };
    }
}
