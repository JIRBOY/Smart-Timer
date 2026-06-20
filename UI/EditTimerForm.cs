using System;
using System.Drawing;
using System.Windows.Forms;
using SmartTimer.Models;

namespace SmartTimer.UI;

/// <summary>
/// 编辑定时窗口
/// </summary>
public class EditTimerForm : Form
{
    public TimerItem Timer { get; private set; }

    private readonly TextBox _txtName;
    private readonly ComboBox _cmbType;
    private readonly DateTimePicker _dtpTarget;
    private readonly NumericUpDown _nudInterval;
    private readonly DateTimePicker _dtpDaily;
    private readonly CheckBox[] _chkWeekDays;
    private readonly TextBox _txtMessage;
    private readonly CheckBox _chkSound;
    private readonly CheckBox _chkPopup;

    public EditTimerForm(TimerItem? existing = null)
    {
        Timer = existing ?? new TimerItem();

        Text = existing == null ? "添加定时" : "编辑定时";
        Size = new Size(450, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        int y = 15;
        const int labelWidth = 80;
        const int controlLeft = 100;

        // 名称
        Controls.Add(new Label { Text = "名称:", Left = 15, Top = y + 3, Width = labelWidth });
        _txtName = new TextBox { Left = controlLeft, Top = y, Width = 300, Text = Timer.Name };
        Controls.Add(_txtName);
        y += 35;

        // 类型
        Controls.Add(new Label { Text = "类型:", Left = 15, Top = y + 3, Width = labelWidth });
        _cmbType = new ComboBox
        {
            Left = controlLeft,
            Top = y,
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbType.Items.AddRange(new object[] { "一次性", "周期性间隔", "每日定时", "每周定时" });
        _cmbType.SelectedIndex = (int)Timer.Type;
        _cmbType.SelectedIndexChanged += CmbType_SelectedIndexChanged;
        Controls.Add(_cmbType);
        y += 35;

        // 目标时间（一次性）
        Controls.Add(new Label { Text = "目标时间:", Left = 15, Top = y + 3, Width = labelWidth });
        _dtpTarget = new DateTimePicker
        {
            Left = controlLeft,
            Top = y,
            Width = 250,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            Value = Timer.TargetTime
        };
        Controls.Add(_dtpTarget);
        y += 35;

        // 间隔分钟（周期性）
        Controls.Add(new Label { Text = "间隔(分):", Left = 15, Top = y + 3, Width = labelWidth });
        _nudInterval = new NumericUpDown
        {
            Left = controlLeft,
            Top = y,
            Width = 100,
            Minimum = 1,
            Maximum = 1440,
            Value = Timer.IntervalMinutes
        };
        Controls.Add(_nudInterval);
        y += 35;

        // 每日时间
        Controls.Add(new Label { Text = "每日时间:", Left = 15, Top = y + 3, Width = labelWidth });
        _dtpDaily = new DateTimePicker
        {
            Left = controlLeft,
            Top = y,
            Width = 150,
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = DateTime.Today.Add(Timer.DailyTime)
        };
        Controls.Add(_dtpDaily);
        y += 35;

        // 每周几（位掩码）
        Controls.Add(new Label { Text = "每周:", Left = 15, Top = y, Width = labelWidth });
        string[] dayNames = { "日", "一", "二", "三", "四", "五", "六" };
        _chkWeekDays = new CheckBox[7];
        for (int i = 0; i < 7; i++)
        {
            _chkWeekDays[i] = new CheckBox
            {
                Text = dayNames[i],
                Left = controlLeft + i * 50,
                Top = y,
                Width = 50,
                Checked = (Timer.WeekDays & (1 << i)) != 0
            };
            Controls.Add(_chkWeekDays[i]);
        }
        y += 30;

        // 消息
        Controls.Add(new Label { Text = "消息:", Left = 15, Top = y + 3, Width = labelWidth });
        _txtMessage = new TextBox { Left = controlLeft, Top = y, Width = 300, Text = Timer.Message };
        Controls.Add(_txtMessage);
        y += 35;

        // 声音
        _chkSound = new CheckBox
        {
            Text = "播放声音",
            Left = controlLeft,
            Top = y,
            Checked = Timer.SoundEnabled
        };
        Controls.Add(_chkSound);
        y += 30;

        // 弹窗
        _chkPopup = new CheckBox
        {
            Text = "显示弹窗",
            Left = controlLeft,
            Top = y,
            Checked = Timer.ShowPopup
        };
        Controls.Add(_chkPopup);
        y += 40;

        // 按钮
        var btnOk = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.OK,
            Left = Width - 200,
            Top = y,
            Width = 80,
            Height = 29
        };
        btnOk.Click += BtnOk_Click;
        Controls.Add(btnOk);

        var btnCancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Left = Width - 110,
            Top = y,
            Width = 80,
            Height = 29
        };
        Controls.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        // 初始化控件可见性
        UpdateControlVisibility();
    }

    private void CmbType_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateControlVisibility();
    }

    private void UpdateControlVisibility()
    {
        var type = (TimerType)_cmbType.SelectedIndex;

        _dtpTarget.Visible = type == TimerType.OneTime;
        _nudInterval.Visible = type == TimerType.Interval;
        _dtpDaily.Visible = type is TimerType.Daily or TimerType.Weekly;

        foreach (var chk in _chkWeekDays)
            chk.Visible = type == TimerType.Weekly;
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("请输入定时名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        Timer.Name = _txtName.Text.Trim();
        Timer.Type = (TimerType)_cmbType.SelectedIndex;
        Timer.TargetTime = _dtpTarget.Value;
        Timer.IntervalMinutes = (int)_nudInterval.Value;
        Timer.DailyTime = _dtpDaily.Value.TimeOfDay;
        Timer.Message = _txtMessage.Text;
        Timer.SoundEnabled = _chkSound.Checked;
        Timer.ShowPopup = _chkPopup.Checked;

        // 计算周几位掩码
        int weekDays = 0;
        for (int i = 0; i < 7; i++)
        {
            if (_chkWeekDays[i].Checked)
                weekDays |= (1 << i);
        }
        Timer.WeekDays = weekDays;
    }
}
