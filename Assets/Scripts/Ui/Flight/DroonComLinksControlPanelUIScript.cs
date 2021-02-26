using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.DroonComLinks;
using Assets.Scripts.Input;
using ModApi.Common;
using ModApi.Input;
using ModApi.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;

namespace Assets.Scripts.Ui.Flight {
    public class DroonComLinksControlPanelUIScript : MonoBehaviour {
        private GameInputs gameInputs;
        private XmlLayoutController _controller;
        private ComLinksManager _ComLinksManager;
        private XmlLayout _xmlLayout;
        private XmlElement _mainPanel;
        private XmlElement _statusPanel;
        private String DisplayInfo;
        private List<String> connectedNodes => _ComLinksManager.connectedNodes;
        private List<String> playerAntennas => _ComLinksManager.playerAntennas;
        private List<String> _connectedNodes = new List<String> ();
        private List<String> _playerAntenna = new List<String> ();
        private bool _playerHasDroonControl;
        private bool _playerHasRemoteControl;
        private bool open;
        private bool inputs;
        private bool UIActive;

        public void OnLayoutRebuilt (IXmlLayoutController xmlLayoutController, ComLinksManager manager) {
            Debug.Log ("Layoutrebuilt");
            _ComLinksManager = manager;
            _controller = (XmlLayoutController) xmlLayoutController;
            _xmlLayout = (XmlLayout) xmlLayoutController.XmlLayout;
            _mainPanel = _xmlLayout.GetElementById ("MainPanel");
            _statusPanel = _xmlLayout.GetElementById ("StatusPanel");
            _mainPanel.SetAndApplyAttribute ("active", (open && UIActive).ToString ());
        }
        public void Update () {
            if (_controller != null) {
                if (open && UIActive) {
                    if (connectedNodes != null && _connectedNodes != connectedNodes) {
                        _connectedNodes = connectedNodes;
                        UpdateList ("InRange", _connectedNodes);
                    }
                    if (playerAntennas != null && _playerAntenna != playerAntennas) {
                        _playerAntenna = playerAntennas;
                        UpdateList ("Antennas", _playerAntenna);
                    }
                }
                if (_ComLinksManager.playerHasDroonControl != _playerHasDroonControl) {
                    _playerHasDroonControl = _ComLinksManager.playerHasDroonControl;
                    UpdateIcon ("Drood", _playerHasDroonControl);
                }
                if (Game.Instance.FlightScene.FlightSceneUI.Visible != UIActive) {
                    UIActive = Game.Instance.FlightScene.FlightSceneUI.Visible;
                    _mainPanel.SetAndApplyAttribute ("active", (open && UIActive).ToString ());
                    _statusPanel.SetAndApplyAttribute ("active", (UIActive).ToString ());
                }
            }
        }
        public void UpdateList (String List, List<String> ListItems) { // Lists: InRange, Antennas
            XmlElement scrollView = _xmlLayout.GetElementById (List + "List");
            XmlElement ListItemTemplate = _xmlLayout.GetElementById (List + "template");

            foreach (String item in ListItems) {
                if (_xmlLayout.GetElementById (List + item) == null) {

                    //Debug.Log ("Adding New item to " + List + " list : " + item);

                    XmlElement ListItem = GameObject.Instantiate (ListItemTemplate);
                    ListItem.name = List + item;
                    XmlElement component = ListItem.GetComponent<XmlElement> ();

                    component.Initialise (_xmlLayout, (RectTransform) ListItem.transform, ListItemTemplate.tagHandler);
                    scrollView.AddChildElement (component);

                    component.SetAttribute ("active", "true");
                    component.SetAttribute ("id", List + item);
                    component.GetElementByInternalId<TextMeshProUGUI> (List + "Name").text = item;
                    component.SetAttribute ("onClick", "On" + List + "ListItemClicked(" + item + ");");
                    component.ApplyAttributes ();
                }
            }
        }

        private void UpdateIcon (String Type, bool On) { // Types: Drood, Connection, PanelDrood, PanelConnection
            if (Type != "Connection") {
                XmlElement Icon = _xmlLayout.GetElementById (Type);
                XmlElement IconCross = _xmlLayout.GetElementById (Type + "Cross");

                if (On) {
                    Icon.SetAndApplyAttribute ("color", "SuccessHover");
                    IconCross.SetAndApplyAttribute ("active", "false");
                } else {
                    Icon.SetAndApplyAttribute ("color", "DangerHover");
                    IconCross.SetAndApplyAttribute ("active", "true");
                }
                if (open) UpdateIcon ("Panel" + Type, On);
            } else UpdateConnection ();
        }

        private void UpdateConnection () {

        }
        private void DisplayInfoOf (String InfoType, String info) {

        }
        public void Close () {
            _xmlLayout.Hide (() => GameObject.Destroy (this.gameObject), true);
            _controller = null;
        }
        public void OnTogglePanelButtonClicked () {
            open = !open;
            _mainPanel.SetAndApplyAttribute ("active", (open && UIActive).ToString ());
            if (open && UIActive) UpdateIcon ("Drood", _playerHasDroonControl);
        }
        private void OnInRangeListItemClicked (String item) {
            DisplayInfo = item;
            DisplayInfoOf ("InRange", item);
            Debug.Log ("InRange List ItemClicked: " + item);
        }
        private void OnAntennasListItemClicked (String item) {
            DisplayInfo = item;
            DisplayInfoOf ("Antennas", item);
            Debug.Log ("Antennas List ItemClicked: " + item);
        }

        private void OnTestButtonClicked () {
            Game.Instance.FlightScene.FlightSceneUI.ShowMessage ("Test Button Working", 12f);
        }
        private void OnNetToggleButtonClicked () {
            gameInputs = (GameInputs) Game.Instance.Inputs;
            foreach (IGameInput input in gameInputs.AllInputs) {
                input.Enabled = inputs;
            }
            inputs = !inputs;
        }
    }
}