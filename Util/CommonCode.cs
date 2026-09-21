using GeekDesk.Constant;
using GeekDesk.Control.Other;
using GeekDesk.ViewModel;
using HandyControl.Data;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Media.Imaging;
using static GeekDesk.Control.Other.GlobalMsgNotification;

/// <summary>
/// 提取一些代码
/// </summary>
namespace GeekDesk.Util
{
    class CommonCode
    {

        /// <summary>
        /// 获取app 数据
        /// </summary>
        /// <returns></returns>
        public static AppData GetAppDataByFile()
        {
            AppData appData;
            if (!File.Exists(Constants.DATA_FILE_PATH))
            {
                using (FileStream fs = File.Create(Constants.DATA_FILE_PATH)) { }
                appData = new AppData();
                SaveAppData(appData, Constants.DATA_FILE_PATH);
                return appData;
            }
            else
            {
                try
                {
                    using (FileStream fs = new FileStream(Constants.DATA_FILE_PATH, FileMode.Open))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        appData = bf.Deserialize(fs) as AppData;


                        return appData;
                    }
                }
                catch
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(Constants.DATA_FILE_BAK_DIR_PATH);
                    FileInfo[] files = dirInfo.GetFiles()
                        .Where(f => f.Extension.Equals(".bak", StringComparison.OrdinalIgnoreCase)).ToArray(); ;
                    if (files.Length > 0)
                    {
                        FileInfo[] sortedFiles = files.OrderByDescending(file => file.CreationTime).ToArray();
                        //循环获取可用备份文件
                        string bakFilePath = "";
                        foreach (FileInfo bakFile in sortedFiles)
                        {
                            if (!Directory.Exists(Constants.DATA_FILE_TEMP_DIR_PATH)) { Directory.CreateDirectory(Constants.DATA_FILE_TEMP_DIR_PATH); }
                            bakFilePath = Constants.DATA_FILE_TEMP_DIR_PATH + "\\" + bakFile.Name;
                            try
                            {
                                File.Copy(bakFile.FullName, bakFilePath, true);
                                using (FileStream fs = new FileStream(bakFilePath, FileMode.Open))
                                {
                                    BinaryFormatter bf = new BinaryFormatter();
                                    appData = bf.Deserialize(fs) as AppData;
                                }
                                DialogMsg msg = new DialogMsg();
                                msg.msg = "不幸的是, GeekDesk当前的数据文件已经损坏, " +
                                    "现在已经启用系统自动备份的数据\n\n" +
                                    "如果你有较新的备份, " +
                                    "请退出GeekDesk, " +
                                    "将备份文件重命名为:Data, " +
                                    "然后将Data覆盖到GeekDesk的根目录即可\n\n" +
                                    "启用的备份文件为: \n" + bakFilePath +
                                    "\n\n如果当前数据就是你想要的数据, 那么请不用管它";
                                GlobalMsgNotification gm = new GlobalMsgNotification(msg);
                                HandyControl.Controls.Notification ntf = HandyControl.Controls.Notification.Show(gm, ShowAnimation.Fade, true);
                                gm.ntf = ntf;
                                File.Delete(bakFilePath);
                                SaveAppData(appData, Constants.DATA_FILE_PATH);
                                return appData;
                            }
                            catch { 
                                if (File.Exists(bakFilePath))
                                {
                                    File.Delete(bakFilePath);
                                }
                            }
                        }
                        MessageBox.Show("不幸的是, GeekDesk当前的数据文件已经损坏\n如果你有备份, 请将备份文件重命名为:Data 然后将Data覆盖到GeekDesk的根目录即可!");
                        Application.Current.Shutdown();
                        return new AppData();
                    } else
                    {
                        MessageBox.Show("不幸的是, GeekDesk当前的数据文件已经损坏\n如果你有备份, 请将备份文件重命名为:Data 然后将Data覆盖到GeekDesk的根目录即可!");
                        Application.Current.Shutdown();
                        return new AppData();
                    }

                    //    if (File.Exists(Constants.DATA_FILE_BAK_PATH))
                    //{
                    //    try
                    //    {
                    //        using (FileStream fs = new FileStream(Constants.DATA_FILE_BAK_PATH, FileMode.Open))
                    //        {
                    //            BinaryFormatter bf = new BinaryFormatter();
                    //            appData = bf.Deserialize(fs) as AppData;
                    //        }

                    //        DialogMsg msg = new DialogMsg();
                    //        msg.msg = "不幸的是, GeekDesk当前的数据文件已经损坏, " +
                    //            "现在已经启用系统自动备份的数据\n\n" +
                    //            "如果你有较新的备份, " +
                    //            "请退出GeekDesk, " +
                    //            "将备份文件重命名为:Data, " +
                    //            "然后将Data覆盖到GeekDesk的根目录即可\n\n" +
                    //            "系统上次备份时间: \n" + appData.AppConfig.SysBakTime +
                    //            "\n\n如果当前数据就是你想要的数据, 那么请不用管它";
                    //        GlobalMsgNotification gm = new GlobalMsgNotification(msg);
                    //        HandyControl.Controls.Notification ntf = HandyControl.Controls.Notification.Show(gm, ShowAnimation.Fade, true);
                    //        gm.ntf = ntf;
                    //    }
                    //    catch
                    //    {
                    //        MessageBox.Show("不幸的是, GeekDesk当前的数据文件已经损坏\n如果你有备份, 请将备份文件重命名为:Data 然后将Data覆盖到GeekDesk的根目录即可!");
                    //        Application.Current.Shutdown();
                    //        return null;
                    //    }

                    //}
                    //else
                    //{
                    //    MessageBox.Show("不幸的是, GeekDesk当前的数据文件已经损坏\n如果你有备份, 请将备份文件重命名为:Data 然后将Data覆盖到GeekDesk的根目录即可!");
                    //    Application.Current.Shutdown();
                    //    return null;
                    //}

                }
            }
        }


        /// <summary>
        /// 迁移图标到网格坐标: 删除空白占位图标, 按顺序分配 GridX/GridY
        /// </summary>
        public static void MigrateIconPositions(AppData appData)
        {
            foreach (var menu in appData.MenuList)
            {
                bool needsMigration = menu.IconList.Count > 1
                    && menu.IconList.All(icon => icon.GridX_NoWrite == 0 && icon.GridY_NoWrite == 0);
                if (!needsMigration) continue;

                var emptyIcons = menu.IconList.Where(icon => icon.IsEmptyIcon).ToList();
                foreach (var empty in emptyIcons)
                {
                    menu.IconList.Remove(empty);
                }

                int itemsPerRow = (int)(appData.AppConfig.WindowWidth / appData.AppConfig.ImgPanelWidth);
                if (itemsPerRow < 1) itemsPerRow = 6;

                for (int i = 0; i < menu.IconList.Count; i++)
                {
                    menu.IconList[i].GridX_NoWrite = i % itemsPerRow;
                    menu.IconList[i].GridY_NoWrite = i / itemsPerRow;
                }
            }
            SaveAppData(appData, Constants.DATA_FILE_PATH);
        }


        private readonly static object _MyLock = new object();
        /// <summary>
        /// 保存app 数据
        /// </summary>
        /// <param name="appData"></param>
        public static void SaveAppData(AppData appData, string filePath)
        {
            lock (_MyLock)
            {
                //if (filePath.Equals(Constants.DATA_FILE_BAK_PATH))
                //{
                //    appData.AppConfig.SysBakTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                //}
                if (!Directory.Exists(filePath.Substring(0, filePath.LastIndexOf("\\"))))
                {
                    Directory.CreateDirectory(filePath.Substring(0, filePath.LastIndexOf("\\")));
                }
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, appData);
                }
            }
        }



        private static string GeneraterUUID()
        {
            try
            {
                if (!File.Exists(Constants.UUID_FILE_BAK_PATH) || string.IsNullOrEmpty(GetUniqueUUID()))
                {
                    using (StreamWriter sw = new StreamWriter(Constants.UUID_FILE_BAK_PATH))
                    {
                        string uuid = Guid.NewGuid().ToString() + "-" + Constants.MY_UUID;
                        sw.Write(uuid);
                        return uuid;
                    }
                }
            } catch (Exception) { }
            return "ERROR_UUID_GeneraterUUID_" + Constants.MY_UUID;
        }

        public static string GetUniqueUUID()
        {
            try
            {
                if (File.Exists(Constants.UUID_FILE_BAK_PATH))
                {
                    using (StreamReader reader = new StreamReader(Constants.UUID_FILE_BAK_PATH))
                    {
                        return reader.ReadToEnd().Trim();
                    }
                } else
                {
                    return GeneraterUUID();
                }
            } catch(Exception) { }
            return "ERROR_UUID_GetUniqueUUID_" + Constants.MY_UUID;
        }


        public static void BakAppData()
        {

            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "备份文件",
                Filter = "bak文件(*.bak)|*.bak",
                FileName = "Data-GD-" + DateTime.Now.ToString("yyMMdd") + ".bak",
            };
            if (sfd.ShowDialog() == true)
            {
                using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, MainWindow.appData);
                }
            }
        }


        /// <summary>
        /// 导入备份文件: 选择 .bak -> 反序列化为 AppData -> 覆盖当前 Data -> 重启程序
        /// </summary>
        public static void ImportBakAppData()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "选择备份文件",
                Filter = "bak文件(*.bak)|*.bak",
            };
            if (ofd.ShowDialog() == true)
            {
                AppData newData = null;
                try
                {
                    using (FileStream fs = new FileStream(ofd.FileName, FileMode.Open))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        newData = bf.Deserialize(fs) as AppData;
                    }
                }
                catch (Exception) { }

                if (newData == null)
                {
                    HandyControl.Controls.Growl.WarningGlobal("备份文件损坏或格式不匹配, 无法导入!");
                    return;
                }

                SaveAppData(newData, Constants.DATA_FILE_PATH);
                HandyControl.Controls.Growl.SuccessGlobal("导入成功, 程序将重启以应用新数据!");
                ProcessUtil.ReStartApp();
            }
        }


        /// <summary>
        /// 重置配置: 用默认 AppData 覆盖当前 Data 文件 -> 重启程序
        /// </summary>
        public static void ResetAppData()
        {
            AppData defaultData = new AppData();
            SaveAppData(defaultData, Constants.DATA_FILE_PATH);
            HandyControl.Controls.Growl.SuccessGlobal("已重置为默认配置, 程序将重启!");
            ProcessUtil.ReStartApp();
        }



        /// <summary>
        /// 根据路径获取文件图标等信息
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IconInfo GetIconInfoByPath(string path)
        {
            string tempPath = path;
            string ext = System.IO.Path.GetExtension(path).ToLower();

            string iconPath = null;
            string targetPath = null;
    
            // 对于快捷方式，获取相关信息但不替换路径
            if (".lnk".Equals(ext))
            {
                targetPath = FileUtil.GetTargetPathByLnk(path);
                iconPath = FileUtil.GetIconPathByLnk(path);
                // 不再将 path = targetPath，保持原始快捷方式路径
            }
    
            if (StringUtil.IsEmpty(iconPath))
            {
                iconPath = path;
            }

            BitmapImage bi = ImageUtil.GetBitmapIconByPath(iconPath);
            IconInfo iconInfo = new IconInfo
            {
                Path_NoWrite = path, // 保持原始路径（快捷方式路径）
                LnkPath_NoWrite = tempPath,
                BitmapImage_NoWrite = bi,
                StartArg_NoWrite = FileUtil.GetArgByLnk(tempPath)
            };
    
            // 如果是快捷方式，保存目标路径信息
            if (!string.IsNullOrEmpty(targetPath))
            {
                iconInfo.TargetPath_NoWrite = targetPath;
            }
    
            iconInfo.DefaultImage_NoWrite = iconInfo.ImageByteArr;
            iconInfo.Name_NoWrite = System.IO.Path.GetFileNameWithoutExtension(tempPath);
    
            if (StringUtil.IsEmpty(iconInfo.Name))
            {
                iconInfo.Name_NoWrite = path;
            }
    
            string relativePath = FileUtil.MakeRelativePath(Constants.APP_DIR + "GeekDesk.exe", iconInfo.Path);
            if (!string.IsNullOrEmpty(relativePath) && !relativePath.Equals(iconInfo.Path))
            {
                iconInfo.RelativePath_NoWrite = relativePath;
            }
            return iconInfo;
        }


        public static IconInfo GetIconInfoByPath_NoWrite(string path)
        {
            string tempPath = path;

            //string base64 = ImageUtil.FileImageToBase64(path, System.Drawing.Imaging.ImageFormat.Png);
            string ext = "";
            if (!ImageUtil.IsSystemItem(path))
            {
                ext = System.IO.Path.GetExtension(path).ToLower();
            }

            string iconPath = null;
            if (".lnk".Equals(ext))
            {

                string targetPath = FileUtil.GetTargetPathByLnk(path);
                iconPath = FileUtil.GetIconPathByLnk(path);
                if (targetPath != null)
                {
                    path = targetPath;
                }
            }
            if (StringUtil.IsEmpty(iconPath))
            {
                iconPath = path;
            }

            BitmapImage bi = ImageUtil.GetBitmapIconByPath(iconPath);
            IconInfo iconInfo = new IconInfo
            {
                Path_NoWrite = path,
                LnkPath_NoWrite = tempPath,
                BitmapImage_NoWrite = bi,
                StartArg_NoWrite = FileUtil.GetArgByLnk(tempPath)
            };
            iconInfo.DefaultImage_NoWrite = iconInfo.ImageByteArr;
            iconInfo.Name = System.IO.Path.GetFileNameWithoutExtension(tempPath);
            if (StringUtil.IsEmpty(iconInfo.Name))
            {
                iconInfo.Name_NoWrite = path;
            }
            return iconInfo;
        }






        /// <summary>
        /// 排序图标
        /// </summary>
        public static void SortIconList(bool sort = true)
        {
            try
            {
                if (MainWindow.appData.AppConfig.IconSortType != SortType.CUSTOM && sort)
                {
                    ObservableCollection<MenuInfo> menuList = MainWindow.appData.MenuList;
                    //List<IconInfo> list = new List<IconInfo>(menuList[MainWindow.appData.AppConfig.SelectedMenuIndex].IconList);
                    List<IconInfo> list;
                    foreach (MenuInfo menuInfo in menuList)
                    {
                        list = new List<IconInfo>(menuInfo.IconList);
                        switch (MainWindow.appData.AppConfig.IconSortType)
                        {
                            case SortType.COUNT_UP:
                                list.Sort((x, y) => x.Count.CompareTo(y.Count));
                                break;
                            case SortType.COUNT_LOW:
                                list.Sort((x, y) => y.Count.CompareTo(x.Count));
                                break;
                            case SortType.NAME_UP:
                                list.Sort((x, y) => x.Name.CompareTo(y.Name));
                                break;
                            case SortType.NAME_LOW:
                                list.Sort((x, y) => y.Name.CompareTo(x.Name));
                                break;
                        }
                        menuInfo.IconList = new ObservableCollection<IconInfo>(list);
                    }
                    MainWindow.appData.AppConfig.SelectedMenuIcons = MainWindow.appData.MenuList[MainWindow.appData.AppConfig.SelectedMenuIndex].IconList;
                }
            }
            catch (Exception) { }
            
        }



        /// <summary>
        /// 判断鼠标是否在窗口内
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static bool MouseInWindow(Window window)
        {
            double windowHeight = window.Height;
            double windowWidth = window.Width;

            double windowTop = window.Top;
            double windowLeft = window.Left;

            //获取鼠标位置
            System.Windows.Point p = MouseUtil.GetMousePosition();
            double mouseX = p.X;
            double mouseY = p.Y;

            //鼠标不在窗口上
            if (mouseX < windowLeft || mouseX > windowLeft + windowWidth
                || mouseY < windowTop || mouseY > windowTop + windowHeight)
            {
                return false;
            }
            return true;
        }

    }
}
