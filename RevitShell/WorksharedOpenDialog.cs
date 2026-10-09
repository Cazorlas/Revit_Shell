using System.Drawing;
using System.Windows.Forms;
using RevitShell.Application;
using RevitShell.Domain;

namespace RevitShell;

/// <summary>Prompts for the opening mode of a workshared model.</summary>
public sealed class WorksharedOpenDialog : Form, IWorksharedOpenPrompt
{
    private WorksharedOpenChoice _choice = WorksharedOpenChoice.Cancel;

    /// <summary>Initializes a prompt that creates a fresh dialog for each model.</summary>
    public WorksharedOpenDialog()
    {
        Text = "Open with exact Revit version";
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

    private WorksharedOpenDialog(RevitInfo info) : this()
    {
        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Fill
        };
        var summary = $"{info.Name} is a {info.Worksharing.Text} model (Revit {info.Version}).";
        if (!string.IsNullOrWhiteSpace(info.Worksharing.CentralModelPath))
        {
            summary += "\r\nCentral model: " + info.Worksharing.CentralModelPath;
        }

        layout.Controls.Add(new Label
        {
            Text = summary,
            AutoSize = true,
            MaximumSize = new Size(440, 0),
            Margin = new Padding(0, 0, 0, 12)
        });
        layout.Controls.Add(CreateChoiceButton("Detach from central", WorksharedOpenChoice.Detach));
        var createLocal = CreateChoiceButton("Create new local", WorksharedOpenChoice.CreateLocal);
        createLocal.Enabled = info.Worksharing.State == WorksharingState.Central;
        layout.Controls.Add(createLocal);
        layout.Controls.Add(CreateChoiceButton("Open directly", WorksharedOpenChoice.Open));
        var cancel = CreateChoiceButton("Cancel", WorksharedOpenChoice.Cancel);
        cancel.DialogResult = DialogResult.Cancel;
        layout.Controls.Add(cancel);
        CancelButton = cancel;
        Controls.Add(layout);
    }

    /// <inheritdoc />
    public WorksharedOpenChoice Ask(RevitInfo info)
    {
        using var dialog = new WorksharedOpenDialog(info);
        return dialog.ShowDialog() == DialogResult.OK ? dialog._choice : WorksharedOpenChoice.Cancel;
    }

    private Button CreateChoiceButton(string text, WorksharedOpenChoice choice)
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
            DialogResult = choice == WorksharedOpenChoice.Cancel ? DialogResult.Cancel : DialogResult.OK;
        };
        return button;
    }
}
