using System.Collections.Generic;
using System.Xml.Linq;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.DroonComLinks.Antennas;
using HarmonyLib;
using ModApi;
using ModApi.Craft.Parts;
using ModApi.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;
using static Assets.Scripts.DroonComLinks.Antennas.AntennaTypes;

namespace Assets.Scripts.DroonComLinks.Ui.Designer
{
    public class AntennaGeneratorDialogController : DialogScript
    {
        private XmlLayout _xmlLayout;
        private XmlElement _inputTemplate;
        private XmlElement _inputParent => _inputTemplate.parentElement;
        private TextMeshProUGUI _partIdText;
        private TMP_Dropdown _typesSelector;
        private IPartScript _selectedPart => Game.Instance.Designer.SelectedPart;
        private readonly Dictionary<string, string> _properties = new();
        private readonly int _defaultHeight = 260;
        private int _height = 0;

        public static AntennaGeneratorDialogController Create()
        {
            AntennaGeneratorDialogController dialog = Game.Instance.UserInterface.CreateDialog<AntennaGeneratorDialogController>(Game.Instance.Designer.DesignerUi.Transform);
            Game.Instance.UserInterface.BuildUserInterfaceFromResource("Droon ComLinks/Designer/VanillaAntennaGeneratorDialog", dialog, delegate (IXmlLayoutController x)
            {
                dialog.OnLayoutRebuilt((XmlLayout)x.XmlLayout);
            });

            return dialog;
        }

        private void OnLayoutRebuilt(XmlLayout xmlLayout)
        {
            //Initialisation
            _xmlLayout = xmlLayout;
            _inputTemplate = _xmlLayout.GetElementById("input-field-template");
            _partIdText = _xmlLayout.GetElementById<TextMeshProUGUI>("selected-part-text");
            _typesSelector = _xmlLayout.GetElementById<TMP_Dropdown>("antenna-types-dropdown");
            foreach (IAntennaType type in AntennaTypes.types) _typesSelector.options.Add(new TMP_Dropdown.OptionData(type.id)); //Add all antenna types the the selector
            _typesSelector.onValueChanged.AddListener(delegate (int x) { UpdateUI(_typesSelector.options[x].text); }); //Listen for changes

            //OnSelectedPartChanged(null, _selectedPart);
            Game.Instance.Designer.DeselectPart();
            UpdateUI(_typesSelector.options[0].text);

            Game.Instance.Designer.SelectedPartChanged += OnSelectedPartChanged;
        }

        private void OnSelectedPartChanged(IPartScript oldPart, IPartScript newPart)
        {
            Game.Instance.Designer.ShowMessage("");
            if (newPart != null)
            {
                if (newPart.GetModifier<DCLAntennaScript>() == null)
                {
                    _partIdText.text = newPart.Data.Id.ToString();
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
                                XmlElement inputField = _xmlLayout.GetElementById(attribute);

                                if (inputField != null)
                                {
                                    inputField.GetComponentInChildren<TMP_InputField>().text = value;
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

        private void UpdateUI(string type)
        {
            //Reset everything
            _properties.Clear();
            List<XmlElement> elements = new(_inputParent.childElements);
            foreach (XmlElement element in elements) if (element != _inputTemplate) _inputParent.RemoveChildElement(element, true);
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

            //Dynamically update the height of the elements
            _inputParent.SetAndApplyAttribute("preferredHeight", _height.ToString());
            _inputParent.parentElement.SetAndApplyAttribute("preferredHeight", (_height + _defaultHeight).ToString());
            _inputParent.parentElement.parentElement.SetAndApplyAttribute("height", (_height + _defaultHeight + 30).ToString());
        }

        private void AddElment(string attribute, string value, bool fix = true, bool hidden = false)
        {
            if (ModSettings.Instance.DebugMode) Debug.Log("adding element " + attribute + " : " + value + " " + fix + " " + hidden);
            //Instantiate the new element
            XmlElement inputfield = GameObject.Instantiate(_inputTemplate);
            XmlElement component = inputfield.GetComponent<XmlElement>();

            //Setting the basic attributes
            component.Initialise(_xmlLayout, (RectTransform)_inputParent.transform, _inputTemplate.tagHandler);
            if (!fix) component.RemoveClass("fix");
            if (!hidden) component.ToggleVisibility();
            component.SetAttribute("id", attribute);
            component.ApplyAttributes();
            _inputParent.AddChildElement(component);

            //Setting the input field attributes
            TMP_InputField inputFieled = component.GetComponentInChildren<TMP_InputField>();
            component.GetComponentInChildren<TextMeshProUGUI>().SetText(Utilities.FormatCodeToDisplayName(attribute));
            inputFieled.onEndEdit.AddListener(delegate (string value) { OnInputFieldEdited(attribute, value); });
            inputFieled.text = value;

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
            if (_selectedPart.GetModifier<DCLAntennaScript>() == null)
            {
                string type = _typesSelector.options[_typesSelector.value].text;
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

        public override void Close()
        {
            base.Close();
            Game.Instance.Designer.SelectedPartChanged -= OnSelectedPartChanged;
            //gameObject.SetActive(value: false);
            GameObject.Destroy(gameObject);
        }
    }
}