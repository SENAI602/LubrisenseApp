namespace Lubrisense
{
    public partial class App : Application
    {
        public static App Instance => (App)Current!;

        public readonly string LubricenseServiceUuid = "97ec4585-9e94-41b6-8902-1a2db274dfc9";
        public readonly string LubricenseCharacteristicUuid = "c04c4646-d355-41ab-9097-89c2c6b9932b";

        public enum CONFIGTYPE { BASICO, AVANCADO }
        public enum INTERVALTYPE {NONE, HORA, DIA, MES }

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}