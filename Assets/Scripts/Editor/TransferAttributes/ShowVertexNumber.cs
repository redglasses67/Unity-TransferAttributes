// Editor script that displays the number and UV coordinates of each individual vertex making up a triangle within a mesh
// To install, place in the Assets folder of a Unity project
// Open via Window > Show Vertex Info
// Author: Luke Gane
// Last updated: 2015-02-07

//http://individual.utoronto.ca/owlman9000/scripts/unity3d/ShowVertexNumber.cs


using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using Object = UnityEngine.Object;

namespace TransferAttributes
{
    using TAP = TransferAttributes.TransferAttributesProcess;
    public class ShowVertexNumber : EditorWindow
    {    
        Mesh dstMesh;
        Mesh dstTmpMesh;

        MeshFilter dstMeshFilter;
        SkinnedMeshRenderer dstSMR;
        Transform dstTrans;

        bool meshAvailable = false;
        int curDstFace = 0, numDstFaces = 0, newDstFace = 0;
        int curSrcFace = 0, numSrcFaces = 0, newSrcFace = 0;
        
        TAP.TriangleData curDstTriangleData;
        private List<TAP.TriangleData> dstTriangleDataList;
        Dictionary<Vector3, List<int>> dstVtxPosIdxDict;
        Dictionary<Vector3, List<int>> dstVtxPosTriangleIdxDict;

        Dictionary<Vector3, List<Vector3>> dstBelongingSpaceOfVtxPosDict = null;
        Dictionary<Vector3, List<Vector3>> srcBelongingSpaceOfVtxPosDict = null;
        

        GUIStyle centredStyle = null;
        GUIStyle intersectedStyle = null;
        
        Vector3 p1, p2, p3, centerP, centerNormal, n1, n2, n3, wp1, wp2, wp3, tmpN1, tmpN2, tmpN3;
        Color c1, c2, c3;

        float sphereRadius, boundsLength;
        float sphereRadiusCoffi = 1;//100;

        private static Dictionary<Vector3, List<int>> dstVertexMap;


        private List<TAP.TriangleData> srcTriangleDataList;

        private GameObject srcObj;
        private Mesh srcMesh;
        private MeshFilter srcMeshFilter;
        private SkinnedMeshRenderer srcSMR;
        private Transform srcTrans;
        private Dictionary<Vector3, List<int>> srcVtxPosIdxDict;
        private Dictionary<Vector3, List<int>> srcVtxPosTriangleIdxDict;

        private bool isDisplayIntersectionPoint = true;
        private bool isDisplayAveragedNormals = false;
        private Vector3[] dstAveragedNormalArray;
        private int[] dstVtxPosIdxCountList;
        // private bool isDisplayAvNormalsFromQuat = false;

        private bool isDisplayRecalculateNormals = false;

        private bool isDisplayVertexColors = true;

        // private bool isDisplayUV3 = false;

        // private bool isDisplayUV2 = false;

        
        // private List<Vector4> uv2List = new List<Vector4>(); //クォータニオン テスト
        // private List<Vector3> uv3List = new List<Vector3>();
        // private List<Vector4> uv4List = new List<Vector4>(); //クォータニオン テスト

        private bool useAveragedVertexNormals = true;
        private bool drawSphereFromVtxPos = true;
        private bool isDisplayVertexNormals = true;

        private bool isDisplaySrcTriangleIdx = false;

        private bool isDisplayVoxelSpace = true;
        private bool isDisplayVoxelSpaceAll = false;
        private int curVoxelSpaceIdx = 0;
        private float voxelSpaceSize = Mathf.Infinity;
        private Dictionary<Vector3, Bounds> voxelSpaceDict;

        public class VoxelSpace
        {
            public Vector3 voxelAddress;
            public Bounds voxelBounds;
        } 

        // private static GameObject intersectionPointObj_1;
        // private static GameObject intersectionPointObj_2;
        // private static GameObject intersectionPointObj_3;
        private KeyValuePair<Vector3,Vector3> intersectionPoint_1 = new KeyValuePair<Vector3, Vector3>(Vector3.zero, Vector3.zero);
        private KeyValuePair<Vector3,Vector3> intersectionPoint_2 = new KeyValuePair<Vector3, Vector3>(Vector3.zero, Vector3.zero);
        private KeyValuePair<Vector3,Vector3> intersectionPoint_3 = new KeyValuePair<Vector3, Vector3>(Vector3.zero, Vector3.zero);

        // private float intersectionSphereSize = 0.05f;


        [MenuItem("Tools/Show Vertex Info")]
        public static void ShowWindow()
        {
            // Show existing window instance; if one doesn't exist, it is created
            EditorWindow.GetWindow(typeof(ShowVertexNumber));
        }
        
        void OnEnable()
        {
            // See http://answers.unity3d.com/questions/58018/drawing-to-the-scene-from-an-editorwindow.html
            if (SceneView.onSceneGUIDelegate != this.RenderStuff)
            {
                // This appears to be a sufficient check
                SceneView.onSceneGUIDelegate += this.RenderStuff;
            }
            // SetObjetc();
        }
        
        void OnDestroy()
        {
            SceneView.onSceneGUIDelegate -= this.RenderStuff;
        }
        
        // void OnSelectionChange()
        void SetObject()
        {
            bool hasNoMesh = true;
            
            if (Selection.activeGameObject)
            {
                dstMeshFilter = Selection.activeGameObject.GetComponentInChildren<MeshFilter>();
                if (dstMeshFilter)
                {
                    dstMesh    = dstMeshFilter.sharedMesh;
                    dstTmpMesh = dstMeshFilter.sharedMesh;

                    meshAvailable = true;
                    numDstFaces = dstMesh.triangles.Length / 3;
                    hasNoMesh = false;
                }
                else
                {
                    dstSMR = Selection.activeGameObject.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (dstSMR)
                    {
                        dstMesh    = dstSMR.sharedMesh;
                        dstTmpMesh = dstSMR.sharedMesh;

                        meshAvailable = true;
                        numDstFaces = dstMesh.triangles.Length / 3;
                        hasNoMesh = false;
                    }
                }
                dstTrans = Selection.activeGameObject.transform;

                dstTmpMesh.RecalculateNormals();

                dstVertexMap = new Dictionary<Vector3, List<int>>();
                for (int v = 0; v < dstTmpMesh.vertexCount; ++v)
                {
                    // if (mesh.vertices[v] == new Vector3(0, -0.1f, 0))
                    // {
                    //     Debug.Log(mesh.vertices[v] + " ありました");
                    // }
                    var worldPos = dstTrans.TransformPoint(dstTmpMesh.vertices[v]);
                    // Debug.Log(v + "  :  " + worldPos.x + " , " + worldPos.y + " , " + worldPos.z);

                    if (dstVertexMap.ContainsKey(worldPos) == false) //mesh.vertices[v]))
                    {
                        // curVertexMap.Add(dstMesh.vertices[v], new List<int>());
                        dstVertexMap.Add(worldPos, new List<int>());
                        // Debug.Log("     " + v +
                        //     "  :  " + worldPos.x + " , " + worldPos.y + " , " + worldPos.z + 
                        //     "  :  " + dstTmpMesh.vertices[v].x + " , " + dstTmpMesh.vertices[v].y + " , " + dstTmpMesh.vertices[v].z);
                    }
                    // else
                    // {
                    //     Debug.Log("<color=grey>     " + v +
                    //         "  :  " + worldPos.x + " , " + worldPos.y + " , " + worldPos.z + 
                    //         "  :  " + dstTmpMesh.vertices[v].x + " , " + dstTmpMesh.vertices[v].y + " , " + dstTmpMesh.vertices[v].z +
                    //         "     はすでに入っています</color>");
                    // }

                    // curVertexMap[dstMesh.vertices[v]].Add(v);
                    dstVertexMap[worldPos].Add(v);
                }

                dstAveragedNormalArray = new Vector3[dstTmpMesh.vertexCount];
                dstVtxPosIdxCountList  = new int[dstTmpMesh.vertexCount];

                foreach (var _worldPos in dstVertexMap.Keys)
                {
                    var tmpAvNml = Vector3.zero;
                    var idxList  = dstVertexMap[_worldPos];
                    // var indexList = curVertexMap[vertexPosLocal];
                    foreach (var idx in idxList)
                    {
                        tmpAvNml += dstTmpMesh.normals[idx];
                    }
                    // Debug.LogError("idxList.Count = " + idxList.Count);
                    tmpAvNml /= idxList.Count;

                    foreach (var idx in idxList)
                    {
                        dstAveragedNormalArray[idx] = tmpAvNml;
                        dstVtxPosIdxCountList[idx]  = idxList.Count;
                    }
                }
            }
            
            if (hasNoMesh)
            {
                meshAvailable = false;
                numDstFaces = 0;
            }
            else
            {
                // Debug.Log(dstMesh.uv.Length + " : " + dstMesh.uv2.Length + " : " + dstMesh.uv3.Length);
                // if (dstMesh.uv2 != null)
                // {
                //     dstMesh.GetUVs(1, uv2List);
                // }

                // if (dstMesh.uv3 != null)
                // {
                //     dstMesh.GetUVs(2, uv3List);
                // }
                
                // if (dstMesh.uv4 != null)
                // {
                //     dstMesh.GetUVs(3, uv4List);
                // }
                // Debug.Log("vertex = " + dstMesh.vertexCount + " : uv2 list = " + uv2List.Count + " : uv3 list = " + uv3List.Count);
            }

            TAP.GetTriangleDataList(
                dstMesh,
                dstMeshFilter,
                dstSMR,
                true,
                true,
                out dstVtxPosIdxDict,
                out dstVtxPosTriangleIdxDict,
                out dstTriangleDataList);

            curDstFace = 0;
            newDstFace = curDstFace;

            if (dstTriangleDataList != null)
            {
                curDstTriangleData = dstTriangleDataList[curDstFace];
            }

            Repaint();
        }
        
