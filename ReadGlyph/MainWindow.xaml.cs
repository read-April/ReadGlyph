using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ReadGlyph.Models;
using ReadGlyph.ViewModels;
using ReadGlyph.Views.Dialogs;
using SixLabors.ImageSharp;
using IImage = SixLabors.ImageSharp.Image;

namespace ReadGlyph;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    // 分页状态
    private int _fontPageSize = 5;
    private int _fontCurrentPage;
    private int _imagePageSize = 4;
    private int _imageCurrentPage;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel(App.DataDir);
        DataContext = _vm;

        // TopBar 关于按钮
        TopBarCtrl.AboutClicked += () =>
        {
            var dlg = new AboutDialog();
            dlg.Closed += CloseDialog;
            ShowDialog(dlg);
        };

        // 项目切换 → 加载资产
        CmbProjects.SelectionChanged += (_, _) =>
        {
            if (CmbProjects.SelectedItem is string name)
                _vm.SelectProject(name);
        };

        // 初始加载：如果有项目，自动选中第一个
        if (_vm.ProjectNames.Count > 0)
        {
            CmbProjects.SelectedIndex = 0;
            _vm.SelectProject(_vm.ProjectNames[0]);
        }

        // 监听资产集合变化 → 切换空状态/列表显示
        _vm.FontAssets.CollectionChanged += (_, _) => { RefreshAssetVisibility(); ApplyFontPagination(); };
        _vm.ImageAssets.CollectionChanged += (_, _) => { RefreshAssetVisibility(); ApplyImagePagination(); };
        RefreshAssetVisibility();
        ApplyFontPagination();
        ApplyImagePagination();
    }

    // ═════════════ 对话框基础设施 ═════════════

    private void ShowDialog(UserControl dialog)
    {
        DialogContainer.Child = dialog;
        Overlay.Visibility = Visibility.Visible;

        // 自动查找并挂接"取消"按钮
        dialog.Loaded += (_, _) =>
        {
            var cancelBtn = FindCancelButton(dialog);
            if (cancelBtn != null)
                cancelBtn.Click += (_, _) => CloseDialog();
        };
    }

    private void CloseDialog()
    {
        Overlay.Visibility = Visibility.Collapsed;
        DialogContainer.Child = null;
    }

    /// <summary>弹出提示对话框（仅确认按钮）</summary>
    private void ShowAlert(string message)
    {
        var dlg = new ConfirmDialog
        {
            Title = "提示",
            Message = message,
            ConfirmText = "知道了"
        };
        dlg.Confirmed += () => CloseDialog();
        ShowDialog(dlg);
    }

    // ═════════════ 新建项目 ═════════════

    private void BtnNewProject_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NewProjectControl();
        dlg.Confirmed += () =>
        {
            var name = dlg.ProjectName;
            var path = dlg.OutputPath;
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(path))
            {
                var error = _vm.NewProject(name, path);
                if (error != null)
                {
                    CloseDialog();
                    ShowAlert(error);
                    return;
                }
                CmbProjects.SelectedItem = name;
            }
            CloseDialog();
        };
        ShowDialog(dlg);
    }

    // ═════════════ 编辑项目 ═════════════

    private void BtnEditProject_Click(object sender, RoutedEventArgs e)
    {
        var oldName = _vm.SelectedProject;
        if (string.IsNullOrEmpty(oldName))
        {
            ShowAlert("请先选择一个项目。");
            return;
        }

        var proj = new Services.JsonStorage().LoadProject(App.DataDir, oldName);
        var dlg = new NewProjectControl();
        dlg.SetProjectInfo(oldName, proj?.OutputPath ?? "");
        dlg.Confirmed += () =>
        {
            var newName = dlg.ProjectName;
            var newPath = dlg.OutputPath;
            if (!string.IsNullOrWhiteSpace(newName) && !string.IsNullOrWhiteSpace(newPath))
            {
                var error = _vm.EditProject(oldName, newName, newPath);
                if (error != null)
                {
                    CloseDialog();
                    ShowAlert(error);
                    return;
                }
                CmbProjects.SelectedItem = newName;
            }
            CloseDialog();
        };
        ShowDialog(dlg);
    }

    // ═════════════ 删除项目 ═════════════

    private void BtnDeleteProject_Click(object sender, RoutedEventArgs e)
    {
        var name = _vm.SelectedProject;
        if (string.IsNullOrEmpty(name))
        {
            ShowAlert("请先选择一个项目。");
            return;
        }

        var dlg = new ConfirmDialog
        {
            Title = "删除项目",
            Message = $"确定删除项目「{name}」吗？\n项目的配置文件将被永久删除（生成的 .c 文件不受影响）。",
            ConfirmText = "删除"
        };
        dlg.Confirmed += () =>
        {
            _vm.DeleteProject(name);
            CmbProjects.SelectedItem = _vm.SelectedProject;
            CloseDialog();
        };
        ShowDialog(dlg);
    }

    // ═════════════ 导入字体源 ═════════════

    private void BtnAddFont_Click(object sender, RoutedEventArgs e) => OpenImportFontSource();
    private void ImportFontSource_Click(object sender, MouseButtonEventArgs e) => OpenImportFontSource();

    private void OpenImportFontSource()
    {
        var dlg = new ImportFontSourceControl();
        dlg.Confirmed += () =>
        {
            var name = dlg.DisplayName;
            var path = dlg.FilePath;
            if (!string.IsNullOrWhiteSpace(name) && File.Exists(path))
            {
                if (_vm.FontSources.Any(f => f.DisplayName == name))
                {
                    CloseDialog();
                    ShowAlert($"字体源「{name}」已存在，请使用其他名称。");
                    return;
                }
                _vm.ImportFont(path, name);
            }
            CloseDialog();
        };
        ShowDialog(dlg);
    }

    // ═════════════ 导入图片源 ═════════════

    private void BtnAddImage_Click(object sender, RoutedEventArgs e) => OpenImportImageSource();
    private void ImportImageSource_Click(object sender, MouseButtonEventArgs e) => OpenImportImageSource();

    private void OpenImportImageSource()
    {
        var dlg = new ImportImageSourceControl();
        dlg.Confirmed += () =>
        {
            var name = dlg.DisplayName;
            var path = dlg.FilePath;
            if (!string.IsNullOrWhiteSpace(name) && File.Exists(path))
            {
                if (_vm.ImageSources.Any(i => i.DisplayName == name))
                {
                    CloseDialog();
                    ShowAlert($"图片源「{name}」已存在，请使用其他名称。");
                    return;
                }
                _vm.ImportImage(path, name);
            }
            CloseDialog();
        };
        ShowDialog(dlg);
    }

    // ═════════════ 删除源 ═════════════

    private void DeleteFontSource_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not Models.FontSource fs)
            return;

        var dlg = new ConfirmDialog
        {
            Title = "删除字体源",
            Message = $"确定删除「{fs.DisplayName}」吗？\n源文件将从 Data\\fonts\\ 永久删除。",
            ConfirmText = "删除"
        };
        dlg.Confirmed += () =>
        {
            _vm.RemoveFontSource(fs);
            CloseDialog();
        };
        ShowDialog(dlg);
    }

    private void DeleteImageSource_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not Models.ImageSource img)
            return;

        var dlg = new ConfirmDialog
        {
            Title = "删除图片源",
            Message = $"确定删除「{img.DisplayName}」吗？\n源文件将从 Data\\images\\ 永久删除。",
            ConfirmText = "删除"
        };
        dlg.Confirmed += () =>
        {
            _vm.RemoveImageSource(img);
            CloseDialog();
        };
        ShowDialog(dlg);
    }

    // ═════════════ 创建字体资产 ═════════════

    private void BtnCreateFont_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_vm.SelectedProject))
        {
            ShowAlert("请先创建或选择一个项目。");
            return;
        }

        var dlg = new CreateFontAssetControl();
        // 填充字体源下拉框
        dlg.Loaded += (_, _) =>
        {
            dlg.CmbFontSource.SelectedValuePath = "Id";
            dlg.CmbFontSource.ItemsSource = _vm.FontSources;
        };
        // 快速导入：直接引用文件路径，不复制到字体库
        var tempFontSources = new List<FontSource>();
        dlg.QuickImportFont += filePath =>
        {
            var name = Path.GetFileNameWithoutExtension(filePath);
            var temp = new FontSource
            {
                Id = $"temp_{Guid.NewGuid():N}"[..10],
                DisplayName = name,
                FilePath = filePath
            };
            tempFontSources.Add(temp);
            _vm.FontSources.Add(temp);
            dlg.CmbFontSource.SelectedValue = temp.Id;
        };
        // 对话框关闭时清理临时字体源
        dlg.Unloaded += (_, _) =>
        {
            foreach (var t in tempFontSources)
                _vm.FontSources.Remove(t);
        };
        dlg.Confirmed += () =>
        {
            var sourceId = dlg.SelectedSourceId;
            if (!string.IsNullOrEmpty(sourceId))
            {
                if (_vm.FontAssets.Any(a => a.Name == dlg.OutputName))
                {
                    CloseDialog();
                    ShowAlert($"字体资产「{dlg.OutputName}」已存在，请使用其他文件名。");
                    return;
                }
                _vm.CreateFontAsset(sourceId, dlg.OutputName, dlg.GlyphFontSize, dlg.Bpp, dlg.Glyphs, dlg.OutputDir, dlg.ExportFormat);
                // 创建完成后立即生成 .c 文件
                if (_vm.FontAssets.Count > 0)
                    _vm.GenerateFontAsset(_vm.FontAssets[^1]);
            }
            CloseDialog();
        };
        ShowDialog(dlg);
    }

    // ═════════════ 创建图片资产 ═════════════

    private void BtnCreateImage_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_vm.SelectedProject))
        {
            ShowAlert("请先创建或选择一个项目。");
            return;
        }

        var dlg = new CreateImageAssetControl();
        dlg.Loaded += (_, _) =>
        {
            dlg.CmbImageSource.SelectedValuePath = "Id";
            dlg.CmbImageSource.ItemsSource = _vm.ImageSources;
        };
        // 快速导入：直接引用文件路径，不复制到图片库
        var tempImageSources = new List<ImageSource>();
        dlg.QuickImportImage += filePath =>
        {
            var name = Path.GetFileNameWithoutExtension(filePath);
            int w = 0, h = 0;
            try { using var img = IImage.Load(filePath); w = img.Width; h = img.Height; }
            catch { }
            var temp = new ImageSource
            {
                Id = $"temp_{Guid.NewGuid():N}"[..10],
                DisplayName = name,
                FilePath = filePath,
                Width = w,
                Height = h
            };
            tempImageSources.Add(temp);
            _vm.ImageSources.Add(temp);
            dlg.CmbImageSource.SelectedValue = temp.Id;
        };
        // 对话框关闭时清理临时图片源
        dlg.Unloaded += (_, _) =>
        {
            foreach (var t in tempImageSources)
                _vm.ImageSources.Remove(t);
        };
        dlg.Confirmed += () =>
        {
            var sourceId = dlg.SelectedSourceId;
            if (!string.IsNullOrEmpty(sourceId))
            {
                if (_vm.ImageAssets.Any(a => a.Name == dlg.OutputName))
                {
                    CloseDialog();
                    ShowAlert($"图片资产「{dlg.OutputName}」已存在，请使用其他文件名。");
                    return;
                }
                var src = _vm.ImageSources.FirstOrDefault(s => s.Id == sourceId);
                _vm.CreateImageAsset(sourceId, dlg.OutputName, src?.Width ?? 0, src?.Height ?? 0, dlg.Format, dlg.OutputDir, dlg.ExportFormat);
                // 创建完成后立即生成 .c 文件
                if (_vm.ImageAssets.Count > 0)
                    _vm.GenerateImageAsset(_vm.ImageAssets[^1]);
            }
            CloseDialog();
        };
        ShowDialog(dlg);
    }

    // ═════════════ 侧边栏展开/折叠 ═════════════

    private bool _fontLibExpanded;
    private bool _imageLibExpanded;

    private void ToggleFontLib_Click(object sender, MouseButtonEventArgs e)
    {
        _fontLibExpanded = !_fontLibExpanded;
        FontLibArrow.Text = _fontLibExpanded ? "▼" : "▶";
        FontLibSubItems.Visibility = _fontLibExpanded ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ToggleImageLib_Click(object sender, MouseButtonEventArgs e)
    {
        _imageLibExpanded = !_imageLibExpanded;
        ImageLibArrow.Text = _imageLibExpanded ? "▼" : "▶";
        ImageLibSubItems.Visibility = _imageLibExpanded ? Visibility.Visible : Visibility.Collapsed;
    }

    // ═════════════ 资产操作 ═════════════

    private void BtnGenerateFont_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Models.FontAsset fa)
            _vm.GenerateFontAsset(fa);
    }

    private void BtnEditGlyphs_Click(object sender, RoutedEventArgs e)
    {
        var fa = (sender as FrameworkElement)?.DataContext as Models.FontAsset;
        if (fa == null) return;

        var dlg = new EditGlyphsControl { Glyphs = fa.Glyphs };
        dlg.Confirmed += () =>
        {
            fa.Glyphs = dlg.Glyphs;
            _vm.SaveCurrentProject();
            _vm.GenerateFontAsset(fa);
            CloseDialog();
        };
        ShowDialog(dlg);
    }

    private void BtnDeleteFont_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not Models.FontAsset fa)
            return;

        var dlg = new ConfirmDialog
        {
            Title = "删除字体资产",
            Message = $"确定删除「{fa.Name}」吗？\n该资产将从项目中移除（已生成的 .c 文件不受影响）。",
            ConfirmText = "删除"
        };
        dlg.Confirmed += () =>
        {
            _vm.FontAssets.Remove(fa);
            _vm.SaveCurrentProject();
            CloseDialog();
        };
        ShowDialog(dlg);
    }

    private void BtnGenerateImage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Models.ImageAsset ia)
            _vm.GenerateImageAsset(ia);
    }

    private void BtnDeleteImage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not Models.ImageAsset ia)
            return;

        var dlg = new ConfirmDialog
        {
            Title = "删除图片资产",
            Message = $"确定删除「{ia.Name}」吗？\n该资产将从项目中移除（已生成的 .c 文件不受影响）。",
            ConfirmText = "删除"
        };
        dlg.Confirmed += () =>
        {
            _vm.ImageAssets.Remove(ia);
            _vm.SaveCurrentProject();
            CloseDialog();
        };
        ShowDialog(dlg);
    }

    /// <summary>根据集合内容切换空状态 / 列表显示</summary>
    private void RefreshAssetVisibility()
    {
        bool hasFont = _vm.FontAssets.Count > 0;
        FontEmptyState.Visibility    = hasFont ? Visibility.Collapsed : Visibility.Visible;
        FontAssetList.Visibility     = hasFont ? Visibility.Visible  : Visibility.Collapsed;
        FontPager.Visibility         = hasFont ? Visibility.Visible  : Visibility.Collapsed;

        bool hasImage = _vm.ImageAssets.Count > 0;
        ImageEmptyState.Visibility   = hasImage ? Visibility.Collapsed : Visibility.Visible;
        ImageAssetList.Visibility    = hasImage ? Visibility.Visible  : Visibility.Collapsed;
        ImagePager.Visibility        = hasImage ? Visibility.Visible  : Visibility.Collapsed;
    }

    // ═════════════ 遮罩层 ═════════════

    /// <summary>点击遮罩背景关闭对话框</summary>
    private void Overlay_MouseDown(object sender, MouseButtonEventArgs e) => CloseDialog();

    /// <summary>阻止点击对话框内部时冒泡到遮罩层</summary>
    private void DialogContainer_MouseDown(object sender, MouseButtonEventArgs e) => e.Handled = true;

    /// <summary>递归查找 Content 为"取消"的 Button</summary>
    private static Button? FindCancelButton(DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is not DependencyObject depChild)
                continue;

            if (depChild is Button btn && btn.Content as string == "取消")
                return btn;

            var found = FindCancelButton(depChild);
            if (found != null)
                return found;
        }
        return null;
    }

    // ═════════════ 分页 ═════════════

    private void ApplyFontPagination()
    {
        if (_vm?.FontAssets == null) return;
        var items = _vm.FontAssets.Skip(_fontCurrentPage * _fontPageSize).Take(_fontPageSize).ToList();
        FontAssetList.ItemsSource = items;
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)_vm.FontAssets.Count / _fontPageSize));
        // 如果当前页超出总页数，回退到最后一页
        if (_fontCurrentPage >= totalPages)
            _fontCurrentPage = totalPages - 1;
        TxtFontPageInfo.Text = $"第 {_fontCurrentPage + 1}/{totalPages} 页";
        BtnFontPrev.IsEnabled = _fontCurrentPage > 0;
        BtnFontNext.IsEnabled = _fontCurrentPage < totalPages - 1;
    }

    private void ApplyImagePagination()
    {
        if (_vm?.ImageAssets == null) return;
        var items = _vm.ImageAssets.Skip(_imageCurrentPage * _imagePageSize).Take(_imagePageSize).ToList();
        ImageAssetList.ItemsSource = items;
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)_vm.ImageAssets.Count / _imagePageSize));
        if (_imageCurrentPage >= totalPages)
            _imageCurrentPage = totalPages - 1;
        TxtImagePageInfo.Text = $"第 {_imageCurrentPage + 1}/{totalPages} 页";
        BtnImagePrev.IsEnabled = _imageCurrentPage > 0;
        BtnImageNext.IsEnabled = _imageCurrentPage < totalPages - 1;
    }

    private void CmbFontPageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbFontPageSize.SelectedItem is not ComboBoxItem item) return;
        _fontPageSize = item.Content.ToString() switch
        {
            "5" => 5,
            "10" => 10,
            "15" => 15,
            "20" => 20,
            _ => 5
        };
        _fontCurrentPage = 0;
        ApplyFontPagination();
    }

    private void CmbImagePageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbImagePageSize.SelectedItem is not ComboBoxItem item) return;
        _imagePageSize = item.Content.ToString() switch
        {
            "5" => 5,
            "10" => 10,
            "15" => 15,
            "20" => 20,
            _ => 5
        };
        _imageCurrentPage = 0;
        ApplyImagePagination();
    }

    private void BtnFontPrev_Click(object sender, RoutedEventArgs e)
    {
        if (_fontCurrentPage > 0) { _fontCurrentPage--; ApplyFontPagination(); }
    }

    private void BtnFontNext_Click(object sender, RoutedEventArgs e)
    {
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)_vm.FontAssets.Count / _fontPageSize));
        if (_fontCurrentPage < totalPages - 1) { _fontCurrentPage++; ApplyFontPagination(); }
    }

    private void BtnImagePrev_Click(object sender, RoutedEventArgs e)
    {
        if (_imageCurrentPage > 0) { _imageCurrentPage--; ApplyImagePagination(); }
    }

    private void BtnImageNext_Click(object sender, RoutedEventArgs e)
    {
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)_vm.ImageAssets.Count / _imagePageSize));
        if (_imageCurrentPage < totalPages - 1) { _imageCurrentPage++; ApplyImagePagination(); }
    }
}