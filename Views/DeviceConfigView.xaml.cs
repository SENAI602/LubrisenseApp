using Lubrisense.ViewModels;

namespace Lubrisense.Views;

public partial class DeviceConfigView : ContentPage
{
    // O construtor recebe o ViewModel pronto (Injeção de Dependência)
    public DeviceConfigView(DeviceConfigViewModel viewModel)
    {
        InitializeComponent();

        // Esta é a única linha necessária. 
        // Ela diz para a tela: "Use este ViewModel para responder aos meus comandos e preencher meus dados."
        BindingContext = viewModel;
    }
}