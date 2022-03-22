using BufferedBleSppXamarin;
using Xamarin.Forms;

namespace BufferedBleSppXam
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new StartupPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
