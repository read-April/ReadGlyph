using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ReadGlyph.Views.Dialogs;

public partial class NewProjectControl : UserControl
{
    public NewProjectControl()
    {
        InitializeComponent();
        Loaded += (_, _) => HookOkButton();
    }

    /// <summary>用户点击「创建」时触发</summary>
    public event Action? Confirmed;

    /// <summary>项目名称</summary>
    public string ProjectName => TxtName.Text.Trim();

    /// <summary>LVGL 输出路径</summary>
    public string OutputPath => TxtProjectPath.Text.Trim();

    /// <summary>是否为编辑模式（标题和按钮文案会变化）</summary>
    public bool IsEditMode { get; set; }

    /// <summary>预填项目信息（用于编辑已有项目）</summary>
    public void SetProjectInfo(string name, string path)
    {
        TxtName.Text = name;
        TxtProjectPath.Text = path;
        IsEditMode = true;

        TxtTitle.Text = "📝 编辑项目";
        BtnConfirm.Content = "保存";
    }

    private void HookOkButton()
    {
        BtnConfirm.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                AlertDialog.Show("提示", "请输入项目名称");
                TxtName.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(OutputPath))
            {
                AlertDialog.Show("提示", "请指定项目根目录");
                TxtProjectPath.Focus();
                return;
            }
            Confirmed?.Invoke();
        };
    }

    private void BtnPickFolder_Click(object sender, RoutedEventArgs e)
    {
        var path = BrowseFolder("选择项目根目录");
        if (!string.IsNullOrEmpty(path))
            TxtProjectPath.Text = path;
    }

    // ---- P/Invoke: SHBrowseForFolder ----

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct BROWSEINFO
    {
        public IntPtr hwndOwner;
        public IntPtr pidlRoot;
        public string pszDisplayName;
        public string lpszTitle;
        public uint ulFlags;
        public IntPtr lpfn;
        public IntPtr lParam;
        public int iImage;
    }

    private const uint BIF_NEWDIALOGSTYLE = 0x0040;
    private const uint BIF_RETURNONLYFSDIRS = 0x0001;

    private static string? BrowseFolder(string title)
    {
        var bi = new BROWSEINFO
        {
            lpszTitle = title,
            ulFlags = BIF_NEWDIALOGSTYLE | BIF_RETURNONLYFSDIRS
        };

        var pidl = SHBrowseForFolder(ref bi);
        if (pidl != IntPtr.Zero)
        {
            var sb = new StringBuilder(260);
            if (SHGetPathFromIDList(pidl, sb))
            {
                Marshal.FreeCoTaskMem(pidl);
                return sb.ToString();
            }
            Marshal.FreeCoTaskMem(pidl);
        }
        return null;
    }
}
