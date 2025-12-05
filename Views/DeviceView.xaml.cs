using Lubrisense.ViewModels;

namespace Lubrisense.Views;

public partial class DeviceView : ContentPage
{
    private readonly DeviceViewModel viewModel;

    public DeviceView(DeviceViewModel _viewModel)
    {
        InitializeComponent();
        viewModel = _viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Avisa a ViewModel que a tela apareceu.
        // A ViewModel vai limpar a lista e iniciar o Scan automaticamente.
        viewModel.OnAppearing();
    }

    // Removemos toda a lógica de "UpdateToolbarIcon" porque o botão não existe mais visualmente.
}