using System;
using System.Collections.Generic;
using Unity.Entities;

namespace DotsUI.Hybrid
{
    internal class RectTransformConverter
    {
        Dictionary<Type, ConverterBase> m_Converters;

        public RectTransformConverter()
        {
            m_Converters = new Dictionary<Type, ConverterBase>{
                {typeof(UnityEngine.UI.Image), new ImageConverter(UnityEngine.Sprite.Create(UnityEngine.Texture2D.whiteTexture, new UnityEngine.Rect(0.0f, 0.0f, 0.0f, 0.0f), UnityEngine.Vector2.zero))},
                {typeof(UnityEngine.Canvas), new CanvasConverter()},
                {typeof(UnityEngine.UI.CanvasScaler), new CanvasScalerConverter()},
                {typeof(TMPro.TextMeshProUGUI), new TextMeshProConverter()},
                {typeof(UnityEngine.UI.Selectable), new SelectableConverter()},
                {typeof(UnityEngine.UI.Graphic), new GraphicConverter()},
                {typeof(UnityEngine.UI.InputField), new InputFieldConverter()},
                {typeof(TMPro.TMP_InputField), new TMPInputFieldConverter()},
                {typeof(UnityEngine.UI.RectMask2D), new RectMaskConverter() },
                {typeof(UnityEngine.UI.Button), new ButtonConverter() }
            };
        }

        public void ConvertComponents(UnityEngine.RectTransform rectTransform, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager mgr)
        {
            foreach (var (type, converter) in m_Converters)
            {
                var component = rectTransform.GetComponent(type);
                if (component != null)
                {
                    try
                    {
                        converter.Convert(component, entity, rectTransformToEntity, assetToEntity, mgr);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
            }

            var components = rectTransform.GetComponents<IRectTransformToEntity>();
            foreach (var component in components)
            {
                component.ConvertToEntity(entity, rectTransformToEntity, assetToEntity, mgr);
            }
        }
    }
}