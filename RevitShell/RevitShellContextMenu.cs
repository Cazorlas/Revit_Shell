using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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
    private static readonly IRevitFileInspector Inspector = new RevitFileInspector(
        new CompositeRevitVersionDetector(
            new BasicFileInfoRevitVersionDetector(),
            new BinaryTextRevitVersionDetector()));
    private static readonly Image MenuIcon = LoadMenuIcon();

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
        infoItem.Image = MenuIcon;
        infoItem.Click += (_, _) => ShowVersionInfo();

        var openItem = new ToolStripMenuItem("Open with exact Revit version");
        openItem.Image = MenuIcon;
        openItem.Click += (_, _) => OpenWithExactVersion();

        menu.Items.Add(infoItem);
        menu.Items.Add(openItem);

        return menu;
    }

    private void ShowVersionInfo()
    {
        var filePaths = GetSelectedPaths();
        if (filePaths.Length == 0)
        {
            return;
        }

        var message = BuildVersionInfoMessage(filePaths);
        MessageBox.Show(
            message,
            "Revit Version Info",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
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

    private static string BuildVersionInfoMessage(string[] filePaths)
    {
        var builder = new StringBuilder();

        foreach (var filePath in filePaths)
        {
            var fullPath = filePath.Trim('"');
            var result = Inspector.Inspect(fullPath);

            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine($"Name: {Path.GetFileName(fullPath)}");
            builder.AppendLine($"Path: {fullPath}");
            builder.Append($"Version: {result.Description}");
        }

        return builder.ToString();
    }

    private static Image LoadMenuIcon()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("RevitShell.Resources.autodesk_logo.png");

        if (stream == null)
        {
            return SystemIcons.Application.ToBitmap();
        }

        using var sourceImage = Image.FromStream(stream);
        return new Bitmap(sourceImage, new Size(16, 16));
    }
}
