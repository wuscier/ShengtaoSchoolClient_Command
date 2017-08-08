using WindowsInput;
using WindowsInput.Native;
using St.Common;

namespace St.CommandListener
{
    public class CommandRepeater
    {
        public static readonly CommandRepeater Instance = new CommandRepeater();

        public void TransmitCommand(int directive)
        {
            // if directive is between 65(A) and 90(Z)
            if (directive > 65 && directive <= 90)
            {
                InputSimulatorManager.Instance.Simulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.MENU, (VirtualKeyCode) directive);
            }


            else if (directive == 255)
            {
                InputSimulatorManager.Instance.Simulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.SHIFT, VirtualKeyCode.TAB);
            }
            else
            {
                InputSimulatorManager.Instance.Simulator.Keyboard.KeyPress((VirtualKeyCode) directive);
            }

        }
    }
}
