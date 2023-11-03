using System;
using System.Collections.Generic;
using Assets.Scripts.DroonComlinks.Ui;
using Assets.Scripts.DroonComLinks.Network;
using TMPro;
using UI.Xml;

namespace Assets.Scripts.DroonComLinks.Ui.ListItems
{
    public class UIListDropDown : IUIListItem
    {
        public string Id { get; private set; }
        public XmlElement template = new();
        private TMP_Dropdown _dropDown;
        private XmlElement _component;
        public delegate void Del(string message);
        private readonly string _label;
        private readonly Func<IEnumerable<string>> ValueGetter;
        private readonly UIListItems.strDelegate _OnInteracted;

        public UIListDropDown(string label, Func<IEnumerable<string>> valueGetter, UIListItems.strDelegate OnInteracted)
        {
            _label = label;
            ValueGetter = valueGetter;
            if (OnInteracted == null) _OnInteracted = delegate (string T, string s, string t, string d) { DCLUIManager.flightUI.OnListItemInteracted(T, s, t, d); };
            else _OnInteracted = OnInteracted;
            template = ComLinksManager.Instance.FlightUI.dropDownListItemTemplate;
        }

        public void AddTo(XmlElement parent)
        {
            _component = UIListItems.InitiliseTemplate(template, parent, out string parentId);
            _component.SetAttribute("id", GetId(parent));
            _component.ApplyAttributes();

            _dropDown = _component.GetComponentInChildren<TMP_Dropdown>();
            _dropDown.onValueChanged.AddListener(delegate (int x) { _OnInteracted(UIListItems.dropDownId, parentId, _label, _dropDown.options[x].text); });

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
            TMP_Dropdown.OptionData optionData = new(text);
            dropDown.options.Add(optionData);
        }

        public string GetId(XmlElement parent) => Id = UIListItems.dropDownId + "," + parent.id.Split(':')[0] + "," + _label;
        public string GetId(string parentId) => Id = UIListItems.dropDownId + "," + parentId + "," + _label;
    }
}