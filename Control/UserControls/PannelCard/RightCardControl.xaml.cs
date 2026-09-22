using GeekDesk.Constant;
using GeekDesk.Control.Other;
using GeekDesk.Control.Windows;
using GeekDesk.CustomComponent.GridPositionPanel;
using GeekDesk.Util;
using GeekDesk.ViewModel;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace GeekDesk.Control.UserControls.PannelCard
{
    /// <summary>
    /// RightCardControl.xaml 的交互逻辑
    /// </summary>
    public partial class RightCardControl : UserControl
    {
        private AppData appData = MainWindow.appData;

        private Point dragStartPoint;
        private IconInfo dragIcon;
        private bool isDragging;

        public RightCardControl()
        {
            InitializeComponent();
            this.Loaded += RightCardControl_Loaded;
        }

        private void RightCardControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateCheckBoxVisibility();
        }


        /// <summary>
        /// 更新所有复选框的可见性
        /// </summary>
        private void UpdateCheckBoxVisibility()
        {
            bool isEditMode = appData.AppConfig.IconBatch_NoWrite;
        
            // 遍历ListBox中的所有项
            foreach (var item in IconListBox.Items)
            {
                ListBoxItem listBoxItem = IconListBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                if (listBoxItem != null)
                {
                    // 在ListBoxItem的内容模板中查找CheckBox
                    CheckBox checkBox = FindVisualChild<CheckBox>(listBoxItem);
                    if (checkBox != null)
                    {
                        checkBox.Visibility = isEditMode ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }

        /// <summary>
        /// 在可视化树中查找指定类型的子元素
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
        
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
            
                T descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }
        private void Icon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (appData.AppConfig.IconBatch_NoWrite)
            {
                dragStartPoint = e.GetPosition(null);
                dragIcon = (sender as Panel)?.Tag as IconInfo;
                isDragging = false;
                return;
            }
            if (appData.AppConfig.DoubleOpen)
            {
                IconClick(sender, e);
            }
        }

        private void Icon_MouseMove(object sender, MouseEventArgs e)
        {
            if (!appData.AppConfig.IconBatch_NoWrite || isDragging || dragIcon == null)
                return;

            Point currentPos = e.GetPosition(null);
            double diffX = Math.Abs(currentPos.X - dragStartPoint.X);
            double diffY = Math.Abs(currentPos.Y - dragStartPoint.Y);

            if (diffX > SystemParameters.MinimumHorizontalDragDistance ||
                diffY > SystemParameters.MinimumVerticalDragDistance)
            {
                isDragging = true;
                DragDrop.DoDragDrop(sender as DependencyObject, dragIcon, DragDropEffects.Move);
                isDragging = false;
                dragIcon = null;
            }
        }

        private void Icon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (appData.AppConfig.IconBatch_NoWrite) return;
            
            if (!appData.AppConfig.DoubleOpen)
            {
                IconClick(sender, e);
            }
        }

        /// <summary>
        /// 图标点击事件
        /// </summary>
        private void IconClick(object sender, MouseButtonEventArgs e)
        {
            if (appData.AppConfig.DoubleOpen && e.ClickCount >= 2)
            {
                IconInfo icon = (IconInfo)((Panel)sender).Tag;
                if (icon.AdminStartUp)
                {
                    ProcessUtil.StartIconApp(icon, IconStartType.ADMIN_STARTUP);
                }
                else
                {
                    ProcessUtil.StartIconApp(icon, IconStartType.DEFAULT_STARTUP);
                }
            }
            else if (!appData.AppConfig.DoubleOpen && e.ClickCount == 1)
            {
                IconInfo icon = (IconInfo)((Panel)sender).Tag;
                if (icon.AdminStartUp)
                {
                    ProcessUtil.StartIconApp(icon, IconStartType.ADMIN_STARTUP);
                }
                else
                {
                    ProcessUtil.StartIconApp(icon, IconStartType.DEFAULT_STARTUP);
                }
            }
        }

        /// <summary>
        /// 图标右键事件 - 直接调用系统菜单
        /// </summary>
        private void Icon_RightMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (appData.AppConfig.IconBatch_NoWrite)
            {
                // 编辑模式下不处理右键，由ContextMenu处理
                return;
            }
            
            // 非编辑模式下直接调用系统菜单
            Panel panel = sender as Panel;
            if (panel != null)
            {
                IconInfo icon = panel.Tag as IconInfo;
                if (icon != null)
                {
                    ShowSystemContextMenu(icon);
                    e.Handled = true; // 阻止默认ContextMenu
                }
            }
        }

        /// <summary>
        /// 显示系统上下文菜单
        /// </summary>
        private void ShowSystemContextMenu(IconInfo icon)
        {
            DirectoryInfo[] folders = new DirectoryInfo[1];
            folders[0] = new DirectoryInfo(icon.Path);
            ShellContextMenu scm = new ShellContextMenu();
            System.Drawing.Point p = System.Windows.Forms.Cursor.Position;
            p.X -= 0;
            p.Y -= 0;
            scm.ShowContextMenu(folders, p);
        }

        /// <summary>
        /// 管理员方式启动
        /// </summary>
        private void IconAdminStart(object sender, RoutedEventArgs e)
        {
            IconInfo icon = (IconInfo)((MenuItem)sender).Tag;
            ProcessUtil.StartIconApp(icon, IconStartType.ADMIN_STARTUP);
        }

        /// <summary>
        /// 打开文件所在位置
        /// </summary>
        private void ShowInExplore(object sender, RoutedEventArgs e)
        {
            IconInfo icon = (IconInfo)((MenuItem)sender).Tag;
            ProcessUtil.StartIconApp(icon, IconStartType.SHOW_IN_EXPLORE);
        }

        /// <summary>
        /// 拖动添加项目
        /// </summary>
        private void Wrap_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(IconInfo)))
            {
                IconInfo draggedIcon = e.Data.GetData(typeof(IconInfo)) as IconInfo;
                if (draggedIcon == null) return;

                Point mousePos = e.GetPosition(IconListBox);
                double cellW = appData.AppConfig.ImgPanelWidth;
                double cellH = appData.AppConfig.ImgPanelHeight;
                if (cellW <= 0) cellW = 100;
                if (cellH <= 0) cellH = 100;

                int targetX = (int)(mousePos.X / cellW);
                int targetY = (int)(mousePos.Y / cellH);
                if (targetX < 0) targetX = 0;
                if (targetY < 0) targetY = 0;

                var iconList = MainWindow.appData.MenuList[appData.AppConfig.SelectedMenuIndex].IconList;

                IconInfo occupant = iconList.FirstOrDefault(
                    i => i != draggedIcon && i.GridX_NoWrite == targetX && i.GridY_NoWrite == targetY);

                draggedIcon.GridX_NoWrite = targetX;
                draggedIcon.GridY_NoWrite = targetY;

                if (occupant != null)
                {
                    int itemsPerRow = (int)(appData.AppConfig.WindowWidth / cellW);
                    if (itemsPerRow < 1) itemsPerRow = 6;
                    int x = targetX, y = targetY;
                    while (true)
                    {
                        x++;
                        if (x >= itemsPerRow) { x = 0; y++; }
                        bool occupied = iconList.Any(
                            i => i != occupant && i != draggedIcon
                                 && i.GridX_NoWrite == x && i.GridY_NoWrite == y);
                        if (!occupied)
                        {
                            occupant.GridX_NoWrite = x;
                            occupant.GridY_NoWrite = y;
                            break;
                        }
                    }
                }

                CommonCode.SaveAppData(MainWindow.appData, Constants.DATA_FILE_PATH);
                RefreshGridPanel();
                UpdateCenterCrosshair();
                AdjustWindowWidthToGrid();
                e.Effects = DragDropEffects.Move;
                return;
            }

            Array dropObject = (System.Array)e.Data.GetData(DataFormats.FileDrop);
            if (dropObject == null) return;
            foreach (object obj in dropObject)
            {
                string path = (string)obj;
                IconInfo iconInfo = CommonCode.GetIconInfoByPath(path);
                var iconList = MainWindow.appData.MenuList[appData.AppConfig.SelectedMenuIndex].IconList;
                CommonCode.AssignEmptyGridCell(iconInfo, iconList);
                iconList.Add(iconInfo);
            }
            CommonCode.SortIconList();
            CommonCode.SaveAppData(MainWindow.appData, Constants.DATA_FILE_PATH);
            UpdateCenterCrosshair();
            AdjustWindowWidthToGrid();
        }

        private void RefreshGridPanel()
        {
            var panel = FindVisualChild<GridPositionPanel>(IconListBox);
            if (panel != null)
            {
                panel.InvalidateMeasure();
                panel.InvalidateArrange();
            }
        }

        /// <summary>
        /// 从列表删除图标
        /// </summary>
        private void RemoveIcon(object sender, RoutedEventArgs e)
        {
            appData.MenuList[appData.AppConfig.SelectedMenuIndex].IconList.Remove((IconInfo)((MenuItem)sender).Tag);
            CheckAndExitEditMode();
            UpdateCenterCrosshair();
            AdjustWindowWidthToGrid();
        }

        /// <summary>
        /// 删除选中的图标
        /// </summary>
        public void RemoveSelectedIcons(object sender, RoutedEventArgs e)
        {
            var selectedIcons = appData.MenuList[appData.AppConfig.SelectedMenuIndex].IconList
                .Where(icon => icon.IsChecked_NoWrite)
                .ToList();
        
            foreach (var icon in selectedIcons)
            {
                appData.MenuList[appData.AppConfig.SelectedMenuIndex].IconList.Remove(icon);
            }

            CheckAndExitEditMode();
            UpdateCenterCrosshair();
            AdjustWindowWidthToGrid();
        }

        /// <summary>
        /// 检查并退出编辑模式
        /// </summary>
        private void CheckAndExitEditMode()
        {

            UpdateCheckBoxVisibility();
        }
        /// <summary>
        /// 弹出Icon属性修改面板
        /// </summary>
        private void PropertyConfig(object sender, RoutedEventArgs e)
        {
            IconInfo info = (IconInfo)((MenuItem)sender).Tag;
            MainWindow.PreventHide = true;
            switch (info.IconType)
            {
                case IconType.URL:
                    IconInfoUrlDialog urlDialog = new IconInfoUrlDialog(info);
                    urlDialog.dialog = HandyControl.Controls.Dialog.Show(urlDialog, "MainWindowDialog");
                    break;
                default:
                    IconInfoDialog dialog = new IconInfoDialog(info);
                    dialog.dialog = HandyControl.Controls.Dialog.Show(dialog, "MainWindowDialog");
                    break;
            }
            Dispatcher.BeginInvoke(new Action(() => MainWindow.PreventHide = false), System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// 修改选中的图标
        /// </summary>
        private void EditSelectedIcon(object sender, RoutedEventArgs e)
        {
            IconInfo info = (IconInfo)((MenuItem)sender).Tag;
            PropertyConfig(sender, e);
            UpdateCheckBoxVisibility();
        }

        private void MenuIcon_MouseEnter(object sender, MouseEventArgs e)
        {
            RunTimeStatus.MOUSE_ENTER_ICON = true;
            if (!RunTimeStatus.ICONLIST_MOUSE_WHEEL)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        IconInfo info = (sender as Panel).Tag as IconInfo;
                        MyPoptipContent.Text = info.Content;
                        MyPoptip.VerticalOffset = 30;
                        Thread.Sleep(50);
                        if (!RunTimeStatus.ICONLIST_MOUSE_WHEEL)
                        {
                            MyPoptip.IsOpen = true;
                        }
                    }));
                });
            }

            double width = appData.AppConfig.ImageWidth;
            double height = appData.AppConfig.ImageHeight;
            width += width * 0.15;
            height += height * 0.15;

            ThreadPool.QueueUserWorkItem(state =>
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ImgStoryBoard(sender, (int)width, (int)height, 1, true);
                }));
            });
        }

        private void MenuIcon_MouseLeave(object sender, MouseEventArgs e)
        {
            RunTimeStatus.MOUSE_ENTER_ICON = false;
            MyPoptip.IsOpen = false;

            ThreadPool.QueueUserWorkItem(state =>
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ImgStoryBoard(sender, appData.AppConfig.ImageWidth, appData.AppConfig.ImageHeight, 260);
                }));
            });
        }

        private void ImgStoryBoard(object sender, int height, int width, int milliseconds, bool checkRmStoryboard = false)
        {
            if (appData.AppConfig.PMModel) return;

            Panel sp = sender as Panel;
            Image img = null;

            foreach (var imgBak in sp.Children.OfType<Image>())
            {
                img = (Image)imgBak;
            }
            if (img == null) return;
            double afterHeight = img.Height;
            double afterWidth = img.Width;

            Storyboard myStoryboard = new Storyboard();

            DoubleAnimation heightAnimation = new DoubleAnimation
            {
                From = afterHeight,
                To = height,
                Duration = new Duration(TimeSpan.FromMilliseconds(milliseconds))
            };
            DoubleAnimation widthAnimation = new DoubleAnimation
            {
                From = afterWidth,
                To = width,
                Duration = new Duration(TimeSpan.FromMilliseconds(milliseconds))
            };

            Timeline.SetDesiredFrameRate(heightAnimation, 60);
            Timeline.SetDesiredFrameRate(widthAnimation, 60);

            Storyboard.SetTarget(widthAnimation, img);
            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath("Width"));
            Storyboard.SetTarget(heightAnimation, img);
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath("Height"));

            myStoryboard.Children.Add(heightAnimation);
            myStoryboard.Children.Add(widthAnimation);

            CheckRemoveStoryboard crs = new CheckRemoveStoryboard
            {
                sb = myStoryboard,
                sp = sp,
                heightAnimation = heightAnimation,
                widthAnimation = widthAnimation,
                img = img,
                isMouseOver = !checkRmStoryboard
            };

            heightAnimation.Completed += (s, ev) =>
            {
                if (checkRmStoryboard)
                {
                    ThreadStart ts = new ThreadStart(crs.Remove);
                    System.Threading.Thread t = new System.Threading.Thread(ts);
                    t.IsBackground = true;
                    t.Start();
                }
                else
                {
                    img.BeginAnimation(WidthProperty, null);
                    img.BeginAnimation(HeightProperty, null);
                }
            };
            img.BeginAnimation(WidthProperty, widthAnimation);
            img.BeginAnimation(HeightProperty, heightAnimation);
        }

        private class CheckRemoveStoryboard
        {
            public Storyboard sb;
            public Panel sp;
            public Image img;
            public DoubleAnimation heightAnimation;
            public DoubleAnimation widthAnimation;
            public bool isMouseOver;
            public void Remove()
            {
                while (true)
                {
                    if (sp.IsMouseOver == isMouseOver)
                    {
                        App.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            img.BeginAnimation(WidthProperty, null);
                            img.BeginAnimation(HeightProperty, null);
                        }));
                        return;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
        }

        /// <summary>
        /// 添加URL项目
        /// </summary>
        public void AddUrlIcon(object sender, RoutedEventArgs e)
        {
            IconInfoUrlDialog urlDialog = new IconInfoUrlDialog();
            MainWindow.PreventHide = true;
            urlDialog.dialog = HandyControl.Controls.Dialog.Show(urlDialog, "MainWindowDialog");
            Dispatcher.BeginInvoke(new Action(() => MainWindow.PreventHide = false), System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// 添加系统项目
        /// </summary>
        public void AddSystemIcon(object sender, RoutedEventArgs e)
        {
            MainWindow.PreventHide = true;
            SystemItemWindow.Show();
            Dispatcher.BeginInvoke(new Action(() => MainWindow.PreventHide = false), System.Windows.Threading.DispatcherPriority.Background);
        }

        public void VisibilitySearchCard(Visibility vb)
        {
            VerticalCard.Visibility = vb;
            if (vb == Visibility.Visible)
            {
                WrapCard.Visibility = Visibility.Collapsed;
            }
            else
            {
                WrapCard.Visibility = Visibility.Visible;
            }
        }
        

        /// <summary>
        /// 设置光标
        /// </summary>
        private void CursorPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// 设置光标
        /// </summary>
        private void CursorPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// 锁定/解锁主面板
        /// </summary>
        public void LockAppPanel(object sender, RoutedEventArgs e)
        {
            RunTimeStatus.LOCK_APP_PANEL = !RunTimeStatus.LOCK_APP_PANEL;
        }

        private void WrapCard_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        /// <summary>
        /// 菜单结果icon 列表鼠标滚轮预处理时间
        /// </summary>
        private void IconListBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            MyPoptip.IsOpen = false;
            if (RunTimeStatus.ICONLIST_MOUSE_WHEEL)
            {
                RunTimeStatus.MOUSE_WHEEL_WAIT_MS = 500;
            }
            else
            {
                RunTimeStatus.ICONLIST_MOUSE_WHEEL = true;

                new Thread(() =>
                {
                    while (RunTimeStatus.MOUSE_WHEEL_WAIT_MS > 0)
                    {
                        Thread.Sleep(1);
                        RunTimeStatus.MOUSE_WHEEL_WAIT_MS -= 1;
                    }
                    if (RunTimeStatus.MOUSE_ENTER_ICON)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MyPoptip.IsOpen = true;
                        }));
                    }
                    RunTimeStatus.MOUSE_WHEEL_WAIT_MS = 100;
                    RunTimeStatus.ICONLIST_MOUSE_WHEEL = false;
                }).Start();
            }

            if (RunTimeStatus.IS_MENU_EDIT) return;

            System.Windows.Controls.ScrollViewer scrollViewer = sender as System.Windows.Controls.ScrollViewer;
            if (scrollViewer == null)
            {
                scrollViewer = ScrollUtil.FindSimpleVisualChild<System.Windows.Controls.ScrollViewer>(IconListBox);
            }
            if (e.Delta < 0)
            {
                int index = MainWindow.mainWindow.LeftCard.MenuListBox.SelectedIndex;
                if (ScrollUtil.IsBootomScrollView(scrollViewer))
                {
                    if (index < MainWindow.mainWindow.LeftCard.MenuListBox.Items.Count - 1)
                    {
                        index++;
                    }
                    else
                    {
                        index = 0;
                    }
                    MainWindow.mainWindow.LeftCard.MenuListBox.SelectedIndex = index;
                    scrollViewer.ScrollToVerticalOffset(0);
                }
            }
            else if (e.Delta > 0)
            {
                if (ScrollUtil.IsTopScrollView(scrollViewer))
                {
                    int index = MainWindow.mainWindow.LeftCard.MenuListBox.SelectedIndex;
                    if (index > 0)
                    {
                        index--;
                    }
                    else
                    {
                        index = MainWindow.mainWindow.LeftCard.MenuListBox.Items.Count - 1;
                    }
                    MainWindow.mainWindow.LeftCard.MenuListBox.SelectedIndex = index;
                    scrollViewer.ScrollToVerticalOffset(0);
                }
            }
        }

        /// <summary>
        /// menu结果ICON鼠标移动事件
        /// </summary>
        private void MenuIcon_MouseMove(object sender, MouseEventArgs e)
        {
            IconInfo info = (sender as Panel).Tag as IconInfo;
            MyPoptipContent.Text = info.Content;
            MyPoptip.VerticalOffset = 30;
        }

        /// <summary>
        /// 控制图标标题显示及隐藏
        /// </summary>
        public void ShowTitle_Click(object sender, RoutedEventArgs e)
        {
            appData.AppConfig.ShowIconTitle = !appData.AppConfig.ShowIconTitle;
        }
        /// <summary>
        /// 上下文菜单打开时更新菜单项
        /// </summary>
        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            ContextMenu contextMenu = sender as ContextMenu;
            if (contextMenu != null)
            {
                // 查找删除所选图标菜单项
                MenuItem deleteSelectedItem = null;
                MenuItem editModeItem = null;
        
                foreach (var item in contextMenu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        if (menuItem.Name == "DeleteSelectedItem")
                        {
                            deleteSelectedItem = menuItem;
                        }
                        else if (menuItem.Name == "EditModeItem")
                        {
                            editModeItem = menuItem;
                        }
                    }
                }

                // 更新编辑模式菜单项文本
                if (editModeItem != null)
                {
                    editModeItem.Header = appData.AppConfig.IconBatch_NoWrite ? "退出编辑模式" : "编辑模式";
                }

                // 更新删除所选图标菜单项
                if (deleteSelectedItem != null)
                {
                    int selectedCount = GetSelectedIconCount();
                    if (appData.AppConfig.IconBatch_NoWrite && selectedCount > 0)
                    {
                        deleteSelectedItem.Visibility = Visibility.Visible;
                        deleteSelectedItem.Header = $"删除所选图标 ({selectedCount})";
                    }
                    else
                    {
                        deleteSelectedItem.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
        /// <summary>
        /// 编辑模式切换
        /// </summary>

        public void EditModeHandle(object sender, RoutedEventArgs e)
        {
            if (!appData.AppConfig.IconBatch_NoWrite)
            {
                foreach (var ic in IconListBox.Items)
                {
                    IconInfo info = ic as IconInfo;
                    info.IsChecked_NoWrite = false;
                }
                appData.AppConfig.CardOpacity = 100;

                CenterWindowOnGrid();
            }
            else
            {
                appData.AppConfig.CardOpacity = 0;
            }
            appData.AppConfig.IconBatch_NoWrite = !appData.AppConfig.IconBatch_NoWrite;
            IconListBox.SelectionMode = SelectionMode.Multiple;
            UpdateCheckBoxVisibility();
            UpdateCenterCrosshair();
            AdjustWindowWidthToGrid();
        }

        private void GetGridRange(out double gridW, out double gridH)
        {
            double cellW = appData.AppConfig.ImgPanelWidth;
            double cellH = appData.AppConfig.ImgPanelHeight;
            int maxCol = 0;
            int maxRow = 0;
            foreach (var item in IconListBox.Items)
            {
                if (item is IconInfo icon && icon.GridX >= 0 && icon.GridY >= 0)
                {
                    maxCol = Math.Max(maxCol, icon.GridX + 1);
                    maxRow = Math.Max(maxRow, icon.GridY + 1);
                }
            }
            gridW = maxCol * cellW;
            gridH = maxRow * cellH;
        }

        public void AdjustWindowWidthToGrid()
        {
            double cellW = appData.AppConfig.ImgPanelWidth;
            if (cellW < 1) return;

            const int gridCols = 13;
            double gridW = gridCols * cellW;

            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            var listBox = IconListBox;
            if (listBox == null || listBox.ActualWidth < 1) return;

            double hOverhead = mainWindow.Width - listBox.ActualWidth;
            double idealW = hOverhead + gridW;
            if (Math.Abs(mainWindow.Width - idealW) > 0.5)
            {
                mainWindow.Width = idealW;
            }
        }

        private void CenterWindowOnGrid()
        {
            GetGridRange(out double gridW, out double gridH);

            const double hOffset = 6;
            const double vOffset = 81;

            var workArea = SystemParameters.WorkArea;
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                double screenCenterX = workArea.Left + workArea.Width / 2;
                double screenCenterY = workArea.Top + workArea.Height / 2;
                mainWindow.Left = screenCenterX - (hOffset + gridW / 2);
                mainWindow.Top = screenCenterY - (vOffset + gridH / 2);
            }
        }

        private void CenterEllipse_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.DragMove();
            }
        }

        private void UpdateCenterCrosshair()
        {
            if (!appData.AppConfig.IconBatch_NoWrite)
            {
                CenterCrosshair.Visibility = Visibility.Collapsed;
                return;
            }

            GetGridRange(out double gridW, out double gridH);

            CenterCrosshair.Width = gridW;
            CenterCrosshair.Height = gridH;
            CenterCrosshair.Margin = new Thickness(10, 20, 0, 0);
            CenterCrosshair.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 获取选中的图标数量
        /// </summary>
        private int GetSelectedIconCount()
        {
            return appData.MenuList[appData.AppConfig.SelectedMenuIndex].IconList
                .Count(icon => icon.IsChecked_NoWrite);
        }

        private void IconListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 选择变化时更新UI
        }
    }
}