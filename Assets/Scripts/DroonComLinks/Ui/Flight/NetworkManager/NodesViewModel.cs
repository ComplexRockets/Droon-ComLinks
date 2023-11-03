using System;
using System.Collections;
using Assets.Scripts.DroonComLinks.Network;
using Assets.Scripts.Menu.ListView;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Ui.Flight.NetworkManager
{
    public class NodesViewModel : ListViewModel
    {
        private NodesDetails _details;

        public override void OnListViewInitialized(ListViewScript listView)
        {
            base.OnListViewInitialized(listView);
            base.DoubleClickIsPrimaryClick = false;
            listView.Title = "NETWORK NODES";
            listView.CanDelete = false;
            //listView.PrimaryButtonText = "ACCEPT";
            listView.PrimaryButtonEnabled = false;
            listView.DisplayType = ListViewScript.ListViewDisplayType.SmallDialog;
            _details = new NodesDetails(base.ListView.ListViewDetails);

            listView.CreateFilter(true, "Show Crafts", "Shows the player controllable nodes", ListViewFilterType.Include, false, "craft");
            listView.CreateFilter(true, "Show Ground Stations", "Shows the ground antennas", ListViewFilterType.Include, false, "gs");
            //listView.CreateContextMenuSeparator();
            //listView.CreateContextMenuItem("Refresh Contracts", OnRefreshContractsClicked).CloseContextMenuWhenClicked = true;
        }

        public override IEnumerator LoadItems()
        {
            yield return new WaitForEndOfFrame();
            base.NoItemsFoundMessage = "No network node found.\nThis is probably a DCL bug, please report it.";
            foreach (NetworkNode node in ComLinksManager.Instance.NetworkNodes)
            {
                ListViewItemScript listViewItemScript = base.ListView.CreateItem(node.id, "", node, "");

                listViewItemScript.FilterKeywords.Add(node.IsCraft ? "craft" : "gs");

                if (!node.IsCraft) listViewItemScript.SetFlair(ListViewItemScript.FlairColorType.Primary, "Ground Station");
                else
                {
                    listViewItemScript.StatusIcon = node.GsInRange ? ListViewItemScript.StatusIconType.Checkmark : ListViewItemScript.StatusIconType.Exclamation;
                    listViewItemScript.StatusIconColor = node.GsInRange ? "Succes" : "Danger";
                    listViewItemScript.StatusIconTooltip = node.GsInRange ? "connected" : "disconnected";
                }
            }
            yield return new WaitForEndOfFrame();
        }

        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
            base.ListView.CanDelete = false;
            if (item != null)
            {
                NetworkNode node = item.ItemModel as NetworkNode;
                base.ListView.DetailsTitleText = node.id;
                _details.UpdateDetails(node);
            }
            completeCallback?.Invoke();
        }
    }
}
