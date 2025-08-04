My attempt to re-create the glow outline effect you see in the Left 4 Dead games.

If you don't have it you can download the library in the S&Box Editor by clicking on the "Library Manager" tab and clicking "sbox.game" and typing glow outline and look for one by bopcomapny or just the files from here and drag it into your scene.

This repository contains the library code and a scene if you want to see how it's used etc.

In order to use it drag the script onto the camera. You should see something similar to the image below.

<img width="378" height="234" alt="image" src="https://github.com/user-attachments/assets/0c882142-7467-469b-9e09-7ac05b9d898b" />

To add an object click the second tab "Objects to Glow" and it should look like this:

<img width="375" height="136" alt="image" src="https://github.com/user-attachments/assets/24554e84-22ed-4cf1-a067-7bdf62e3176a" />

To add an object click the "+" symbol then either drag the object you want to glow on the bar, or hover over the bar and click the eyedrop image which will allow you to select a GameObject that is in your scene.

<img width="381" height="164" alt="image" src="https://github.com/user-attachments/assets/71d0958f-8dda-4d60-a84c-386d655115d7" />

Once the object is dragged onto the bar you can either leave the color how it is and it will choose the default color in the other tab, or you can select a color. If done correctly it should look something like this (There is no default anti aliasing in the engine so it might be blocky):

<img width="631" height="344" alt="image" src="https://github.com/user-attachments/assets/18261743-3910-48ee-a828-f849add33a5e" />


Documentation:

`void Add(GameObject item)`

Adds a GameObject with the default glow color.

`void Add(GameObject item, Color color)`

Adds a GameObject with a specific glow color and renderer.

`bool TryAdd(GameObject item)`

Adds the object only if it doesn't already exist. Uses default glow color.

`bool TryAdd(GameObject item, Color color)`

Adds the object only if it doesn't already exist. Uses provided color.

`void SetGlowColor(GameObject item, Color color)`

Changes the glow color of a specific object.

`GlowSettings GetGlowObject(GameObject item)`

Gets the GlowSettings for the given object. Returns default if not found.

`bool Contains(GameObject item)`

Checks whether a GameObject is in the list.

`void Remove(GameObject item)`

Removes the GameObject if it exists.

`void RemoveAt(int index)`

Removes the object at a specific index.

`void Clear()`

Clears the entire list of glowing objects.

`List<GlowSettings> GlowingObjects()`

Returns the full list of current glowing objects.
