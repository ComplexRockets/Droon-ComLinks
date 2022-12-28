using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.DroonComLinks.Ui.Designer;
using Assets.Scripts.DroonComLinks.Ui.Flight;
using Assets.Scripts.DroonComLinks.Network;
using ModApi.Ui;
using UI.Xml;

namespace Assets.Scripts.DroonComlinks.Ui
{
    public class DCLUIManager
    {

        public static DCLFlightUIScript flightUI;
        public static AntennaGeneratorDialogController antennaGenerator;
        private static ComLinksManager _comLinksManager => Mod.Instance.comLinksManager;
        private static IUserInterface ui => Game.Instance.UserInterface;
        private const string _designerButtonId = "DCL-button";
        private static XmlElement _flyoutButton;

        public static void InitializeFlightUI() => flightUI = ui.BuildUserInterfaceFromResource<DCLFlightUIScript>(
            "Droon ComLinks/Flight/DroonComLinksControlPanel",
            (script, controller) => script.OnLayoutRebuilt(controller));

        public static void InitialiseDesignerUI() => ui.AddBuildUserInterfaceXmlAction(UserInterfaceIds.Design.DesignerUi, OnBuildDesignerUI);

        private static void OnDesignerButtonClicked()
        {
            if (antennaGenerator == null) antennaGenerator = AntennaGeneratorDialogController.Create();
            else { antennaGenerator.Close(); antennaGenerator = null; }
        }

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
                    new XAttribute("tooltip", "Droon ComLinks Tools"),
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
        }

        public static void OnOpenPanelButtonClicked()
        {
            if (flightUI == null) flightUI = ui.BuildUserInterfaceFromResource<DCLFlightUIScript>(
                "Droon ComLinks/Flight/DroonComLinksControlPanel",
                (script, controller) => script.OnLayoutRebuilt(controller));

            flightUI.OnToggleReducedStatus();
        }

        public static void Update()
        {
            if (flightUI != null) flightUI.Update();
        }

        public static void CloseFlightUI()
        {
            if (flightUI != null)
            {
                flightUI.Close();
                flightUI = null;
            }
        }
    }
}