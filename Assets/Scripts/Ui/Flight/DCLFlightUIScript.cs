using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.DroonComLinks;
using Assets.Scripts.DroonComLinks.Interfaces;
using Assets.Scripts.DroonComLinks.Objects;
using Assets.Scripts.DroonComLinks.Objects.Antennas;
using Assets.Scripts.DroonComLinks.Objects.ListItems;
using Assets.Scripts.Flight.UI;
using Assets.Scripts.Input;
using HarmonyLib;
using ModApi.Flight.UI;
using ModApi.Input;
using ModApi.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;
using static Assets.Scripts.Flight.UI.FlightSceneUiController;

namespace Assets.Scripts.Ui.Flight
{
    public class DCLFlightUIScript : MonoBehaviour
    {
        private GameInputs gameInputs;
        private ComLinksManager _comLinksManager => Mod.Instance.comLinksManager;
        private XmlLayoutController _controller;
        private XmlLayout _xmlLayout;
        private XmlElement _statusPanel, _reducedStatusPanel, _statusIconsPanel;
        private TextMeshProUGUI _infoSubHeader;
        public XmlElement buttonListItemTemplate, textValueListItemTemplate, dropDownListItemTemplate, labelButtonListItemTemplate;
        private List<string> _connectedNodes, _playerAntenna, _networkNodes, _networkConnections;
        private bool _playerHasDroonControl, _playerHasRemoteControl, reducedStatusPanelOpen, statusPanelOpen, inputs;
        private bool uiActive;
        private static string _inRangeList = "inrange", _antennasList = "antennas", networkNodesList = "network-nodes", networkConnectionsList = "network-connections", listSufix = "-list";
        private static string infoTable = "info", globalInfoTable = "globalinfo", tableSuffix = "-table";
        private static string noObjectSelectedtext = "No object selected";
        private static string objectNullText = "The object you are trying to get info from doesn't exist anymore";
        private static string displayedInfoPrefix = "Selected Object: ";
        private Dictionary<string, int> tablesWidth = new Dictionary<string, int> { { infoTable, 2 }, { globalInfoTable, 4 } };
        private Dictionary<string, IDisplayable> displayed = new Dictionary<string, IDisplayable> { { infoTable, null }, { globalInfoTable, null } };
        private IDisplayable Displayed(string table) => displayed[table];
        private IInputResponder _inputResponder;

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

            OnDroonControlChanged(_comLinksManager.playerHasDroonControl);
            OnRemoteControlChanged(_comLinksManager.playerHasRemoteControl);

            _comLinksManager.NetworkNodesUpdated += OnNetworkNodesUpdated;
            _comLinksManager.NetworkConnectionAdded += OnNetworkConnectionChanged;
            _comLinksManager.NetworkConnectionRemoved += OnNetworkConnectionChanged;
            _comLinksManager.DroonControlChanged += OnDroonControlChanged;
            _comLinksManager.RemoteControlChanged += OnRemoteControlChanged;
            _connectedNodes = _playerAntenna = _networkNodes = _networkConnections = new List<string>();
        }

        public void Update()
        {
            if (_controller != null)
            {
                if (Game.Instance.FlightScene.FlightSceneUI.Visible != uiActive) UpdateUI();
                UpdateTable(infoTable, displayed[infoTable] == null ? _comLinksManager.player : Displayed(infoTable));
                UpdateTable(globalInfoTable, displayed[globalInfoTable]);
                if (ModSettings.Instance.blockControls && !Mod.Instance.comLinksManager.playerHasControl)
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage("<color=#b33e46FF><b><size=180%> CRAFT IS UNCONTROLLABLE");
            }
        }

        public void OnDroonControlChanged(bool droonControl)
        {
            if (_controller != null)
            {
                if (droonControl) Game.Instance.FlightScene.FlightSceneUI.ShowMessage("");
                if (!droonControl) OverrideInputs();
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
                if (!remoteControl) OverrideInputs();
                if (remoteControl != _playerHasRemoteControl)
                {
                    _playerHasRemoteControl = remoteControl;
                    UpdateIcon("Connection", remoteControl);
                }
            }
        }

        private void OverrideInputs()
        {
            List<InspectorPanelWrapper> partInspectorPanels = (List<InspectorPanelWrapper>)Traverse.Create(((FlightSceneInterfaceScript)Game.Instance.FlightScene.FlightSceneUI).UiController).Field("_partInspectorPanels").GetValue();
            foreach (InspectorPanelWrapper inspectorPanel in partInspectorPanels)
            {
            }
        }

        public void OnNetworkNodesUpdated()
        {
            _networkNodes = _comLinksManager.networkNodeFromId.Keys.ToList();
            if (statusPanelOpen) UpdateList(networkNodesList, _networkNodes);
        }

        public void OnNetworkConnectionChanged(string id)
        {
            _networkConnections = _comLinksManager.NetworkConnectionFromId.Keys.ToList();
            if (statusPanelOpen) UpdateList(networkConnectionsList, _networkConnections);
        }

