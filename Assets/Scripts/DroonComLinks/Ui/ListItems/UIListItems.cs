using UI.Xml;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Ui.ListItems
{
    public static class UIListItems
    {
        public delegate void strDelegate(string itemType, string source, string text, string data);
        public static string buttonId = "button-list-item", labelButtonId = "labelbutton-list-item", textValueId = "textvalue-list-item", dropDownId = "dropdown-list-item";
        public static string antennasDropDownID = "antennas_dropdown", nodesInDirectRangeDropDownID = "nodes_in_direct_range_dropdown", nodesInRelayRangeDropDownID = "nodes_in_relay_range_dropdown";
        public static string GetButtonId(string parentId, string text) => buttonId + "," + parentId + "," + text;

        public static XmlElement InitiliseTemplate(XmlElement template, XmlElement parent, out string parentId)
        {
            XmlElement listItem = GameObject.Instantiate(template);
            XmlElement component = listItem.GetComponent<XmlElement>();
            parentId = parent.id.Split(':')[0];

            component.Initialise(parent.xmlLayoutInstance, (RectTransform)listItem.transform, template.tagHandler);
            parent.AddChildElement(component);
            component.SetAttribute("active", "true");
            component.AddOnMouseEnterEvent(delegate { component.SetAndApplyAttribute("width", "200%"); });
            component.AddOnMouseExitEvent(delegate { component.SetAndApplyAttribute("width", "100%"); });
            component.ApplyAttributes();
            return component;
        }
    }
}