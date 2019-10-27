using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;
using DotsUI.Controls;
using DotsUI.Core;

public class TestSlider : MonoBehaviour, IConvertGameObjectToEntity
{
    public TextMeshProUGUI ValueText;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new TestSliderComponent
        {
            ValueText = conversionSystem.GetPrimaryEntity(ValueText)
        });
        dstManager.AddComponent(entity, typeof(SliderValueChangedEvent));   // force TestSliderSystem update
    }
}

public struct TestSliderComponent : IComponentData
{
    public Entity ValueText;
}


[UpdateInGroup(typeof(UserInputSystemGroup))]
public class TestSliderSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.WithAllReadOnly<TestSliderComponent, Slider, SliderValueChangedEvent>().ForEach((Entity entity, ref TestSliderComponent valueTarget, ref Slider slider,
            ref SliderValueChangedEvent evt) =>
        {
            var textData = EntityManager.GetBuffer<TextData>(valueTarget.ValueText);
            TextData.Set(textData, $"Value: {slider.Value}");
        });
    }
}
