using System;
using Assets.Scripts.DroonComLinks.Interfaces;
using TMPro;
using UI.Xml;

namespace Assets.Scripts.DroonComLinks.Objects.ListItems
{
    public class UIListButton : IUIListItem
    {
        public XmlElement template = new XmlElement();
        public string Id { get; private set; }
        private string _text;

        public UIListButton(string text)
        {
            _text = text;
            template = Mod.Instance.comLinksManager.flightUI.buttonListItemTemplate;
        }

        public void AddTo(XmlElement parent, UIListItems.strDelegate OnInteracted)
        {
            string parentId;
            XmlElement component = UIListItems.InitiliseTemplate(template, parent, out parentId);

            component.AddOnClickEvent(delegate { OnInteracted(UIListItems.buttonId, parentId, _text, _text); });
            component.SetAttribute("id", GetId(parentId));
            component.GetElementByInternalId<TextMeshProUGUI>("text").SetText(_text);
            component.ApplyAttributes();
        }

        public void Update() { }
        public string GetId(XmlElement parent) => Id = UIListItems.buttonId + "," + parent.id.Split(':')[0] + "," + _text;
        public string GetId(string parentId) => Id = UIListItems.buttonId + "," + parentId + "," + _text;

    }
}