        void OnInspectorUpdate()
        {
            Repaint();
        }
        
        void OnGUI()
        {
            var labelStyle = new GUIStyle();
            labelStyle.richText = true;
            labelStyle.font = GUI.skin.font;
        
            // if (meshAvailable == false)
            // {
            //     EditorGUILayout.LabelField("Current selection contains no mesh.");
            //     return;
            // }


            if (GUILayout.Button ("Set Object"))
            {
                SetObject();
            }

            if (dstMesh == null || dstVertexMap == null)
            {
            return; 
            }

            EditorGUILayout.LabelField("Set Object Name    : ", dstMesh.name.ToString());
            EditorGUILayout.LabelField("Number of faces    : ", numDstFaces.ToString());
            EditorGUILayout.LabelField("Current face index : ", curDstFace.ToString());
            
            // EditorGUI.BeginChangeCheck();
            newDstFace = EditorGUILayout.IntField("Jump to face index: ", curDstFace);
            if (newDstFace != curDstFace)
            {
                if (newDstFace >= 0 && newDstFace < numDstFaces)
                {
                    curDstFace = newDstFace;
                }
            }
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button ("Prev Face"))
            {
                curDstFace = (curDstFace-1) % numDstFaces;
                if (curDstFace < 0)
                {
                    curDstFace = curDstFace + numDstFaces;
                }

                if (dstTriangleDataList != null)
                {
                    curDstTriangleData = dstTriangleDataList[curDstFace];
                }
                // SetIntersectedPoint(1);
                // SetIntersectedPoint(2);
                // SetIntersectedPoint(3);
            }
            
            if (GUILayout.Button ("Next Face"))
            {
                curDstFace = (curDstFace+1) % numDstFaces;
                if (dstTriangleDataList != null)
                {
                    curDstTriangleData = dstTriangleDataList[curDstFace];
                }
                // SetIntersectedPoint(1);
                // SetIntersectedPoint(2);
                // SetIntersectedPoint(3);
            }

            EditorGUILayout.EndHorizontal();
            

            EditorGUILayout.Space();
            
            isDisplayVertexNormals   = EditorGUILayout.ToggleLeft(
                                        "Display Each Vertex Normals", isDisplayVertexNormals);

            isDisplayAveragedNormals = EditorGUILayout.ToggleLeft(
                                        "Display Each Averaged Normals", isDisplayAveragedNormals);

            // isDisplayAvNormalsFromQuat = EditorGUILayout.ToggleLeft(
            //                             "Display Each Averaged Normals From Quaternion", isDisplayAvNormalsFromQuat);

            isDisplayRecalculateNormals = EditorGUILayout.ToggleLeft(
                                        "Display Recalculate Normals", isDisplayRecalculateNormals);

            isDisplayVertexColors = EditorGUILayout.ToggleLeft(
                                        "Display Each Vertex Colors", isDisplayVertexColors);

            useAveragedVertexNormals = EditorGUILayout.ToggleLeft(
                                        "Use Averaged Vertex Normals for Get Intersected Triangle", useAveragedVertexNormals);

            using (new EditorGUILayout.HorizontalScope())
            {
                drawSphereFromVtxPos = EditorGUILayout.ToggleLeft(
                                            "Draw Sphere From Vertex Pos", drawSphereFromVtxPos);

                sphereRadiusCoffi    = EditorGUILayout.FloatField(
                                            "Sphere Radius Coffisient", sphereRadiusCoffi);
            } 
            EditorGUILayout.Space();

            var index1 = dstMesh.triangles[curDstFace * 3 + 0];
            var index2 = dstMesh.triangles[curDstFace * 3 + 1];
            var index3 = dstMesh.triangles[curDstFace * 3 + 2];

            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (curDstTriangleData != null)
                {
                    // EditorGUILayout.LabelField(
                    //     "Face Normal:",
                    //     "<color=black>" + "     Normal ( " + curTriangleData.faceNml.x + ", " + curTriangleData.faceNml.y + ", " + curTriangleData.faceNml.z + ")</color>",
                    //     labelStyle);
                        
                    // EditorGUILayout.LabelField(
                    //     "Triangle d :",
                    //     "<color=black>     " + curTriangleData.d + "</color>",
                    //     labelStyle);
                    EditorGUILayout.LabelField(
                        "curTriangleData CenterPos: ",
                        "  <color=yellow> " + curDstTriangleData.centerPos.x + ", " + curDstTriangleData.centerPos.y + ", " + curDstTriangleData.centerPos.z + "</color>",
                        labelStyle);

                    EditorGUILayout.Space();
                }
                EditorGUILayout.LabelField(
                    "Index of red vertex:",
                    "<color=red>" + index1.ToString() + "     Pos ( " + p1.x + ", " + p1.y + ", " + p1.z + ") : " +
                    dstVtxPosIdxCountList[index1] + "</color>",
                    labelStyle);

                // EditorGUILayout.LabelField(
                //     "<color=red>          world pos:</color>",
                //     "<color=red>" + index1.ToString() + "     Pos(" + p1.x + ", " + p1.y + ", " + p1.z + ")</color>",
                //     labelStyle);

