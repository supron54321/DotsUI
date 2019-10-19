using System.Linq;
using DotsUI.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using RectTransform = UnityEngine.RectTransform;


public class DotsUIInspector : EditorWindow
{
    private int m_WorldVersion;
    private ListView m_ListView;

    private EntityQuery m_RootQuery;

    [MenuItem("DotsUI/Inspector")]
    public static void ShowExample()
    {
        DotsUIInspector wnd = GetWindow<DotsUIInspector>();
        wnd.titleContent = new GUIContent("DotsUIInspector");
    }

    public void OnEnable()
    {
        m_WorldVersion = World.Active?.Version ?? 0;
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;


        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.dotsui.core/DotsUI.Editor/Editor/DotsUIInspector.uxml");
        VisualElement ui = visualTree.CloneTree();
        ui.style.flexGrow = 1.0f;

        root.Add(ui);

        //ListView items = ui.Children().First().Children().First() as ListView;
        m_ListView = ui.Q<ListView>("EntityList");//.Q("unity-content-container");
        var itemTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.ui.builder/Editor/UI/Explorer/BuilderExplorerItem.uxml");
        for (int i = 0; i < 10; i++)
        {
            var label = new Label();
            label.text = "dsfdsfds";
            m_ListView.contentContainer.Add(label);
        }

    }


    private void Update()
    {
        if (World.Active != null)
            Repaint();
    }

    void Repaint()
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
        Debug.Log($"Query: {rootQuery.CalculateEntityCount()}");
        using (var roots = rootQuery.ToEntityArray(Allocator.TempJob))
        {
            foreach (var root in roots)
            {
                var label = new Label();
                label.text = entityManager.GetName(root);
                m_ListView.contentContainer.Add(label);
            }
        }
    }
}