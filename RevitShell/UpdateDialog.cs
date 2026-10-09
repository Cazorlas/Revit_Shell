using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using RevitShell.Application;

namespace RevitShell;

/// <summary>Prompts for installation of an available shell extension update.</summary>
public sealed class UpdateDialog : Form, IUpdatePrompt
{
    private UpdatePromptChoice _choice = UpdatePromptChoice.Later;

    /// <summary>Initializes a prompt that creates a fresh dialog for each update.</summary>
    public UpdateDialog()
    {
        Text = "PaperEngineer Shell update";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);
    }

    private UpdateDialog(Version current, UpdateRelease release) : this()
    {
        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Fill
        };
        layout.Controls.Add(new Label
        {
            Text = $"PaperEngineer Shell {release.Version.ToString(3)} is available. You have {current.ToString(3)}.\r\nWindows may ask to close File Explorer while it installs.",
            AutoSize = true,
            MaximumSize = new Size(440, 0),
            Margin = new Padding(0, 0, 0, 12)
        });
        var whatsNew = new LinkLabel
        {
            Text = "What's new",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        whatsNew.LinkClicked += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo(release.PageUrl.AbsoluteUri) { UseShellExecute = true });
            }
            catch
            {
                // Opening release notes must not interrupt the update prompt.
            }
        };
        layout.Controls.Add(whatsNew);
        layout.Controls.Add(CreateChoiceButton("Update now", UpdatePromptChoice.Install));
        layout.Controls.Add(CreateChoiceButton("Skip this version", UpdatePromptChoice.Skip));
        var later = CreateChoiceButton("Later", UpdatePromptChoice.Later);
        later.DialogResult = DialogResult.Cancel;
        layout.Controls.Add(later);
        CancelButton = later;
        Controls.Add(layout);
    }

    /// <inheritdoc />
    public UpdatePromptChoice Ask(Version current, UpdateRelease release)
    {
        using var dialog = new UpdateDialog(current, release);
        return dialog.ShowDialog() == DialogResult.OK ? dialog._choice : UpdatePromptChoice.Later;
    }

    /// <inheritdoc />
    public void ShowUpToDate(Version current)
    {
        MessageBox.Show(
            $"PaperEngineer Shell {current.ToString(3)} is up to date.",
            "PaperEngineer Shell update",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    /// <inheritdoc />
    public void ShowError(string message)
    {
        MessageBox.Show(
            "Could not update PaperEngineer Shell:" + Environment.NewLine + message,
            "PaperEngineer Shell update",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private Button CreateChoiceButton(string text, UpdatePromptChoice choice)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(300, 32),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 4, 0, 0)
        };
        button.Click += (_, _) =>
        {
            _choice = choice;
            DialogResult = choice == UpdatePromptChoice.Later ? DialogResult.Cancel : DialogResult.OK;
        };
        return button;
    }
}
