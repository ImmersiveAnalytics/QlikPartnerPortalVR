***************************************
* WORLD POLITICAL MAP - GLOBE EDITION *
*             README FILE             *
***************************************


How to use this asset
---------------------
Firstly, you should run the Demo Scene provided to get an idea of the overall functionality.
Later, you should read the documentation and experiment with the API/prefabs.


Demo Scene
----------
There's one demo scene, located in "Demo" folder. Just go there from Unity, open "GlobeDemo" scene and run it.


Documentation/API reference
---------------------------
The PDF is located in the Doc folder. It contains instructions on how to use the prefab and the API so you can control it from your code.


Support
-------
Please read the documentation PDF and browse/play with the demo scene and sample source code included before contacting us for support :-)

* Support: contact@kronnect.me
* Website-Forum: http://kronnect.me
* Twitter: @KronnectGames


Version history
---------------

Version 5.3 - Current release

Improvements:
  - Ability to constraint rotation around a position (APIs: constraintPosition, constraintAngle, constraintPositionEnabled)
  - Increased color contrast of Scenic Scatter style
  - Thicker lines for inland frontiers
  - Added OnLeftClick and OnRightClick events
  - Added CIRCLE_PROJECTION marker type
  
Fixes:
  - Fixed issues in inverted mode and globe scale greater than 2
  - Fixed issue with latitude and longitude lines not being drawn when option enabled
  - Fixed issue with SetSimulatedMouseClick not working with GamePads
  - Fixed Pala (Chad) province
  - Fixed circle drawing when crossing 180 degree longitude

Version 5.2 - 2016.02.16

New features:
  - New high density mesh option and ability to replace the Earth mesh
  - New distance calculator (improved inspector and new API).
  
Improvements:
  - FlyTo is now compatible with simultaneous globe translations
  - New option to prevent interaction with other UI elements (Respect Other UI)
  - Labels now fade in/out automatically depending on camera distance and screen size  
  
Fixes:
  - Fixed new scenic atmosphere scattering style on mobile
  

Version 5.1 - 2016.01.29

New features:
  - New Scenic High Resolution with physically-based Atmosphere Scattering effect
  - New demo scene featuring sprites/billboard positioning

Improvements:
  - Can hide completely individual countries using decorators, Editor or through API (country.hidden property)
  - VR: compatibility with Virtual Reality gaze
  - Performance improvement in city look up functions
  - Increased sphere mesh density for sharper geometry

Fixes:
  - Min population filter now returns to previous value when closing the map editor
  - Fixed cities and markers rotation when inverted mode is enabled
  

Version 5.0 - 2015.12.24

New features:
  - New Camera navigation mode (can rotate camera instead of Earth). Supports user drag, Constant Drag Speed, Keep Straight, FlyTo methods. Updated Scenic Shader. 
  - Mount Points. Allows you to define custom landmarks for special purposes. Mount Points attributes includes a name, position, a type and a collection of tags. Manual updated.
  - Country and region capitals. Different icons + colors, new class filter (cityClassFilter), new "cityClass" property and editor support.
  - Added Hidden GameObjects Tool for dealing with hidden residual gameobjects (located under GameObject main menu)
   
Improvements:
  - New options for country decorator and for country object (now can hide/rotate/offset country label)
  - New line drawing method compatible with inverted mode (still needs some work)
  - Right clicking on a province now centers on that province instead of its country center
  - Improved zoom acceleration in inverted mode
  - (Set/Get)ZoomLevel now works in inverted mode
  - "Constant drag speed" and "Keep straight" now work in inverted mode
  - API: added new events: OnCityClick, OnCountryClick, OnProvinceClick
  - API: added GetCountryUnderSpherePosition and GetProvinceUnderSpherePosition
  - Editor: country's continent is displayed and can be renamed
  - Editor: continent can be destroyed, including countries, provinces and cities
  - Editor: deleting a country now deletes all cities belonging to that country as well
  - Editor: new options to delete a country, a province or all provinces belonging to a country
 
Fixes:
  - Cities were not being visible when inverted mode was enabled
  - Some countries and provinces surrounded by other countries/provinces could not be highlighted
  - Right click to center was not working when inverted mode was enabled


Version 4.2 - 2015.12.02
  
 New features:
  - Number of cities increased from 1249 to 7144
 
 Improvements:
  - Improved performance when cities are visible on the map
  - Improved straightening of globe at current position (right click or new improved API: StraightenGlobe)
  - Added dragConstantSpeed to prevent rotation acceleration
  - Added keepStraight to maintain the globe always straight
  - Added zoom max/min distance
  - Country is now highlighted as well when provinces are shown
  - API: new overload for GetCityIndex to fetch the index of the nearest city around a location (lat/lon or sphere position).
  - API: new events: OnCityEnter, OnCityExit, OnCountryEnter, OnCountryExit, OnProvinceEnter, OnProvinceExit

 Fixes:
  - Fixed geodata issues with Republic of Congo, South Sudan and provinces of British Columbia, Darién, Atlántico Sur, Saskatchewan and Krasnoyarsk
  - Minor fixes regarding province highlighting and some lines crossing Earth
  - Fixed a bug in FlyToxxx() methods when globe is not a 0,0,0 position
  

