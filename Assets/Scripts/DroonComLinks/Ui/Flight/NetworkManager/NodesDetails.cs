using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.DroonComLinks.Antennas;
using Assets.Scripts.DroonComLinks.Network;
using Assets.Scripts.Menu.ListView;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Ui.Flight.NetworkManager
{
    public class NodesDetails
    {
        private readonly DetailsPropertyPairScript _nodeTypeLocationText, _gsRangeText;
        private readonly List<DetailsButtonScript> _nodesInDirectRange, _nodesInRange, _antennas;
        private readonly DetailsWidgetGroup _nodesInDirectRangeGroup, _nodesInRangeGroup, _antennasGroup;
        private readonly DetailsHeaderScript _nodesInDirectRangeHeader, _nodesInRangeHeader, _antennasHeader;
        private readonly DetailsButtonScript _reportButton;

        public NodesDetails(ListViewDetailsScript listViewDetails)
        {
            _nodeTypeLocationText = listViewDetails.Widgets.AddPropertyPair("Node Type", "Location");
            _gsRangeText = listViewDetails.Widgets.AddPropertyPair("Ground Station In Range", "Relay Count");
            _reportButton = listViewDetails.Widgets.AddButton("Print Report");

            listViewDetails.Widgets.AddSpacer();

            _antennasHeader = listViewDetails.Widgets.AddHeader("Antennas");
            _antennasGroup = listViewDetails.Widgets.AddGroup();
            _nodesInDirectRangeHeader = listViewDetails.Widgets.AddHeader("Nodes in Direct range");
            _nodesInDirectRangeGroup = listViewDetails.Widgets.AddGroup();
            _nodesInRangeHeader = listViewDetails.Widgets.AddHeader("Nodes in Relay Range");
            _nodesInRangeGroup = listViewDetails.Widgets.AddGroup();
        }

        public void UpdateDetails(NetworkNode node)
        {
            _nodeTypeLocationText.LeftValueText = node.IsCraft ? "Craft" : "Ground Station";
            _nodeTypeLocationText.RightValueText = node.parent.Name;
            _gsRangeText.LeftValueText = DCLUtilities.YesNo(node.GsInRange);
            _gsRangeText.RightValueText = node.RelayCount.ToString();
            _reportButton.Clicked = (DetailsButtonScript b) => node.OnPrintReport();

            UpdateList<Antenna>(_antennas, node.Antennas, _antennasGroup);
            UpdateList<NetworkNode>(_nodesInDirectRange, node.objectNodesInDirectRange, _nodesInDirectRangeGroup);
            UpdateList<NetworkNode>(_nodesInRange, node.nodesInRelayRange, _nodesInRangeGroup);
        }

        private void UpdateList<T>(List<DetailsButtonScript> list, List<T> items, DetailsWidgetGroup group)
        {
            List<DetailsButtonScript> notUpdatedButtons = new(list);
            foreach (T item in items)
            {
                NetworkNode node = item as NetworkNode;
                NetworkConnection connection = item as NetworkConnection;
                string name = node != null ? node.id : connection.id;

                DetailsButtonScript button = list.Find(item => item.name == name);
                if (button != null)
                {
                    notUpdatedButtons.Remove(button);
                    continue;
                }

                list.Add(group.AddButton(name));
                list.Last().Clicked = (DetailsButtonScript b) => OnButtonClicked<T>(item);
            }

            foreach (DetailsButtonScript button in notUpdatedButtons)
            {
                list.Remove(button);
                button.DestroyWidget();
            }
        }

        private void OnButtonClicked<T>(T item)
        {
            NetworkNode node = item as NetworkNode;
            NetworkConnection connection = item as NetworkConnection;
            string name = node != null ? node.id : connection.id;
            Debug.Log("NetworkNode Button Clicked " + name);
        }

    }
}