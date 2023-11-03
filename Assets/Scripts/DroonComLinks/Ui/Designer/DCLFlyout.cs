using System.Collections.Generic;
using System.Xml.Linq;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Design;
using Assets.Scripts.DroonComlinks.Ui;
using Assets.Scripts.DroonComLinks.Antennas;
using HarmonyLib;
using ModApi;
using ModApi.Craft.Parts;
using ModApi.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;
using UnityEngine.UI;
using static Assets.Scripts.DroonComLinks.Antennas.AntennaTypes;

namespace Assets.Scripts.Ui.Designer
{
    public class DCLFlyout : MonoBehaviour
    {

        public FlyoutScript flyout = new();
        private DesignerScript _designer => (DesignerScript)Game.Instance.Designer;
        private readonly Mod _mod = Mod.Instance;
        private IXmlLayoutController _controller;
        public XmlLayout xmlLayout { get; private set; }
        private XmlElement _flyoutElement;
        private XmlElement _inputTemplate;
        private XmlElement _inputParent => _inputTemplate.parentElement;
        private TextMeshProUGUI _partIdText;
        private TMP_Dropdown _typesSelector;
        private IPartScript _selectedPart => Game.Instance.Designer.SelectedPart;
        private readonly Dictionary<string, string> _properties = new();
        private readonly string _inputTemplateID = "input-field-template";
        private int _height = 0;

        public void OnLayoutRebuilt(IXmlLayoutController xmlLayoutController)
        {
            //Initialisation
            _controller = xmlLayoutController;
            xmlLayout = (XmlLayout)_controller.XmlLayout;

            _flyoutElement = xmlLayout.GetElementById("flyout-dcl");
            _inputTemplate = xmlLayout.GetElementById(_inputTemplateID);
            _partIdText = xmlLayout.GetElementById<TextMeshProUGUI>("selected-part-text");
            _typesSelector = xmlLayout.GetElementById<TMP_Dropdown>("antenna-types-dropdown");

            foreach (IAntennaType type in AntennaTypes.types) _typesSelector.options.Add(new TMP_Dropdown.OptionData(type.name.ToString())); //Add all antenna types the the selector
            _typesSelector.onValueChanged.AddListener(delegate (int x) { UpdateUI(_typesSelector.options[x].text); }); //Listen for changes

            Game.Instance.Designer.DeselectPart();
            UpdateUI(_typesSelector.options[0].text);

            Game.Instance.Designer.SelectedPartChanged += OnSelectedPartChanged;

            flyout.Initialize(_flyoutElement);
            flyout.Open();
        }

        private void OnSelectedPartChanged(IPartScript oldPart, IPartScript newPart)
        {
            Game.Instance.Designer.ShowMessage("");
            if (newPart != null)
            {
                if (newPart.GetModifier<DCLAntennaScript>() == null)
                {
                    _partIdText.text = newPart.Data.Id.ToString();
                    Debug.Log(newPart.Data.Name);
                    if (newPart.Data.Name.StartsWith(AntennaTypes.dclAntennaPrefix))
                    {
                        CustomAntennaResult antennaData = AntennaTypes.ParseCustomAntennaName(newPart.Data.Name);

                        if (antennaData.error == "")
                        {
                            //UpdateUI(antennaData.type.id);
                            _typesSelector.value = AntennaTypes.types.IndexOf(AntennaTypes.GetType(antennaData.type.id));

                            foreach (string attribute in antennaData.properties.Keys)
                            {
                                string value = antennaData.properties[attribute];
                                TMP_InputField inputField = xmlLayout.GetElementById<TMP_InputField>(attribute);

                                if (inputField != null)
                                {
                                    inputField.text = value;
                                    _properties[attribute] = value;
                                }
                            }

                            Game.Instance.Designer.ShowMessage("Part " + _selectedPart.Data.Id + " is an antenna of type " + antennaData.type.id);
                        }
                        else Debug.LogError("Error when parsing custom antenna data: " + antennaData.error);
                    }
                    else UpdateUI(_typesSelector.options[_typesSelector.value].text);
                }
                else
                {
                    _partIdText.text = "invalid";
                    Game.Instance.Designer.ShowMessage("The selected part is a already DCL antenna, can't generate a custom antenna");
                }
            }
            else
            {
                _partIdText.text = "none";
                UpdateUI(_typesSelector.options[_typesSelector.value].text);
            }

        }

