using System;
using System.Drawing;
using System.Windows.Forms;

namespace Installer;

internal sealed class InstallerForm : Form
{
    private readonly SharpShellExtensionInstaller _installer;
    private readonly Label _statusLabel;
    private readonly Button _installButton;
    private readonly Button _uninstallButton;

    public InstallerForm(SharpShellExtensionInstaller installer)
    {
        _installer = installer ?? throw new ArgumentNullException(nameof(installer));
        Text = "Revit Shell Installer";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(700, 340);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        var titleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 72,
            Padding = new Padding(18, 18, 18, 0),
            Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point),
            Text = "Revit Shell"
        };

        var subtitleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 78,
            Padding = new Padding(20, 0, 20, 0),
            Text = "Installs a SharpShell-based Explorer extension for .rvt, .rfa, .rft, and .rte files with the commands Revit Version Info and Open with exact Revit version."
        };

        _statusLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 56,
            Padding = new Padding(20, 0, 20, 0),
            Text = "Ready"
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(16),
            WrapContents = false
        };

        _installButton = new Button
        {
            Text = "Install",
            Width = 120,
            Height = 32,
            Margin = new Padding(8, 10, 0, 10)
        };
        _installButton.Click += (_, _) => Install();

        _uninstallButton = new Button
        {
            Text = "Uninstall",
            Width = 120,
            Height = 32,
            Margin = new Padding(8, 10, 0, 10)
        };
        _uninstallButton.Click += (_, _) => Uninstall();

        var closeButton = new Button
        {
            Text = "Close",
            Width = 120,
            Height = 32,
            Margin = new Padding(8, 10, 0, 10)
        };
        closeButton.Click += (_, _) => Close();

        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(_uninstallButton);
        buttonPanel.Controls.Add(_installButton);

        Controls.Add(buttonPanel);
        Controls.Add(_statusLabel);
        Controls.Add(subtitleLabel);
        Controls.Add(titleLabel);

        Load += (_, _) => RefreshState();
    }

    private void RefreshState()
    {
        var installed = _installer.IsInstalled();
        _statusLabel.Text = installed
            ? $"Installed at {_installer.InstallDirectory}"
            : $"Not installed. Target path: {_installer.InstallDirectory}";
        _installButton.Enabled = true;
        _uninstallButton.Enabled = installed;
    }

    private void Install()
    {
        try
        {
            _installer.Install();
            MessageBox.Show(
                "Installed. If Explorer does not refresh right away, restart File Explorer.",
                "Revit Shell Installer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Install failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        RefreshState();
    }

    private void Uninstall()
    {
        try
        {
            _installer.Uninstall();
            MessageBox.Show(
                "Uninstalled.",
                "Revit Shell Installer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Uninstall failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        RefreshState();
    }
}
