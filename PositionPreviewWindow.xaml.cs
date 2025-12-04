using System.Windows;

namespace TaskbarLyrics
{
    public partial class PositionPreviewWindow : Window
    {
        public PositionPreviewWindow()
        {
            InitializeComponent();
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
            this.Activate();
        }

        public void HidePreview()
        {
            this.Hide();
        }
    }
}