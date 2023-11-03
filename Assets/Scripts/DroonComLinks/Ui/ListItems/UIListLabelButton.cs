using System;
using Assets.Scripts.DroonComlinks.Ui;
using Assets.Scripts.DroonComLinks.Network;
using TMPro;
using UI.Xml;

namespace Assets.Scripts.DroonComLinks.Ui.ListItems
{
    public class UIListLabelButton<T> : IUIListItem
    {
        public XmlElement template = new();
        private TextMeshProUGUI _valueElement;
        public string Id { get; private set; }
        private readonly Func<T> _ValueGetter;
        private readonly string _label;
        private readonly UIListItems.strDelegate _OnInteracted;

        public UIListLabelButton(string label, Func<T> ValueGetter, UIListItems.strDelegate OnInteracted)
        {
            _ValueGetter = ValueGetter;
            if (OnInteracted == null) _OnInteracted = delegate (string T, string s, string t, string d) { DCLUIManager.flightUI.OnListItemInteracted(T, s, t, d); };
            else _OnInteracted = OnInteracted;
            _label = label + "   ";
            template = ComLinksManager.Instance.FlightUI.labelButtonListItemTemplate;
        }

        public void AddTo(XmlElement parent)
        {
            XmlElement component = UIListItems.InitiliseTemplate(template, parent, out string parentId);
            XmlElement button = component.GetElementByInternalId("button");

            component.SetAndApplyAttribute("id", GetId(parentId));
            component.GetElementByInternalId<TextMeshProUGUI>("label").SetText(_label);
            button.AddOnClickEvent(delegate { _OnInteracted(UIListItems.labelButtonId, parentId, _label, _ValueGetter().ToString()); });
            _valueElement = button.GetElementByInternalId<TextMeshProUGUI>("value");
            Update();
        }

        public void Update()
        {
            _valueElement.SetText(_ValueGetter().ToString());
        }
        public string GetId(XmlElement parent) => Id = UIListItems.labelButtonId + "," + parent.id.Split(':')[0] + "," + _label;
        public string GetId(string parentId) => Id = UIListItems.labelButtonId + "," + parentId + "," + _label;
    }
}