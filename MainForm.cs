namespace InputOutputSwitcher;

internal sealed class MainForm : Form
{
    private readonly AudioDeviceService deviceService = new();
    private readonly ComboBox outputComboBox = new();
    private readonly ComboBox inputComboBox = new();
    private readonly Button refreshButton = new();
    private readonly CheckBox pinCheckBox = new();
    private readonly Label statusLabel = new();

    public MainForm()
    {
        SuspendLayout();
        BuildUi();
        ResumeLayout(false);

        Load += (_, _) => ReloadDevices();
    }

    private void BuildUi()
    {
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        Text = "Input & Output Switcher";
        ClientSize = new Size(460, 248);
        MinimumSize = new Size(460, 248);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(248, 249, 252);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var headerPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleLabel = new Label
        {
            Text = "Quickly switch your audio output and microphone",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(24, 28, 34)
        };

        pinCheckBox.Text = "Pin on top";
        pinCheckBox.AutoSize = true;
        pinCheckBox.Anchor = AnchorStyles.Right;
        pinCheckBox.CheckedChanged += (_, _) => TopMost = pinCheckBox.Checked;

        var outputLabel = CreateSectionLabel("Output");
        var inputLabel = CreateSectionLabel("Input");

        ConfigureComboBox(outputComboBox);
        ConfigureComboBox(inputComboBox);
        ConfigureActionButton(refreshButton, "Refresh");

        outputComboBox.SelectionChangeCommitted += (_, _) => ApplySelection(outputComboBox, "Output");
        inputComboBox.SelectionChangeCommitted += (_, _) => ApplySelection(inputComboBox, "Input");
        refreshButton.Click += (_, _) => ReloadDevices();

        var outputPanel = CreateDevicePanel(outputLabel, outputComboBox);
        var inputPanel = CreateDevicePanel(inputLabel, inputComboBox);

        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.TopLeft;
        statusLabel.ForeColor = Color.FromArgb(73, 80, 87);
        statusLabel.Text = "Choose a device from either dropdown to switch instantly.";
        statusLabel.Padding = new Padding(6, 6, 6, 0);

        headerPanel.Controls.Add(titleLabel, 0, 0);
        headerPanel.Controls.Add(pinCheckBox, 1, 0);

        layout.Controls.Add(headerPanel, 0, 0);
        layout.Controls.Add(outputPanel, 0, 1);
        layout.Controls.Add(inputPanel, 0, 2);

        var bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 0)
        };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        bottomPanel.Controls.Add(statusLabel, 0, 0);
        bottomPanel.Controls.Add(refreshButton, 1, 0);

        layout.Controls.Add(bottomPanel, 0, 3);

        Controls.Add(layout);
    }

    private void ReloadDevices()
    {
        SetBusyState(true, "Refreshing devices...");

        try
        {
            var snapshot = new DeviceSnapshot(
                deviceService.GetRenderDevices(),
                deviceService.GetDefaultRenderDeviceId(),
                deviceService.GetCaptureDevices(),
                deviceService.GetDefaultCaptureDeviceId());

            BindDevices(outputComboBox, snapshot.Outputs, snapshot.DefaultOutputId);
            BindDevices(inputComboBox, snapshot.Inputs, snapshot.DefaultInputId);

            statusLabel.Text = "Choose a device from either dropdown to switch instantly.";
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Could not load audio devices. {ex.Message}";
        }
        finally
        {
            SetBusyState(false, statusLabel.Text);
        }
    }

    private void ApplySelection(ComboBox comboBox, string category)
    {
        if (comboBox.SelectedItem is not AudioDevice device)
        {
            statusLabel.Text = $"Select a {category.ToLowerInvariant()} device first.";
            return;
        }

        SetBusyState(true, $"Switching {category.ToLowerInvariant()}...");

        try
        {
            deviceService.SetDefaultDevice(device.Id);
            statusLabel.Text = $"{category} switched to {device.Name}.";
            ReloadDevices();
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Could not switch {category.ToLowerInvariant()}. {ex.Message}";
        }
        finally
        {
            SetBusyState(false, statusLabel.Text);
        }
    }

    private void SetBusyState(bool isBusy, string statusText)
    {
        outputComboBox.Enabled = !isBusy && outputComboBox.Items.Count > 0;
        inputComboBox.Enabled = !isBusy && inputComboBox.Items.Count > 0;
        refreshButton.Enabled = !isBusy;
        statusLabel.Text = statusText;
        UseWaitCursor = isBusy;
    }

    private static void ConfigureComboBox(ComboBox comboBox)
    {
        comboBox.Dock = DockStyle.Fill;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.FlatStyle = FlatStyle.Flat;
        comboBox.Margin = new Padding(0, 4, 0, 0);
        comboBox.DisplayMember = nameof(AudioDevice.Name);
    }

    private static void ConfigureActionButton(Button button, string text)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Color.FromArgb(33, 37, 41);
        button.ForeColor = Color.White;
        button.Margin = new Padding(12, 4, 0, 4);
        button.Cursor = Cursors.Hand;
    }

    private static Label CreateSectionLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 22,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(52, 58, 64)
        };
    }

    private static Panel CreateDevicePanel(Label label, ComboBox comboBox)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4)
        };

        panel.Controls.Add(comboBox);
        panel.Controls.Add(label);
        return panel;
    }

    private static void BindDevices(ComboBox comboBox, IReadOnlyList<AudioDevice> devices, string? selectedDeviceId)
    {
        comboBox.BeginUpdate();
        try
        {
            comboBox.DataSource = null;
            comboBox.DisplayMember = nameof(AudioDevice.Name);
            comboBox.ValueMember = nameof(AudioDevice.Id);
            comboBox.DataSource = devices.ToList();

            if (!string.IsNullOrWhiteSpace(selectedDeviceId))
            {
                var selected = devices.FirstOrDefault(device => string.Equals(device.Id, selectedDeviceId, StringComparison.OrdinalIgnoreCase));
                if (selected is not null)
                {
                    comboBox.SelectedItem = selected;
                }
            }

            if (comboBox.SelectedIndex < 0 && comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }
        }
        finally
        {
            comboBox.EndUpdate();
        }
    }

    private sealed record DeviceSnapshot(
        IReadOnlyList<AudioDevice> Outputs,
        string? DefaultOutputId,
        IReadOnlyList<AudioDevice> Inputs,
        string? DefaultInputId);
}