                EditorGUI.indentLevel++;
                if (isDisplayVertexNormals)
                {
                    EditorGUILayout.LabelField(
                        "normal of red: ",
                        "  <color=red>Normal ( " + n1.x + ", " + n1.y + ", " + n1.z + ")</color>",
                        labelStyle);
                }
                else
                {
                    EditorGUILayout.LabelField("");
                }
                // if (isDisplayRecalculateNormals)
                // {
                //     EditorGUILayout.LabelField(
                //         "recalculate normal of red: ",
                //         "  <color=red>Normal ( " + tmpN1.x + ", " + tmpN1.y + ", " + tmpN1.z + ")</color>",
                //         labelStyle);
                // }
                if (isDisplayVertexColors == true && curDstTriangleData != null)
                {
                    EditorGUILayout.LabelField(
                        "colors of red: ",
                        "  <color=red>Colors ( " + c1.r + ", " + c1.g + ", " + c1.b + ", " + c1.a + ")</color>",
                        labelStyle);
                }
                else
                {
                    EditorGUILayout.LabelField("");
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.LabelField(
                    "Index of green vertex:",
                    "<color=green>" + index2.ToString() + "     Pos ( " + p2.x + ", " + p2.y + ", " + p2.z + ") : " +
                    dstVtxPosIdxCountList[index2] + "</color>",
                    labelStyle);

                EditorGUI.indentLevel++;
                if (isDisplayVertexNormals)
                {
                    EditorGUILayout.LabelField(
                        "normal of green:",
                        "  <color=green>Normal ( " + n2.x + ", " + n2.y + ", " + n2.z + ")</color>",
                        labelStyle);
                }
                else
                {
                    EditorGUILayout.LabelField("");
                }
                // if (isDisplayRecalculateNormals)
                // {
                //     EditorGUILayout.LabelField(
                //         "recalculate normal of green: ",
                //         "  <color=red>Normal ( " + tmpN2.x + ", " + tmpN2.y + ", " + tmpN2.z + ")</color>",
                //         labelStyle);
                // }
                if (isDisplayVertexColors == true && curDstTriangleData != null)
                {
                    EditorGUILayout.LabelField(
                        "colors of green: ",
                        "  <color=green>Colors ( " + c2.r + ", " + c2.g + ", " + c2.b + ", " + c2.a + ")</color>",
                        labelStyle);
                }
                else
                {
                    EditorGUILayout.LabelField("");
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.LabelField(
                    "Index of blue vertex: ",
                    "<color=blue>" + index3.ToString() + "     Pos ( " + p3.x + ", " + p3.y + ", " + p3.z + ") : " +
                    dstVtxPosIdxCountList[index3] + "</color>",
                    labelStyle);
                
                EditorGUI.indentLevel++;
                if (isDisplayVertexNormals)
                {
                    EditorGUILayout.LabelField(
                        "normal of blue: ",
                        "  <color=blue>Normal ( " + n3.x + ", " + n3.y + ", " + n3.z + ")</color>",
                        labelStyle);
                }
                else
                {
                    EditorGUILayout.LabelField("");
                }
                // if (isDisplayRecalculateNormals)
                // {
                //     EditorGUILayout.LabelField(
                //         "recalculate normal of blue: ",
                //         "  <color=red>Normal ( " + tmpN3.x + ", " + tmpN3.y + ", " + tmpN3.z + ")</color>",
                //         labelStyle);
                // }
                if (isDisplayVertexColors == true && curDstTriangleData != null)
                {
                    EditorGUILayout.LabelField(
                        "colors of blue: ",
                        "  <color=blue>Colors ( " + c3.r + ", " + c3.g + ", " + c3.b + ", " + c3.a + ")</color>",
                        labelStyle);
                }
                else
                {
                    EditorGUILayout.LabelField("");
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            


            // EditorGUILayout.LabelField(
            //     "UV coords red vertex: ",
            //     "(" + dstMesh.uv[dstMesh.triangles[curDstFace * 3]].x.ToString() + ", " +
            //     dstMesh.uv[dstMesh.triangles[curDstFace * 3]].y.ToString() + ")");

            // EditorGUILayout.LabelField(
            //     "UV coords green vertex: ",
            //     "(" + dstMesh.uv[dstMesh.triangles[curDstFace * 3 + 1]].x.ToString() + ", " +
            //     dstMesh.uv[dstMesh.triangles[curDstFace * 3 + 1]].y.ToString() + ")");

            // EditorGUILayout.LabelField(
            //     "UV coords blue vertex: ",
            //     "(" + dstMesh.uv[dstMesh.triangles[curDstFace * 3 + 2]].x.ToString() + ", " +
            //     dstMesh.uv[dstMesh.triangles[curDstFace * 3 + 2]].y.ToString() + ")");

            EditorGUILayout.BeginVertical("box");
            // using (new EditorGUILayout.VerticalScope("box"))
            // {
                using (new EditorGUILayout.HorizontalScope())
                {
                    isDisplayIntersectionPoint = EditorGUILayout.ToggleLeft(
                        "Display Intersection Point with Src Obj",
                        isDisplayIntersectionPoint);

                    if ( GUILayout.Button( "Set All IntesesectPoint", GUILayout.Width( 200.0f ), GUILayout.Height(20) ) )
                    {
                        var sw = new System.Diagnostics.Stopwatch();
                        sw.Start();

                        SetIntersectedPoint(1);
                        SetIntersectedPoint(2);
                        SetIntersectedPoint(3);
                        
                        // 計測停止
                        sw.Stop();

                        // 結果表示
                        Debug.Log("■処理Aにかかった時間");
                        TimeSpan ts = sw.Elapsed;
                        Debug.Log(ts);
                        Debug.Log(ts.Minutes + "分 : " + ts.Seconds + "秒 : " + ts.Milliseconds + "ミリ秒");
                        Debug.Log("経過 " + sw.ElapsedMilliseconds + " ミリ秒");
                    }
                }

                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    newSrcFace = EditorGUILayout.IntField("Jump to src face index: ", curSrcFace);
                    if (newSrcFace != curSrcFace)
                    {
                        if (newSrcFace >= 0 && newSrcFace < numSrcFaces)
                        {
                            curSrcFace = newSrcFace;
                        }
                    }

                    if ( GUILayout.Button( "Set curDstFace IntesesectPoint", GUILayout.Width( 200.0f ), GUILayout.Height(20) ) )
                    {
                        var sw = new System.Diagnostics.Stopwatch();
                        sw.Start();

                        SetIntersectedPoint(1, false);
                        SetIntersectedPoint(2, false);
                        SetIntersectedPoint(3, false);
                        
                        // 計測停止
                        sw.Stop();

                        // 結果表示
                        Debug.Log("■処理Aにかかった時間");
                        TimeSpan ts = sw.Elapsed;
                        Debug.Log(ts);
                        Debug.Log(ts.Minutes + "分 : " + ts.Seconds + "秒 : " + ts.Milliseconds + "ミリ秒");
                        Debug.Log("経過 " + sw.ElapsedMilliseconds + " ミリ秒");
                    }
                }


                if (isDisplayIntersectionPoint == true)
                {
                    TAP.TriangleData targetSrcTriangleData = null;
                    int srcIdx1 = 0;
                    int srcIdx2 = 0;
                    int srcIdx3 = 0;
                    var srcVtx1 = Vector3.zero;
                    var srcVtx2 = Vector3.zero;
                    var srcVtx3 = Vector3.zero;

                    if (srcTriangleDataList != null)
                    {
                        targetSrcTriangleData = srcTriangleDataList[curSrcFace];
                        srcIdx1 = targetSrcTriangleData.vtxIdx1;
                        srcIdx2 = targetSrcTriangleData.vtxIdx2;
                        srcIdx3 = targetSrcTriangleData.vtxIdx3;
                        srcVtx1 = targetSrcTriangleData.vtxPos1;
                        srcVtx2 = targetSrcTriangleData.vtxPos2;
                        srcVtx3 = targetSrcTriangleData.vtxPos3;
                    }

                    using (new EditorGUILayout.HorizontalScope("box"))
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.ObjectField("Source Object", srcObj, typeof(GameObject), true);
                        if (EditorGUI.EndChangeCheck())
                        {
                            SetSrcMeshObject();
                        }

                        if( GUILayout.Button( "Set", GUILayout.Width( 32.0f ), GUILayout.Height(20) ) )
                        {
                            SetSrcMeshObject();
                        }
                    }
                    
                    isDisplaySrcTriangleIdx = EditorGUILayout.ToggleLeft(
                                                "Display Src Triangle Index",
                                                isDisplaySrcTriangleIdx);
                    
                    EditorGUILayout.Space();

                    var intersectionPoint_1_text = "";
                    if (intersectionPoint_1.Key != Vector3.positiveInfinity)
                    {
                        intersectionPoint_1_text = "     (" + intersectionPoint_1.Key.x + ", " + intersectionPoint_1.Key.y + ", " + intersectionPoint_1.Key.z + ")";
                    }
                    else
                    {
                        intersectionPoint_1_text = "     交差なし…";
                    }

                    if (targetSrcTriangleData != null)
                    {
                        EditorGUILayout.LabelField(
                            "Src Index of red vertex: ",
                            "<color=red>" + srcIdx1.ToString() +
                            "     Pos ( " + srcVtx1.x + ", " + srcVtx1.y + ", " + srcVtx1.z + ")</color>",
                            labelStyle);
                    }
                    EditorGUILayout.LabelField("Intersect Point of red: ", intersectionPoint_1_text);

                    if( GUILayout.Button( "Set IntersectionPoint 1", GUILayout.Width( 200.0f ), GUILayout.Height(20) ) )
                    {
                        SetIntersectedPoint(1);
                    }
                    // EditorGUILayout.LabelField(
                    //     "     Averaged Normal:",
                    //     "     (" + intersectionPoint_1.Value.x + ", " + intersectionPoint_1.Value.y + ", " + intersectionPoint_1.Value.z + ")");
                    
                    EditorGUILayout.Space();

                    var intersectionPoint_2_text = "";
                    if (intersectionPoint_2.Key != Vector3.positiveInfinity)
                    {
                        intersectionPoint_2_text = "     (" + intersectionPoint_2.Key.x + ", " +
                                                            intersectionPoint_2.Key.y + ", " +
                                                            intersectionPoint_2.Key.z + ")";
                    }
                    else
                    {
                        intersectionPoint_2_text = "     交差なし…";
                    }

                    if (targetSrcTriangleData != null)
                    {
                        EditorGUILayout.LabelField(
                            "Src Index of green vertex: ",
                            "<color=green>" + srcIdx2.ToString() +
                            "     Pos ( " + srcVtx2.x + ", " + srcVtx2.y + ", " + srcVtx2.z + ")</color>",
                            labelStyle);
                    }
                    EditorGUILayout.LabelField("Intersect Point of green: ", intersectionPoint_2_text);

                    if( GUILayout.Button( "Set IntersectionPoint 2", GUILayout.Width( 200.0f ), GUILayout.Height(20) ) )
                    {
                        SetIntersectedPoint(2);
                    }
                    // EditorGUILayout.LabelField(
                    //     "     Averaged Normal:",
                    //     "     (" + intersectionPoint_2.Value.x + ", " + intersectionPoint_2.Value.y + ", " + intersectionPoint_2.Value.z + ")");

                    EditorGUILayout.Space();

                    var intersectionPoint_3_text = "";
                    if (intersectionPoint_3.Key != Vector3.positiveInfinity)
                    {
                        intersectionPoint_3_text = "     (" + intersectionPoint_3.Key.x + ", " +
                                                            intersectionPoint_3.Key.y + ", " +
                                                            intersectionPoint_3.Key.z + ")";
                    }
                    else
                    {
                        intersectionPoint_3_text = "     交差なし…";
                    }

                    if (targetSrcTriangleData != null)
                    {
                        EditorGUILayout.LabelField(
                            "Src Index of blue vertex: ",
                            "<color=blue>" + srcIdx3.ToString() +
                            "     Pos ( " + srcVtx3.x + ", " + srcVtx3.y + ", " + srcVtx3.z + ")</color>",
                            labelStyle);
                    }
                    EditorGUILayout.LabelField("Intersect Point of blue: ", intersectionPoint_3_text);
                    
                    if( GUILayout.Button( "Set IntersectionPoint 3", GUILayout.Width( 200.0f ), GUILayout.Height(20) ) )
                    {
                        SetIntersectedPoint(3);
                    }
                    // EditorGUILayout.LabelField(
                        // "     Averaged Normal:",
                        // "     (" + intersectionPoint_3.Value.x + ", " +
                        //             intersectionPoint_3.Value.y + ", " +
                        //             intersectionPoint_3.Value.z + ")");

                    
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            isDisplayVoxelSpace = EditorGUILayout.ToggleLeft(
                                                    "Display Grid Voxel",
                                                    isDisplayVoxelSpace);

                            isDisplayVoxelSpaceAll = EditorGUILayout.ToggleLeft(
                                                    "Display Voxel All",
                                                    isDisplayVoxelSpaceAll);
                        }

                        if (isDisplayVoxelSpaceAll == false && voxelSpaceDict != null && voxelSpaceDict.Count > 0)
                        {
                            curVoxelSpaceIdx = EditorGUILayout.IntSlider(
                                                "Voxel List iIdex: ",
                                                curVoxelSpaceIdx,
                                                0,
                                                voxelSpaceDict.Count - 1);

                            var keyList = voxelSpaceDict.Keys.ToList();
                            var curVoxelAddress = keyList[curVoxelSpaceIdx];
                            var curVoxelBounds = voxelSpaceDict[curVoxelAddress];
                            
                            EditorGUILayout.LabelField(
                                "Voxel Address",
                                curVoxelAddress.x + " : " + curVoxelAddress.y + " : " + curVoxelAddress.z);
                            
                            EditorGUILayout.LabelField(
                                "Voxel Center",
                                curVoxelBounds.center.x + " : " +
                                curVoxelBounds.center.y + " : " +
                                curVoxelBounds.center.z);

                            EditorGUILayout.LabelField(
                                "Voxel Size",
                                curVoxelBounds.size.x + " : " +
                                curVoxelBounds.size.y + " : " +
                                curVoxelBounds.size.z);

                            // EditorGUILayout.LabelField(
                            //     "Voxel Max",
                            //     curVoxel.voxelBounds.max.x + " : " +
                            //     curVoxel.voxelBounds.max.y + " : " +
                            //     curVoxel.voxelBounds.max.z);

                            // EditorGUILayout.LabelField(
                            //     "Voxel Min",
                            //     curVoxel.voxelBounds.min.x + " : " +
                            //     curVoxel.voxelBounds.min.y + " : " +
                            //     curVoxel.voxelBounds.min.z);
                        }

                        using (new EditorGUILayout.HorizontalScope("box"))
                        {
                            // if (GUILayout.Button ("Check Voxel Space List"))
                            // {
                            //     Debug.Log("====================================================================================");

                            //     foreach (var voxelSpace in voxelSpaceDict)
                            //     {
                            //         var ads = voxelSpace.Key;
                            //         var bds = voxelSpace.Value;
                            //         Debug.Log(
                            //             ads.x + " _ " + ads.y + " _ " + ads.z +
                            //             "  :  Voxel Size = " + bds.size.x + " , " + bds.size.y + " , " + bds.size.z +
                            //             "  :  Voxel Center = " + bds.center.x + " , " + bds.center.y + " , " + bds.center.z );
                            //     }
                            // }

                            if (GUILayout.Button ("Check Belonging Space Of Triangle Dict"))
                            {
                                Debug.Log("====================================================================================");

                                foreach (var srcBelongingSpaceOfTriangle in srcBelongingSpaceOfVtxPosDict)
                                {
                                    var srcAddress  = srcBelongingSpaceOfTriangle.Key;
                                    var srcPosList = srcBelongingSpaceOfTriangle.Value;
                                    foreach (var srcPos in srcPosList)
                                    {
                                        Debug.Log("<color=red>" + srcAddress.x + " _ " + srcAddress.y + " _ " + srcAddress.z +
                                                "  :  srcPos = " + srcPos.x + " , " + srcPos.y + " , " + srcPos.z + "</color>");
                                    }
                                    Debug.Log("");
                                }

                                Debug.Log("");

                                foreach (var dstBelongingSpaceOfTriangle in dstBelongingSpaceOfVtxPosDict)
                                {
                                    var dstAddress  = dstBelongingSpaceOfTriangle.Key;
                                    var dstPosList = dstBelongingSpaceOfTriangle.Value;
                                    foreach (var dstPos in dstPosList)
                                    {
                                        Debug.Log("<color=green>" + dstAddress.x + " _ " + dstAddress.y + " _ " + dstAddress.z +
                                                "  :  dstPos = " + dstPos.x + " , " + dstPos.y + " , " + dstPos.z + "</color>");
                                    }
                                    Debug.Log("");
                                }
                            }

                            EditorGUILayout.Space();

                            if (GUILayout.Button ("Check Current Voxel In Voxel Space"))
                            {
                                Debug.Log("====================================================================================");

                                // foreach (var voxelSpace in voxelSpaceDict)
                                // {
                                //     var ads = voxelSpace.Key;
                                //     var bds = voxelSpace.Value;
                                //     Debug.Log(
                                //         ads.x + " _ " + ads.y + " _ " + ads.z +
                                //         "  :  Voxel Size = " + bds.size.x + " , " + bds.size.y + " , " + bds.size.z +
                                //         "  :  Voxel Center = " + bds.center.x + " , " + bds.center.y + " , " + bds.center.z );
                                // }

                                Debug.Log("curTriangleData.centerPos = " +
                                            curDstTriangleData.centerPos.x + ", " +
                                            curDstTriangleData.centerPos.y + ", " +
                                            curDstTriangleData.centerPos.z);
                                
                                // var curVoxelAddress = curBelongingSpaceOfTriangleDict.FirstOrDefault(
                                //                         x => x.Value.Contains(curTriangleData.centerPos)).Key;
                                
                                var minusVector = new Vector3(-1, -1, -1);
                                Vector3 dstVoxelAddress = minusVector;

                                Debug.Log("dstBelongingSpaceOfTriangleDict Count = " + dstBelongingSpaceOfVtxPosDict.Count);

                                foreach (var dstBelongingSpaceAddress in dstBelongingSpaceOfVtxPosDict.Keys)
                                {
                                    var dstPosList = dstBelongingSpaceOfVtxPosDict[dstBelongingSpaceAddress];

                                    foreach (var dstPos in dstPosList)
                                    {
                                        // Debug.Log("dstPos = " + dstPos.x + ", " + dstPos.y + ", " + dstPos.z);
                                        if (dstPos == curDstTriangleData.centerPos)
                                        {
                                            Debug.Log("あった！！！！");
                                            dstVoxelAddress = dstBelongingSpaceAddress;
                                            break;
                                        }
                                    }
                                    // if (dstPosList.Contains(curDstTriangleData.centerPos))
                                    // {
                                    //     Debug.Log("あった！！！！");
                                    //     dstVoxelAddress = dstBelongingSpaceAddress;
                                    //     break;
                                    // }
                                }

                                Debug.Log("dstVoxelAddress = " + dstVoxelAddress);

                                if (dstVoxelAddress != minusVector)
                                {
                                    var srcTriangleDataCenterPosList = srcBelongingSpaceOfVtxPosDict[dstVoxelAddress];

                                    // foreach (var srcTriangleDataCenterPos in srcTriangleDataCenterPosList)
                                    // {
                                    //     Debug.Log("srcTriangleDataCenterPos = " + srcTriangleDataCenterPos.x + " , " + srcTriangleDataCenterPos.y + " , " + srcTriangleDataCenterPos.z);
                                    // }
                                    Debug.Log("srcTriangleDataCenterPosList Count = " + srcTriangleDataCenterPosList.Count);
                                }
                            }
                        }
                    }
                    
