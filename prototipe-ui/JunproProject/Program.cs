namespace Junpro_Project;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(Navigasi.Ambil<DashboardForm>());
    }
}
