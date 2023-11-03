using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.DroonComlinks.Ui;
using Assets.Scripts.DroonComLinks.Network;
using Assets.Scripts.DroonComLinks.Ui.Flight.NetworkManager;
using Assets.Scripts.DroonComLinks.Ui.ListItems;
using ModApi.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Ui.Flight
{
    public class DCLFlightUIScript : MonoBehaviour
    {
        public ComLinksManager ComLinksManager => ComLinksManager.Instance;
        private readonly NetworkManagerDialog _networkMangerDialogScript;
        private XmlLayoutController _controller;
        private XmlLayout _xmlLayout;
        private XmlElement _statusPanel, _reducedStatusPanel, _statusIconsPanel;
        private TextMeshProUGUI _infoSubHeader;
        public XmlElement buttonListItemTemplate, textValueListItemTemplate, dropDownListItemTemplate, labelButtonListItemTemplate;
        private List<string> _connectedNodes, _playerAntenna;
        private List<string> NetworkNodes => ComLinksManager.NetworkNodes.Select(n => n.id).ToList();
        private List<string> NetworkConnections => ComLinksManager.NetworkConnections.Select(c => c.id).ToList();
        private bool _playerHasDroonControl;
        private bool _playerHasRemoteControl;
        private readonly bool reducedStatusPanelOpen;
        private readonly bool statusPanelOpen;
        private readonly bool inputs;
        private bool uiActive;
        public bool infoViewActive;
        private const string networkNodesList = "network-nodes", networkConnectionsList = "network-connections", listSufix = "-list";
        private const string infoTable = "info", globalInfoTable = "globalinfo", tableSuffix = "-table";
        private const string noObjectSelectedtext = "No object selected";
        private const string objectNullText = "The object you are trying to get info from doesn't exist anymore";
        private const string displayedInfoPrefix = "Selected Object: ";
        private readonly Dictionary<string, int> tablesWidth = new() { { infoTable, 2 }, { globalInfoTable, 4 } };
        private readonly Dictionary<string, IDisplayable> displayed = new() { { infoTable, null }, { globalInfoTable, null } };
        private IDisplayable Displayed(string table) => displayed[table];
        public IUIListItem EmptyListItem => new UIListTextValue<string>("---", () => "---", delegate (string T, string s, string t, string d) { OnListItemInteracted(T, s, t, d); });

        public void OnLayoutRebuilt(IXmlLayoutController xmlLayoutController)
        {
            _controller = (XmlLayoutController)xmlLayoutController;
            _xmlLayout = (XmlLayout)xmlLayoutController.XmlLayout;

            _statusPanel = _xmlLayout.GetElementById("status-panel");
            _reducedStatusPanel = _xmlLayout.GetElementById("reduced-status-panel");
            _statusIconsPanel = _xmlLayout.GetElementById("status-icons-panel");
            _infoSubHeader = _xmlLayout.GetElementById<TextMeshProUGUI>("info-sub-header");
            buttonListItemTemplate = _xmlLayout.GetElementById(UIListItems.buttonId);
            textValueListItemTemplate = _xmlLayout.GetElementById(UIListItems.textValueId);
            dropDownListItemTemplate = _xmlLayout.GetElementById(UIListItems.dropDownId);
            labelButtonListItemTemplate = _xmlLayout.GetElementById(UIListItems.labelButtonId);

            _infoSubHeader.SetText(noObjectSelectedtext);

            OnDroonControlChanged(ComLinksManager.PlayerHasDroonControl);
            OnRemoteControlChanged(ComLinksManager.PlayerHasRemoteControl);

            ComLinksManager.NetworkNodesUpdated += OnNetworkNodesUpdated;
            ComLinksManager.NetworkConnectionAdded += OnNetworkConnectionChanged;
            ComLinksManager.NetworkConnectionRemoved += OnNetworkConnectionChanged;
            ComLinksManager.DroonControlChanged += OnDroonControlChanged;
            ComLinksManager.RemoteControlChanged += OnRemoteControlChanged;
            _connectedNodes = _playerAntenna = new List<string>();
        }

        public void Update()
        {
            if (_controller != null)
            {
                if (Game.Instance.FlightScene.FlightSceneUI.Visible != uiActive) UpdateUI();
                UpdateTable(infoTable, displayed[infoTable] == null ? ComLinksManager.Player : Displayed(infoTable));
                UpdateTable(globalInfoTable, displayed[globalInfoTable]);
                if (ModSettings.Instance.BlockControls && !ComLinksManager.Instance.PlayerHasControl)
                    DCLUtilities.ShowMapViewMessage("CRAFT IS UNCONTROLLABLE", error:true, size:1.8f, bold:true);
            }
        }

        public void OnDroonControlChanged(bool droonControl)
        {
            if (_controller != null)
            {
                if (droonControl) Game.Instance.FlightScene.FlightSceneUI.ShowMessage("");
                if (droonControl != _playerHasDroonControl)
                {
                    _playerHasDroonControl = droonControl;
                    UpdateIcon("Drood", droonControl);
                }
            }
        }

        public void OnRemoteControlChanged(bool remoteControl)
        {
            if (_controller != null)
            {
                if (remoteControl) Game.Instance.FlightScene.FlightSceneUI.ShowMessage("");
                if (remoteControl != _playerHasRemoteControl)
                {
                    _playerHasRemoteControl = remoteControl;
                    UpdateIcon("Connection", remoteControl);
                }
            }
        }

        public void OnNetworkNodesUpdated()
        {
            if (statusPanelOpen) UpdateList(networkNodesList, NetworkNodes);
        }

        public void OnNetworkConnectionChanged(string id)
        {
            if (statusPanelOpen) UpdateList(networkConnectionsList, NetworkConnections);
        }

        private void UpdateUI()
        {
            uiActive = Game.Instance.FlightScene.FlightSceneUI.Visible;
            _statusPanel.SetAndApplyAttribute("active", (statusPanelOpen && uiActive).ToString());
            _reducedStatusPanel.SetAndApplyAttribute("active", (reducedStatusPanelOpen && uiActive).ToString());
            _statusIconsPanel.SetAndApplyAttribute("active", uiActive.ToString());

            if (uiActive && statusPanelOpen)
            {
                UpdateList(networkNodesList, NetworkNodes);
                UpdateList(networkConnectionsList, NetworkConnections);
                UpdateTable(infoTable, displayed[infoTable] == null ? ComLinksManager.Player : Displayed(infoTable));
                UpdateTable(globalInfoTable, ComLinksManager);
            }
        }

        private void UpdateList(string listName, List<string> items)
        {
            XmlElement scrollView = _xmlLayout.GetElementById(listName + listSufix);
            List<XmlElement> elements = new(scrollView.childElements);

            foreach (XmlElement element in elements)
                if (!items.Contains(element.id.Split(',')[2])) scrollView.RemoveChildElement(element, true);

            foreach (string item in items)
                if (_xmlLayout.GetElementById(UIListItems.GetButtonId(listName + listSufix, item)) == null)
                    new UIListButton(item, delegate (string T, string s, string t, string d) { OnListItemInteracted(T, s, t, d); }).AddTo(scrollView);

        }

        private void UpdateTable(string tableName, IDisplayable obj)
        {
            if (obj == null)
            {
                if (tableName == infoTable)
                {
                    _infoSubHeader.SetText(objectNullText);
                    for (int c = 0; c < tablesWidth[tableName]; c++)
                    {
                        XmlElement column = _xmlLayout.GetElementById(tableName + tableSuffix + ":" + c);
                        List<XmlElement> elements = new(column.childElements);
                        foreach (XmlElement element in elements)
                            column.RemoveChildElement(element, true);

                    }
                }
            }
            else if (Displayed(tableName) != null && obj == Displayed(tableName))
            {
                IUIListItem[,] items = obj.GetInfo();
                for (int x = 0; x < items.GetLength(0); x++)
                    for (int y = 0; y < items.GetLength(1); y++)
                        if (items[x, y] != null) items[x, y].Update();
            }
            else
            {
                if (tableName == infoTable) _infoSubHeader.SetText(displayedInfoPrefix + obj.id.Replace("\n", " / "));

                IUIListItem[,] items = obj.GetInfo();
                int columnCount = items.GetLength(0);
                int lineCount = items.GetLength(1);

                if ((tablesWidth[tableName] == columnCount))
                    for (int c = 0; c < columnCount; c++)
                    {
                        XmlElement column = _xmlLayout.GetElementById(tableName + tableSuffix + ":" + c);
                        List<XmlElement> elements = new(column.childElements);

                        foreach (XmlElement element in elements)
                            column.RemoveChildElement(element, true);

                        for (int l = 0; l < lineCount; l++)
                        {
                            IUIListItem item = items[c, l];
                            if (item != null) item.AddTo(column);
                            else EmptyListItem.AddTo(column);
                        }
                    }
                displayed[tableName] = obj;
            }
        }

        private void UpdateIcon(string iconId, bool active)
        { // Types: Drood, Connection, PanelDrood, PanelConnection
            XmlElement icon = _xmlLayout.GetElementById(iconId);

            if (active) icon.SetAndApplyAttribute("color", "SuccessHover");
            else icon.SetAndApplyAttribute("color", "DangerHover");
        }

        public void OnListItemInteracted(string itemType, string source, string text, string data)
        {
            if (ModSettings.Instance.DebugMode) Debug.Log("Received list interacted event of type [" + itemType + "] from [" + source + "] text: [" + text + "] data:[" + data + "]");

            IDisplayable cast = null;
            try { cast = ComLinksManager.GetNetworkNodeFromId(data); } catch { }
            if (cast == null) try { cast = ComLinksManager.GetNetworkConnectionFromId(data); } catch { }
            if (cast == null) try { cast = ((NetworkNode)Displayed(infoTable)).GetAntennaFromId(data); } catch { }
            UpdateTable(infoTable, cast);
        }

        public void OnToggleNetworkStatus()
        {
            // statusPanelOpen = !statusPanelOpen;
            // UpdateUI();

            DCLUIManager.OnOpenNetworkInfoButtonClicked();
            // _networkMangerDialogScript = (Instantiate(Resources.Load("Ui/Prefabs/Dialog")) as GameObject).AddComponent<NetworkManagerDialog>();
            // _networkMangerDialogScript.Initialize();
            // _networkMangerDialogScript.transform.SetParent(transform, worldPositionStays: false);
            // _networkMangerDialogScript.Closed += delegate { _networkMangerDialogScript = null; };
        }

        public void Close()
        {
            _controller = null;
            ComLinksManager.NetworkNodesUpdated -= OnNetworkNodesUpdated;
            ComLinksManager.NetworkConnectionAdded -= OnNetworkConnectionChanged;
            ComLinksManager.NetworkConnectionRemoved -= OnNetworkConnectionChanged;
            ComLinksManager.DroonControlChanged -= OnDroonControlChanged;
            ComLinksManager.RemoteControlChanged -= OnRemoteControlChanged;

            _xmlLayout.Hide(() => GameObject.Destroy(this.gameObject), true);
        }
    }
}