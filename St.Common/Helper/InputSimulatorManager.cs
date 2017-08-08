using WindowsInput;

namespace St.Common
{
    public class InputSimulatorManager
    {
        private InputSimulatorManager()
        {
            Simulator = new InputSimulator();
        }

        public static readonly InputSimulatorManager Instance = new InputSimulatorManager();

        public InputSimulator Simulator;
    }
}
