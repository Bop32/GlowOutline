My attempt to re-create the glow outline effect you see in the Left 4 Dead games.

# THIS WILL ONLY WORK FOR STAGING BRANCH

> If you are on staging branch you might see compilation errors for the shaders, if seen just recompile the shaders it tells you to in console then restart the editor and it should work

If you don't have it you can download the library in the S&Box Editor by clicking on the "Library Manager" tab and clicking "sbox.game" and typing glow outline and look for one by bopcomapny or just the files from here and drag it into your scene.

In order to use it drag the script onto the camera. You should see something similar to the image below.

![image](https://github.com/user-attachments/assets/037e0046-7dfb-4f52-897f-80b767fe5db6)

To add an object click the second tab "Objects to Glow" and it should look like this:

![image](https://github.com/user-attachments/assets/3106cd0e-68bf-4d23-8d3f-fae300ca9879)

To add an object click the "+" symbol then either drag the object you want to glow on the bar, or hover over the bar and click the eyedrop image which will allow you to select a GameObject that is in your scene.

![image](https://github.com/user-attachments/assets/7735547c-3b16-483e-ba4e-f4bd7f41a1b3)

Once the object is dragged onto the bar you can either leave the color how it is and it will choose the default color in the other tab, or you can select a color. If done correctly it should look something like this (There is no default anti aliasing in the engine so it might be blocky):

![image](https://github.com/user-attachments/assets/456eda6a-8a25-4f64-84be-ba6e21f87109)

Documentation:

`glowOutline.Add(GameObject item);`

Allows the user to add a object they wish to glow via code (Color will be default color that is set in the editor).

`glowOutline.Add(GameObject item, Color color)`

Allows the user to add a object they wish to glow with a specified color.

`glowOutline.Remove(GameObject item);`

Allows the user to remove an object they don't want to glow anymore.

`glowOutline.RemoveAt(int index);`

Allow the user to remove an object at a certain index

`glowOutline.Clear();`

Allows the user to remove all objects that are glowing
