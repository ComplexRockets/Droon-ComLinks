using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Menu.ListView;
using ModApi.Ui;
using UI.Xml;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Ui.Flight.NetworkManager
{
    public class NetworkManagerDialog : MonoBehaviour, IDialog, IListView
    {
        public const string NodesViewID = "nodes";
        public const string connetionsViewId = "connections";
        private readonly bool _closed;
        private readonly Dictionary<string, Func<ListViewModel>> _modelBuilders = new();

        private readonly Dictionary<string, ListViewModel> _models = new();
        private string _selectedId;
        private ListViewModel _viewModel;
        private XmlLayout _xmlLayout;
        public bool AllowCameraZoom => false;
        public NodesViewModel Nodes => GetModel(NodesViewID) as NodesViewModel;
        public ConnectionsViewModel Connections => GetModel(connetionsViewId) as ConnectionsViewModel;
        public bool PreviewEnabled => false;
        public bool RequiresSceneReload { get; private set; }
        public string SelectedViewId => _selectedId;
        public event DialogDelegate Closed;

        public virtual void Initialize()
        {
            Game.Instance.UserInterface.RegisterDialog(this);
            _xmlLayout = base.gameObject.AddComponent<XmlLayout>();
            base.gameObject.AddComponent<XmlLayoutController>().EventTarget = this;
            Game.Instance.UserInterface.BuildUserInterfaceFromResource("Droon ComLinks/Flight/NetworkManagerDialog", _xmlLayout);
            _modelBuilders[NodesViewID] = () => new NodesViewModel();
            _modelBuilders[connetionsViewId] = () => new ConnectionsViewModel();
            StartCoroutine(LoadInitialViewModel());
        }

        private void BuildListView(string id)
        {
            Func<ListViewModel> func = _modelBuilders[id];
            InitializeController(id, func());
        }

        private ListViewModel GetModel(string id)
        {
            if (!_models.ContainsKey(id))
            {
                BuildListView(id);
            }
            return _models[id];
        }

        private T InitializeController<T>(string id, T viewModel) where T : ListViewModel
        {
            XmlElement elementById = _xmlLayout.GetElementById("list-view-panel");
            GameObject obj = new(id, typeof(RectTransform));
            RectTransform component = obj.GetComponent<RectTransform>();
            component.SetParent(elementById.transform, worldPositionStays: false);
            component.anchorMin = Vector2.zero;
            component.anchorMax = Vector2.one;
            component.sizeDelta = Vector2.zero;
            XmlLayout xmlLayout = obj.AddComponent<XmlLayout>();
            ListViewChildController listViewChildController = obj.AddComponent<ListViewChildController>();
            Game.Instance.UserInterface.BuildUserInterfaceFromResource("Droon ComLinks/Flight/NetworkManagerListView", xmlLayout);
            _models[id] = viewModel;
            listViewChildController.Initialize(viewModel, this);
            viewModel.ListView.gameObject.SetActive(value: false);
            viewModel.ListView.Closed += OnListViewClosed;
            return viewModel;
        }

        private IEnumerator LoadInitialViewModel()
        {
            yield return new WaitForEndOfFrame();
            SelectViewModel(NodesViewID);
        }

        private void OnCategoryClicked(XmlElement element)
        {
            SelectViewModel(element.internalId);
        }

        private void OnCloseButtonClicked()
        {
            Close();
        }

        private void OnListViewClosed(object sender, EventArgs e)
        {
            Close();
        }

        private void SelectViewModel(string id)
        {
            foreach (XmlElement item in _xmlLayout.GetElementsByClass("category"))
            {
                item.RemoveClass("selected");
                item.RemoveClass("btn-primary");
            }
            XmlElement elementByInternalId = _xmlLayout.XmlElement.GetElementByInternalId(id);
            elementByInternalId.AddClass("selected");
            elementByInternalId.AddClass("btn-primary");
            _selectedId = id;
            ListViewModel model = GetModel(id);
            if (_viewModel != null)
            {
                _viewModel.ListView.gameObject.SetActive(value: false);
            }
            _viewModel = model;
            if (_viewModel != null)
            {
                _viewModel.ListView.gameObject.SetActive(value: true);
            }
        }

        public void Close()
        {
            UnityEngine.Object.Destroy(base.gameObject);
            this.Closed?.Invoke(this);
        }
    }
}
