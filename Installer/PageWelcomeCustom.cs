using CFIT.Installer.UI.Behavior;
using System.Windows;

namespace Installer
{
    public class PageWelcomeCustom : PageWelcome
    {
        public PageWelcomeCustom() : base()
        {

        }

        protected override void SetFooterWarnings()
        {
            base.SetFooterWarnings();

            string text = "The Installer will enable the Remote Control Server in the GSX Config.\r\nThat Setting is required for Any2GSX to operate.";
            var header = CreateTextBlock(text, 12);
            header.FontWeight = FontWeights.DemiBold;
            header.TextWrapping = TextWrapping.Wrap;
            header.TextAlignment = TextAlignment.Center;
            header.VerticalAlignment = VerticalAlignment.Center;
            header.Margin = new Thickness(0, 16, 0, 0);
            AddFooter(header);
        }
    }
}
