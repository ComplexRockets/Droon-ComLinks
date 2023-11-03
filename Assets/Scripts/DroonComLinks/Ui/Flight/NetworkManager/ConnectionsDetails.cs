using Assets.Scripts.DroonComLinks.Network;
using Assets.Scripts.Menu.ListView;

namespace Assets.Scripts.DroonComLinks.Ui.Flight.NetworkManager
{
    public class ConnectionsDetails
    {
        private readonly DetailsTextScript _description;

        public ConnectionsDetails(ListViewDetailsScript listViewDetails)
        {
            _description = listViewDetails.Widgets.AddText("Description");
        }

        public void UpdateDetails(NetworkConnection node)
        {
            _description.Text = node.id;
        }
    }
}