using System.Windows;

namespace TaskbarLyrics
{
    public partial class PositionPreviewWindow : Window
    {
        public PositionPreviewWindow()
        {
            InitializeComponent();
            this.Topmost = true;
        }

        public void SetPreviewRect(double x, double y, double width, double height)
        {
            this.Left = x;
            this.Top = y;
            this.Width = width;
            this.Height = height;
        }

        public void ShowPreview()
        {
            this.Show();
            this.Topmost = true;
            // 不调用 Activate()，避免抢夺焦点
        }

        public void HidePreview()
        {
            this.Hide();
        }
    }
}