        private void UpdateUI()
        {
            uiActive = Game.Instance.FlightScene.FlightSceneUI.Visible;
            _statusPanel.SetAndApplyAttribute("active", (statusPanelOpen && uiActive).ToString());
            _reducedStatusPanel.SetAndApplyAttribute("active", (reducedStatusPanelOpen && uiActive).ToString());
            _statusIconsPanel.SetAndApplyAttribute("active", (uiActive).ToString());

            if (uiActive && statusPanelOpen)
            {
                _networkConnections = _comLinksManager.NetworkConnectionFromId.Keys.ToList();
                _networkNodes = _comLinksManager.networkNodeFromId.Keys.ToList();

                UpdateList(networkNodesList, _networkNodes);
                UpdateList(networkConnectionsList, _networkConnections);
                UpdateTable(infoTable, displayed[infoTable] == null ? _comLinksManager.player : Displayed(infoTable));
                UpdateTable(globalInfoTable, _comLinksManager);
            }
        }

        public void UpdateList(string listName, List<string> items)
        {
            XmlElement scrollView = _xmlLayout.GetElementById(listName + listSufix);
            List<XmlElement> elements = new List<XmlElement>(scrollView.childElements);

            foreach (XmlElement element in elements)
                if (!items.Contains(element.id.Split(',')[2])) scrollView.RemoveChildElement(element, true);

            foreach (string item in items)
                if (_xmlLayout.GetElementById(UIListItems.GetButtonId(listName + listSufix, item)) == null)
                    new UIListButton(item).AddTo(scrollView, delegate (string T, string s, string t, string d) { OnListItemInteracted(T, s, t, d); });

        }

        public void UpdateTable(string tableName, IDisplayable obj)
        {
            if (obj == null)
            {
                if (tableName == infoTable)
                {
                    _infoSubHeader.SetText(objectNullText);
                    for (int c = 0; c < tablesWidth[tableName]; c++)
                    {
                        XmlElement column = _xmlLayout.GetElementById(tableName + tableSuffix + ":" + c);
                        List<XmlElement> elements = new List<XmlElement>(column.childElements);
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
                        List<XmlElement> elements = new List<XmlElement>(column.childElements);

                        foreach (XmlElement element in elements)
                            column.RemoveChildElement(element, true);

                        for (int l = 0; l < lineCount; l++)
                        {
                            IUIListItem item = items[c, l];
                            if (item != null) item.AddTo(column, delegate (string T, string s, string t, string d) { OnListItemInteracted(T, s, t, d); });
                            else UIListItems.emptyListItem.AddTo(column, delegate (string T, string s, string t, string d) { OnListItemInteracted(T, s, t, d); });
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

        private void UpdateConnection() { }

        private void OnListItemInteracted(string itemType, string source, string text, string data)
        {
            if (ModSettings.Instance.debugMode) Debug.Log("Received list interacted event of type [" + itemType + "] from [" + source + "] text: [" + text + "] data:[" + data + "]");

            if (text == "Antenna Xml")
            {
                ModApi.Ui.MessageDialogScript dialog = Game.Instance.UserInterface.CreateMessageDialog(((Antenna)Displayed(infoTable)).antennaModifier + "\n" + ((Antenna)Displayed(infoTable)).typeModifier);
                dialog.ExtraWide = true;
            }
            else
            {
                IDisplayable cast = null;
                try { cast = _comLinksManager.networkNodeFromId[data]; } catch { }
                if (cast == null) try { cast = _comLinksManager.NetworkConnectionFromId[data]; } catch { }
                if (cast == null) try { cast = ((NetworkNode)Displayed(infoTable)).antennaFromId[data]; } catch { }
                UpdateTable(infoTable, cast);
            }
        }

        public void OnToggleReducedStatus()
        {
            statusPanelOpen = !statusPanelOpen;
            UpdateUI();
        }

        private void OnToggleStatus()
        {
            statusPanelOpen = !statusPanelOpen;
            UpdateUI();
        }

        private void OnTestButtonClicked()
        {
            Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Test Button Working", duration: 12f);
        }

        private void ToggleKeyboardInputs()
        {
            gameInputs = (GameInputs)Game.Instance.Inputs;
            foreach (IGameInput input in gameInputs.AllInputs)
            {
                input.Enabled = inputs;
            }
            inputs = !inputs;
        }

        public void Close()
        {
            _controller = null;
            _comLinksManager.NetworkNodesUpdated -= OnNetworkNodesUpdated;
            _comLinksManager.NetworkConnectionAdded -= OnNetworkConnectionChanged;
            _comLinksManager.NetworkConnectionRemoved -= OnNetworkConnectionChanged;
            _comLinksManager.DroonControlChanged -= OnDroonControlChanged;
            _comLinksManager.RemoteControlChanged -= OnRemoteControlChanged;

            _xmlLayout.Hide(() => GameObject.Destroy(this.gameObject), true);
        }
    }
}