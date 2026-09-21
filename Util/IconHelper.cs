using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace GeekDesk.Util
{
    public class IconHelper
    {
        [DllImport("Shell32.dll")]
        private static extern IntPtr SHGetFileInfo
        (
            string pszPath, //һ������Ҫȡ����Ϣ���ļ���Ի����·���Ļ��塣�����Դ�������ļ�������Ҳ����ָ�����ļ�·����ע[1]
            uint dwFileAttributes,//������˵���������������uFlags�а���SHGFI_USEFILEATTRIBUTES��־�����(һ�㲻ʹ��)����ˣ���Ӧ�����ļ����Ե���ϣ��浵��ֻ����Ŀ¼��ϵͳ�ȡ�
            out SHFILEINFO psfi,
            uint cbfileInfo,//�򵥵ظ�������ṹ�ĳߴ硣
            SHGFI uFlags//�����ĺ��ı�����ͨ�����п��ܵı�־������ܼ�Ԧ��������Ϊ��ʵ�ʵصõ���Ϣ��
        );


        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public SHFILEINFO(bool b)
            {
                hIcon = IntPtr.Zero; iIcon = 0; dwAttributes = 0; szDisplayName = ""; szTypeName = "";
            }
            public IntPtr hIcon;//ͼ����
            public int iIcon;//ϵͳͼ���б������
            public uint dwAttributes; //�ļ�������
            [MarshalAs(UnmanagedType.LPStr, SizeConst = 260)]
            public string szDisplayName;//�ļ���·���� �ļ����256��ANSI���������̷���X:\��3�ֽڣ�259�ֽڣ��ټ��Ͻ�����1�ֽڣ���260
            [MarshalAs(UnmanagedType.LPStr, SizeConst = 80)]
            public string szTypeName;//�ļ��������� �̶�80�ֽ�
        };



        private enum SHGFI
        {
            SmallIcon = 0x00000001,
            LargeIcon = 0x00000000,
            Icon = 0x00000100,
            DisplayName = 0x00000200,//Retrieve the display name for the file, which is the name as it appears in Windows Explorer. The name is copied to the szDisplayName member of the structure specified in psfi. The returned display name uses the long file name, if there is one, rather than the 8.3 form of the file name. Note that the display name can be affected by settings such as whether extensions are shown.
            Typename = 0x00000400,  //Retrieve the string that describes the file's type. The string is copied to the szTypeName member of the structure specified in psfi.
            SysIconIndex = 0x00004000, //Retrieve the index of a system image list icon. If successful, the index is copied to the iIcon member of psfi. The return value is a handle to the system image list. Only those images whose indices are successfully copied to iIcon are valid. Attempting to access other images in the system image list will result in undefined behavior.
            UseFileAttributes = 0x00000010 //Indicates that the function should not attempt to access the file specified by pszPath. Rather, it should act as if the file specified by pszPath exists with the file attributes passed in dwFileAttributes. This flag cannot be combined with the SHGFI_ATTRIBUTES, SHGFI_EXETYPE, or SHGFI_PIDL flags.
        }

        /// <summary>
        /// �����ļ���չ���õ�ϵͳ��չ����ͼ��
        /// </summary>
        /// <param name="fileName">�ļ���(�磺win.rar;setup.exe;temp.txt)</param>
        /// <param name="largeIcon">ͼ��Ĵ�С</param>
        /// <returns></returns>
        public static Icon GetFileIcon(string fileName, bool largeIcon)
        {
            SHFILEINFO info = new SHFILEINFO(true);
            int cbFileInfo = Marshal.SizeOf(info);
            SHGFI flags;
            if (largeIcon)
                flags = SHGFI.Icon | SHGFI.LargeIcon | SHGFI.UseFileAttributes;
            else
                flags = SHGFI.Icon | SHGFI.SmallIcon | SHGFI.UseFileAttributes;
            IntPtr IconIntPtr = SHGetFileInfo(fileName, 256, out info, (uint)cbFileInfo, flags);
            if (IconIntPtr.Equals(IntPtr.Zero))
                return null;
            return Icon.FromHandle(info.hIcon);
        }

        /// <summary>  
        /// ��ȡ�ļ���ͼ��
        /// </summary>  
        /// <returns>ͼ��</returns>  
        public static Icon GetDirectoryIcon(string path, bool largeIcon)
        {
            SHFILEINFO _SHFILEINFO = new SHFILEINFO();
            int cbFileInfo = Marshal.SizeOf(_SHFILEINFO);
            SHGFI flags;
            if (largeIcon)
                flags = SHGFI.Icon | SHGFI.LargeIcon;
            else
                flags = SHGFI.Icon | SHGFI.SmallIcon;

            IntPtr IconIntPtr = SHGetFileInfo(path, 256, out _SHFILEINFO, (uint)cbFileInfo, flags);
            if (IconIntPtr.Equals(IntPtr.Zero))
                return null;
            Icon _Icon = Icon.FromHandle(_SHFILEINFO.hIcon);
            return _Icon;
        }

    }
}