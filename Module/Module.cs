using Avalonia.Controls;
using Module.ViewModels;
using StreamBurst.Abstractions.Event;
using StreamBurst.Abstractions.Module;

namespace Module
{
    public class Module : IModule, IModuleUI
    {
        public string Id => throw new NotImplementedException();
        public string DisplayName => throw new NotImplementedException();
        public Version Version => throw new NotImplementedException();

        private IModuleContext? _context;

        public Task InitializeAsync(IModuleContext context, CancellationToken ct)
        {
            _context = context;
            return Task.CompletedTask;
        }

        public Task ShutdownAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
        public Control CreateSettingsView()
        {
            if (_context == null) throw new InvalidOperationException("Module context is not initialized.");

            ModuleViewModel viewModel = new ModuleViewModel(this, _context);
            return new ModuleView() { DataContext = viewModel };
        }

        public IReadOnlyList<ActionDescriptor> GetActionCatalog()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<EventDescriptor> GetEventCatalog()
        {
            throw new NotImplementedException();
        }

        public void AppReady()
        {
            return;
        }
    }
}
