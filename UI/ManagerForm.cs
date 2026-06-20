using System;
using System.Drawing;
using System.Windows.Forms;
using SmartTimer.Models;
using SmartTimer.Services;

namespace SmartTimer.UI;

/// <summary>
/// 定时管理窗口
/// </summary>
public class ManagerForm : Form
{
    private readonly TimerEngine _engine;
    private readonly ListView _listView;
    private readonly Button _btnAdd;
    private readonly Button _btnEdit;
    private readonly Button _btnDelete;
    private readonly Button _btnToggle;

    public ManagerForm(TimerEngine engine)
    {
        _engine = engine;

        Text = "SmartTimer - 定时管理";
        Size = new Size(700, 450);
        MinimumSize = new Size(600, 350);
        StartPosition = FormStartPosition.CenterScreen;

        // 创建工具栏
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45,
            Padding = new Padding(5)
        };

        _btnAdd = new Button { Text = "添加", Width = 70, Height = 29, Left = 5, Top = 8 };
        _btnEdit = new Button { Text = "编辑", Width = 70, Height = 29, Left = 80, Top = 8 };
        _btnDelete = new Button { Text = "删除", Width = 70, Height = 29, Left = 155, Top = 8 };
        _btnToggle = new Button { Text = "启用/禁用", Width = 80, Height = 29, Left = 230, Top = 8 };

        _btnAdd.Click += BtnAdd_Click;
        _btnEdit.Click += BtnEdit_Click;
        _btnDelete.Click += BtnDelete_Click;
        _btnToggle.Click += BtnToggle_Click;

        toolbar.Controls.AddRange(new Control[] { _btnAdd, _btnEdit, _btnDelete, _btnToggle });

        // 创建列表
        _listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false
        };

        _listView.Columns.Add("名称", 150);
        _listView.Columns.Add("类型", 100);
        _listView.Columns.Add("下次触发", 150);
        _listView.Columns.Add("状态", 60);
        _listView.Columns.Add("消息", 180);

        _listView.DoubleClick += BtnEdit_Click;

        Controls.Add(_listView);
        Controls.Add(toolbar);

        // 刷新列表
        RefreshTimerList();

        // 监听列表变化
        _engine.ListChanged += (_, _) => RefreshTimerList();
    }

    /// <summary>
    /// 刷新定时列表
    /// </summary>
    public void RefreshTimerList()
    {
        if (_listView.InvokeRequired)
        {
            _listView.Invoke(new Action(RefreshTimerList));
            return;
        }

        _listView.Items.Clear();

        foreach (var timer in _engine.Timers)
        {
            var item = new ListViewItem(timer.Name);
            item.SubItems.Add(timer.GetDescription());
            item.SubItems.Add(timer.Enabled ? timer.GetNextTriggerText() : "-");
            item.SubItems.Add(timer.Enabled ? "启用" : "禁用");
            item.SubItems.Add(timer.Message);
            item.Tag = timer.Id;

            if (!timer.Enabled)
                item.ForeColor = Color.Gray;

            _listView.Items.Add(item);
        }
    }

    /// <summary>
    /// 选中指定定时
    /// </summary>
    public void SelectTimer(string id)
    {
        foreach (ListViewItem item in _listView.Items)
        {
            if ((string?)item.Tag == id)
            {
                item.Selected = true;
                item.Focused = true;
                item.EnsureVisible();
                break;
            }
        }
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var form = new EditTimerForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _engine.Add(form.Timer);
        }
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0) return;

        var id = (string)_listView.SelectedItems[0].Tag!;
        var timer = _engine.Timers.Find(t => t.Id == id);
        if (timer == null) return;

        using var form = new EditTimerForm(timer);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _engine.Update(form.Timer);
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0) return;

        var result = MessageBox.Show(
            "确定要删除选中的定时吗？",
            "确认删除",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            var id = (string)_listView.SelectedItems[0].Tag!;
            _engine.Remove(id);
        }
    }

    private void BtnToggle_Click(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0) return;

        var id = (string)_listView.SelectedItems[0].Tag!;
        var timer = _engine.Timers.Find(t => t.Id == id);
        if (timer == null) return;

        _engine.Toggle(id, !timer.Enabled);
    }
}
