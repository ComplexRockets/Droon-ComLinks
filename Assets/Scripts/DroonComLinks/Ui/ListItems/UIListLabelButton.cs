using System;
using Assets.Scripts.DroonComlinks.Ui;
using TMPro;
using UI.Xml;

namespace Assets.Scripts.DroonComLinks.Ui.ListItems
{
    public class UIListLabelButton<T> : IUIListItem
    {
        public XmlElement template = new XmlElement();
        private TextMeshProUGUI _valueElement;
        public string Id { get; private set; }
        private Func<T> ValueGetter;
        private string _label;

        public UIListLabelButton(string label, Func<T> valueGetter)
        {
            ValueGetter = valueGetter;
            _label = label + "   ";
            template = Mod.Instance.comLinksManager.flightUI.labelButtonListItemTemplate;
        }

        public void AddTo(XmlElement parent, UIListItems.strDelegate OnInteracted)
        {
            string parentId;
            XmlElement component = UIListItems.InitiliseTemplate(template, parent, out parentId);
            XmlElement button = component.GetElementByInternalId("button");

            component.SetAndApplyAttribute("id", GetId(parentId));
            component.GetElementByInternalId<TextMeshProUGUI>("label").SetText(_label);
            button.AddOnClickEvent(delegate { OnInteracted(UIListItems.labelButtonId, parentId, _label, ValueGetter().ToString()); });
            _valueElement = button.GetElementByInternalId<TextMeshProUGUI>("value");
            Update();
        }

        public void Update()
        {
            _valueElement.SetText(ValueGetter().ToString());
        }
        public string GetId(XmlElement parent) => Id = UIListItems.labelButtonId + "," + parent.id.Split(':')[0] + "," + _label;
        public string GetId(string parentId) => Id = UIListItems.labelButtonId + "," + parentId + "," + _label;
    }
}