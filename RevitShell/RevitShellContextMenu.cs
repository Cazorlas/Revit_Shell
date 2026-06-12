using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RevitShell.Core;

namespace RevitShell;

[ComVisible(true)]
[Guid("7C7656C0-A90F-4B96-8B24-86C68A191F14")]
[COMServerAssociation(AssociationType.ClassOfExtension, ".rvt")]
[COMServerAssociation(AssociationType.ClassOfExtension, ".rfa")]
[COMServerAssociation(AssociationType.ClassOfExtension, ".rft")]
[COMServerAssociation(AssociationType.ClassOfExtension, ".rte")]
public class RevitShellContextMenu : SharpContextMenu
{
    private static readonly IRevitFileInspector Inspector = new RevitFileInspector(new BinaryTextRevitVersionDetector());

    protected override bool CanShowMenu()
    {
        var selectedPaths = SelectedItemPaths?.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
        if (selectedPaths == null || selectedPaths.Length == 0)
        {
            return false;
        }

        return selectedPaths.All(Inspector.IsSupportedFile);
    }

    protected override ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();

        var infoItem = new ToolStripMenuItem("Revit Version Info");
        infoItem.Click += (_, _) => ShowVersionInfo();

        var openItem = new ToolStripMenuItem("Open with exact Revit version");
        openItem.Click += (_, _) => OpenWithExactVersion();

        menu.Items.Add(infoItem);
        menu.Items.Add(openItem);

        return menu;
    }

    private void ShowVersionInfo()
    {
        var filePaths = GetSelectedPaths();
        using var form = new FileInfoForm(filePaths, Inspector);
        form.ShowDialog();
    }

    private void OpenWithExactVersion()
    {
        try
        {
            var launcher = new RevitFileLauncher(Inspector, new RegistryRevitApplicationLocator());
            launcher.Open(GetSelectedPaths());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Open with exact Revit version",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private string[] GetSelectedPaths()
    {
        return SelectedItemPaths?
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray()
            ?? Array.Empty<string>();
    }
}
