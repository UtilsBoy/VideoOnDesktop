using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

public class VideoWallpaper : Form {
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll")] static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private static Form videoForm; 
    private string currentPath = "";

    public VideoWallpaper() {
        this.Text = "Video UI (Close me - video stays)";
        this.Size = new Size(350, 180);
        this.BackColor = Color.FromArgb(10, 10, 10);
        this.ForeColor = Color.Cyan;
        this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
        this.StartPosition = FormStartPosition.CenterScreen;

        Label lbl = new Label { Text = "Выберите видео файл", Location = new Point(20, 20), Size = new Size(300, 20) };
        Button btnPath = new Button { Text = "ПУТЬ", Location = new Point(20, 60), Size = new Size(90, 40), FlatStyle = FlatStyle.Flat };
        btnPath.FlatAppearance.BorderColor = Color.Cyan;
        Button btnStart = new Button { Text = "START", Location = new Point(120, 60), Size = new Size(90, 40), FlatStyle = FlatStyle.Flat };
        btnStart.FlatAppearance.BorderColor = Color.Cyan;
        Button btnStop = new Button { Text = "EXIT ALL", Location = new Point(220, 60), Size = new Size(90, 40), FlatStyle = FlatStyle.Flat };
        btnStop.FlatAppearance.BorderColor = Color.Cyan;

        btnPath.Click += (s, e) => {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK) { 
                currentPath = ofd.FileName; 
                lbl.Text = "Выбрано: " + Path.GetFileName(currentPath); 
            }
        };

        btnStart.Click += (s, e) => StartVideo(currentPath);
        
        // Блять иди нахуй я заебался СУКА 
        btnStop.Click += (s, e) => { 
            if (videoForm != null) videoForm.Close(); 
            Application.Exit(); 
        };

        this.Controls.Add(lbl); this.Controls.Add(btnPath); this.Controls.Add(btnStart); this.Controls.Add(btnStop);
    }

    private void StartVideo(string videoPath) {
        if (string.IsNullOrEmpty(videoPath)) return;
        if (videoForm != null) videoForm.Close();

        Rectangle screen = Screen.PrimaryScreen.Bounds;

        videoForm = new Form {
            FormBorderStyle = FormBorderStyle.None,
            Left = 0, Top = 0,
            Width = screen.Width, Height = screen.Height,
            StartPosition = FormStartPosition.Manual,
            ShowInTaskbar = false
        };

        AxHostControl player = new AxHostControl("6bf52a52-394a-11d3-b153-00c04f79faa6");
        videoForm.Controls.Add(player);
        player.Dock = DockStyle.Fill;
        
        // Показываем окно видео отдельным процессом в рамках приложения
        videoForm.Show();

        IntPtr progman = FindWindow("Progman", null);
        SendMessage(progman, 0x052C, new IntPtr(0), IntPtr.Zero);

        IntPtr workerW = IntPtr.Zero;
        EnumWindows((hwnd, lParam) => {
            if (FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                workerW = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
            return true;
        }, IntPtr.Zero);

        if (workerW != IntPtr.Zero) SetParent(videoForm.Handle, workerW);

        try {
            dynamic wmp = player.GetOcx();
            wmp.URL = videoPath;
            wmp.settings.setMode("loop", true);
            wmp.uiMode = "none";
            wmp.settings.mute = true;
            wmp.stretchToFit = true;
        } catch {}
    }

    [STAThread] static void Main() { 
        Application.EnableVisualStyles();
        VideoWallpaper mainUI = new VideoWallpaper();
        
        // Хитрый запуск: закрытие mainUI не завершит процесс, пока жива videoForm
        mainUI.Show();
        Application.Run(); 
    }

    // Переопределяем закрытие главного окна
    protected override void OnFormClosing(FormClosingEventArgs e) {
        base.OnFormClosing(e);
        // Если мы нажали крестик, просто скрываем UI, но не выходим из приложения
        this.Hide(); 
        // Приложение продолжит работу в фоне, пока активно окно видео
    }
}

public class AxHostControl : AxHost {
    public AxHostControl(string clsid) : base(clsid) { }
    public new object GetOcx() { return base.GetOcx(); }
}
