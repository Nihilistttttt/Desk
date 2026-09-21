using GeekDesk.Constant;
using GeekDesk.Util;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>
/// 程序数据
/// </summary>
namespace GeekDesk.ViewModel
{
    [Serializable]
    public class AppData : INotifyPropertyChanged
    {
        private ObservableCollection<MenuInfo> menuList; //菜单信息及菜单对应icon信息
        private AppConfig appConfig = new AppConfig(); //程序设置信息

        public ObservableCollection<MenuInfo> MenuList
        {
            get
            {
                if (menuList == null)
                {
                    menuList = new ObservableCollection<MenuInfo>();
                }
                return menuList;
            }
            set
            {
                menuList = value;
                OnPropertyChanged("MenuList");
            }
        }


        public AppConfig AppConfig
        {
            get
            {
                return appConfig;
            }
            set
            {
                appConfig = value;
                OnPropertyChanged("AppConfig");
            }
        }

        [field: NonSerializedAttribute()]
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            CommonCode.SaveAppData(MainWindow.appData, Constants.DATA_FILE_PATH);
        }

    }
}
