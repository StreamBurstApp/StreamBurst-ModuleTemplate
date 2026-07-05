using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using StreamBurst.Abstractions.Module;
using System.Threading;

namespace DesignerPreview
{
    public partial class MainWindow : Window
    {
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
            IModuleUI testModule = new Module.Module();

            // Initialize module asynchronously (which loads stored events if any)
            testModule.InitializeAsync(mockContext, CancellationToken.None).GetAwaiter().GetResult();

            // Create SettingsView using the normal module method
            var settingsView = testModule.CreateSettingsView();

            // Place it in the window content
            Content = settingsView;
        }
    }
}