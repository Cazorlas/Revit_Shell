using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RevitShell.Application;

namespace RevitShell;

/// <summary>
/// Provides the Explorer context menu for supported Revit files.
/// </summary>
[ComVisible(true)]
[Guid("7C7656C0-A90F-4B96-8B24-86C68A191F14")]
[COMServerAssociation(AssociationType.AllFiles)]
public class RevitShellContextMenu : SharpContextMenu
{
    private static readonly Image MenuIcon = LoadMenuIcon();

    /// <summary>
    /// Determines whether the context menu should be shown for the current Explorer selection.
    /// </summary>
    /// <returns><see langword="true"/> when the selection only contains supported Revit files; otherwise, <see langword="false"/>.</returns>
    protected override bool CanShowMenu()
    {
        var selectedPaths = SelectedItemPaths?.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
        if (selectedPaths == null || selectedPaths.Length == 0)
        {
            return false;
        }

        return selectedPaths.All(RevitShellCompositionRoot.FileInspector.IsSupportedFile);
    }

    /// <summary>
    /// Creates the context menu and attaches command handlers.
    /// </summary>
    /// <returns>The populated context menu.</returns>
    protected override ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();

        var infoItem = new ToolStripMenuItem("Revit Version Info");
        infoItem.Image = MenuIcon;
        infoItem.Click += (_, _) => ShowVersionInfo();

        var openItem = new ToolStripMenuItem("Open with exact Revit version");
        openItem.Image = MenuIcon;
        openItem.Click += (_, _) => OpenWithExactVersion();

        var updateItem = new ToolStripMenuItem("Check for updates");
        updateItem.Image = MenuIcon;
        updateItem.Click += (_, _) => CheckForUpdates();

        menu.Items.Add(infoItem);
        menu.Items.Add(openItem);
        menu.Items.Add(updateItem);

        return menu;
    }

    /// <summary>
    /// Displays the version information dialog for the selected Revit files.
    /// </summary>
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
        UpdateScheduler.StartAutomaticCheck();
    }

    /// <summary>
    /// Attempts to open the selected files with their exact installed Revit versions.
    /// </summary>
    private void OpenWithExactVersion()
    {
        try
        {
            var result = RevitShellCompositionRoot.OpenRevitFiles.Execute(GetSelectedPaths());
            if (result.Failures.Count > 0)
            {
                MessageBox.Show(
                    string.Join(Environment.NewLine, result.Failures.Select(failure =>
                        $"{Path.GetFileName(failure.FilePath)}: {failure.Message}")),
                    "Open with exact Revit version",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Open with exact Revit version",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        UpdateScheduler.StartAutomaticCheck();
    }

    /// <summary>Checks for shell extension updates at the user's request.</summary>
    private void CheckForUpdates()
    {
        try
        {
            RevitShellCompositionRoot.CheckForUpdates.Run(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "PaperEngineer Shell update",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Returns the sanitized set of selected Explorer paths.
    /// </summary>
    /// <returns>An array of non-empty selected paths.</returns>
    private string[] GetSelectedPaths()
    {
        return SelectedItemPaths?
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray()
            ?? Array.Empty<string>();
    }

    /// <summary>
    /// Builds the version information message shown in the dialog.
    /// </summary>
    /// <param name="filePaths">The selected Revit file paths.</param>
    /// <returns>A formatted multi-line message for the selected files.</returns>
    private static string BuildVersionInfoMessage(string[] filePaths)
    {
        return RevitVersionInfoFormatter.Build(filePaths.Select(filePath =>
            RevitShellCompositionRoot.FileInspector.Inspect(filePath.Trim('"'))));
    }

    /// <summary>
    /// Loads the embedded PaperEngineer Shell icon used by the context menu items.
    /// </summary>
    /// <returns>A 16x16 image for the context menu.</returns>
    private static Image LoadMenuIcon()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("RevitShell.Resources.PaperEngineerShell_16.png");

        if (stream == null)
        {
            return SystemIcons.Application.ToBitmap();
        }

        using var sourceImage = Image.FromStream(stream);
        return new Bitmap(sourceImage, new Size(16, 16));
    }
}