                    using (new EditorGUILayout.HorizontalScope("box"))
                    {
                        if (GUILayout.Button ("Triangle Output"))
                        {
                            // foreach(var srcTriangle in srcTriangleDataList)
                            // {
                            //     Debug.Log("vertexPos1 ( " + srcTriangle.vertexPos1.x + ", " + srcTriangle.vertexPos1.y + ", " + srcTriangle.vertexPos1.z + " )");
                            //     Debug.Log("vertexPos2 ( " + srcTriangle.vertexPos2.x + ", " + srcTriangle.vertexPos2.y + ", " + srcTriangle.vertexPos2.z + " )");
                            //     Debug.Log("vertexPos3 ( " + srcTriangle.vertexPos3.x + ", " + srcTriangle.vertexPos3.y + ", " + srcTriangle.vertexPos3.z + " )");
                            //     Debug.Log("");
                            // }

                            if (dstVertexMap != null)
                            {
                                foreach (var key in dstVertexMap.Keys)
                                {
                                    // Debug.Log("curVertexMap key ( " + key.x + ", " + key.y + ", " + key.z + " )");
                                    var indexList = dstVertexMap[key];
                                    if (key == p1)
                                    {
                                        Debug.Log("=====================================================");
                                        Debug.Log("p1 (red) と同じ位置の頂点は "  + indexList.Count + " 個でした");
                                        // foreach (var idx in indexList)
                                        // {
                                        //     Debug.Log();
                                        // }
                                        // Debug.LogWarning("     ありました : " + indexUnite);
                                    }
                                    else if (key == p2)
                                    {
                                        Debug.Log("=====================================================");
                                        Debug.Log("p2 (green) と同じ位置の頂点は "  + indexList.Count + " 個でした");
                                        // foreach (var idx in indexList)
                                        // {
                                        //     Debug.Log();
                                        // }
                                        // Debug.LogWarning("     ありました : " + indexUnite);
                                    }
                                    else if (key == p3)
                                    {
                                        Debug.Log("=====================================================");
                                        Debug.Log("p3 (blue) と同じ位置の頂点は "  + indexList.Count + " 個でした");
                                        // foreach (var idx in indexList)
                                        // {
                                        //     Debug.Log();
                                        // }
                                        // Debug.LogWarning("     ありました : " + indexUnite);
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogError("     curVertexMap が null です・・・");
                            }
                        }
                        // intersectionSphereSize = EditorGUILayout.FloatField("Intersection Size", intersectionSphereSize, GUILayout.Width(220));
                    }

                }
            // }
            EditorGUILayout.EndVertical();
            

            // using (new EditorGUILayout.VerticalScope("box"))
            // {
            //     isDisplayUV3 = EditorGUILayout.ToggleLeft("Display UV3", isDisplayUV3);

            //     if (isDisplayUV3 == true)
            //     {
            //         EditorGUILayout.LabelField(
            //             "     (" + uv3List[index1].x + ", " + uv3List[index1].y + ", " + uv3List[index1].z + ")");
                
            //         EditorGUILayout.Space();

            //         EditorGUILayout.LabelField(
            //             "     (" + uv3List[index2].x + ", " + uv3List[index2].y + ", " + uv3List[index2].z + ")");

            //         EditorGUILayout.Space();

            //         EditorGUILayout.LabelField(
            //             "     (" + uv3List[index3].x + ", " + uv3List[index3].y + ", " + uv3List[index3].z + ")");
            //     }
            // }
        
            // using (new EditorGUILayout.VerticalScope("box"))
            // {
            //     isDisplayUV2 = EditorGUILayout.ToggleLeft("Display UV2 (Quaternion)", isDisplayUV2);

            //     if (isDisplayUV2 == true && uv2List != null && uv2List.Count != 0)
            //     {
            //         // Debug.Log("uv2List length = " + uv2List.Count() );
            //         var tempVec1 = QuaternionTransformVector(n1, uv2List[index1]);
            //         var tempVec2 = QuaternionTransformVector(n2, uv2List[index2]);
            //         var tempVec3 = QuaternionTransformVector(n3, uv2List[index3]);

            //         // EditorGUILayout.LabelField(
            //         //     "     (" + uv2List[curDstFace * 3].x + ", " + uv2List[curDstFace * 3].y + ", " + uv2List[curDstFace * 3].z + ")");
                
            //         // EditorGUILayout.Space();

            //         // EditorGUILayout.LabelField(
            //         //     "     (" + uv2List[curDstFace * 3 + 1].x + ", " + uv2List[curDstFace * 3 + 1].y + ", " + uv2List[curDstFace * 3 + 1].z + ")");

            //         // EditorGUILayout.Space();

            //         // EditorGUILayout.LabelField(
            //         //     "     (" + uv2List[curDstFace * 3 + 2].x + ", " + uv2List[curDstFace * 3 + 2].y + ", " + uv2List[curDstFace * 3 + 2].z + ")");

            //         EditorGUILayout.LabelField(
            //             "     (" + tempVec1.x + ", " + tempVec1.y + ", " + tempVec1.z + ")");
                
            //         EditorGUILayout.Space();

            //         EditorGUILayout.LabelField(
            //             "     (" + tempVec2.x + ", " + tempVec2.y + ", " + tempVec2.z + ")");

            //         EditorGUILayout.Space();

            //         EditorGUILayout.LabelField(
            //             "     (" + tempVec3.x + ", " + tempVec3.y + ", " + tempVec3.z + ")");
            //     }
            // }
        }

