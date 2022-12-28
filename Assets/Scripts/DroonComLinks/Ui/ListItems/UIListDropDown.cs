using System;
using System.Collections.Generic;
using Assets.Scripts.DroonComlinks.Ui;
using TMPro;
using UI.Xml;

namespace Assets.Scripts.DroonComLinks.Ui.ListItems
{
    public class UIListDropDown : IUIListItem
    {
        public string Id { get; private set; }
        public XmlElement template = new XmlElement();
        private TMP_Dropdown _dropDown;
        private XmlElement _component;
        public delegate void Del(string message);
        private string _label;
        private Func<IEnumerable<string>> ValueGetter;

        public UIListDropDown(string label, Func<IEnumerable<string>> valueGetter)
        {
            _label = label;
            ValueGetter = valueGetter;
            template = Mod.Instance.comLinksManager.flightUI.dropDownListItemTemplate;
        }

        public void AddTo(XmlElement parent, UIListItems.strDelegate OnInteracted)
        {
            string parentId;
            _component = UIListItems.InitiliseTemplate(template, parent, out parentId);
            _component.SetAttribute("id", GetId(parent));
            _component.ApplyAttributes();

            _dropDown = _component.GetComponentInChildren<TMP_Dropdown>();
            _dropDown.onValueChanged.AddListener(delegate (int x) { OnInteracted(UIListItems.dropDownId, parentId, _label, _dropDown.options[x].text); });

            //XmlElement dropdownElement = _component.GetElementByInternalId ("dropdown");
            //dropdownElement.AddOnMouseEnterEvent (delegate { dropdownElement.SetAndApplyAttribute ("width", "240%"); });
            //dropdownElement.AddOnMouseExitEvent (delegate { dropdownElement.SetAndApplyAttribute ("width", "100%"); });

            Update();
        }

        public void Update()
        {
            _dropDown.ClearOptions();
            foreach (string value in ValueGetter())
                AddOption(value, _dropDown);
        }

        private void AddOption(string text, TMP_Dropdown dropDown)
        {
            TMP_Dropdown.OptionData optionData = new TMP_Dropdown.OptionData(text);
            dropDown.options.Add(optionData);
        }

        public string GetId(XmlElement parent) => Id = UIListItems.dropDownId + "," + parent.id.Split(':')[0] + "," + _label;
        public string GetId(string parentId) => Id = UIListItems.dropDownId + "," + parentId + "," + _label;
    }
}