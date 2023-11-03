using System;
using System.Collections;
using Assets.Scripts.DroonComLinks.Network;
using Assets.Scripts.Menu.ListView;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Ui.Flight.NetworkManager
{
    public class ConnectionsViewModel : ListViewModel
    {
        private ConnectionsDetails _details;

        public override void OnListViewInitialized(ListViewScript listView)
        {
            base.OnListViewInitialized(listView);
            base.DoubleClickIsPrimaryClick = false;
            listView.Title = "NETWORK CONNECTIONS";
            listView.CanDelete = false;
            //listView.PrimaryButtonText = "ACCEPT";
            listView.PrimaryButtonEnabled = false;
            listView.DisplayType = ListViewScript.ListViewDisplayType.SmallDialog;
            _details = new ConnectionsDetails(base.ListView.ListViewDetails);
            listView.ViewModel.UseGrid = true;

            //listView.CreateFilter(true, "Show Crafts", "Shows the player controllable nodes", ListViewFilterType.Include, false, "craft");
            //listView.CreateFilter(true, "Show Ground Stations", "Shows the ground antennas", ListViewFilterType.Include, false, "gs");
            //listView.CreateContextMenuSeparator();
            //listView.CreateContextMenuItem("Refresh Contracts", OnRefreshContractsClicked).CloseContextMenuWhenClicked = true;
        }

        public override IEnumerator LoadItems()
        {
            yield return new WaitForEndOfFrame();
            base.NoItemsFoundMessage = "No network node found.\nThis is probably a DCL bug, please report it.";
            foreach (NetworkConnection connection in ComLinksManager.Instance.NetworkConnections)
            {
                ListViewItemScript listViewItemScript = base.ListView.CreateItem(connection.id, "", connection, "");

                //listViewItemScript.FilterKeywords.Add(node.isCraft ? "craft" : "gs");
            }
            yield return new WaitForEndOfFrame();
        }

        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
            base.ListView.CanDelete = false;
            if (item != null)
            {
                NetworkConnection connection = item.ItemModel as NetworkConnection;
                base.ListView.DetailsTitleText = connection.id;
                _details.UpdateDetails(connection);
            }
            completeCallback?.Invoke();
        }
    }
}