        private void SetIntersectedPoint(int index, bool useSrcTriangleDataList = true)
        {
            if (srcMesh != null && dstMesh.name != srcMesh.name)
            {
                if (srcTriangleDataList != null && isDisplayIntersectionPoint == true)
                {
                    // intersectionPoint_1 = GetIntersectionPoint(1, dstMesh.vertices[index1], dstMesh, dstTrans, srcTriangleDataList);
                    
                    // intersectionPoint_2 = GetIntersectionPoint(2, dstMesh.vertices[index2], dstMesh, dstTrans, srcTriangleDataList);

                    // intersectionPoint_3 = GetIntersectionPoint(3, dstMesh.vertices[index3], dstMesh, dstTrans, srcTriangleDataList);
                    
                    // intersectionPoint_1 = new KeyValuePair<Vector3, Vector3>(Vector3.one, Vector3.one);
                    var targetSrcTriangleData = srcTriangleDataList[curSrcFace];
                    var tmpSrcTriangleDataList = new List<TAP.TriangleData>(){targetSrcTriangleData};

                    if (index == 1)
                    {
                        if (useSrcTriangleDataList)
                        {
                            intersectionPoint_1 = GetIntersectionPoint(1, p1, dstMesh, dstTrans, srcTriangleDataList);
                        }
                        else
                        {
                            intersectionPoint_1 = GetIntersectionPoint(1, p1, dstMesh, dstTrans, tmpSrcTriangleDataList);
                        }
                    }
                    else if (index == 2)
                    {
                        if (useSrcTriangleDataList)
                        {
                            intersectionPoint_2 = GetIntersectionPoint(2, p2, dstMesh, dstTrans, srcTriangleDataList);
                        }
                        else
                        {
                            intersectionPoint_2 = GetIntersectionPoint(2, p2, dstMesh, dstTrans, tmpSrcTriangleDataList);
                        }
                    }
                    else if (index == 3)
                    {
                        if (useSrcTriangleDataList)
                        {
                            intersectionPoint_3 = GetIntersectionPoint(3, p3, dstMesh, dstTrans, srcTriangleDataList);
                        }
                        else
                        {
                            intersectionPoint_3 = GetIntersectionPoint(3, p3, dstMesh, dstTrans, tmpSrcTriangleDataList);
                        }
                    }
                    // Debug.Log("交点1 = " + intersectionPoint_1 + "  :  交点2 = " + intersectionPoint_2 + "  :  交点3 = " + intersectionPoint_3 + " : centerP = " + centerP);
                }
            }
        }


        private void SetSrcMeshObject()
        {
            srcObj = Selection.activeGameObject;

            if (srcObj == null)
            {
                Debug.LogWarning("何も選択されていません");
                return;
            }

            srcMesh       = null;
            srcMeshFilter = null;
            srcSMR        = null;

            Mesh curSrcMesh = null;

            var curSrcMeshFilter = srcObj.GetComponentInChildren<MeshFilter>();
            var curSrcSMR = srcObj.GetComponentInChildren<SkinnedMeshRenderer>();

            if (curSrcMeshFilter != null)
            {
                curSrcMesh  = curSrcMeshFilter.sharedMesh;
                srcMeshFilter = curSrcMeshFilter;
            }
            else
            {
                if (curSrcSMR != null)
                {
                    curSrcMesh = curSrcSMR.sharedMesh;
                    srcSMR = curSrcSMR;
                }
                else
                {
                    Debug.LogWarning("Meshを持つオブジェクトが選択されていません");
                    return;
                }
            }

            srcTrans = srcObj.transform;

            srcMesh = curSrcMesh;
            
            // var hoge = false;
            // var fuga = false;

            TAP.GetTriangleDataList(
                srcMesh,
                srcMeshFilter,
                srcSMR,
                true,
                true,
                out srcVtxPosIdxDict,
                out srcVtxPosTriangleIdxDict,
                out srcTriangleDataList);
            
            // numSrcFaces = curSrcMesh.triangles.Length / 3;
            numSrcFaces = srcTriangleDataList != null ? srcTriangleDataList.Count : 0;

            curSrcFace = 0;
            newSrcFace = curSrcFace;


            if (dstMesh != null && dstTrans != null)
            {
                var dstMeshBounds = dstMesh.bounds;
                Vector3 dstMeshBoundsMax;
                Vector3 dstMeshBoundsMin;
                if (dstTrans != null)
                {
                    dstMeshBoundsMax = Vector3.Scale(dstMeshBounds.max, dstTrans.lossyScale);
                    dstMeshBoundsMin = Vector3.Scale(dstMeshBounds.min, dstTrans.lossyScale);
                }
                else
                {
                    dstMeshBoundsMax = dstMeshBounds.max;
                    dstMeshBoundsMin = dstMeshBounds.min;
                }


                var srcMeshBounds = srcMesh.bounds;
                Vector3 srcMeshBoundsMax;
                Vector3 srcMeshBoundsMin;
                if (srcTrans != null)
                {
                    srcMeshBoundsMax = Vector3.Scale(srcMeshBounds.max, srcTrans.lossyScale);
                    srcMeshBoundsMin = Vector3.Scale(srcMeshBounds.min, srcTrans.lossyScale);
                }
                else
                {
                    srcMeshBoundsMax = srcMeshBounds.max;
                    srcMeshBoundsMin = srcMeshBounds.min;
                }

                TAP.RegisterBelongingSpaceOfVtxPosInMeshes(
                    srcMeshBoundsMax,
                    srcMeshBoundsMin,
                    srcTriangleDataList,
                    ref srcBelongingSpaceOfVtxPosDict,
                    dstMeshBoundsMax,
                    dstMeshBoundsMin,
                    dstTriangleDataList,
                    ref dstBelongingSpaceOfVtxPosDict,
                    ref voxelSpaceSize,
                    ref voxelSpaceDict);

                // Debug.Log("===============================================================================");
                // foreach (var voxelSpace in voxelSpaceDict.Keys)
                // {
                //     Debug.Log("voxelSpace = " + voxelSpace.x + " , " + voxelSpace.y + " , " + voxelSpace.z);
                // }
            }
            this.Repaint();
        }


