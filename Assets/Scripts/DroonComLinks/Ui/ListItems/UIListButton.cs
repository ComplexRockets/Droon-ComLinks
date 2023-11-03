using System;
using Assets.Scripts.DroonComlinks.Ui;
using Assets.Scripts.DroonComLinks.Network;
using TMPro;
using UI.Xml;

namespace Assets.Scripts.DroonComLinks.Ui.ListItems
{
    public class UIListButton : IUIListItem
    {
        public XmlElement template = new();
        public string Id { get; private set; }
        private readonly string _text;
        private readonly UIListItems.strDelegate _OnInteracted;

        public UIListButton(string text, UIListItems.strDelegate OnInteracted)
        {
            _text = text;
            if (OnInteracted == null) _OnInteracted = delegate (string T, string s, string t, string d) { DCLUIManager.flightUI.OnListItemInteracted(T, s, t, d); };
            else _OnInteracted = OnInteracted;
            template = ComLinksManager.Instance.FlightUI.buttonListItemTemplate;
        }

        public void AddTo(XmlElement parent)
        {
            XmlElement component = UIListItems.InitiliseTemplate(template, parent, out string parentId);

            component.AddOnClickEvent(delegate { _OnInteracted(UIListItems.buttonId, parentId, _text, _text); });
            component.SetAttribute("id", GetId(parentId));
            component.GetElementByInternalId<TextMeshProUGUI>("text").SetText(_text);
            component.ApplyAttributes();
        }

        public void Update() { }
        public string GetId(XmlElement parent) => Id = UIListItems.buttonId + "," + parent.id.Split(':')[0] + "," + _text;
        public string GetId(string parentId) => Id = UIListItems.buttonId + "," + parentId + "," + _text;
    }
}