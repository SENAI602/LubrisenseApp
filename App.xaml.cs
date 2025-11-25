namespace Lubrisense
{
    public partial class App : Application
    {
        public static App Instance => (App)Current!;

        // --- UUIDs CORRIGIDOS PARA O SEU FIRMWARE ---
        public readonly string LubricenseServiceUuid = "4fafc201-1fb5-459e-8fcc-c5c9c331914b";
        public readonly string LubricenseCharacteristicUuid = "1c95d5e3-d8f7-413a-bf3d-7a2e5d7be87e";

        public enum CONFIGTYPE { BASICO, AVANCADO }
        public enum INTERVALTYPE { NONE, HORA, DIA, MES }

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