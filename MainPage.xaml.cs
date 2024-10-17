namespace MauiEpubTTSReader
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        ///<summary> Occurs when the user finalizes the text in an entry with the return key. </summary>
        private async void SearchTextEntry_Completed(object sender, EventArgs e)
        {
            if (BindingContext is MainPageViewModel viewModel)
                await viewModel.FindTextCommand.ExecuteAsync(null);
        }
    }
}
