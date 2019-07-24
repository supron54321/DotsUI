# List of known issues

* Mobile input is not implemented yet
* ConvertToEntity workflow doesn't work. Use RectTransformConversionUtils and DotsUIPrefab to convert from GameObject world to DOTS
* Drag/drop events are not supported yet
* Prefab or canvas conversion creates new entities for sprites and fonts (conversion system holds only per-prefab asset->entity mapping)
* Every change on the scene, requires manual set of DirtyElement/UpdateColor components in hierarchy
* DotsUI implements its own parent system instead of using Unity.Transforms
* There are unnecessary sync points
* UnityEngine API is not fully separated from the core
* InputField has very naive and ugly implementation
* Input field caret breaks last vertices of UI mesh
* Tests are disabled (namespace Unity.Entities.Tests not found without manual manifest.json editing)
* Camera viewport size is not automatically updated