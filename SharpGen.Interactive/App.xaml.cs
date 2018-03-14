using Avalonia;
using Avalonia.Markup.Xaml;

namespace SharpGen.Interactive
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
