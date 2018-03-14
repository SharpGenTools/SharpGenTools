using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SharpGen.Interactive
{
    public class ProgressView : Window
    {
        public ProgressView(SharpGenModel model)
            :this()
        {
            DataContext = model;
        }

        public ProgressView()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
