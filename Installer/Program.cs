using System;
using System.Windows.Forms;

namespace Installer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var installer = new SharpShellExtensionInstaller();
        Application.Run(new InstallerForm(installer));
    }
}

