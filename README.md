# Unity-Gradient-Helper
A tool for creating and managing gradients in Unity. Automatically assigns and updates the materials with changes. 

![Gradients image](https://i.imgur.com/3wy0pMe.png)

# Usage:

- Put this script in Assets/Editor

- Create folder Assets/gradientData

- Create a shader with property:

```
	_Gradient("Gradient",2D) = "white" {}
```

- Go to Window -> GradientHelper

- Click "Create new Gradient Texture"

- Click the coloured bar which comes up in the window.

- Create the gradient in the visual editor, then close gradient editor window.

- Click an object which has the gradient shader applied to it.

- Click "Assign to Material" in the gradientHelper window. 

The editor will store your gradients and the shaders they are assigned to in a JSON file called gradientData.json inside Assets/gradientData, alongside the gradient images. 
