# List of known issues

* 0.4.0 regression - Screen space overlay canvas throws exception in subscene
* Mobile input is not implemented yet (It may work, but it's not supported yet)
* Every change on the scene, requires manual set of DirtyElement/UpdateColor components in hierarchy
* UnityEngine API is not fully separated from the core
* InputField has very naive and ugly implementation
* Tests are disabled (namespace Unity.Entities.Tests not found without manual manifest.json editing)
* Slider selectable effect doesn't work