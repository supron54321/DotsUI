using System.Linq;
using Boo.Lang;
using DotsUI.Core;
using DotsUI.Hybrid;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class DotsUIInspector : EditorWindow
{
    private ListView m_ListView;
    private System.Collections.Generic.List<(string, Entity)> m_Roots = new System.Collections.Generic.List<(string, Entity)>();

    private EntityQuery m_RootQuery;

    private Foldout m_MeshProperties;

    [MenuItem("DotsUI/Inspector")]
    public static void ShowExample()
    {
        DotsUIInspector wnd = GetWindow<DotsUIInspector>();
        wnd.titleContent = new GUIContent("DotsUIInspector");
    }

    public void OnEnable()
    {
        VisualElement root = rootVisualElement;


        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.dotsui.core/DotsUI.Editor/Editor/DotsUIInspector.uxml");
        VisualElement ui = visualTree.CloneTree();
        ui.style.flexGrow = 1.0f;

        root.Add(ui);

        m_ListView = ui.Q<ListView>("EntityList");
        m_ListView.makeItem = () => new Label();
        m_ListView.bindItem = (e, i) => ((Label)e).text = m_Roots[i].Item1;
        m_ListView.itemHeight = 20; // Just an arbitrary value for now
        m_ListView.itemsSource = m_Roots;
        m_ListView.onSelectionChanged += OnCanvasSelected;

        m_MeshProperties = ui.Q<Foldout>("MeshProperties");
    }

    private void Update()
    {
        if (World.Active != null)
            UpdateEntities();
    }

    void UpdateEntities()
    {
        EntityManager entityManager = World.Active.EntityManager;
        var rootQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<DotsUI.Core.RectTransform>(),
                ComponentType.ReadOnly<CanvasScreenSize>(),
                ComponentType.ReadOnly<Child>(),
                typeof(WorldSpaceRect),
            },
            None = new ComponentType[]
            {
                typeof(Parent)
            },
            Any = new ComponentType[]
            {
                ComponentType.ReadOnly<CanvasConstantPixelSizeScaler>(),
                ComponentType.ReadOnly<CanvasConstantPhysicalSizeScaler>(),
            }
        });
        m_ListView.Clear();
        using (var roots = rootQuery.ToEntityArray(Allocator.TempJob))
        {
            m_Roots.Clear();
            foreach (var root in roots)
            {
                m_Roots.Add((entityManager.GetName(root), root));
            }

            m_ListView.itemsSource = m_Roots;
        }
    }

    private void OnCanvasSelected(System.Collections.Generic.List<object> obj)
    {
        var item = ((string, Entity)) obj[0];
        m_MeshProperties.contentContainer.Clear();
        var meshContainer = World.Active.EntityManager.GetSharedComponentData<CanvasMeshContainer>(item.Item2);
        m_MeshProperties.contentContainer.Add(new Label($"Vertices: {meshContainer.UnityMesh.vertexCount}"));
        m_MeshProperties.contentContainer.Add(new Label($"SubMeshes: {meshContainer.UnityMesh.subMeshCount}"));
    }
}