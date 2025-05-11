using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EverythingCmdPal.Pages
{
    internal partial class ExplorePage : ResultsPage
    {
        public ExplorePage() 
        {
            Id = "EverythingExplore";
            Icon = new IconInfo("\uF89A");
            Name = Properties.Resources.browse;
            ShowDetails = true;
        }
        public override IListItem[] GetItems()
        {
            PlaceholderText = pre;
            return base.GetItems();
        }
    }
}
