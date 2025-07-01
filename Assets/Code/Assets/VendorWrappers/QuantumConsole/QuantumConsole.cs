namespace QFSW.QC
{
    public class QuantumConsole
    {
        public static QuantumConsole Instance { get; private set; } = new();

        public bool IsActive => true;
        
        public void LogToConsoleAsync(string message) { }
        public void LogToConsole(string name) { }

        public void Activate() { }
        public void Deactivate() { }
    }
}