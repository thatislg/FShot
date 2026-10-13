using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace BambooMintKeyTest;

public partial class MainWindow : Window
{
    private TextBox? _mainTextBox;
    private TextBlock? _textStatsBlock;
    private TextBlock? _tsfStatusBlock;
    private TextBox? _logTextBox;
    private Button? _btnActivateTsf;
    private Button? _btnOpenWin32Edit;
    private Button? _btnCloseWin32Edit;
    private Button? _btnClearText;

    private readonly StringBuilder _logBuilder = new();

    public MainWindow()
    {
        InitializeComponent();
        WireControls();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void WireControls()
    {
        _mainTextBox = this.FindControl<TextBox>("MainTextBox");
        _textStatsBlock = this.FindControl<TextBlock>("TextStatsBlock");
        _tsfStatusBlock = this.FindControl<TextBlock>("TsfStatusBlock");
        _logTextBox = this.FindControl<TextBox>("LogTextBox");
        _btnActivateTsf = this.FindControl<Button>("BtnActivateTsf");
        _btnOpenWin32Edit = this.FindControl<Button>("BtnOpenWin32Edit");
        _btnCloseWin32Edit = this.FindControl<Button>("BtnCloseWin32Edit");
        _btnClearText = this.FindControl<Button>("BtnClearText");

        if (_mainTextBox != null)
        {
            _mainTextBox.TextChanged += OnMainTextBoxTextChanged;
            _mainTextBox.KeyDown += OnMainTextBoxKeyDown;
            _mainTextBox.TextInput += OnMainTextBoxTextInput;
        }

        if (_btnActivateTsf != null)
        {
            _btnActivateTsf.Click += OnBtnActivateTsfClick;
        }

        if (_btnOpenWin32Edit != null)
        {
            _btnOpenWin32Edit.Click += OnBtnOpenWin32EditClick;
        }

        if (_btnCloseWin32Edit != null)
        {
            _btnCloseWin32Edit.Click += OnBtnCloseWin32EditClick;
        }

        if (_btnClearText != null)
        {
            _btnClearText.Click += (s, e) =>
            {
                if (_mainTextBox != null)
                {
                    _mainTextBox.Text = string.Empty;
                    _mainTextBox.Focus();
                }
            };
        }

        this.Closing += (s, e) =>
        {
            Win32EditHelper.ClosePopupEdit();
        };

        Log("Ứng dụng khởi động. Sẵn sàng gõ thử với BambooMintKey.");
    }

    private void OnMainTextBoxTextChanged(object? sender, TextChangedEventArgs e)
    {
        string text = _mainTextBox?.Text ?? string.Empty;
        var codePoints = new StringBuilder();
        foreach (char c in text)
        {
            codePoints.Append($"U+{(int)c:X4} ");
        }

        if (_textStatsBlock != null)
        {
            _textStatsBlock.Text = $"Độ dài: {text.Length} ký tự | Nội dung: \"{text}\" | Mã Unicode: [{codePoints.ToString().TrimEnd()}]";
        }
    }

    private void OnMainTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        Log($"[KeyDown] Key={e.Key}, KeySymbol={e.KeySymbol}, PhysicalKey={e.PhysicalKey}");
    }

    private void OnMainTextBoxTextInput(object? sender, TextInputEventArgs e)
    {
        Log($"[TextInput] Text=\"{e.Text}\"");
    }

    private void OnBtnActivateTsfClick(object? sender, RoutedEventArgs e)
    {
        var (ok, msg) = TsfHelper.Activate();
        if (_tsfStatusBlock != null)
        {
            _tsfStatusBlock.Text = $"Trạng thái TSF: {(ok ? "ĐÃ KÍCH HOẠT" : "THẤT BẠI")} - {msg}";
            _tsfStatusBlock.Foreground = ok ? Avalonia.Media.Brushes.LightGreen : Avalonia.Media.Brushes.Salmon;
        }
        Log($"[TSF] {msg}");
        _mainTextBox?.Focus();
    }

    private void OnBtnOpenWin32EditClick(object? sender, RoutedEventArgs e)
    {
        var pos = this.Position;
        int popupX = pos.X + 80;
        int popupY = pos.Y + 220;
        var hwnd = Win32EditHelper.CreatePopupEdit(popupX, popupY, 320, 60);
        Log($"[Win32 EDIT] Đã mở popup EDIT window (HWND=0x{hwnd.ToInt64():X}). Hãy thử gõ vào popup này.");
    }

    private void OnBtnCloseWin32EditClick(object? sender, RoutedEventArgs e)
    {
        Win32EditHelper.ClosePopupEdit();
        Log("[Win32 EDIT] Đã đóng popup EDIT window.");
        _mainTextBox?.Focus();
    }

    private void Log(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string line = $"[{timestamp}] {message}";
        _logBuilder.AppendLine(line);
        if (_logTextBox != null)
        {
            _logTextBox.Text = _logBuilder.ToString();
            _logTextBox.CaretIndex = _logTextBox.Text.Length;
        }
    }
}