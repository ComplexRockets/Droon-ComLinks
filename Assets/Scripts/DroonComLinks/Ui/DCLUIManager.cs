using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.DroonComLinks.Ui.Flight;
using Assets.Scripts.DroonComLinks.Network;
using ModApi.Ui;
using UI.Xml;
using Assets.Scripts.Ui.Designer;
using Assets.Scripts.Design;
using System;

namespace Assets.Scripts.DroonComlinks.Ui
{
    public class DCLUIManager
    {

        public static DCLFlightUIScript flightUI;
        public static DCLFlyout dclFlyout;
        public static ComLinksManager ComLinksManager => ComLinksManager.Instance;
        public static DesignerScript Designer => (DesignerScript)Game.Instance.Designer;
        private static IUserInterface UI => Game.Instance.UserInterface;
        private static XmlElement _flyoutButton;
        public static NetworkInfofView infoView;
        private const string _designerButtonId = "DCL-button";
        public static bool FlyoutOpened => dclFlyout != null;

        public static void InitializeFlightUI()
        {
            flightUI = UI.BuildUserInterfaceFromResource<DCLFlightUIScript>("Droon ComLinks/Flight/DroonComLinksControlPanel", (script, controller) => script.OnLayoutRebuilt(controller));
            infoView = new NetworkInfofView();
        }

        public static void InitialiseDesignerUI() => UI.AddBuildUserInterfaceXmlAction(UserInterfaceIds.Design.DesignerUi, OnBuildDesignerUI);

        private static void OnBuildDesignerUI(BuildUserInterfaceXmlRequest request)
        {
            var ns = XmlLayoutConstants.XmlNamespace;
            var viewButton = request.XmlDocument
                .Descendants(ns + "Panel")
                .First(x => (string)x.Attribute("internalId") == "flyout-view");

            viewButton.Parent.Add(
                new XElement(
                    ns + "Panel",
                    new XAttribute("id", _designerButtonId),
                    new XAttribute("class", "toggle-button audio-btn-click"),
                    new XAttribute("name", "ButtonPanel.DroonComLinksUI"),
                    new XAttribute("tooltip", "Droon ComLinks"),
                    new XElement(
                        ns + "Image",
                        new XAttribute("class", "toggle-button-icon"),
                        new XAttribute("sprite", "Droon ComLinks/Sprites/DCLLogoBlured"))));

            request.AddOnLayoutRebuiltAction(xmlLayoutController =>
            {
                var button = xmlLayoutController.XmlLayout.GetElementById(_designerButtonId);
                _flyoutButton = (XmlElement)button;
                button.AddOnClickEvent(OnDesignerButtonClicked);
            });

            Designer.DesignerUi.SelectedFlyoutChanged += SelectedFlyoutChanged;
        }

        private static void SelectedFlyoutChanged(IFlyout flyout)
        {
            if (flyout != null && FlyoutOpened && flyout.Title != dclFlyout.flyout.Title) CloseDesignerFlyout();
        }

        private static void OnDesignerButtonClicked()
        {
            if (FlyoutOpened)
            {
                Designer.DesignerUi.SelectedFlyout = null;
                CloseDesignerFlyout();
            }
            else
            {
                var ui = Game.Instance.UserInterface;
                dclFlyout = ui.BuildUserInterfaceFromResource<DCLFlyout>("Droon ComLinks/Designer/DCLFlyout", (script, controller) => script.OnLayoutRebuilt(controller));
                _flyoutButton.AddClass("toggle-button-toggled");
                Designer.DesignerUi.SelectedFlyout = dclFlyout.flyout;
            }
        }

        public static void OnOpenControlPanelButtonClicked()
        {
            // if (flightUI == null) flightUI = ui.BuildUserInterfaceFromResource<DCLFlightUIScript>(
            //     "Droon ComLinks/Flight/DroonComLinksControlPanel",
            //     (script, controller) => script.OnLayoutRebuilt(controller));

            flightUI.OnToggleNetworkStatus();
        }

        public static void OnOpenNetworkInfoButtonClicked() => infoView?.OnToggleNetworkInfoPanel();

        public static void Update()
        {
            flightUI?.Update();
            infoView?.Update();
        }

        public static void CloseFlightUI()
        {
            flightUI?.Close();
            infoView?.Close();
            flightUI = null;
            infoView = null;
        }

        public static void CloseDesignerFlyout()
        {
            dclFlyout.Close();
            dclFlyout = null;
            _flyoutButton.RemoveClass("toggle-button-toggled");
        }
    }
}