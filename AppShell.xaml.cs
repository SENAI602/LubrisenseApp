using Lubrisense.Views;

namespace Lubrisense
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(DeviceDetailView), typeof(DeviceDetailView));
            Routing.RegisterRoute(nameof(DeviceConfigView), typeof(DeviceConfigView));
        }
    }
}
