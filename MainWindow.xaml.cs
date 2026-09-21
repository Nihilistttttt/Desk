using GeekDesk.Constant;
using GeekDesk.Control.UserControls.Config;
using GeekDesk.Control.Windows;
using GeekDesk.Interface;
using GeekDesk.MyThread;
using GeekDesk.Task;
using GeekDesk.Util;
using GeekDesk.ViewModel;
using ShowSeconds;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static GeekDesk.Util.ShowWindowFollowMouse;

namespace GeekDesk
{
    
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    /// 
    public partial class MainWindow : Window, IWindowCommon
    {

        public static AppData appData;
        public static int hotKeyId = -1;
        public static int colorPickerHotKeyId = -1;
        public static MainWindow mainWindow;
        // 添加 Windows API 常量
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, 
            int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hwnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hwnd);

        private const uint GW_OWNER = 4;
       
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            try
            {
                // 禁用窗口最大化
                WindowUtil.DisableMaxWindow(this);
                
                // 关键：将窗口设置为工具窗口，防止在任务栏显示
                MakeWindowToolWindow();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Window_SourceInitialized error: {ex.Message}");
            }
        }

        /// <summary>
        /// 将窗口设置为工具窗口，确保不会在任务栏显示
        /// </summary>
        private void MakeWindowToolWindow()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                IntPtr handle = helper.Handle;
                
                // 获取当前窗口样式
                int extendedStyle = GetWindowLong(handle, GWL_EXSTYLE);
                
                // 添加工具窗口样式，移除应用窗口样式
                extendedStyle |= WS_EX_TOOLWINDOW;
                extendedStyle &= ~WS_EX_APPWINDOW;
                
                // 设置新样式
                SetWindowLong(handle, GWL_EXSTYLE, extendedStyle);
                
                // 强制更新窗口
                SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 
                    0x0020 | 0x0001 | 0x0002 | 0x0020); // SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_FRAMECHANGED
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MakeWindowToolWindow error: {ex.Message}");
            }
        }
        private static bool dataFileExist = true;
        public MainWindow()
        {
            
            //加载数据
            LoadData();
            InitializeComponent();

            this.ShowInTaskbar = false;
            //用于其他类访问
            mainWindow = this;

            ////实例化隐藏 Hide类，进行时间timer设置
            MarginHide.ReadyHide(this);
            if (appData.AppConfig.MarginHide)
            {
                MarginHide.StartHide();
            }
        }
        
        /// <summary>
        /// 加载缓存数据
        /// </summary>
        private void LoadData()
        {
            //判断数据文件是否存在 如果不存在那么是第一次打开程序
            dataFileExist = File.Exists(Constants.DATA_FILE_PATH);

            appData = CommonCode.GetAppDataByFile();
            CommonCode.MigrateIconPositions(appData);

            this.DataContext = appData;
            if (appData.MenuList.Count == 0)
            {
                appData.MenuList.Add(new MenuInfo() { MenuName = "NewMenu", MenuId = System.Guid.NewGuid().ToString(), MenuEdit = Visibility.Collapsed });
            }

            this.Width = appData.AppConfig.WindowWidth;
            this.Height = appData.AppConfig.WindowHeight;
        }

        /// <summary>
        /// 窗口加载完毕 执行方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BGSettingUtil.BGSetting();
            if (!appData.AppConfig.StartedShowPanel)
            {
                this.Visibility = Visibility.Collapsed;
            }
            else
            {
                ShowApp();
            }
            //给任务栏图标一个名字
            BarIcon.Text = Constants.MY_NAME;

            //注册热键
            if (true == appData.AppConfig.EnableAppHotKey)
            {
                RegisterHotKey(true);
            }
            if (true == appData.AppConfig.EnableColorPickerHotKey)
            {
                RegisterColorPickerHotKey(true);
            }

            //注册自启动
            if (!appData.AppConfig.SelfStartUped && !Constants.DEV)
            {
                RegisterUtil.SetSelfStarting(appData.AppConfig.SelfStartUp, Constants.MY_NAME);
            }

            //注册鼠标监听事件
            if (appData.AppConfig.MouseMiddleShow)
            {
                MouseHookThread.Hook();
            }

            //显秒插件
            if (appData.AppConfig.SecondsWindow == true)
            {
                SecondsWindow.ShowWindow();
            }

            //监听实时文件夹菜单
            FileWatcher.EnableLinkMenuWatcher(appData);
            
            //建立相对路径
            RelativePathThread.MakeRelativePath();


            //设置归属桌面  解决桌面覆盖程序界面的bug
            WindowUtil.SetOwner(this, WindowUtil.GetDesktopHandle(this, DesktopLayer.Progman));

            //启动文件备份任务
            BakTask.Start();

            MessageUtil.ChangeWindowMessageFilter(MessageUtil.WM_COPYDATA, 1);
            
            CenterWindowOnScreen();
        }

        private void CenterWindowOnScreen()
        {
                // 计算居中位置
                this.Left = -8;
                this.Top = 0;
        }


        /// <summary>
        /// 注册当前窗口的热键
        /// </summary>
        public static void RegisterHotKey(bool first)
        {
            try
            {
                if (appData.AppConfig.HotkeyModifiers != GlobalHotKey.HotkeyModifiers.None)
                {
                    hotKeyId = GlobalHotKey.RegisterHotKey(appData.AppConfig.HotkeyModifiers, appData.AppConfig.Hotkey, () =>
                    {
                        if (RunTimeStatus.MAIN_HOT_KEY_DOWN) return;
                        RunTimeStatus.MAIN_HOT_KEY_DOWN = true;
                        new Thread(() =>
                        {
                            Thread.Sleep(RunTimeStatus.MAIN_HOT_KEY_TIME);
                            RunTimeStatus.MAIN_HOT_KEY_DOWN = false;
                        }).Start();

                        if (MotionControl.hotkeyFinished)
                        {
                            if (CheckShouldShowApp())
                            {
                                ShowApp();
                            }
                            else
                            {
                                HideApp();
                            }
                        }
                    });
                    if (!first)
                    {
                        HandyControl.Controls.Growl.Success("GeekDesk快捷键注册成功(" + appData.AppConfig.HotkeyStr + ")!", "HotKeyGrowl");
                    }
                }
                else
                {
                }
            }
            catch (Exception)
            {
                if (first)
                {
                    HandyControl.Controls.Growl.WarningGlobal("GeekDesk启动快捷键已被其它程序占用(" + appData.AppConfig.HotkeyStr + ")!");
                }
                else
                {
                    HandyControl.Controls.Growl.Warning("GeekDesk启动快捷键已被其它程序占用(" + appData.AppConfig.HotkeyStr + ")!", "HotKeyGrowl");

                }
            }
        }
        

        /// <summary>
        /// 注册屏幕拾色器的热键
        /// </summary>
        public static void RegisterColorPickerHotKey(bool first)
        {
            try
            {
                if (appData.AppConfig.HotkeyModifiers != GlobalHotKey.HotkeyModifiers.None)
                {
                    //加载完毕注册热键
                    colorPickerHotKeyId = GlobalHotKey.RegisterHotKey(appData.AppConfig.ColorPickerHotkeyModifiers, appData.AppConfig.ColorPickerHotkey, () =>
                    {
                        if (MotionControl.hotkeyFinished)
                        {
                            GlobalColorPickerWindow.CreateNoShow();
                        }
                    });
                    if (!first)
                    {
                        HandyControl.Controls.Growl.Success("拾色器快捷键注册成功(" + appData.AppConfig.ColorPickerHotkeyStr + ")!", "HotKeyGrowl");
                    }
                }
            }
            catch (Exception)
            {
                if (first)
                {
                    HandyControl.Controls.Growl.WarningGlobal("拾色器快捷键已被其它程序占用(" + appData.AppConfig.ColorPickerHotkeyStr + ")!");
                }
                else
                {
                    HandyControl.Controls.Growl.Warning("拾色器快捷键已被其它程序占用(" + appData.AppConfig.ColorPickerHotkeyStr + ")!", "HotKeyGrowl");
                }
            }
        }




        /// <summary>
        /// 程序窗体拖动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DragMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }




        /// <summary>
        /// 关闭按钮单击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            HideApp();
        }



        ///// <summary>
        ///// 左侧栏宽度改变 持久化
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void LeftCardResize(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        //{
        //    appData.AppConfig.MenuCardWidth = LeftColumn.Width.Value;
        //}



        /// <summary>
        /// 右键任务栏图标 显示主面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ShowApp(object sender, RoutedEventArgs e)
        {
            ShowApp();
        }
        public static void ShowApp()
        {
            //有全屏化应用则不显示
            //if (CommonCode.IsPrimaryFullScreen())
            //{
            //    return;
            //}

            if (MarginHide.ON_HIDE)
            {
                //修改贴边隐藏状态为未隐藏
                MarginHide.IS_HIDE = false;
                if (!CommonCode.MouseInWindow(mainWindow))
                {
                    RunTimeStatus.MARGIN_HIDE_AND_OTHER_SHOW = true;
                    MarginHide.WaitHide(3000);
                }
            }

            mainWindow.CenterWindowOnScreen();
            if (appData.AppConfig.FollowMouse)
            {
                ShowWindowFollowMouse.Show(mainWindow, MousePosition.CENTER, 0, 0);
                //ShowWindowFollowMouse.FollowMouse(mainWindow);
            }


            MainWindow.mainWindow.Activate();
            mainWindow.Show();
            //mainWindow.Visibility = Visibility.Visible;
            if (appData.AppConfig.AppAnimation)
            {
                appData.AppConfig.IsShow = true;
            }
            else
            {
                appData.AppConfig.IsShow = null;
                //防止永远不显示界面
                if (mainWindow.Opacity < 1)
                {
                    mainWindow.Opacity = 1;
                }
            }


            //FadeStoryBoard(1, (int)CommonEnum.WINDOW_ANIMATION_TIME, Visibility.Visible);

            Keyboard.Focus(mainWindow);
        }

        public static void HideApp()
        {
            if (appData.AppConfig.AppAnimation)
            {
                appData.AppConfig.IsShow = false;
            }
            else
            {
                appData.AppConfig.IsShow = null;
                HideAppVis();
            }

        }

        private static void HideAppVis()
        {
            //关闭锁定
            RunTimeStatus.LOCK_APP_PANEL = false;
            mainWindow.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 淡入淡出效果
        /// </summary>
        /// <param name="opacity"></param>
        /// <param name="milliseconds"></param>
        /// <param name="visibility"></param>
        public static void FadeStoryBoard(int opacity, int milliseconds, Visibility visibility)
        {
            if (appData.AppConfig.AppAnimation)
            {
                DoubleAnimation opacityAnimation = new DoubleAnimation
                {
                    From = mainWindow.Opacity,
                    To = opacity,
                    Duration = new Duration(TimeSpan.FromMilliseconds(milliseconds))
                };
                opacityAnimation.Completed += (s, e) =>
                {
                    mainWindow.BeginAnimation(OpacityProperty, null);
                    if (visibility == Visibility.Visible)
                    {
                        mainWindow.Opacity = 1;
                    }
                    else
                    {
                        mainWindow.Opacity = 0;
                        CommonCode.SortIconList();
                    }
                };
                Timeline.SetDesiredFrameRate(opacityAnimation, 60);
                mainWindow.BeginAnimation(OpacityProperty, opacityAnimation);
            }
            else
            {
                //防止关闭动画后 窗体仍是0透明度
                mainWindow.Opacity = 1;
                mainWindow.Visibility = visibility;
                if (visibility == Visibility.Collapsed)
                {
                    CommonCode.SortIconList();
                }
            }
        }
        
        /// <summary>
        /// 图片图标单击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyIcon_Click(object sender, RoutedEventArgs e)
        {
            if (CheckShouldShowApp())
            {
                ShowApp();
            }
            else
            {
                HideApp();
            }
        }

        private static bool CheckShouldShowApp()
        {
            return mainWindow.Visibility == Visibility.Collapsed
                || mainWindow.Opacity == 0
                || MarginHide.IS_HIDE
                || !WindowUtil.WindowIsTop(mainWindow);
        }

        /// <summary>
        /// 右键任务栏图标 设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigApp(object sender, RoutedEventArgs e)
        {
            ConfigWindow.Show(appData.AppConfig, this);
        }


        /// <summary>
        /// 右键任务栏图标打开程序目录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenThisDir(object sender, RoutedEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = "Explorer.exe";
            p.StartInfo.Arguments = "/e,/select," + Constants.APP_DIR + Constants.MY_NAME + ".exe";
            p.Start();
        }




        /// <summary>
        /// 设置图标
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigButtonClick(object sender, RoutedEventArgs e)
        {
            SettingMenus.IsOpen = true;
        }

        private void PanelSettingButtonClick(object sender, RoutedEventArgs e)
        {
            EditModeItem.Header = appData.AppConfig.IconBatch_NoWrite ? "退出编辑模式" : "编辑模式";
            PanelSettingMenus.IsOpen = true;
        }

        private void PanelAddUrlIcon(object sender, RoutedEventArgs e) { RightCard.AddUrlIcon(sender, e); }
        private void PanelAddSystemIcon(object sender, RoutedEventArgs e) { RightCard.AddSystemIcon(sender, e); }
        private void PanelLockAppPanel(object sender, RoutedEventArgs e) { RightCard.LockAppPanel(sender, e); }
        private void PanelShowTitle_Click(object sender, RoutedEventArgs e) { RightCard.ShowTitle_Click(sender, e); }
        private void PanelEditModeHandle(object sender, RoutedEventArgs e)
        {
            RightCard.EditModeHandle(sender, e);
            EditModeItem.Header = appData.AppConfig.IconBatch_NoWrite ? "退出编辑模式" : "编辑模式";
        }
        private void PanelRemoveSelectedIcons(object sender, RoutedEventArgs e) { RightCard.RemoveSelectedIcons(sender, e); }

        /// <summary>
        /// 设置菜单点击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigMenuClick(object sender, RoutedEventArgs e)
        {
            ConfigWindow.Show(appData.AppConfig, this);
        }
       
        private void SettingButton_Initialized(object sender, EventArgs e)
        {
            SettingButton.ContextMenu = null;
        }


        public static bool PreventHide = false;

        private void AppWindowLostFocus()
        {
            if (PreventHide) return;
            if (appData.AppConfig.IconBatch_NoWrite) return;
            if ((appData.AppConfig.AppHideType == AppHideType.LOST_FOCUS
                && this.Opacity == 1 )||(MainWindow.appData.AppConfig.AppHideType == AppHideType.START_EXE ))
            {
                //如果开启了贴边隐藏 则窗体不贴边才隐藏窗口
                if (!appData.AppConfig.MarginHide || (appData.AppConfig.MarginHide && !MarginHide.IS_HIDE))
                {
                    HideApp();
                }
            }
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.DataContext != null)
            {
                AppData appData = this.DataContext as AppData;
                appData.AppConfig.WindowWidth = this.Width;
                appData.AppConfig.WindowHeight = this.Height;
            }
        }



        /// <summary>
        /// 右键任务栏图标退出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitApp(object sender, RoutedEventArgs e)
        {
            if (appData.AppConfig.MouseMiddleShow || appData.AppConfig.SecondsWindow == true)
            {
                MouseHookThread.Dispose();
            }
            Application.Current.Shutdown();
        }
        /// <summary>
        /// 重启
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ReStartApp(object sender, RoutedEventArgs e)
        {
            ProcessUtil.ReStartApp();
        }

        /// <summary>
        /// 关闭托盘图标
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseBarIcon(object sender, RoutedEventArgs e)
        {
            appData.AppConfig.ShowBarIcon = false;
        }


        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            //char c = (char)e.Key;

            if (e.Key == Key.Escape)
            {
                HideApp();
            }
        }



      

        /// <summary>
        /// 鼠标进入后 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            //防止延迟贴边隐藏
            RunTimeStatus.MARGIN_HIDE_AND_OTHER_SHOW = false;
        }

        /// <summary>
        /// 打开屏幕拾色器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorPicker(object sender, RoutedEventArgs e)
        {
            TaskbarContextMenu.IsOpen = false;
            GlobalColorPickerWindow.CreateNoShow();
        }

        private void AppWindow_Deactivated(object sender, EventArgs e)
        {
            AppWindowLostFocus();
        }

        /// <summary>
        /// 备份数据文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [Obsolete]
        private void BakDataFile(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(() =>
            {
                CommonCode.BakAppData();
            });
            t.ApartmentState = ApartmentState.STA;
            t.Start();
        }

        private void ImportDataFile(object sender, RoutedEventArgs e)
        {
            CommonCode.ImportBakAppData();
        }

        private void ResetConfigFile(object sender, RoutedEventArgs e)
        {
            bool confirmed = HandyControl.Controls.MessageBox.Show(
                "确定要重置所有配置吗？\n\n此操作将清空全部自定义设置和图标列表, 恢复为默认状态, 且不可撤销！",
                "重置配置确认",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning) == MessageBoxResult.OK;
            if (confirmed)
            {
                CommonCode.ResetAppData();
            }
        }

        private void AppButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //点击了面板
            RunTimeStatus.APP_BTN_IS_DOWN = true;
            new Thread(() =>
            {
                Thread.Sleep(50);
                RunTimeStatus.APP_BTN_IS_DOWN = false;
            }).Start();
        }


        private ICommand _hideCommand;
        public ICommand HideCommand
        {
            get
            {
                if (_hideCommand == null)
                {
                    _hideCommand = new RelayCommand(
                        p =>
                        {
                            return true;
                        },
                        p =>
                        {
                            HideAppVis();
                        });
                }
                return _hideCommand;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource != null)
            {
                IntPtr handle = hwndSource.Handle;
                hwndSource.AddHook(new HwndSourceHook(WndProc));
            }
        }

        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == MessageUtil.WM_COPYDATA)
            {
                MessageUtil.CopyDataStruct cds = (MessageUtil.CopyDataStruct)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(MessageUtil.CopyDataStruct));
                if ("ShowApp".Equals(cds.msg))
                {
                    ShowApp();
                }
            }
            return hwnd;
        }
        
        private void GrayBoderClip(double x, double y, double w, double h, Thickness margin)
        {
            PathGeometry borGeometry = new PathGeometry();

            RectangleGeometry rg = new RectangleGeometry();
            rg.Rect = new Rect(0, 0, this.Width, this.Height);
            borGeometry = Geometry.Combine(borGeometry, rg, GeometryCombineMode.Union, null);

            RectangleGeometry rg1 = new RectangleGeometry();
            rg1.Rect = new Rect(x - 20, y - 20, w, h);
            borGeometry = Geometry.Combine(borGeometry, rg1, GeometryCombineMode.Exclude, null);
        }
    }
}
