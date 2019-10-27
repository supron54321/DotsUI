# Change log

**This file contains changelog for all DotsUI packages**

## [0.4.0] 2019-10-27

### Added

* Slider control
* DotsUI inspector window (now displays only UI mesh info)

### Changes

* Removed old conversion pipeline
* Refactored GO->Entity conversion code

### Fixes

* Fixed sibling order after GO->Entity conversion

## [0.3.0] - 2019-09-11

### Added

* Added support for ConvertToEntity
* Added support for Screen Space - Overlay canvas rendering mode


## [0.2.0] - 2019-08-19

### Added

* Added drag & drop support
* Added ScrollRect

### Fixes

* Removed unnecessary sync point form input system

### Changes

* Removed custom parent system. Now DotsUI uses Unity.Transforms
* Updated entities packages
* Updated obsolete API
* Unit tests upgraded to latest changes in the core
* Minor performance improvements
* Updated roadmap
* LegacyRenderSystem renamed to HybridRenderSystem
* Improved idle performance of HybridRenderSystem (added RequireForUpdate EntityQuery)

### Fixes

* Fixed issue with last few triangles not being rendered arter InputField lost focus
* Renamed folders in DotsUI.Hybrid
* Removed unnecessary sync point in button system
* Fixed exception in selectable system caused by NativeHashmap being overfilled
* Fixed issue with canvas not being properly rebuilt after camera size changed
* Canvas pixel size is now properly taken from camera size (instead of screen size)
* Pointer events are now properly stopped from being propagated to parents


## [0.0.1] - 2019-07-23

 * First release