using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using StreamBurst.Abstractions.Module;
using System.Threading;

namespace DesignerPreview
{
    public partial class MainWindow : Window
    {
        private IModuleUI _module;
        public MainWindow()
        {
            InitializeComponent();
            AttachModuleView();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void AttachModuleView()
        {
            // Setup mock context
            IModuleContext mockContext = new MockContext();

            // Instantiate Module
            _module = new Module.Module();

            // Initialize module asynchronously (which loads stored events if any)
            _module.InitializeAsync(mockContext, CancellationToken.None).GetAwaiter().GetResult();

            // Create SettingsView using the normal module method
            var settingsView = _module.CreateSettingsView();

            // Place it in the window content
            Content = settingsView;
        }

        public void ShutDown()
        {
            _module.ShutdownAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}