        // Analogous to OnSceneGUI message handler of the Editor class
        void RenderStuff(SceneView sceneView)
        {
            if (meshAvailable)
            {
                int index1 = dstMesh.triangles[curDstFace * 3 ];
                int index2 = dstMesh.triangles[curDstFace * 3 + 1];
                int index3 = dstMesh.triangles[curDstFace * 3 + 2];

    //            p1 = dstMesh.vertices[index1];
    //            p2 = dstMesh.vertices[index2];
    //            p3 = dstMesh.vertices[index3];

                if (dstMeshFilter)
                {
                    p1 = dstMeshFilter.transform.TransformPoint(dstMesh.vertices[index1]);
                    p2 = dstMeshFilter.transform.TransformPoint(dstMesh.vertices[index2]);
                    p3 = dstMeshFilter.transform.TransformPoint(dstMesh.vertices[index3]);

                    // n1 = dstMeshFilter.transform.TransformPoint(dstMesh.normals[index1]);
                    // n2 = dstMeshFilter.transform.TransformPoint(dstMesh.normals[index2]);
                    // n3 = dstMeshFilter.transform.TransformPoint(dstMesh.normals[index3]);
                }
                else if (dstSMR)
                {
                    p1 = dstSMR.transform.TransformPoint(dstMesh.vertices[index1]);
                    p2 = dstSMR.transform.TransformPoint(dstMesh.vertices[index2]);
                    p3 = dstSMR.transform.TransformPoint(dstMesh.vertices[index3]);

                    // n1 = dsrSMR.transform.TransformPoint(dstMesh.normals[index1]);
                    // n2 = dsrSMR.transform.TransformPoint(dstMesh.normals[index2]);
                    // n3 = dsrSMR.transform.TransformPoint(dstMesh.normals[index3]);
                }
                else
                {
                    p1 = dstMesh.vertices[index1];
                    p2 = dstMesh.vertices[index2];
                    p3 = dstMesh.vertices[index3];
                }

                centerP = new Vector3(
                    (p1.x + p2.x + p3.x) / 3,
                    (p1.y + p2.y + p3.y) / 3,
                    (p1.z + p2.z + p3.z) / 3);

                var edge1 = p2 - p1;
                var edge2 = p3 - p1;

                centerNormal = Vector3.Cross(edge1.normalized, edge2.normalized).normalized;

                if (isDisplayAveragedNormals == true)
                {
                    n1 = dstAveragedNormalArray[index1];
                    n2 = dstAveragedNormalArray[index2];
                    n3 = dstAveragedNormalArray[index3];
                }
                else
                {
                    n1 = dstMesh.normals[index1];
                    n2 = dstMesh.normals[index2];
                    n3 = dstMesh.normals[index3];
                }

                if (isDisplayVertexColors == true)
                {
                    c1 = curDstTriangleData.vtxCol1;
                    c2 = curDstTriangleData.vtxCol2;
                    c3 = curDstTriangleData.vtxCol3;
                }

                // tmpN1 = dstTempMesh.normals[index1];
                // tmpN2 = dstTempMesh.normals[index2];
                // tmpN3 = dstTempMesh.normals[index3];


                Handles.color = Color.red;
                // Handles.DotCap(0, p1, Quaternion.identity, 0.01f*HandleUtility.GetHandleSize(p1));
                Handles.DotHandleCap(
                    0, p1, Quaternion.identity,
                    0.01f * HandleUtility.GetHandleSize(p1),
                    EventType.Repaint);

                Handles.color = Color.green;
                // Handles.DotCap(0, p2, Quaternion.identity, 0.01f*HandleUtility.GetHandleSize(p2));
                Handles.DotHandleCap(
                    0, p2, Quaternion.identity,
                    0.01f * HandleUtility.GetHandleSize(p2),
                    EventType.Repaint);

                Handles.color = Color.blue;
                // Handles.DotCap(0, p3, Quaternion.identity, 0.01f*HandleUtility.GetHandleSize(p3));
                Handles.DotHandleCap(
                    0, p3, Quaternion.identity,
                    0.01f * HandleUtility.GetHandleSize(p3),
                    EventType.Repaint);

                Handles.color = Color.black;
                // Handles.DotCap(0, centerP, Quaternion.identity, 0.01f*HandleUtility.GetHandleSize(centerP));
                Handles.DotHandleCap(
                    0, centerP, Quaternion.identity,
                    0.01f * HandleUtility.GetHandleSize(centerP),
                    EventType.Repaint);

                Handles.color = Color.magenta;
                // Handles.DotCap(0, centerNormal, Quaternion.identity, 0.01f*HandleUtility.GetHandleSize(centerNormal));
                Handles.DotHandleCap(
                    0, centerNormal, Quaternion.identity,
                    0.01f * HandleUtility.GetHandleSize(centerNormal),
                    EventType.Repaint);

                Handles.color = Color.yellow;
                Handles.DrawDottedLine(p1, p2, 5f);
                Handles.DrawDottedLine(p2, p3, 5f);
                Handles.DrawDottedLine(p3, p1, 5f);

                // Handles.DrawDottedLine(centerP, normal, 5f);
                Handles.DrawDottedLine(centerP, centerP + centerNormal, 5f);
                // Handles.DrawDottedLine(centerP, centerNormal_w, 5f);
                // Handles.DrawDottedLine(centerP, normal_w, 5f);
                
                if (useAveragedVertexNormals == false && drawSphereFromVtxPos == true)
                {
                    Handles.color = Color.red;
                    Handles.SphereHandleCap(0, p1, Quaternion.identity, sphereRadius, EventType.Repaint);
                    // Handles.SphereCap(0, p1, Quaternion.identity, sphereRadius * 2);
                    Handles.color = Color.green;
                    Handles.SphereHandleCap(0, p2, Quaternion.identity, sphereRadius, EventType.Repaint);
                    // Handles.SphereCap(0, p2, Quaternion.identity, sphereRadius * 2);
                    Handles.color = Color.blue;
                    Handles.SphereHandleCap(0, p3, Quaternion.identity, sphereRadius, EventType.Repaint);
                    // Handles.SphereCap(0, p3, Quaternion.identity, sphereRadius * 2);
                }
                
                // 頂点法線を表示
                if (isDisplayVertexNormals)
                {
                    Handles.color = Color.blue;
                    Handles.DrawDottedLine(p1, p1 + n1, 5f);
                    Handles.DrawDottedLine(p2, p2 + n2, 5f);
                    Handles.DrawDottedLine(p3, p3 + n3, 5f);
                }
                // if (isDisplayRecalculateNormals)
                // {
                //     Handles.color = Color.cyan;
                //     Handles.DrawDottedLine(p1, p1 + tmpN1, 5f);
                //     Handles.DrawDottedLine(p2, p2 + tmpN2, 5f);
                //     Handles.DrawDottedLine(p3, p3 + tmpN3, 5f);
                // }

                // // Quaternionから計算して平均化した法線を表示
                // if (isDisplayAveragedNormalsFromQuaternion == true && uv2List != null && uv2List.Count != 0)
                // {
                //     var tempVec1 = QuaternionTransformVector(n1, uv2List[index1]);
                //     var tempVec2 = QuaternionTransformVector(n2, uv2List[index2]);
                //     var tempVec3 = QuaternionTransformVector(n3, uv2List[index3]);
                //     Handles.color = Color.red;

                //     Handles.DrawDottedLine(p1, p1 + tempVec1, 5f);
                //     Handles.DrawDottedLine(p2, p2 + tempVec2, 5f);
                //     Handles.DrawDottedLine(p3, p3 + tempVec3, 5f);
                // }

                // // 平均化した法線を表示
                if (isDisplayAveragedNormals == true)// && uv3List != null && uv3List.Count != 0)
                {
                    Handles.color = Color.green;

                    // Handles.DrawDottedLine(p1, p1 + uv3List[index1], 5f);
                    // Handles.DrawDottedLine(p2, p2 + uv3List[index2], 5f);
                    // Handles.DrawDottedLine(p3, p3 + uv3List[index3], 5f);
                    Handles.DrawDottedLine(p1, p1 + dstAveragedNormalArray[index1], 5f);
                    Handles.DrawDottedLine(p2, p2 + dstAveragedNormalArray[index2], 5f);
                    Handles.DrawDottedLine(p3, p3 + dstAveragedNormalArray[index3], 5f);
                }


                if (centredStyle == null)
                {
                    centredStyle = new GUIStyle(GUI.skin.label);
                    centredStyle.normal.textColor = Color.black;
                    // If you regularly inspect meshes with more than 999 vertices you may wish to increase this value
                    centredStyle.fixedWidth = 20; 
                    centredStyle.fixedHeight = 10;
                    centredStyle.alignment = TextAnchor.MiddleCenter;
                    // For reference, whiteTexture is 4x4 (not particularly relevant here)
                    centredStyle.normal.background = Texture2D.blackTexture;
                    centredStyle.fontSize = 12;
                    centredStyle.clipping = TextClipping.Overflow;
                }
                
                Handles.Label(p1, index1.ToString(), centredStyle);
                Handles.Label(p2, index2.ToString(), centredStyle);
                Handles.Label(p3, index3.ToString(), centredStyle);

                if (srcTriangleDataList != null)
                {
                    var targetSrcTriangleData = srcTriangleDataList[curSrcFace];
                    centredStyle.normal.textColor = Color.grey;

                    Handles.Label(
                        targetSrcTriangleData.vtxPos1,
                        "src_" + targetSrcTriangleData.vtxIdx1.ToString(),
                        centredStyle);
                    Handles.Label(
                        targetSrcTriangleData.vtxPos2,
                        "src_" + targetSrcTriangleData.vtxIdx2.ToString(),
                        centredStyle);
                    Handles.Label(
                        targetSrcTriangleData.vtxPos3,
                        "src_" + targetSrcTriangleData.vtxIdx3.ToString(),
                        centredStyle);

                    Handles.color = Color.magenta;
                    Handles.DrawDottedLine(
                        targetSrcTriangleData.centerPos,
                        targetSrcTriangleData.centerPos + targetSrcTriangleData.faceNml,
                        5f);
                }

                // Handles.Label(centerP, "Center", centredStyle);
                // Handles.Label(centerP + centerNormal, "Center Normal", centredStyle);
                
    //            Handles.Label(normal, "Normal", centredStyle);

    //            Handles.Label(centerNormal_w, "Center Normal_w", centredStyle);
    //            Handles.Label(normal_w, "Normal_w", centredStyle);

                // var pos1 = Vector3.zero;
                // var pos2 = Vector3.zero;
                // var pos3 = Vector3.zero;
                // if (newFace != curDstFace)
                // {
                // if (srcMesh != null && dstMesh.name != srcMesh.name)
                // {
                //     if (srcTriangleDataList != null && isDisplayIntersectionPoint == true)
                //     {
                //         // intersectionPoint_1 = GetIntersectionPoint(1, dstMesh.vertices[index1], dstMesh, dstTrans, srcTriangleDataList);
                        
                //         // intersectionPoint_2 = GetIntersectionPoint(2, dstMesh.vertices[index2], dstMesh, dstTrans, srcTriangleDataList);

                //         // intersectionPoint_3 = GetIntersectionPoint(3, dstMesh.vertices[index3], dstMesh, dstTrans, srcTriangleDataList);

                //         intersectionPoint_1 = GetIntersectionPoint(1, p1, dstMesh, dstTrans, srcTriangleDataList);
                        
                //         intersectionPoint_2 = GetIntersectionPoint(2, p2, dstMesh, dstTrans, srcTriangleDataList);

                //         intersectionPoint_3 = GetIntersectionPoint(3, p3, dstMesh, dstTrans, srcTriangleDataList);
                        
                //         // Debug.Log("交点1 = " + intersectionPoint_1 + "  :  交点2 = " + intersectionPoint_2 + "  :  交点3 = " + intersectionPoint_3 + " : centerP = " + centerP);
                //     }
                // }
                // else
                // {
                //     Debug.Log("(srcMesh != null && dstMesh.name != srcMesh.name) じゃない  :  srcMesh = " +
                //         srcMesh.name + " : dstMesh = " + dstMesh.name);
                // }
                    
                // }

                if (isDisplaySrcTriangleIdx == true && srcTriangleDataList != null)
                {
                    var srcTriangleStyle = new GUIStyle(GUI.skin.label);
                    srcTriangleStyle.normal.textColor = Color.cyan;
                    srcTriangleStyle.clipping = TextClipping.Clip;
                    srcTriangleStyle.fontSize = 12;
                    foreach (var srcTriangleData in srcTriangleDataList)
                    {
                        Handles.DrawDottedLine(
                            srcTriangleData.centerPos,
                            srcTriangleData.centerPos + srcTriangleData.faceNml,
                            5f);
                        Handles.Label(
                            srcTriangleData.centerPos + srcTriangleData.faceNml,
                            "src_" + srcTriangleData.triangleIdx.ToString(),
                            srcTriangleStyle);
                    }
                }

                if (isDisplayIntersectionPoint)
                {
                    if (intersectedStyle == null)
                    {
                        intersectedStyle = new GUIStyle(GUI.skin.label);
                        intersectedStyle.normal.textColor = Color.white;
                        // If you regularly inspect meshes with more than 999 vertices you may wish to increase this value
                        intersectedStyle.fixedWidth = 20; 
                        intersectedStyle.fixedHeight = 10;
                        intersectedStyle.alignment = TextAnchor.MiddleCenter;
                        // For reference, whiteTexture is 4x4 (not particularly relevant here)
                        intersectedStyle.normal.background = Texture2D.blackTexture;
                        intersectedStyle.fontSize = 16;
                        intersectedStyle.clipping = TextClipping.Overflow;
                    }
                    

                    Handles.color = Handles.xAxisColor;
                    // Handles.color = new Color(1f, 0f, 0f, 0.4f);
                    Handles.DrawLine(p1, intersectionPoint_1.Key);

                    // Handles.SphereHandleCap(
                    //     0, intersectionPoint_1.Key, Quaternion.identity, 0.05f, EventType.Repaint);

                    Handles.Label(
                        intersectionPoint_1.Key,
                        "①",
                        // "p1 (" + intersectionPoint_1.Key.x + ", " +
                        // intersectionPoint_1.Key.y + ", " +
                        // intersectionPoint_1.Key.z + ")",
                        intersectedStyle);

                    

                    Handles.color = Handles.yAxisColor;
                    // Handles.color = new Color(0f, 1f, 0f, 0.4f);
                    Handles.DrawLine(p2, intersectionPoint_2.Key);

                    // Handles.SphereHandleCap(
                    //     0, intersectionPoint_2.Key, Quaternion.identity, 0.05f, EventType.Repaint);
                    Handles.Label(
                        intersectionPoint_2.Key,
                        "②",
                        // "p2 (" + intersectionPoint_2.Key.x + ", " +
                        // intersectionPoint_2.Key.y + ", "
                        // + intersectionPoint_2.Key.z + ")",
                        intersectedStyle);
                

                    Handles.color = Handles.zAxisColor;
                    // Handles.color = new Color(0f, 0f, 1f, 0.4f);
                    Handles.DrawLine(p3, intersectionPoint_3.Key);

                    // Handles.SphereHandleCap(
                    //     0, intersectionPoint_3.Key, Quaternion.identity, 0.05f, EventType.Repaint);
                    Handles.Label(
                        intersectionPoint_3.Key,
                        "③",
                        // "p3 (" + intersectionPoint_3.Key.x + ", " +
                        // intersectionPoint_3.Key.y + ", " +
                        // intersectionPoint_3.Key.z + ")",
                        intersectedStyle);
                    
                }


                if (isDisplayVoxelSpace == true && voxelSpaceDict != null && voxelSpaceSize != Mathf.Infinity)
                {
                    var voxelCount = voxelSpaceDict.Count;
                    var hColorBase = (float)1 / voxelCount;

                    if (isDisplayVoxelSpaceAll == true)
                    {
                        for (var i = 0; i < voxelCount; i++)
                        {
                            var keyList = voxelSpaceDict.Keys.ToList();
                            var bounds  = voxelSpaceDict[keyList[i]];

                            Handles.color = new Color(hColorBase * i, hColorBase * i, hColorBase * i);
                            Handles.DrawWireCube(bounds.center, bounds.size);
                        }
                    }
                    else
                    {
                        Handles.color = new Color(
                                            hColorBase * curVoxelSpaceIdx,
                                            hColorBase * curVoxelSpaceIdx,
                                            hColorBase * curVoxelSpaceIdx);

                        var keyList = voxelSpaceDict.Keys.ToList();
                        var bounds  = voxelSpaceDict[keyList[curVoxelSpaceIdx]];

                        Handles.DrawWireCube(bounds.center, bounds.size);
                    }
                }

                sceneView.Repaint();
            }
        }


