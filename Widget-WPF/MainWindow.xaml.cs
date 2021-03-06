﻿using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using Widget_WPF.Win32;
using MessageBox = System.Windows.MessageBox;

namespace Widget_WPF
{

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public Thread t;

        public NotifyIcon notify;
        readonly BrushConverter bc = new BrushConverter();
        readonly Setting s;
        static bool IsWindows10;

        public MainWindow()
        {
            InitializeComponent();
            ReadConfig();
            RefreshBackColor(Data.backColor);
            RefreshFontColor(Data.fontColor);
            this.Left = double.Parse(Data.jo["left"].ToString());
            this.Top = double.Parse(Data.jo["top"].ToString());
            InitializeNotifyIcon();
            InitializeContextMenuStrip();
            t = new Thread(new ThreadStart(SetTime));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            MouseMove += MainWindow_MouseMove;
            Topmost = false;
            s = new Setting(this);
        }

        private bool IsHtmlColorCode(string code)
        {
            if (code.StartsWith("#"))
            {
                code = code.Substring(1);
                if (code.Length == 6 || code.Length == 8)
                {
                    char[] _ = code.ToCharArray();
                    string _s = "";
                    for (int i = 0; i < code.Length; i++)
                    {
                        _s += _[i];

                        if ((i + 1) % 2 == 0)
                        {
                            try
                            {
                                Convert.ToInt32(_s, 16);
                            }
                            catch
                            {
                                return false;
                            }
                            _s = "";
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void ReadConfig()
        {
            if (File.Exists(Data.DEFAULT_CONFIG_PATH))
            {
                try
                {
                    Data.jo = JObject.Parse(File.ReadAllText(Data.DEFAULT_CONFIG_PATH));
                }
                catch
                {
                    CreateConfig(Data.DEFAULT_CONFIG_PATH);
                    ReadConfig();
                }
                bool needSave = false;
                if (Data.jo["backcolor"] == null || !IsHtmlColorCode(Data.jo["backcolor"].ToString()))
                {
                    needSave = true;
                    Data.jo["backcolor"] = Data.DEFAULT_BACK_COLOR;
                }
                if (Data.jo["fontcolor"] == null || !IsHtmlColorCode(Data.jo["fontcolor"].ToString()))
                {
                    needSave = true;
                    Data.jo["fontcolor"] = Data.DEFAULT_FONT_COLOR;
                }
                if (Data.jo["top"] == null)
                {
                    needSave = true;
                    Data.jo["top"] = Data.DEFAULT_FORM_TOP;
                }
                if (Data.jo["left"] == null)
                {
                    needSave = true;
                    Data.jo["left"] = Data.DEFAULT_FORM_LEFT;
                }
                try
                {
                    double.Parse(Data.jo["left"].ToString());
                }
                catch
                {
                    needSave = true;
                    Data.jo["left"] = Data.DEFAULT_FORM_LEFT;
                }
                try
                {
                    double.Parse(Data.jo["top"].ToString());
                }
                catch
                {
                    needSave = true;
                    Data.jo["top"] = Data.DEFAULT_FORM_TOP;
                }

                if (needSave)
                {
                    File.WriteAllText(Data.DEFAULT_CONFIG_PATH, Data.jo.ToString());
                    ReadConfig();
                }
                Data.backColor = Data.jo["backcolor"].ToString();
                Data.fontColor = Data.jo["fontcolor"].ToString();
            }
            else
            {
                CreateConfig(Data.DEFAULT_CONFIG_PATH);
                ReadConfig();
            }
        }

        private void CreateConfig(string path)
        {
            try
            {
                File.WriteAllText(Data.DEFAULT_CONFIG_PATH, Data.DEFAULT_CONFIG_JSON);
            }
            catch (Exception ex)
            {
                MessageBox.Show("创建配置文件出现错误，错误信息：\r\n" + ex.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }

        private void MainWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !lockForm.Checked)
            {
                this.DragMove();

                Data.jo["top"] = this.Top;
                Data.jo["left"] = this.Left;
                File.WriteAllText(Data.DEFAULT_CONFIG_PATH, Data.jo.ToString());
            }
        }

        private void Setting_Click(object sender, EventArgs e) => ShowSetting();

        private void Exit_Click(object sender, EventArgs e)
        {
            notify.Visible = false;
            notify.Dispose();
            Environment.Exit(0);
        }

        public void RefreshBackColor(string htmlColor)
        {
            try
            {
                Background = (Brush)bc.ConvertFrom(htmlColor);
            }
            catch
            {
                //MessageBox.Show("设置颜色出现错误，您输入的颜色有问题？已回滚至上一个色彩。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RefreshFontColor(string htmlColor)
        {
            try
            {
                Week.Foreground = Time.Foreground = Date.Foreground = (Brush)bc.ConvertFrom(htmlColor);
            }
            catch
            {
                //MessageBox.Show("设置颜色出现错误，您输入的颜色有问题？已回滚至上一个色彩。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public void SetTime()
        {
            while (true)
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    //Hour.Content = Tool.GetDate(Tool.Mode.Hour).PadLeft(2, '0');
                    Date.Content = Tool.GetDate(Tool.Mode.Date);
                    //Minute.Content = Tool.GetDate(Tool.Mode.Minute).PadLeft(2, '0');
                    //Second.Content = Tool.GetDate(Tool.Mode.Second).PadLeft(2, '0');
                    Week.Content = Tool.GetDate(Tool.Mode.Week);
                    Time.Content = Tool.GetDate(Tool.Mode.Time);
                }));

                Thread.Sleep(100);
            }
        }

        #region 初始化
        private void MW_Loaded(object sender, RoutedEventArgs e)
        {
            IsWindows10 = (new ComputerInfo().OSFullName).StartsWith("Microsoft Windows 10");

            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)Api.GetWindowLong(wndHelper.Handle, (int)Api.GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)Api.ExtendedWindowStyles.WS_EX_TOOLWINDOW;

            Api.SetWindowLong(wndHelper.Handle, (int)Api.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

            if (!IsWindows10)
            {
                if (MessageBox.Show("Windows 7及以前系统可能会遭受错误的兼容性问题，是否禁用部分代码（会出现BUG）以解决问题？", "兼容性问题", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    Api.SetParent(new WindowInteropHelper(this).Handle, Api.GetDesktopPtr());
            }
            else
                Api.SetParent(new WindowInteropHelper(this).Handle, Api.GetDesktopPtr());
        }

        readonly ContextMenuStrip cms = new ContextMenuStrip();
        private void InitializeNotifyIcon()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Setting));
            notify = new System.Windows.Forms.NotifyIcon
            {
                Text = "Widget-WPF",
                Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon"))),
                Visible = false,
                ContextMenuStrip = cms
            };
            notify.Visible = true;
        }

        ToolStripMenuItem exit, lockForm, setting, resetLocation;
        ToolStripSeparator tss;
        ToolStripLabel name;
        private void InitializeContextMenuStrip()
        {
            cms.RenderMode = ToolStripRenderMode.Professional;
            cms.ShowCheckMargin = true;
            cms.ShowImageMargin = false;

            exit = new ToolStripMenuItem();
            exit.Click += Exit_Click;
            exit.Text = "退出";

            lockForm = new ToolStripMenuItem();
            lockForm.Click += LockForm_Click;
            lockForm.Text = "锁定";

            setting = new ToolStripMenuItem();
            setting.Click += Setting_Click;
            setting.Text = "设置";

            resetLocation = new ToolStripMenuItem();
            resetLocation.Click += ResetLocation_Click;
            resetLocation.Text = "重置窗体位置";


            tss = new ToolStripSeparator();

            name = new ToolStripLabel
            {
                Text = "Widget-WPF"
            };

            cms.Items.AddRange(new ToolStripItem[]
            {
                name,
                tss,
                resetLocation,
                lockForm,
                setting,
                exit

            });
        }

        private void ResetLocation_Click(object sender, EventArgs e)
        {
            Data.jo["left"] = this.Left = Data.DEFAULT_FORM_LEFT;
            Data.jo["top"] = this.Top = Data.DEFAULT_FORM_TOP;
            File.WriteAllText(Data.DEFAULT_CONFIG_PATH, Data.jo.ToString());
        }

        private void ShowSetting()
        {
            try
            {
                s.ShowDialog();
            }
            catch
            { }
        }

        private void LockForm_Click(object sender, EventArgs e)
        {
            if (lockForm.Checked == true)
            {
                lockForm.Checked = false;
            }
            else
            {
                lockForm.Checked = true;
            }
        }
        #endregion

    }

    class Tool
    {
        public enum Mode
        {
            Hour = 0,
            Minute = 1,
            Date = 2,
            Second = 3,
            Week = 4,
            Time = 5
        }

        public static string GetDate(Mode mode)
        {
            switch (mode)
            {
                case Mode.Minute:
                    return DateTime.Now.Minute.ToString();
                case Mode.Hour:
                    return DateTime.Now.Hour.ToString();
                case Mode.Date:
                    //return DateTime.Now.ToLongDateString().ToString();
                    //return DateTime.Now.ToString("yyyy-MM-dd");
                    return DateTime.Now.ToString("MMMM dd", CultureInfo.CreateSpecificCulture("en-US"));
                case Mode.Second:
                    return DateTime.Now.Second.ToString();
                case Mode.Week:
                    return DateTime.Now.DayOfWeek.ToString();
                case Mode.Time:
                    return DateTime.Now.ToString("hh:mm tt").Replace("上午", "AM").Replace("下午", "PM");
                default:
                    throw new ArgumentOutOfRangeException("Your mode is not valid!");
            }

        }

    }
}
