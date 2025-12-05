using Lubrisense.Views;

namespace Lubrisense
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(DeviceConfigView), typeof(DeviceConfigView));
            Routing.RegisterRoute(nameof(DeviceMenuView), typeof(DeviceMenuView));
        }
    }
}