        // Tupleが標準で使えないのでとりあえずKeyValuePairで（Keyが頂点、Valueが法線）
        public KeyValuePair<Vector3, Vector3> GetIntersectionPoint (
            int index,
            Vector3 vertexPos,
            Mesh mesh,
            Transform trans,
            List<TAP.TriangleData> triangleDataList)
        {
            var vxtIndex = 0;
            if (index == 1)
            {
                vxtIndex = dstMesh.triangles[curDstFace * 3 + 0];
            }
            else if (index == 2)
            {
                vxtIndex = dstMesh.triangles[curDstFace * 3 + 1];
            }
            else if (index == 3)
            {
                vxtIndex = dstMesh.triangles[curDstFace * 3 + 2];
            }

            var normals         = mesh.normals;
            var averagedNormal  = Vector3.zero;

            var vertices = mesh.vertices;

            // var count = 0;
            var mapCount = dstVertexMap.Count;

            // var intersectPoint = Vector3.zero;

            // var vertexPosLocal = trans.InverseTransformPoint(vertexPos);
            // if (curVertexMap.ContainsKey(vertexPos) == false)
            // {
                // Debug.LogWarning("curVertexMap に " + vertexPosLocal.x + ", " + vertexPosLocal.y + ", " + vertexPosLocal.z + "  がありませんでした (Local)");
                // Debug.LogWarning(
                //     "curVertexMap に " +
                //     vertexPos.x + ", " +
                //     vertexPos.y + ", " +
                //     vertexPos.z + "  がありませんでした (World)");
                // return new KeyValuePair<Vector3, Vector3>(Vector3.zero, Vector3.zero);
            // }

            var indexList = dstVertexMap[vertexPos];
            // var indexList = curVertexMap[vertexPosLocal];
            foreach (var idx in indexList)
            {
                averagedNormal += mesh.normals[idx];
            }

            averagedNormal /= indexList.Count;

            sphereRadius = 0.0f;
            boundsLength = 0.0f;
            // if (typeOfSearchMethod == TypeOfSearchMethod.UseVertexPosition)
            // {
                var meshBounds = mesh.bounds;
                var boundsMax  = meshBounds.max;
                var boundsMin  = meshBounds.min;

                // dstTrans があった場合、そのオブジェクトのグレーバルスケール（lossyScale）をboundsに乗算しておく
                if (trans != null)
                {
                    boundsMax = Vector3.Scale(boundsMax, trans.lossyScale);
                    boundsMin = Vector3.Scale(boundsMin, trans.lossyScale);
                }

                boundsLength = Vector3.Distance(boundsMax, boundsMin);
                sphereRadius = boundsLength / sphereRadiusCoffi;
            // }

            // 交差するTriangleDataを取得
            TAP.TriangleData intersectedTriangle  = null;
            // var outSrcMatchSignTriangleList = new List<TAP.TriangleData>();
            // var intersectPointList          = new List<Vector3>();
            var intersectedPosWorld           = Vector3.positiveInfinity;

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var processType = "";

            // var hitCount    = 0;
            var searchCount = 1;
            var searchedTriangleCount = 0;
            if (useAveragedVertexNormals)
            {
                processType = "  GetIntersectedTriangleDataWithNormal  ";
                // intersectedTriangle = TAP.GetIntersectedTriangleDataWithNormal (
                //                         vertexPos,
                //                         averagedNormal,
                //                         triangleDataList,
                //                         out outSrcMatchSignTriangleList);

                // if (intersectedTriangle != null)
                // {
                //     // 交差しているTriangleData上の交差点を取得
                //     intersectedPosWorld = TAP.GetIntersectedPointFromTriangleData (
                //                             vertexPos,
                //                             averagedNormal,
                //                             intersectedTriangle);
                // }

                var firstVoxelAddress = dstBelongingSpaceOfVtxPosDict.FirstOrDefault(
                                        x => x.Value.Contains(vertexPos)).Key;
                var searchedVoxelAddressList = new List<Vector3>();
                var searchedTriangleIdxList  = new List<int>();
                
                TAP.GetIntersectedTriangleDataWithAveragedNormal (
                    dstTrans,
                    vertexPos,
                    averagedNormal,
                    firstVoxelAddress,
                    triangleDataList,
                    // srcVtxPosIdxDict,
                    srcVtxPosTriangleIdxDict,
                    srcBelongingSpaceOfVtxPosDict,
                    // dstBelongingSpaceOfVtxPosDict,
                    ref searchedVoxelAddressList,
                    ref searchedTriangleIdxList,
                    ref intersectedTriangle,
                    ref intersectedPosWorld,
                    // ref outSrcMatchSignTriangleList,
                    ref searchCount,
                    ref searchedTriangleCount);

                EditorUtility.ClearProgressBar();

                var distance = Mathf.Infinity;
                if (intersectedPosWorld != Vector3.positiveInfinity)
                {
                    distance = Vector3.Distance(intersectedPosWorld, vertexPos);
                }

                if (intersectedTriangle == null)
                {
                    Debug.Log("dst vtx index = " + vxtIndex + //" : hitCount = " + hitCount +
                        " : searchCount = " + searchCount + " : Src Triangle Index = null ...." );
                }
                else
                {
                    Debug.Log("dst vtx index = " + vxtIndex + //" : hitCount = " + hitCount +
                        " : searchCount = " + searchCount +
                        " : Src Triangle Index = " + intersectedTriangle.triangleIdx +
                        " : distance = " + distance);
                }
            }
            else
            {
                processType = "  GetIntersectedTriangleDataWithVtxPos   ";
                // intersectedTriangle = TAP.GetIntersectedTriangleDataWithVtxPos (
                //                         vertexPos,
                //                         triangleDataList,
                //                         srcVtxPosIdxDict,
                //                         srcVtxPosTriangleIdxDict,
                //                         ref intersectPosWorld,
                //                         ref outSrcMatchSignTriangleList,
                //                         ref searchCount);

                var firstVoxelAddress = dstBelongingSpaceOfVtxPosDict.FirstOrDefault(
                                        x => x.Value.Contains(vertexPos)).Key;
                var searchedVoxelAddressList = new List<Vector3>();
                
                TAP.GetIntersectedTriangleDataWithVtxPos(
                    vertexPos,
                    firstVoxelAddress,
                    triangleDataList,
                    // srcVtxPosIdxDict,
                    srcVtxPosTriangleIdxDict,
                    srcBelongingSpaceOfVtxPosDict,
                    // dstBelongingSpaceOfVtxPosDict,
                    ref searchedVoxelAddressList,
                    ref intersectedTriangle,
                    ref intersectedPosWorld,
                    // ref outSrcMatchSignTriangleList,
                    ref searchCount);

                // Debug.Log(" sphereRadius = " + sphereRadius);
                // Debug.Log(" boundsLength = " + boundsLength);
                
                var distance = Mathf.Infinity;
                if (intersectedPosWorld != Vector3.positiveInfinity)
                {
                    distance = Vector3.Distance(intersectedPosWorld, vertexPos);
                }

                EditorUtility.ClearProgressBar();

                if (intersectedTriangle == null)
                {
                    Debug.Log("dst vtx index = " + vxtIndex + //" : hitCount = " + hitCount +
                        " : searchCount = " + searchCount + " : Src Triangle Index = null ...." );
                }
                else
                {
                    Debug.Log("dst vtx index = " + vxtIndex + //" : hitCount = " + hitCount +
                        " : searchCount = " + searchCount +
                        " : Src Triangle Index = " + intersectedTriangle.triangleIdx +
                        " : distance = " + distance);
                }
                
                Debug.Log("                                  ");

                Debug.Log(" ========================================================================  ");
            }
            // 計測停止
            sw.Stop();

            // 結果表示
            Debug.Log("■処理Aにかかった時間 -" + processType);
            TimeSpan ts = sw.Elapsed;
            Debug.Log(ts);
            Debug.Log(ts.Hours + "時間 : " + ts.Minutes + "分 : " + ts.Seconds + "秒 : " + ts.Milliseconds + "ミリ秒");
            Debug.Log("経過 " + sw.ElapsedMilliseconds + " ミリ秒");

            // Debug.Log("pos = " +
            //         vertexPos.x + ", " +
            //         vertexPos.y + ", " +
            //         vertexPos.z + "   :   " +
            //         " avNml = " +
            //         averagedNormal.x + ", " +
            //         averagedNormal.y + ", " +
            //         averagedNormal.z);

            if (intersectedTriangle == null)
            {
                Debug.LogError("intersectTriangle がnullでした・・・");
            }
            else if (intersectedPosWorld == Vector3.positiveInfinity)
            {
                Debug.LogError("intersectPosWorld がpositiveInfinityでした・・・");
            }

            // ワールド座標からローカル座標に変換して返す
            // return new KeyValuePair<Vector3, Vector3>(trans.InverseTransformPoint(intersectedPosWorld), averagedNormal);
            return new KeyValuePair<Vector3, Vector3>(intersectedPosWorld, averagedNormal);
        }



