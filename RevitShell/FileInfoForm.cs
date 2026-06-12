using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RevitShell.Core;

namespace RevitShell;

internal sealed class FileInfoForm : Form
{
    public FileInfoForm(string[] filePaths, IRevitFileInspector inspector)
    {
        Text = "Revit File Info";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 360);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        var heading = new Label
        {
            Dock = DockStyle.Top,
            Height = 52,
            Padding = new Padding(16, 14, 16, 0),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point),
            Text = filePaths.Length == 1 ? Path.GetFileName(filePaths[0]) : "Selected Revit files"
        };

        var listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        listView.Columns.Add("File", 320);
        listView.Columns.Add("Extension", 90);
        listView.Columns.Add("Version", 150);
        listView.Columns.Add("Status", 170);

        foreach (var filePath in filePaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var fullPath = filePath.Trim('"');
            var result = inspector.Inspect(fullPath);
            var extension = Path.GetExtension(fullPath);

            var item = new ListViewItem(Path.GetFileName(fullPath));
            item.SubItems.Add(extension);
            item.SubItems.Add(result.Description);
            item.SubItems.Add(result.Status);
            listView.Items.Add(item);
        }

        var closeButton = new Button
        {
            Text = "Close",
            Dock = DockStyle.Right,
            Width = 120
        };
        closeButton.Click += (_, _) => Close();

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            Padding = new Padding(16)
        };
        footer.Controls.Add(closeButton);

        Controls.Add(listView);
        Controls.Add(footer);
        Controls.Add(heading);
    }
}
