using System;
using Assets.Scripts.DroonComlinks.Ui;
using Assets.Scripts.DroonComLinks.Network;
using TMPro;
using UI.Xml;

namespace Assets.Scripts.DroonComLinks.Ui.ListItems
{
    public class UIListTextValue<T> : IUIListItem
    {
        public XmlElement template = new();
        public string Id { get; private set; }
        private readonly string _label;
        private readonly Func<T> ValueGetter;
        private XmlElement _component;
        private TextMeshProUGUI _labelElement;
        private TextMeshProUGUI _valueElement;

        public UIListTextValue(string label, Func<T> valueGetter, UIListItems.strDelegate OnInteracted)
        {
            ValueGetter = valueGetter;
            _label = label;
            template = ComLinksManager.Instance.FlightUI.textValueListItemTemplate;
        }

        public void AddTo(XmlElement parent)
        {
            _component = UIListItems.InitiliseTemplate(template, parent, out string parentId);
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