using Assets.Scripts.DroonComLinks;
using UnityEngine;

namespace Assets.Scripts.Ui.Flight
{
    public class droonComLinksUI : MonoBehaviour {

        private static DroonComLinksControlPanelUIScript _controlPanel;
        private static ComLinksManager _manager;

        public static void InitializeDCLUI (ComLinksManager manager) {
            var ui = Game.Instance.UserInterface;
            _manager = manager;
            _controlPanel = ui.BuildUserInterfaceFromResource<DroonComLinksControlPanelUIScript> ("Droon ComLinks/Flight/DroonComLinksControlPanel", (script, controller) => script.OnLayoutRebuilt (controller, _manager));
        }

        public static void OnOpenPanelButtonClicked () {
            if (_controlPanel != null) _controlPanel.OnTogglePanelButtonClicked ();
            else {
                var ui = Game.Instance.UserInterface;
                _controlPanel = ui.BuildUserInterfaceFromResource<DroonComLinksControlPanelUIScript> ("Droon ComLinks/Flight/DroonComLinksControlPanel", (script, controller) => script.OnLayoutRebuilt (controller, _manager));
            }
        }

        public static void Update () {
            _controlPanel.Update ();
        }

        public static void Close () {
            if (_controlPanel != null) {
                _controlPanel.Close ();
                _controlPanel = null;
            }
        }
    }
}