Version 4.1 - 11/11/2015

 New features:
  - Option to show inland frontiers
  - Improved Scenic shaders including a new 8K + Scenic style (atmosphere falloff + scattering effect)
  
 Improvements:
  - New option to invert zoom direction when using mouse wheel (invertZoomDirection property)
  - New option to automatically hide cursor on the globe if mouse if not over it (cursorAlwaysVisible)
  - New API to obtain the country reference under any sphere position (GetCountryUnderSpherePosition) 
  - New option to mask grid so it only appears over oceans
  - Improved Earth glow
  
 Fixes:
  - Globe interaction is now properly blocked when mouse is hovering an UI element (Canvas, ScrollRect, ...)
  - Labels shadows were not being drawn due to a regression bug


Version 4.0 - 23/10/2015
  New features:
  - Map Editor: new extra component for editing countries, provinces and cities.
  
  Improvements:
  - New APIs for setting/getting normalized zoom factor (SetZoomLevel/GetZoomLevel)
  - New APIs in the Calculator component (prettyCurrentLatLon, toLatCardinal, toLonCardinal, from/toSphereLocation)
  - New API variant for adding circle markers (AddMarker)
  - New API for getting cities from a specified country - GetCities(country)
  - New APIs for getting/setting cities information to a packed string - map.editor.GetCityGeoData/map.ReadCitiesPackedString
  - Option for changing city icon size
  - Can assign custom font to individual country labels
  - Even faster country/province hovering detection
  - Better polygon outline (thicker line and best positioning thanks to new custom shader for outline)
  - Country outline is shown when show provinces mode is activated
  - Improved low-res map generator including Douglas-Peucker implementation + automatic detection of self-crossing polygons
  
 Fixes:
  - Removed requirement of SM3 for the Scenic shader

    
Version 3.2 - 21/09/2015
  New features:
  - New markers and line drawing and animation support
  - New "Scenic" style with custom shader (relief + cloud effects)
  
  Improvements:
  - Pinch in/out support for mobile
  - Improved resolution of high-def frontiers while reducing data file size
  - Single city catalogue with improved size
  - Significant performance improvement in detecting country hover
  - More efficient highlghting system
  - New option in inspector: labels elevation
 
 Fixes:
  - Corrected frontiers distortion
  - Population of cities fixed and approximated to the metro area
 

Version 3.1 - 28/08/2015
  New features:
  - New Inverted Mode view (toggle in the inspector)
  - Bake Earth texture command (available from gear's icon in the inspector title bar)
  
  Improvements:
  - New buttons to straighten and tilt the Earth (also available in API)
  - New option to adjust the drag speed
  - New option to enable rotation using keyboard (WASD)
  - x2 speed increase of colorize/highlight system
  
  Fixes:
  - Fixed bug related to labels drawing when the Earth is rotated on certain angles
  - Fixed colorizing countries when field of view of camera was not default 60
  
  

Version 3.0.1 - 11.08.2015
  New features:
  
  Improvements:
  - Better outline implementation with improved performance)
  
  Fixes:
  - Calculator component: fixed an error in spherical to degree conversion
  - Colorize shader was not showing when Earth was not visible
  - A few countries had parts visible when colorized and Earth rotates



Version 3.0 - 11.08.2015
  New features:
  - New component: World Map Calculator
  - New component: World Map Ticker
  - New component: World Map Decorator
  
  Improvements:
  - Some shaders have been optimized
  - Improved algorithm for centering destinations (produces a straighten view)
  - New option: right click centers on a selected country
  - Lots of internal changes and new APIs
  
  Fixes:
  - Fixed country label positioning bug when some labels overlap
  - Fixed colorizing of some countries which appeared inverted


Version 2.1 - 3.08.2015
  New features:
  - Option to draw country labels with automatic placement
    
  Improvements:
  - Additional high-res (8K) Earth texture
  
  Fixes:
  - Some countries highlight were rendered incorrectly when using high detail frontiers

Version 2.0 - 31/07/2015
  New features:
  - Second detail level for country frontiers
  - New option to draw provinces/states for active country
  - Option to draw an outline around highlighted/colored countries
  - New options to show a cursor over custom/mouse position
  - New options to show latitude/longitude lines
    
  Improvements:
  - Even faster frontier line rendering (+20%)
  - Tweaked triangulation algorithm to improve poly-fill
  - Can locate a country from the inspector
  - Cities are now drawn as small circular dots, instead of small boxes
  - Can change the color of the cities
  - Additional Earth style: CutOut
  
  Fixes:
  - Some new properties were not being correctly saved from Editor
  - Colored countries hide correctly when Earth rotates

Version 1.1 - 25.07.2015
  New features:
  - added 3 new material/textures for Earth
  - extended city catalog (now 1249 cities included!)
  - can filter cities by population
  Improvements: 
  - better frontiers line quality and fasterer render
  - better poly-fill algorithm
  - can change navigation time in Editor
  - moved mouse interactions (rotation/zoom) to the main script and expose that as part of the API
  - reorganized project folder structure
  Fixes:
  - setting navigation time to zero causes error
  - some properties where not being persisted


Version 1.0 - Initial launch 16.07.2015



Credits
-------

All code, data files and images, otherwise specified, is (C) Copyright 2015 Kronnect Games
Non high-res Earth textures derived from NASA source (Visible Earth)
Flag images: Licensed under Public Domain via Wikipedia


