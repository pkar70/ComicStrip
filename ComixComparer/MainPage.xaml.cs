namespace ComixComparer;

public partial class MainPage : ContentPage
{

    public MainPage()
    {
        InitializeComponent();
        PokazObrazkiAsync();
    }

    private void uiDel0_Click(object sender, EventArgs e)
    {
        // usun seria0
        File.Delete(GetPicPath(uiDate0.Date));
        PrzewinOba(1);
    }

    private void uiDel1_Click(object sender, EventArgs e)
    {
        // usun seria1
        File.Delete(GetPicPath(uiDate0.Date));
        PrzewinOba(1);
    }

    private void uiGoPrev_Click(object sender, EventArgs e)
    {
        // przewin oba
        PrzewinOba(-1);
    }

    private void uiGoNext_Click(object sender, EventArgs e)
    {
        // przewin oba
        PrzewinOba(1);
    }

    private void PrzewinOba(int dni)
    {
        uiDate0.Date = uiDate0.Date.AddDays(dni);
        uiDate1.Date = uiDate1.Date.AddDays(dni);
        PokazObrazkiAsync();
    }

    private async void PokazObrazkiAsync()
    {
        await PokazObrazekAsync(uiPicture0, uiDate0.Date);
        await PokazObrazekAsync(uiPicture1, uiDate1.Date);
    }

    private string GetPicPath(DateTime naKiedy)
    {
        return @"C:\Users\pkar\Pictures\ComicStrips\peanuts\" + naKiedy.ToString("yyyy-MM-dd");
    }

    private async Task PokazObrazekAsync(Image oImage, DateTime naKiedy)
    {
        oImage.Source = FileImageSource.FromFile(GetPicPath(naKiedy));
    }

    //SemanticScreenReader.Announce(CounterBtn.Text);
}