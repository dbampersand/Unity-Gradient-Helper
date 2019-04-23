using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
[ExecuteInEditMode]
public class gradientHelperWindow : EditorWindow
{
    Gradient gradient = new Gradient();
    int sizeX = 128;
    string fileName = "gradientData.json";

    public class FileModificationWarning : UnityEditor.AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            EditorWindow ew = EditorWindow.GetWindow(typeof(gradientHelperWindow));
            gradientHelperWindow g = (gradientHelperWindow)ew;
            g.saveData();
            Debug.Log("gg");
            return paths;
        }
    }

    [System.Serializable]
    public class ListContainer
    {
        public List<gradientMaterial> dataList;
        public List<Material> materialDataList = new List<Material>();
        public ListContainer(List<gradientMaterial> _dataList)
        {
            dataList = _dataList;
        }
        public ListContainer(List<Material> _dataList)
        {
            materialDataList = _dataList;
        }

    }

    [System.Serializable]
    public class gradientMaterial
    {
        public Gradient gradient = new Gradient();
        public List<Material> materialsAssigned = new List<Material>();
        public List<string> materialNames; // materials cannot be serialized in a way that retains through closing unity so we need to regenerate from a list of material names
        public string name = "";
        public string assignedTo = "";
        Texture2D texture;

        public gradientMaterial(Gradient grad, string Name)
        {
            gradient = grad;
            name = Name;

        }
        public void removeMat(Material m)
        {
            m.SetTexture("_Gradient", null);
            materialsAssigned.Remove(m);
        }
        public void updateName(string newName)
        {
            //change the file name
            string filePath = Path.Combine(Application.dataPath, "gradientData", name);
            filePath += ".png";
            string filePathNew = Path.Combine(Application.dataPath, "gradientData", newName);
            filePathNew += ".png";
            name = newName;
            File.Move(filePath, filePathNew);

            //change the associated metadata file name
            filePath = Path.Combine(Application.dataPath, "gradientData", name);
            filePath += ".png.meta";
            filePathNew = Path.Combine(Application.dataPath, "gradientData", newName);
            filePathNew += ".png.meta";
            File.Move(filePath, filePathNew);

        }
        public void updateFast()
        {
            foreach (Material m in materialsAssigned)
            {
                m.SetTexture("_Gradient", texture);
            }

        }
        public void updateMaterials()
        {
            //full update - call when finished
            AssetDatabase.Refresh();

            //set the metadata for wrapmode = clamp and filtermode = point
            string filePath = Path.Combine(Application.dataPath, "gradientData", name);
            filePath += ".png.meta";
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
            StreamReader streamReader = new StreamReader(fs);
            string fullString = streamReader.ReadToEnd();

            //regex to set the filtermode to 0 (point) and wrapping to clamp (1)
            fullString = System.Text.RegularExpressions.Regex.Replace(fullString,"filterMode: +(-[0-9]|[0-9])", "filterMode: 0");
            fullString = System.Text.RegularExpressions.Regex.Replace(fullString, "wrapU: +(-[0-9]|[0-9])", "wrapU: 1");
            fullString = System.Text.RegularExpressions.Regex.Replace(fullString, "wrapV: +(-[0-9]|[0-9])", "wrapV: 1");
            fullString = System.Text.RegularExpressions.Regex.Replace(fullString, "wrapW: +(-[0-9]|[0-9])", "wrapW: 1");
            fullString = System.Text.RegularExpressions.Regex.Replace(fullString, "alphaIsTransparency: +(-[0-9]|[0-9])", "alphaIsTransparency: 1");

            //truncuate the file to 0
            fs.SetLength(0);


            byte[] toWrite = Encoding.UTF8.GetBytes(fullString);
            fs.Write(toWrite, 0, toWrite.Length);
            streamReader.Close();
            fs.Close();

            filePath = "Assets/gradientData/" + name + ".png";
            Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D));
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;
            foreach (Material m in materialsAssigned)   
            {
                m.SetTexture("_Gradient", tex);
            }
        }

        public void updateTexture(int sizeX)
        {
            texture = new Texture2D(sizeX, 1);
            for (int i = 0; i < sizeX; i++)
            {

                Color c = gradient.Evaluate((float)i / (float)sizeX);
                texture.SetPixel(i, 0, c);

            }
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            texture.Apply();
            string filePath = Path.Combine(Application.dataPath, "gradientData", name);
            filePath += ".png";

            FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
            byte[] img = texture.EncodeToPNG();
            fs.Write(img, 0, img.Length);
            fs.Close();

            updateFast();
        }
    }

    public List<gradientMaterial> gradientList;

    public gradientMaterial currentMat;


    public void saveData()
    {

        foreach (gradientMaterial gm in gradientList)
        {
            gm.materialNames = new List<string>();
            foreach (Material m in gm.materialsAssigned)
            {
                // string[] s = m.name.Split('/');
                string s = m.name;
                if (s.Length > 0)
                    gm.materialNames.Add(s);
                gm.updateMaterials();
            }
        }
        ListContainer lc = new ListContainer(gradientList);
        

        string dataAsJson = JsonUtility.ToJson(lc);
    string filePath = Path.Combine(Application.dataPath, "gradientData",fileName);
        StreamWriter sw = new StreamWriter(filePath);
        sw.WriteLine(dataAsJson);
        sw.Close();

    }
    void loadData()
    {
        string filePath = Path.Combine(Application.dataPath, "gradientData", fileName);
        if (File.Exists(filePath))
        {
            string jsonData = File.ReadAllText(filePath);
            ListContainer lc = JsonUtility.FromJson<ListContainer>(jsonData);
            gradientList = lc.dataList;
            
            for (int i = 0; i < gradientList.Count; i++)
            {
                //purge the list of null entries, just in case
                if (gradientList[i] == null)
                {
                    gradientList.Remove(gradientList[i]);
                    i--; //account for the list shifting
                    if (i < 0) i = 0;
                }
                gradientList[i].materialsAssigned = new List<Material>();
                for (int z = 0; z < gradientList[i].materialNames.Count; z++)
                {
                    string s = gradientList[i].materialNames[z];
                    Material m = (Material)AssetDatabase.LoadAssetAtPath("Assets/Materials/" + s + ".mat", typeof(Material));
                    if (m != null)
                       gradientList[i].materialsAssigned.Add(m);
                }
                gradientList[i].updateTexture(sizeX);
            }
            if (gradientList.Count > 0)
                currentMat = gradientList[0];
            else
                addNewTexture();
        }
        else
        {
            gradientList = new List<gradientMaterial>();
        }
    }
    void Start()
    {
        if (gradientList == null)
        {
            gradientList = new List<gradientMaterial>();
        }
    }
    private void Awake()
    { 
        loadData();
        if (currentMat == null)
        {
            currentMat = new gradientMaterial(new Gradient(), "New Gradient");
        }
    }
    [MenuItem("Window/GradientHelper")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(gradientHelperWindow));

    }
    void addNewTexture()
    {
        if (gradientList == null)
        {
            gradientList = new List<gradientMaterial>();
        }

        gradientMaterial g = new gradientMaterial(new Gradient(), "New material number " + gradientList.Count); //SerializedObject serializedObject = new UnityEditor.SerializedObject(g);
        gradientList.Add(g);
        currentMat = g;

    }
    void OnDestroy()
    {
        foreach (gradientMaterial gm in gradientList)
        {
            gm.updateMaterials();
        }
        saveData();
    }
    void deleteTexture()
    {
        gradientList.Remove(currentMat);
        if (gradientList.Count > 0)
            currentMat = gradientList[0];
        else
            addNewTexture();
    }
    void changeMaterialSelected(object grad)
    {
        currentMat = (gradientMaterial)grad;
    }

    void OnGUI()
    {
        if (currentMat != null)
         gradient = currentMat.gradient;
        if (GUILayout.Button("Select gradient"))
        {
            GenericMenu menu = new GenericMenu();
            foreach (gradientMaterial g in gradientList)
            {
                menu.AddItem(new GUIContent(g.name), true, changeMaterialSelected, g);
            }
            menu.ShowAsContext();
            GUI.FocusControl(null);

        }
        if (GUILayout.Button("Refresh textures"))
        {
            if (currentMat != null)
                currentMat.updateTexture(sizeX);
        }
        if (GUILayout.Button("Create new gradient texture"))
        {
            addNewTexture();
        }
        if (GUILayout.Button("Delete gradient texture"))
        {
            deleteTexture();
        }
        if (GUILayout.Button("Reset gradient"))
        {
            currentMat.gradient = new Gradient();
            gradient = currentMat.gradient; 
        }
        if (GUILayout.Button("Assign to material"))
        {
            EditorGUIUtility.ShowObjectPicker<Material>(null, false, "", 0);
        }
        if (Event.current.commandName == "ObjectSelectorUpdated")
        {
            Object pickedObj = EditorGUIUtility.GetObjectPickerObject();
            bool cancel = false;
            foreach (Material m in currentMat.materialsAssigned)
            {
                if (pickedObj == m)
                {
                    cancel = true;
                }
            }
            if (pickedObj != null && !cancel)
            {
                //remove all other instances of this material being assigned
                for (int i = 0; i < gradientList.Count; i++)
                {
                    for (int c = 0; c < gradientList[i].materialsAssigned.Count; c++)
                    {

                        if (gradientList[i].materialsAssigned[c] == pickedObj)
                        {
                            gradientList[i].materialsAssigned.Remove((Material)pickedObj);
                        }
                    }
                }
                //finally, assign the material 
                currentMat.materialsAssigned.Add((Material)pickedObj);
            }
        }
        if (gradientList != null)
        {
            if (gradientList.Count > 0)
            {
                string newNameCheck = EditorGUILayout.TextField(currentMat.name);
                if (currentMat.name != newNameCheck)
                {
                    currentMat.updateName(newNameCheck);
                }
                if (currentMat != null)
                {
                    GUILayout.Label("Assigned materials: ");
                    if (currentMat.materialsAssigned.Count > 0)
                    {
                        for (int i = 0; i < currentMat.materialsAssigned.Count; i++)
                        {
                            Material m = currentMat.materialsAssigned[i];
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label(m.name);
                            if (GUILayout.Button("X"))
                            {
                                currentMat.removeMat(m);
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
            }



            Rect pos = new Rect(new Vector2(0, 0), new Vector2(200, 50));
            gradient = EditorGUILayout.GradientField(currentMat.gradient);
            bool update = true;
            if (currentMat.gradient.colorKeys.Length != gradient.colorKeys.Length || currentMat.gradient.alphaKeys.Length != gradient.alphaKeys.Length)
            {
                update = false;
            }
            for (int i = 0; i < currentMat.gradient.colorKeys.Length; i++)
            {
                if (currentMat.gradient.colorKeys[i].color != gradient.colorKeys[i].color)
                {
                    update = false;
                }
                if (currentMat.gradient.colorKeys[i].time != gradient.colorKeys[i].time)
                {
                    update = false;
                }

            }
            for (int i = 0; i < currentMat.gradient.alphaKeys.Length; i++)
            {
                if (currentMat.gradient.alphaKeys[i].alpha != gradient.alphaKeys[i].alpha)
                {
                    update = false;
                }
                if (currentMat.gradient.alphaKeys[i].time != gradient.alphaKeys[i].time)
                {
                    update = false;
                }


            }
            if (update)
            {
                currentMat.updateTexture(sizeX);
            }
                currentMat.gradient = gradient;
        }


    }

    // Update is called once per frame
    void Update()
    {

    }
}