        private Vector3 QuaternionTransformVector(Vector3 srcVec, Quaternion qua)
        {
            var tempVec = new Vector3();
    /*
    Maths - Using vectors with quaternions - Martin Baker
    http://www.euclideanspace.com/maths/algebra/realNormedAlgebra/quaternions/transforms/derivations/vectors/index.htm
    */
            var quaXpow2 = qua.x * qua.x;
            var quaYpow2 = qua.y * qua.y;
            var quaZpow2 = qua.z * qua.z;
            var quaWpow2 = qua.w * qua.w;

            tempVec.x = srcVec.x * (quaXpow2 + quaWpow2 - quaYpow2 - quaZpow2) +
                        srcVec.y * (2 * qua.x * qua.y - 2 * qua.w * qua.z) +
                        srcVec.z * (2 * qua.x * qua.z + 2 * qua.w * qua.y);

            tempVec.y = srcVec.x * (2 * qua.w * qua.z + 2 * qua.x * qua.y) +
                        srcVec.y * (quaWpow2 - quaXpow2 + quaYpow2 - quaZpow2) +
                        srcVec.z * (-2 * qua.w * qua.x + 2 * qua.y * qua.z);


            tempVec.z = srcVec.x * (-2 * qua.w * qua.y + 2 * qua.x * qua.z) +
                        srcVec.y * (2 * qua.w * qua.x + 2 * qua.y * qua.z) +
                        srcVec.z * (quaWpow2 - quaXpow2 - quaYpow2 + quaZpow2);

            return tempVec;
        }

        private Vector3 QuaternionTransformVector(Vector3 srcVec, Vector4 qua)
        {
            var tempVec = new Vector3();
    /*
    Maths - Using vectors with quaternions - Martin Baker
    http://www.euclideanspace.com/maths/algebra/realNormedAlgebra/quaternions/transforms/derivations/vectors/index.htm
    */
            var quaXpow2 = qua.x * qua.x;
            var quaYpow2 = qua.y * qua.y;
            var quaZpow2 = qua.z * qua.z;
            var quaWpow2 = qua.w * qua.w;

            tempVec.x = srcVec.x * (quaXpow2 + quaWpow2 - quaYpow2 - quaZpow2) +
                        srcVec.y * (2 * qua.x * qua.y - 2 * qua.w * qua.z) +
                        srcVec.z * (2 * qua.x * qua.z + 2 * qua.w * qua.y);

            tempVec.y = srcVec.x * (2 * qua.w * qua.z + 2 * qua.x * qua.y) +
                        srcVec.y * (quaWpow2 - quaXpow2 + quaYpow2 - quaZpow2) +
                        srcVec.z * (-2 * qua.w * qua.x + 2 * qua.y * qua.z);


            tempVec.z = srcVec.x * (-2 * qua.w * qua.y + 2 * qua.x * qua.z) +
                        srcVec.y * (2 * qua.w * qua.x + 2 * qua.y * qua.z) +
                        srcVec.z * (quaWpow2 - quaXpow2 - quaYpow2 + quaZpow2);

            return tempVec;
        }

        
    }
}