        private void UpdateUI(string typeName)
        {
            string type = AntennaTypes.GetTypeFromName(typeName);

            //Reset everything
            _properties.Clear();
            List<XmlElement> elements = new(_inputParent.childElements);
            foreach (XmlElement element in elements) if (element.id != _inputTemplateID) _inputParent.RemoveChildElement(element, true);
            _height = 0;

            //Add the common attributes to all types
            AddElment("customAntenna", "true", hidden: true);
            foreach (XAttribute attribute in AntennaTypes.defaultDCLModiferXml.Attributes())
            {
                string name = attribute.Name.LocalName;
                if (name != "customAntenna") AddElment(name, attribute.Value, fix: false);
            }

            //Add all the attributes from the selected antenna type
            foreach (XAttribute attribute in AntennaTypes.GetType(type).defaultModfierXml.Attributes())
                AddElment(attribute.Name.LocalName, attribute.Value, fix: false);

            //Update the height of the elements
            _inputParent.SetAndApplyAttribute("preferredHeight", _height.ToString());
        }

        private void AddElment(string attribute, string value, bool fix = true, bool hidden = false)
        {
            if (ModSettings.Instance.DebugMode) Debug.Log("adding element " + attribute + " : " + value + " " + fix + " " + hidden);
            //Instantiate the new element
            XmlElement inputfield = GameObject.Instantiate(_inputTemplate);
            XmlElement component = inputfield.GetComponent<XmlElement>();

            //Setting the basic attributes
            component.Initialise(xmlLayout, (RectTransform)_inputParent.transform, _inputTemplate.tagHandler);
            if (!fix) component.RemoveClass("fix");
            if (!hidden) component.ToggleVisibility();
            component.SetAttribute("id", attribute);
            component.ApplyAttributes();
            _inputParent.AddChildElement(component);

            //Setting the input field attributes
            TMP_InputField inputField = component.GetComponentInChildren<TMP_InputField>();
            component.GetComponentInChildren<TextMeshProUGUI>().SetText(Utilities.FormatCodeToDisplayName(attribute));
            inputField.onEndEdit.AddListener(delegate (string value) { OnInputFieldEdited(attribute, value); });
            inputField.text = value;

            _height += 30;
            _properties[attribute] = value;
        }

        private void OnInputFieldEdited(string attribute, string value)
        {
            if (ModSettings.Instance.DebugMode) Debug.Log("Attribute " + attribute + " edited " + value);
            _properties[attribute] = value;
        }

        private void OnGenerateAntenna()
        {
            if (!Game.InDesignerScene) return;

            if (_selectedPart.GetModifier<DCLAntennaScript>() == null)
            {
                string type = AntennaTypes.GetTypeFromName(_typesSelector.options[_typesSelector.value].text);
                _selectedPart.Data.Name = AntennaTypes.GenerateCustomAntennaName(type, _properties);
                if (!_selectedPart.Data.Config.SupportsActivation)
                {
                    Traverse.Create(_selectedPart.Data.Config).Field("_supportsActivation").SetValue(true);
                    _selectedPart.Data.Activated = true;
                }
                _selectedPart.OnCraftStructureChanged();
                Game.Instance.Designer.ShowMessage("Generated Antenna\nPart " + _selectedPart.Data.Id + " is now an antenna of type " + type);

            }
            else
            {
                Game.Instance.Designer.ShowMessage("The selected part is a already DCL antenna, can't generate a custom antenna");
            }
        }

        public void OnFlyoutCloseButtonClicked() => DCLUIManager.CloseDesignerFlyout();

        public void Close()
        {
            Game.Instance.Designer.SelectedPartChanged -= OnSelectedPartChanged;
            xmlLayout.Hide(() => Destroy(this.gameObject), true);
        }
    }
}
