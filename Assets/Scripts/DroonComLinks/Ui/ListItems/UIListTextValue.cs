using System;
using Assets.Scripts.DroonComlinks.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Ui.ListItems
{
    public class UIListTextValue<T> : IUIListItem
    {
        public XmlElement template = new XmlElement();
        public string Id { get; private set; }
        private string _label;
        private Func<T> ValueGetter;
        private XmlElement _component;
        private TextMeshProUGUI _labelElement;
        private TextMeshProUGUI _valueElement;

        public UIListTextValue(string label, Func<T> valueGetter)
        {
            ValueGetter = valueGetter;
            _label = label;
            template = Mod.Instance.comLinksManager.flightUI.textValueListItemTemplate;
        }

        public void AddTo(XmlElement parent, UIListItems.strDelegate OnInteracted)
        {
            string parentId;
            _component = UIListItems.InitiliseTemplate(template, parent, out parentId);
            _component.SetAttribute("id", GetId(parent));
            _component.ApplyAttributes();

            _labelElement = _component.GetElementByInternalId<TextMeshProUGUI>("label");
            _valueElement = _component.GetElementByInternalId<TextMeshProUGUI>("value");
            _labelElement.SetText(_label);
            Update();
        }

        public void Update()
        {
            _valueElement.SetText(ValueGetter().ToString());
        }

        public string GetId(XmlElement parent) => Id = UIListItems.textValueId + "," + parent.id.Split(':')[0] + "," + _label;
        public string GetId(string parentId) => Id = UIListItems.textValueId + "," + parentId + "," + _label;
    }
}